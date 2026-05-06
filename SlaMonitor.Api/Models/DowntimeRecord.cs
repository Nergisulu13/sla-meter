namespace SlaMonitor.Api.Models
{
    public class DowntimeRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Environment { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string Customers { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}