using Microsoft.AspNetCore.Mvc;
using MonitoraUFF_API.Application.DTOs;
using MonitoraUFF_API.Core.Interfaces;
using MonitoraUFF_API.Infrastructure.Services;

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

    [HttpGet("camera/{cameraId}")]
    public async Task<ActionResult<IEnumerable<RecordingDto>>> GetRecordingsForCamera(int cameraId)
    {
        var camera = await _cameraRepository.GetByIdAsync(cameraId);
        if (camera == null) return NotFound("Câmera não encontrada no banco de dados.");

        var instance = await _zoneminderRepository.GetByIdAsync(camera.ZoneminderInstanceId);
        if (instance == null) return NotFound("Instância do ZoneMinder associada à câmera não foi encontrada.");

        // A URL da API que será usada para construir o link de download (testar no swagger, não sei se está certo)
        var apiUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        var recordings = await _zoneMinderService.GetRecordingsForMonitor(instance.UrlServer, instance.User, instance.Password, camera.Id, apiUrl);
        return Ok(recordings);
    }

    [HttpGet("download/{eventId}")]
    public async Task<IActionResult> DownloadRecording(string eventId, [FromQuery] string baseUrl, [FromQuery] string user, [FromQuery] string pass)
    {
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(eventId))
        {
            return BadRequest("Parâmetros inválidos para download.");
        }

        var client = _httpClientFactory.CreateClient();
        var zmUrl = $"{baseUrl}/zm/index.php?view=video&eid={eventId}&export=1&user={user}&pass={pass}";

        try
        {
            var response = await client.GetAsync(zmUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Atua como um proxy, repassando o stream de vídeo diretamente por enquanto.
            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp4";
            var fileName = $"recording_{eventId}.mp4";

            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao fazer proxy do download: {ex.Message}");
        }
    }
}
