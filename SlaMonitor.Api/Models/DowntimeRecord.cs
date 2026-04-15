namespace SlaMonitor.Api.Models;

public class DowntimeRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Environment { get; set; } = default!;
    public int DurationMinutes { get; set; }
    public string Customers { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}