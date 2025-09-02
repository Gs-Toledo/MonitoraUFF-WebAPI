using MonitoraUFF_API.Application.DTOs;
using MonitoraUFF_API.Core.Entities;

namespace MonitoraUFF_API.Core.Interfaces;

public interface IZoneMinderService
{
    Task<List<RecordingDto>> GetRecordingsForMonitor(ZoneminderInstance instance, int monitorId);
    Task<byte[]> DownloadVideoAsync(ZoneminderInstance instance, string eventId);

}
