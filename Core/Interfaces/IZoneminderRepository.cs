using MonitoraUFF_API.Core.Entities;

namespace MonitoraUFF_API.Core.Interfaces;

public interface IZoneminderRepository
{
    Task<ZoneminderInstance> GetByIdAsync(int id);
    Task<IEnumerable<ZoneminderInstance>> GetAllAsync();
    Task AddAsync(ZoneminderInstance entity);
    Task UpdateAsync(ZoneminderInstance entity);
    Task DeleteAsync(int id);
}
