using System.Globalization;
using MonitoraUFF_API.Core.Entities;
using MonitoraUFF_API.Core.Interfaces;

namespace MonitoraUFF_API.BackgroundServices;

public class RecordingSyncService : BackgroundService
{
    private readonly ILogger<RecordingSyncService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public RecordingSyncService(ILogger<RecordingSyncService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de Sincronização de Gravações iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Iniciando ciclo de sincronização de gravações do ZoneMinder...");

            // Usa um 'scope' para obter instâncias 'scoped' (como os repositórios)
            // dentro de um serviço 'singleton'
            using (var scope = _scopeFactory.CreateScope())
            {
                var zoneminderRepo = scope.ServiceProvider.GetRequiredService<IZoneminderRepository>();
                var cameraRepo = scope.ServiceProvider.GetRequiredService<ICameraRepository>();
                var recordingRepo = scope.ServiceProvider.GetRequiredService<IRecordingRepository>();
                var zmService = scope.ServiceProvider.GetRequiredService<IZoneMinderService>();

                try
                {
                    var instances = await zoneminderRepo.GetAllAsync();
                    foreach (var instance in instances)
                    {
                        var cameras = await cameraRepo.GetByZoneminderInstanceIdAsync(instance.Id);
                        foreach (var camera in cameras)
                        {
                            var zmRecordings = await zmService.GetRecordingsForMonitor(instance.Id, instance.UrlServer, instance.User, instance.Password, camera.Id);

                            foreach (var zmRecording in zmRecordings)
                            {
                                var existingRecording = await recordingRepo.FindByEventIdAsync(camera.Id, zmRecording.EventId);
                                if (existingRecording == null)
                                {
                                    if (DateTime.TryParse(zmRecording.StartTime, out var startTime) &&
                                        double.TryParse(zmRecording.Length, NumberStyles.Any, CultureInfo.InvariantCulture, out var lengthSeconds))
                                    {
                                        var newRecording = new Recording
                                        {
                                            EventId = zmRecording.EventId,
                                            CameraId = camera.Id,
                                            StartTime = startTime,
                                            EndTime = startTime.AddSeconds(lengthSeconds),
                                            RecordingUrl = $"/api/recordings/instance/{instance.Id}/download/{zmRecording.EventId}"
                                        };
                                        await recordingRepo.AddAsync(newRecording);
                                        _logger.LogInformation("Nova gravação (Evento ID: {EventId}) da câmera {CameraName} sincronizada.", zmRecording.EventId, camera.Name);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocorreu um erro durante o ciclo de sincronização.");
                }
            }

            _logger.LogInformation("Ciclo de sincronização finalizado. Próxima execução em 1 hora.");
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
