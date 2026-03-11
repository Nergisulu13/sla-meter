using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SlaMonitor.Api.Data;
using SlaMonitor.Api.Models;

namespace SlaMonitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    private static readonly string[] Environments =
    [
        "Eclit",
        "Paris",
        "Huawei",
        "Ohio",
        "UAE",
        "Preprod Ireland"
    ];

    // Yıllık hedef SLA
    private const double DefaultTargetSlaPercent = 99.90;

    // Preprod için farklı hedef istenirse burada kalabilir
    private const double PreprodTargetSlaPercent = 100.00;

    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var now = DateTime.UtcNow;

        // Sadece bu yılın kayıtları
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = startOfYear.AddYears(1);

        var all = await _db.Downtimes
            .AsNoTracking()
            .Where(x => x.OccurredAt >= startOfYear && x.OccurredAt < endOfYear)
            .ToListAsync();

        int totalCount = all.Count;
        int totalMinutes = all.Sum(x => x.DurationMinutes);

        // Yıllık toplam dakika
        int daysInYear = DateTime.IsLeapYear(now.Year) ? 366 : 365;
        int minutesInPeriod = daysInYear * 24 * 60;

        var cards = new List<EnvironmentSlaCard>();

        foreach (var env in Environments)
        {
            int envMinutes = all
                .Where(x => x.Environment == env)
                .Sum(x => x.DurationMinutes);

            double sla = CalcSlaPercent(envMinutes, minutesInPeriod);

            double targetSla = GetTargetSlaPercent(env);
            int allowedMinutes = CalcAllowedDowntimeMinutesFromTarget(targetSla, minutesInPeriod);

            int points = GetPoints(sla);

            cards.Add(new EnvironmentSlaCard(
                Environment: env,
                SlaPercent: sla,
                DowntimeMinutes: envMinutes,
                AllowedDowntimeMinutes: allowedMinutes,
                Points: points
            ));
        }

        double avgSla = cards.Count > 0
            ? Math.Round(cards.Average(x => x.SlaPercent), 3)
            : 100.000;

        int avgPoints = GetPoints(avgSla);

        int avgAllowed = cards.Count > 0
            ? (int)Math.Round(cards.Average(x => x.AllowedDowntimeMinutes), MidpointRounding.AwayFromZero)
            : 0;

        return Ok(new DashboardDto(
            TotalDowntimeCount: totalCount,
            TotalDowntimeMinutes: totalMinutes,
            AverageSlaPercent: avgSla,
            AveragePoints: avgPoints,
            AverageAllowedDowntimeMinutes: avgAllowed,
            EnvironmentCards: cards
        ));
    }

    private static double GetTargetSlaPercent(string environment)
    {
        if (environment == "Preprod Ireland")
            return PreprodTargetSlaPercent;

        return DefaultTargetSlaPercent;
    }

    private static double CalcSlaPercent(int downtimeMinutes, int totalMinutesInPeriod)
    {
        if (downtimeMinutes <= 0) return 100.000;
        if (downtimeMinutes >= totalMinutesInPeriod) return 0.000;

        double sla = 100.0 * (1.0 - (double)downtimeMinutes / totalMinutesInPeriod);
        return Math.Round(sla, 3);
    }

    private static int CalcAllowedDowntimeMinutesFromTarget(double targetSlaPercent, int totalMinutesInPeriod)
    {
        double ratioUp = targetSlaPercent / 100.0;
        double allowed = totalMinutesInPeriod * (1.0 - ratioUp);
        return (int)Math.Round(allowed, MidpointRounding.AwayFromZero);
    }

    private static int GetPoints(double sla)
    {
        if (sla >= 99.99) return 20;
        if (sla >= 99.95) return 16;
        if (sla >= 99.90) return 14;
        if (sla >= 99.50) return 12;
        if (sla >= 99.00) return 10;
        return 0;
    }
}