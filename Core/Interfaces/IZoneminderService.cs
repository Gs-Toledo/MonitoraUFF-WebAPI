using MonitoraUFF_API.Application.DTOs;

namespace MonitoraUFF_API.Core.Interfaces;

public interface IZoneMinderService
{
    Task<List<RecordingDto>> GetRecordingsForMonitor(int instanceId, string baseUrl, string user, string password, int monitorId, string apiUrl);
}
