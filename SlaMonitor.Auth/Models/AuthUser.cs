using Microsoft.AspNetCore.Identity;

namespace SlaMonitor.Auth.Models;

public class AuthUser : IdentityUser
{
    public string Tenant { get; set; } = "default";
}