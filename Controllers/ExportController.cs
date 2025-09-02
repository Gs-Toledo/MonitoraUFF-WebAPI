using System.Globalization;
using System.IO.Compression;
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
    public async Task<IActionResult> ExportRecordingsAsZip([FromBody] ExportRequestDto request)
    {
        if (request == null || !request.Cameras.Any())
        {
            return BadRequest("A lista de IDs de câmera não pode ser vazia.");
        }

        var downloadTasks = new List<Task<DownloadedVideo>>();
        foreach (var cameraIdentifier in request.Cameras)
        {
            var instance = await _zoneminderRepository.GetByIdAsync(cameraIdentifier.ZoneminderInstanceId);
            var camera = (await _cameraRepository.GetByZoneminderInstanceIdAsync(cameraIdentifier.ZoneminderInstanceId))
                           .FirstOrDefault(c => c.Id == cameraIdentifier.CameraId);

            if (instance == null || camera == null) continue;

            var allRecordings = await _zoneMinderService.GetRecordingsForMonitor(instance, camera.Id);
            var filteredRecordings = allRecordings.Where(r => DateTime.TryParse(r.StartTime, out var d) && d >= request.StartDate && d <= request.EndDate).ToList();

            foreach (var recording in filteredRecordings)
            {
                downloadTasks.Add(DownloadRecordingDataAsync(recording, camera.Name, instance));
            }
        }

        var downloadedVideos = await Task.WhenAll(downloadTasks);

        byte[] zipBytes;

        //  MemoryStream para criar o arquivo ZIP em memória.
        using (var memoryStream = new MemoryStream())
        {
            // Cria o arquivo ZIP dentro do memoryStream
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var videoData in downloadedVideos)
                {
                    if (videoData?.Content == null) continue;

                    var zipEntry = archive.CreateEntry(videoData.FileName, CompressionLevel.Optimal);
                    using (var zipEntryStream = zipEntry.Open())
                    {
                        // Escreve o conteúdo do vídeo (já em memória) para o arquivo ZIP
                        await zipEntryStream.WriteAsync(videoData.Content);
                    }
                }
            } // O 'using' do archive garante que tudo seja finalizado no memoryStream

            // Converte o stream finalizado para um array de bytes
            zipBytes = memoryStream.ToArray();
        }

        return File(zipBytes, "application/zip", $"export_{DateTime.Now:yyyy-MM-dd_HH-mm}.zip");
    }

    private async Task<DownloadedVideo> DownloadRecordingDataAsync(RecordingDto recording, string cameraName, ZoneminderInstance instance)
    {
        {
            string fileName = FormatFileName(recording, cameraName);
            if (string.IsNullOrEmpty(fileName)) return null;

            try
            {
                var content = await _zoneMinderService.DownloadVideoAsync(instance, recording.EventId);

                if (content == null) return null;

                return new DownloadedVideo { FileName = Path.Combine(cameraName, fileName), Content = content };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar o download da gravação {EventId} da câmera {CameraName}.", recording.EventId, cameraName);
                return null;
            }
        }
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
