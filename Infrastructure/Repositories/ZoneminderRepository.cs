using Microsoft.EntityFrameworkCore;
using MonitoraUFF_API.Core.Entities;
using MonitoraUFF_API.Core.Interfaces;
using MonitoraUFF_API.Infrastructure.Data;

namespace MonitoraUFF_API.Infrastructure.Repositories;

public class ZoneminderRepository : IZoneminderRepository
{
    private readonly AppDbContext _context;

    public ZoneminderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ZoneminderInstance> GetByIdAsync(int id)
    {
        return await _context.ZoneminderInstances.FindAsync(id);
    }

    public async Task<IEnumerable<ZoneminderInstance>> GetAllAsync()
    {
        // CORREÇÃO: Usa-se ToListAsync() para obter todos os registros de uma tabela.
        return await _context.ZoneminderInstances.ToListAsync();
    }

    public async Task AddAsync(ZoneminderInstance entity)
    {
        await _context.ZoneminderInstances.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ZoneminderInstance entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.ZoneminderInstances.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
