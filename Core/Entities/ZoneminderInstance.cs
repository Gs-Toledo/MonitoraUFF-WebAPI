namespace MonitoraUFF_API.Core.Entities;

public class ZoneminderInstance
{
    public int Id { get; set; }
    public string UrlServer { get; set; }
    public string User { get; set; }
    public string Password { get; set; }

    public ICollection<Camera> Cameras { get; set; } = new List<Camera>();
}

