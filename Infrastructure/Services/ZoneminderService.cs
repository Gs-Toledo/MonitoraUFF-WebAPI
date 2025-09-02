using System.Collections.Concurrent;
using System.Text.Json;
using MonitoraUFF_API.Application.DTOs;
using MonitoraUFF_API.Core.Entities;
using MonitoraUFF_API.Core.Interfaces;


namespace MonitoraUFF_API.Infrastructure.Services;

public class ZoneMinderService : IZoneMinderService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ZoneMinderService> _logger;

    private class AuthCredentials
    {
        public string AccessToken { get; set; }
        public string AuthHash { get; set; } // hash para downloads
    }

    private static readonly ConcurrentDictionary<int, AuthCredentials> _credentialsCache = new();


    public ZoneMinderService(IHttpClientFactory httpClientFactory, ILogger<ZoneMinderService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private async Task<AuthCredentials> GetCredentialsAsync(ZoneminderInstance instance)
    {
        if (_credentialsCache.TryGetValue(instance.Id, out var cachedCreds))
        {
            return cachedCreds;
        }

        _logger.LogInformation("Nenhuma credencial em cache para a instância {InstanceUrl}. Realizando login...", instance.UrlServer);
        var client = _httpClientFactory.CreateClient();
        var loginUrl = $"{instance.UrlServer}/api/host/login.json";

        try
        {
            var loginData = new Dictionary<string, string>
                {
                    { "user", instance.User },
                    { "pass", instance.Password }
                };
            var content = new FormUrlEncodedContent(loginData);

            // Envia a requisição POST com os dados no formato de formulário
            var response = await client.PostAsync(loginUrl, content);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = jsonContent.GetProperty("access_token").GetString();
            var authHash = jsonContent.GetProperty("credentials").GetString();

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(authHash))
            {
                var newCreds = new AuthCredentials { AccessToken = accessToken, AuthHash = authHash };
                _credentialsCache[instance.Id] = newCreds;
                _logger.LogInformation("Login bem-sucedido para a instância {InstanceUrl}.", instance.UrlServer);
                return newCreds;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao autenticar na instância do ZoneMinder: {InstanceUrl}", instance.UrlServer);
        }
        return null;
    }

    public async Task<List<RecordingDto>> GetRecordingsForMonitor(ZoneminderInstance instance, int monitorId)
    {

        var credentials = await GetCredentialsAsync(instance);
        if (credentials == null) return new List<RecordingDto>();

        var client = _httpClientFactory.CreateClient();
        var requestUrl = $"{instance.UrlServer}/api/events/index/MonitorId:{monitorId}.json?token={credentials.AccessToken}";

        try
        {
            var response = await client.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao buscar eventos do ZoneMinder para o monitor {MonitorId}. Status: {StatusCode}", monitorId, response.StatusCode);
                return new List<RecordingDto>();
            }

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var events = content.GetProperty("events").EnumerateArray();

            return events.Select(e =>
            {
                var eventId = e.GetProperty("Event").GetProperty("Id").ToString();
                var apiUrl = "https://localhost:7171";
                return new RecordingDto
                {
                    EventId = eventId,
                    Name = e.GetProperty("Event").GetProperty("Name").GetString(),
                    StartTime = e.GetProperty("Event").GetProperty("StartTime").GetString(),
                    Length = e.GetProperty("Event").GetProperty("Length").ToString(),
                    Frames = int.Parse(e.GetProperty("Event").GetProperty("Frames").ToString()),
                    DownloadUrl = $"{apiUrl}/api/recordings/instance/{instance.Id}/download/{eventId}"
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao comunicar com a API do ZoneMinder em {BaseUrl}", instance.UrlServer);
            return new List<RecordingDto>();
        }
    }

    public async Task<byte[]> DownloadVideoAsync(ZoneminderInstance instance, string eventId)
    {
        var credentials = await GetCredentialsAsync(instance);
        if (credentials == null) return null;

        var client = _httpClientFactory.CreateClient("ZoneminderVideoClient");
        var downloadUrl = $"{instance.UrlServer}/index.php?mode=mp4&view=view_video&eid={eventId}&token={credentials.AccessToken}";

        try
        {
            var response = await client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao baixar o vídeo do evento {EventId} da instância {InstanceUrl}", eventId, instance.UrlServer);
            return null;
        }
    }
}