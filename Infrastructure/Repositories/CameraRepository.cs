using Microsoft.EntityFrameworkCore;
using MonitoraUFF_API.Core.Entities;
using MonitoraUFF_API.Core.Interfaces;
using MonitoraUFF_API.Infrastructure.Data;

namespace MonitoraUFF_API.Infrastructure.Repositories;

public class CameraRepository : ICameraRepository
{
    private readonly AppDbContext _context;

    public CameraRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Camera> GetByIdAsync(int id)
    {
        return await _context.Cameras.FindAsync(id);
    }

    public async Task<IEnumerable<Camera>> GetAllAsync()
    {
        return await _context.Cameras.ToListAsync();
    }

    public async Task<IEnumerable<Camera>> GetByZoneminderInstanceIdAsync(int zoneminderInstanceId)
    {
        return await _context.Cameras
            .Where(c => c.ZoneminderInstanceId == zoneminderInstanceId)
            .ToListAsync();
    }

    public async Task AddAsync(Camera entity)
    {
        await _context.Cameras.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Camera entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.Cameras.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
