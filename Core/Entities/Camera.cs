namespace MonitoraUFF_API.Core.Entities
{
    public class Camera
    {
        public int Id { get; set; }
        public int ZoneminderInstanceId { get; set; }
        public string Name { get; set; }
        public string? Coordinates { get; set; }
        public string Url { get; set; }
        public bool IsSavingRecords { get; set; }

        // Propriedade de navegação para a instância do Zoneminder
        public ZoneminderInstance ZoneminderInstance { get; set; }

        // Propriedade de navegação para as gravações
        public ICollection<Recording> Recordings { get; set; } = new List<Recording>();
    }
}
