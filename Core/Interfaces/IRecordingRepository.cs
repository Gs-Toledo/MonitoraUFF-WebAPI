using MonitoraUFF_API.Core.Entities;

namespace MonitoraUFF_API.Core.Interfaces;

public interface IRecordingRepository
{
    Task<Recording> GetByIdAsync(int id);
    Task<IEnumerable<Recording>> GetAllAsync();
    Task<IEnumerable<Recording>> GetByCameraIdAsync(int cameraId);
    Task AddAsync(Recording entity);
    Task UpdateAsync(Recording entity);
    Task DeleteAsync(int id);
    Task<Recording> FindByEventIdAsync(int cameraId, string eventId);

}
