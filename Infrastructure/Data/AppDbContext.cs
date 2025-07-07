using Microsoft.EntityFrameworkCore;
using MonitoraUFF_API.Core.Entities;

namespace MonitoraUFF_API.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ZoneminderInstance> ZoneminderInstances { get; set; }
    public DbSet<Camera> Cameras { get; set; }
    public DbSet<Recording> Recordings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações de relacionamento (o EF Core já infere muito disso)
        modelBuilder.Entity<Camera>()
            .HasOne(c => c.ZoneminderInstance)
            .WithMany(z => z.Cameras)
            .HasForeignKey(c => c.ZoneminderInstanceId);

        modelBuilder.Entity<Recording>()
            .HasOne(r => r.Camera)
            .WithMany(c => c.Recordings)
            .HasForeignKey(r => r.CameraId);
    }
}

