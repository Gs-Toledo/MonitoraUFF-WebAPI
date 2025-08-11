using Microsoft.EntityFrameworkCore;
using MonitoraUFF_API.Core.Entities;
using MonitoraUFF_API.Core.Interfaces;
using MonitoraUFF_API.Infrastructure.Data;

namespace MonitoraUFF_API.Infrastructure.Repositories;

public class RecordingRepository : IRecordingRepository
{
    private readonly AppDbContext _context;

    public RecordingRepository(AppDbContext context)
    {
        _context = context;
    }


    public async Task<Recording> GetByIdAsync(int id)
    {
        return await _context.Recordings.FindAsync(id);
    }

    public async Task<IEnumerable<Recording>> GetAllAsync()
    {
        return await _context.Recordings.ToListAsync();
    }

    public async Task<IEnumerable<Recording>> GetByCameraIdAsync(int cameraId)
    {
        return await _context.Recordings
            .Where(r => r.CameraId == cameraId)
            .ToListAsync();
    }

    public async Task AddAsync(Recording entity)
    {
        await _context.Recordings.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Recording entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.Recordings.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Recording> FindByEventIdAsync(int cameraId, string eventId)
    {
        return await _context.Recordings
            .FirstOrDefaultAsync(r => r.CameraId == cameraId && r.EventId == eventId);
    }

}
