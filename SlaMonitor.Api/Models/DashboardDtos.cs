namespace SlaMonitor.Api.Models;

public record EnvironmentSlaCard(
    string Environment,
    double SlaPercent,
    int DowntimeMinutes,
    int AllowedDowntimeMinutes,
    int Points
);

public record DashboardDto(
    int TotalDowntimeCount,
    int TotalDowntimeMinutes,
    double AverageSlaPercent,
    int AveragePoints,
    int AverageAllowedDowntimeMinutes,
    List<EnvironmentSlaCard> EnvironmentCards
);