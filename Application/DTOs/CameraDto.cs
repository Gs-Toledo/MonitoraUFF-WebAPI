namespace MonitoraUFF_API.Application.DTOs;

public class CameraDto
{
    public int Id { get; set; }
    public int ZoneminderInstanceId { get; set; }
    public string ZoneminderInstanceUrl { get; set; }
    public string Name { get; set; }
    public string? Coordinates { get; set; }
    public bool IsSavingRecords { get; set; }
}
