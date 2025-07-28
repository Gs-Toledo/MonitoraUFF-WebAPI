using System.ComponentModel.DataAnnotations;

namespace MonitoraUFF_API.Application.DTOs;

public class ExportRequestDto
{
    [Required]
    public List<CameraIdentifierDto> Cameras { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
