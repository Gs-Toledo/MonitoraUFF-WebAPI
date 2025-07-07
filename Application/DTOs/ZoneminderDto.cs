namespace MonitoraUFF_API.Application.DTOs;

public class ZoneminderDto
{
    public int Id { get; set; }
    public string UrlServer { get; set; }
}

public class CreateZoneminderDto
{
    public string UrlServer { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
}
