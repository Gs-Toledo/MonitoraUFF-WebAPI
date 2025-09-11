using System.Globalization;
using System.IO.Compression;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MonitoraUFF_API.Application.DTOs;
using MonitoraUFF_API.Core.Entities;
using MonitoraUFF_API.Core.Interfaces;
namespace MonitoraUFF_API.Controllers;

[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly ICameraRepository _cameraRepository;
    private readonly IZoneminderRepository _zoneminderRepository;
    private readonly IZoneMinderService _zoneMinderService;
    private readonly ILogger<ExportController> _logger;

    private class DownloadedVideo
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }

        public string EventId { get; set; }

        public string CameraName { get; set; }
    }

    public ExportController(
        ICameraRepository cameraRepository,
        IZoneminderRepository zoneminderRepository,
        IZoneMinderService zoneMinderService,
        ILogger<ExportController> logger)
    {
        _cameraRepository = cameraRepository;
        _zoneminderRepository = zoneminderRepository;
        _zoneMinderService = zoneMinderService;
        _logger = logger;
    }

    [HttpPost("zip")]
    public async Task ExportRecordingsAsZip([FromBody] ExportRequestDto request)
    {

        var syncIoFeature = HttpContext.Features.Get<IHttpBodyControlFeature>();
        if (syncIoFeature != null)
        {
            syncIoFeature.AllowSynchronousIO = true;
        }

        Response.ContentType = "application/zip";
        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"export_{DateTime.Now:yyyy-MM-dd_HH-mm}.zip\"");
        Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // cria o ZipArchive diretamente no fluxo de resposta do corpo HTTP.
        using (var archive = new ZipArchive(Response.Body, ZipArchiveMode.Create, true))
        {

            var allDownloadTasks = new List<Task<DownloadedVideo>>();



            foreach (var cameraIdentifier in request.Cameras)
            {
                var instance = await _zoneminderRepository.GetByIdAsync(cameraIdentifier.ZoneminderInstanceId);
                var camera = (await _cameraRepository.GetByZoneminderInstanceIdAsync(cameraIdentifier.ZoneminderInstanceId))
                               .FirstOrDefault(c => c.Id == cameraIdentifier.CameraId);

                if (instance == null || camera == null) continue;

                var allRecordings = await _zoneMinderService.GetRecordingsForMonitor(instance, camera.Id);
                var filteredRecordings = allRecordings.Where(r => DateTime.TryParse(r.StartTime, out var d) && d >= request.StartDate && d <= request.EndDate).ToList();

                _logger.LogInformation("Câmera '{CameraName}': {Count} gravações dentro do intervalo {Start} - {End}.",
                    camera.Name, filteredRecordings.Count, request.StartDate, request.EndDate);
                foreach (var recording in filteredRecordings)
                {
                    allDownloadTasks.Add(DownloadAndPrepareVideoAsync(instance, recording, camera.Name));
                }
            }

            _logger.LogInformation("Total de {Count} tarefas de download iniciadas.", allDownloadTasks.Count);

            foreach (var task in allDownloadTasks)
            {
                try
                {
                    var videoData = await task;
                    if (videoData?.Content == null || videoData.Content.Length == 0)
                    {
                        _logger.LogWarning("Gravação ignorada (sem conteúdo) - EventId={EventId}, Camera={CameraName}",
                   videoData?.EventId, videoData?.CameraName);
                        continue;
                    }

                    _logger.LogInformation("Iniciando escrita da gravação {EventId} da câmera {CameraName} no ZIP...",
          videoData.EventId, videoData.CameraName);

                    var zipEntry = archive.CreateEntry(videoData.FileName, CompressionLevel.NoCompression);
                    using (var zipEntryStream = zipEntry.Open())
                    {
                        await zipEntryStream.WriteAsync(videoData.Content);
                    }

                    _logger.LogInformation("Gravação {EventId} da câmera {CameraName} adicionada com sucesso ao ZIP.",
           videoData.EventId, videoData.CameraName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                            "Falha ao processar a gravação {EventId} da câmera {CameraName}.",
                            (task as Task<DownloadedVideo>)?.Result?.EventId,
                            (task as Task<DownloadedVideo>)?.Result?.CameraName);
                }
            }




            //foreach (var cameraIdentifier in request.Cameras)
            //{
            //    var instance = await _zoneminderRepository.GetByIdAsync(cameraIdentifier.ZoneminderInstanceId);
            //    var camera = (await _cameraRepository.GetByZoneminderInstanceIdAsync(cameraIdentifier.ZoneminderInstanceId))
            //                   .FirstOrDefault(c => c.Id == cameraIdentifier.CameraId);

            //    if (instance == null || camera == null) continue;

            //    var allRecordings = await _zoneMinderService.GetRecordingsForMonitor(instance, camera.Id);
            //    var filteredRecordings = allRecordings.Where(r => DateTime.TryParse(r.StartTime, out var d) && d >= request.StartDate && d <= request.EndDate).ToList();

            //    _logger.LogInformation("Câmera '{CameraName}': {Count} gravações encontradas para download.", camera.Name, filteredRecordings.Count);

            //    foreach (var recording in filteredRecordings)
            //    {
            //        var videoContent = await _zoneMinderService.DownloadVideoAsync(instance, recording.EventId);
            //        if (videoContent == null || videoContent.Length == 0) continue;

            //        string fileName = FormatFileName(recording, camera.Name);
            //        if (string.IsNullOrEmpty(fileName)) continue;

            //        var zipEntry = archive.CreateEntry(Path.Combine(camera.Name, fileName), CompressionLevel.NoCompression);

            //        using (var zipEntryStream = zipEntry.Open())
            //        {
            //            await zipEntryStream.WriteAsync(videoContent);
            //        }

            //        // o vídeo é descartado da memória ao final de cada iteração
            //    }
            //}

            _logger.LogInformation("Exportação concluída. Total de {Count} gravações processadas.", allDownloadTasks.Count);
        } // O 'using' garante que o ZipArchive seja finalizado e a resposta seja completada.
    }

    //private async Task<DownloadedVideo> DownloadRecordingDataAsync(RecordingDto recording, string cameraName, ZoneminderInstance instance)
    //{
    //    {
    //        string fileName = FormatFileName(recording, cameraName);
    //        if (string.IsNullOrEmpty(fileName)) return null;

    //        try
    //        {
    //            var content = await _zoneMinderService.DownloadVideoAsync(instance, recording.EventId);

    //            if (content == null) return null;

    //            return new DownloadedVideo { FileName = Path.Combine(cameraName, fileName), Content = content };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Falha ao processar o download da gravação {EventId} da câmera {CameraName}.", recording.EventId, cameraName);
    //            return null;
    //        }
    //    }
    //}

    private async Task<DownloadedVideo> DownloadAndPrepareVideoAsync(ZoneminderInstance instance, RecordingDto recording, string cameraName)
    {
        var videoContent = await _zoneMinderService.DownloadVideoAsync(instance, recording.EventId);
        if (videoContent == null || videoContent.Length == 0)
        {
            _logger.LogWarning("Download vazio ou nulo - EventId={EventId}, Camera={CameraName}", recording.EventId, cameraName);
            return null;
        }

        string fileName = FormatFileName(recording, cameraName);
        if (string.IsNullOrEmpty(fileName))
        {
            _logger.LogWarning("Nome de arquivo inválido - EventId={EventId}, Camera={CameraName}", recording.EventId, cameraName);
            return null;
        }

        _logger.LogInformation("Download concluído - EventId={EventId}, Camera={CameraName}", recording.EventId, cameraName);


        return new DownloadedVideo
        {
            Content = videoContent,
            FileName = Path.Combine(cameraName, fileName),
            EventId = recording.EventId,
            CameraName = cameraName,
        };
    }

    private string FormatFileName(RecordingDto recording, string cameraName)
    {
        if (!DateTime.TryParse(recording.StartTime, out DateTime startTime) ||
            !double.TryParse(recording.Length, NumberStyles.Any, CultureInfo.InvariantCulture, out double lengthSeconds))
        {
            return null;
        }

        var endTime = startTime.AddSeconds(lengthSeconds);

        // Formato baseado no exemplo ajustado com o Mestre Guerra: 14h15m00s-14h20m00s --- 2025-07-23 --- ext-grab-bloc.mp4
        string startTimeStr = startTime.ToString("HH'h'mm'm'ss's'");
        string endTimeStr = endTime.ToString("HH'h'mm'm'ss's'");
        string dateStr = startTime.ToString("yyyy-MM-dd");

        string sanitizedCameraName = string.Join("_", cameraName.Split(Path.GetInvalidFileNameChars()));

        return $"{startTimeStr}-{endTimeStr} --- {dateStr} --- {sanitizedCameraName}.mp4";
    }
}
