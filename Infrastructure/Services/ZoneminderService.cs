using System.Text.Json;
using MonitoraUFF_API.Application.DTOs;


namespace MonitoraUFF_API.Infrastructure.Services;

public interface IZoneMinderService
{
    Task<List<RecordingDto>> GetRecordingsForMonitor(string baseUrl, string user, string password, int monitorId, string apiUrl);
}

public class ZoneMinderService : IZoneMinderService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ZoneMinderService> _logger;

    public ZoneMinderService(IHttpClientFactory httpClientFactory, ILogger<ZoneMinderService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<RecordingDto>> GetRecordingsForMonitor(string baseUrl, string user, string password, int monitorId, string apiUrl)
    {
        var client = _httpClientFactory.CreateClient();
        var requestUrl = $"{baseUrl}/zm/api/events/index/MonitorId:{monitorId}.json?user={user}&pass={password}";

        try
        {
            var response = await client.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao buscar eventos do ZoneMinder para o monitor {MonitorId}. Status: {StatusCode}", monitorId, response.StatusCode);
                return new List<RecordingDto>();
            }

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var events = content.GetProperty("events").EnumerateArray();

            return events.Select(e => new RecordingDto
            {
                EventId = e.GetProperty("Event").GetProperty("Id").GetString(),
                Name = e.GetProperty("Event").GetProperty("Name").GetString(),
                StartTime = e.GetProperty("Event").GetProperty("StartTime").GetString(),
                Length = e.GetProperty("Event").GetProperty("Length").GetString(),
                Frames = int.Parse(e.GetProperty("Event").GetProperty("Frames").GetString()),
                DownloadUrl = $"{apiUrl}/recordings/download/{e.GetProperty("Event").GetProperty("Id").GetString()}?baseUrl={baseUrl}&user={user}&pass={password}"
            }).ToList();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao comunicar com a API do ZoneMinder em {BaseUrl}", baseUrl);
            return new List<RecordingDto>();
        }
    }
}
