using MonitoraUFF_API.Core.Entities;

namespace MonitoraUFF_API.Core.Interfaces;

public interface ICameraRepository
{
    Task<Camera> GetByIdAsync(int id);
    Task<IEnumerable<Camera>> GetAllAsync();
    Task<IEnumerable<Camera>> GetByZoneminderInstanceIdAsync(int zoneminderInstanceId);
    Task AddAsync(Camera entity);
    Task UpdateAsync(Camera entity);
    Task DeleteAsync(int id);
}