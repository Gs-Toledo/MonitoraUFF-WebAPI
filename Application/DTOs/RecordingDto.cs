namespace MonitoraUFF_API.Application.DTOs;

public class RecordingDto
{
    public string EventId { get; set; }
    public string Name { get; set; }
    public string StartTime { get; set; }
    public string Length { get; set; }
    public int Frames { get; set; }
    // URL para download através da nossa API
    public string DownloadUrl { get; set; }
}
