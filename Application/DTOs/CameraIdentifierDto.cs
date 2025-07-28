using System.ComponentModel.DataAnnotations;

namespace MonitoraUFF_API.Application.DTOs;

public class CameraIdentifierDto
{
    [Required]
    public int ZoneminderInstanceId { get; set; }

    [Required]
    public int CameraId { get; set; }
}
