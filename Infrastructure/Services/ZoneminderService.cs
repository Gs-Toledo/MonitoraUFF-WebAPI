using System.Text.Json;
using MonitoraUFF_API.Application.DTOs;
using MonitoraUFF_API.Core.Interfaces;


namespace MonitoraUFF_API.Infrastructure.Services;

public class ZoneMinderClient : IZoneMinderService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ZoneMinderClient> _logger;

    public ZoneMinderClient(IHttpClientFactory httpClientFactory, ILogger<ZoneMinderClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<RecordingDto>> GetRecordingsForMonitor(int instanceId, string baseUrl, string user, string password, int monitorId, string apiUrl)
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

            return events.Select(e =>
            {
                var eventId = e.GetProperty("Event").GetProperty("Id").GetString();
                return new RecordingDto
                {
                    EventId = eventId,
                    Name = e.GetProperty("Event").GetProperty("Name").GetString(),
                    StartTime = e.GetProperty("Event").GetProperty("StartTime").GetString(),
                    Length = e.GetProperty("Event").GetProperty("Length").GetString(),
                    Frames = int.Parse(e.GetProperty("Event").GetProperty("Frames").GetString()),
                    DownloadUrl = $"{apiUrl}/api/recordings/instance/{instanceId}/download/{eventId}"
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao comunicar com a API do ZoneMinder em {BaseUrl}", baseUrl);
            return new List<RecordingDto>();
        }
    }
}