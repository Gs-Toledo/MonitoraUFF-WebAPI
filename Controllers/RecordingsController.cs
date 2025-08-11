using Microsoft.AspNetCore.Mvc;
using MonitoraUFF_API.Application.DTOs;
using MonitoraUFF_API.Core.Interfaces;

namespace MonitoraUFF_API.Controllers;

[ApiController]
[Route("api/recordings")]
public class RecordingsController : ControllerBase
{
    private readonly ICameraRepository _cameraRepository;
    private readonly IZoneminderRepository _zoneminderRepository;
    private readonly IZoneMinderService _zoneMinderService;
    private readonly IHttpClientFactory _httpClientFactory;

    public RecordingsController(ICameraRepository cameraRepository, IZoneminderRepository zoneminderRepository, IZoneMinderService zoneMinderService, IHttpClientFactory httpClientFactory)
    {
        _cameraRepository = cameraRepository;
        _zoneminderRepository = zoneminderRepository;
        _zoneMinderService = zoneMinderService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("instance/{instanceId}/camera/{cameraId}")]
    public async Task<ActionResult<IEnumerable<RecordingDto>>> GetRecordingsForCamera(int instanceId, int cameraId)
    {
        var instance = await _zoneminderRepository.GetByIdAsync(instanceId);
        if (instance == null) return NotFound("Instância do ZoneMinder não encontrada.");

        // busca a camera dentro do escopo da instancia
        var camera = (await _cameraRepository.GetByZoneminderInstanceIdAsync(instanceId))
                       .FirstOrDefault(c => c.Id == cameraId);
        if (camera == null) return NotFound("Câmera não encontrada nesta instância do ZoneMinder.");

        // var apiUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        var recordings = await _zoneMinderService.GetRecordingsForMonitor(instance.Id, instance.UrlServer, instance.User, instance.Password, camera.Id);
        return Ok(recordings);
    }

    [HttpGet("instance/{instanceId}/download/{eventId}")]
    public async Task<IActionResult> DownloadRecording(int instanceId, string eventId)
    {
        var instance = await _zoneminderRepository.GetByIdAsync(instanceId);
        if (instance == null)
        {
            return NotFound("Instância do ZoneMinder não encontrada para este download.");
        }

        var client = _httpClientFactory.CreateClient();
        var zmUrl = $"{instance.UrlServer}/index.php?view=video&eid={eventId}&export=1&user={instance.User}&pass={instance.Password}";

        try
        {
            var response = await client.GetAsync(zmUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp4";
            var fileName = $"recording_{instanceId}_{eventId}.mp4";

            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao fazer proxy do download: {ex.Message}");
        }
    }
}
