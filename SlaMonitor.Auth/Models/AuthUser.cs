using Microsoft.AspNetCore.Identity;

namespace SlaMonitor.Auth.Models
{
    public class AuthUser : IdentityUser
    {
        public Guid? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}