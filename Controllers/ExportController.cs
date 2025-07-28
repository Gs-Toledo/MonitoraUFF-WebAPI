using System.Globalization;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using MonitoraUFF_API.Application.DTOs;
using MonitoraUFF_API.Core.Interfaces;
using MonitoraUFF_API.Infrastructure.Services;

namespace MonitoraUFF_API.Controllers;

[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly ICameraRepository _cameraRepository;
    private readonly IZoneminderRepository _zoneminderRepository;
    private readonly IZoneMinderService _zoneMinderService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        ICameraRepository cameraRepository,
        IZoneminderRepository zoneminderRepository,
        IZoneMinderService zoneMinderService,
        IHttpClientFactory httpClientFactory,
        ILogger<ExportController> logger)
    {
        _cameraRepository = cameraRepository;
        _zoneminderRepository = zoneminderRepository;
        _zoneMinderService = zoneMinderService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("zip")]
    public async Task<IActionResult> ExportRecordingsAsZip([FromBody] ExportRequestDto request)
    {
        if (request == null || !request.Cameras.Any())
        {
            return BadRequest("A lista de IDs de câmera não pode ser vazia.");
        }

        //  MemoryStream para criar o arquivo ZIP em memória.
        var memoryStream = new MemoryStream();

        try
        {
            // O 'using' garante que os recursos sejam liberados, mesmo em caso de erro.
            // O terceiro argumento 'true' mantém o stream aberto para que possa ser lido depois.
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var cameraIdentifier in request.Cameras)
                {
                    var instance = await _zoneminderRepository.GetByIdAsync(cameraIdentifier.ZoneminderInstanceId);
                    if (instance == null)
                    {
                        _logger.LogWarning("Exportação pulou a câmera ID {CameraId} pois sua instância ZM (ID: {InstanceId}) não foi encontrada.",
                            cameraIdentifier.CameraId, cameraIdentifier.ZoneminderInstanceId);
                        continue;
                    }

                    // Busca a câmera específica dentro da instância correta
                    var camera = (await _cameraRepository.GetByZoneminderInstanceIdAsync(instance.Id))
                                   .FirstOrDefault(c => c.Id == cameraIdentifier.CameraId);

                    if (camera == null)
                    {
                        _logger.LogWarning("Exportação pulou a câmera ID {CameraId} pois não foi encontrada na instância {InstanceName} (ID: {InstanceId}).",
                            cameraIdentifier.CameraId, instance.UrlServer, instance.Id);
                        continue;
                    }

                    // Busca todas as gravações para a câmera e filtra localmente.
                    var allRecordings = await _zoneMinderService.GetRecordingsForMonitor(instance.UrlServer, instance.User, instance.Password, camera.Id, "");

                    var filteredRecordings = allRecordings.Where(r =>
                    {
                        // Tenta converter a data da gravação para DateTime para poder comparar.
                        if (DateTime.TryParse(r.StartTime, out DateTime recordingDate))
                        {
                            return recordingDate >= request.StartDate && recordingDate <= request.EndDate;
                        }
                        return false;
                    }).ToList();

                    if (!filteredRecordings.Any()) continue;

                    // Adiciona cada gravação filtrada ao ZIP.
                    foreach (var recording in filteredRecordings)
                    {
                        await AddRecordingToArchive(archive, recording, camera.Name, instance);
                    }
                }
            }

            // Reseta a posição do stream para o início para que o cliente possa lê-lo.
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Retorna o arquivo ZIP.
            return File(memoryStream, "application/zip", $"export_{DateTime.Now:yyyy-MM-dd_HH-mm}.zip");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro fatal durante a criação do arquivo ZIP.");
            // Se um erro ocorrer, o memoryStream pode precisar ser descartado para liberar recursos.
            await memoryStream.DisposeAsync();
            return StatusCode(500, "Ocorreu um erro interno ao gerar o arquivo de exportação.");
        }
    }

    private async Task AddRecordingToArchive(ZipArchive archive, RecordingDto recording, string cameraName, Core.Entities.ZoneminderInstance instance)
    {
        // Formata o nome do arquivo conforme o padrão definido.
        string fileName = FormatFileName(recording, cameraName);
        if (string.IsNullOrEmpty(fileName)) return;

        // Cria uma nova entrada no arquivo ZIP (ex: "nome_da_camera/gravacao.mp4").
        var zipEntry = archive.CreateEntry(Path.Combine(cameraName, fileName), CompressionLevel.Optimal);

        // Monta a URL de download direto do ZoneMinder.
        var client = _httpClientFactory.CreateClient();
        var downloadUrl = $"{instance.UrlServer}/zm/index.php?view=video&eid={recording.EventId}&export=1&user={instance.User}&pass={instance.Password}";

        try
        {
            // Baixa o vídeo como um stream.
            var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using (var videoStream = await response.Content.ReadAsStreamAsync())
            using (var zipEntryStream = zipEntry.Open())
            {
                // Copia o stream do vídeo diretamente para o stream da entrada do ZIP.
                // Importante para não carregar o vídeo inteiro na memória.
                await videoStream.CopyToAsync(zipEntryStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao baixar ou adicionar a gravação {EventId} da câmera {CameraName} ao ZIP.", recording.EventId, cameraName);
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
