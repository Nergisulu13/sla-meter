using Microsoft.EntityFrameworkCore;
using SlaMonitor.Api.Models;

namespace SlaMonitor.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DowntimeRecord> Downtimes => Set<DowntimeRecord>();
    }
}