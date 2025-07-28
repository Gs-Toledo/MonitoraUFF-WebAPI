using System.ComponentModel.DataAnnotations;

namespace MonitoraUFF_API.Application.DTOs;

public class ExportRequestDto
{
    [Required]
    public List<int> CameraIds { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
