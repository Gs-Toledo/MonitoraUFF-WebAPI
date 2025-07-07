namespace MonitoraUFF_API.Core.Entities;

public class Recording
{
    public int Id { get; set; }
    public int CameraId { get; set; } // Chave estrangeira
    public string RecordingUrl { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Propriedade de navegação para a câmera
    public Camera Camera { get; set; }
}
