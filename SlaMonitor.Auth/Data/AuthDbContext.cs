using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SlaMonitor.Auth.Models;

namespace SlaMonitor.Auth.Data
{
    public class AuthDbContext : IdentityDbContext<AuthUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.UseOpenIddict();

            builder.Entity<Tenant>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
                entity.Property(x => x.DisplayName).HasMaxLength(150);
                entity.HasIndex(x => x.Name).IsUnique();
            });

            builder.Entity<AuthUser>(entity =>
            {
                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}