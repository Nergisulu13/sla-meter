using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using SlaMonitor.Auth.Data;
using SlaMonitor.Auth.Models;

namespace SlaMonitor.Auth
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<AuthUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var appManager = services.GetRequiredService<IOpenIddictApplicationManager>();
            var db = services.GetRequiredService<AuthDbContext>();

            string[] roles = { "Admin", "Operator", "Viewer", "SuperAdmin" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await EnsureTenantsAsync(db);

            var huaweiTenant = await db.Tenants.FirstAsync(x => x.Name == "Huawei");

            await CreateUserAsync(userManager, "admin", "Admin123!", "Admin", huaweiTenant);
            await CreateUserAsync(userManager, "operator", "Operator123!", "Operator", huaweiTenant);
            await CreateUserAsync(userManager, "viewer", "Viewer123!", "Viewer", huaweiTenant);
            await CreateUserAsync(userManager, "admin2", "Admin123!", "SuperAdmin", null);

            await CreateClientApplicationAsync(appManager);
        }

        private static async Task EnsureTenantsAsync(AuthDbContext db)
        {
            string[] tenantNames =
            {
                "Eclit",
                "Paris",
                "Huawei",
                "Ohio",
                "UAE",
                "Preprod Ireland"
            };

            foreach (var tenantName in tenantNames)
            {
                if (!await db.Tenants.AnyAsync(x => x.Name == tenantName))
                {
                    db.Tenants.Add(new Tenant
                    {
                        Name = tenantName
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task CreateUserAsync(
            UserManager<AuthUser> userManager,
            string username,
            string password,
            string role,
            Tenant? tenant)
        {
            var user = await userManager.FindByNameAsync(username);

            if (user == null)
            {
                user = new AuthUser
                {
                    UserName = username,
                    Email = $"{username}@local.com",
                    EmailConfirmed = true,
                    TenantId = tenant?.Id,
                    Tenant = tenant?.Name ?? "ALL"
                };

                var result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"{username} oluşturulamadı: {errors}");
                }
            }
            else
            {
                user.TenantId = tenant?.Id;
                user.Tenant = tenant?.Name ?? "ALL";

                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    throw new Exception($"{username} kullanıcısının tenant bilgisi güncellenemedi: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"{username} kullanıcısına {role} rolü atanamadı: {errors}");
                }
            }

            if (tenant is not null)
            {
                await EnsureClaimAsync(userManager, user, "tenant_id", tenant.Id.ToString());
                await EnsureClaimAsync(userManager, user, "tenant_name", tenant.Name);
            }
            else
            {
                await EnsureClaimAsync(userManager, user, "tenant_name", "ALL");
            }
        }

        private static async Task EnsureClaimAsync(
            UserManager<AuthUser> userManager,
            AuthUser user,
            string claimType,
            string claimValue)
        {
            var claims = await userManager.GetClaimsAsync(user);
            var existingClaims = claims.Where(x => x.Type == claimType).ToList();

            foreach (var claim in existingClaims.Where(x => x.Value != claimValue))
            {
                var removeResult = await userManager.RemoveClaimAsync(user, claim);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    throw new Exception($"{user.UserName} kullanıcısından {claimType} claim'i silinemedi: {errors}");
                }
            }

            var hasCorrectClaim = existingClaims.Any(x => x.Value == claimValue);
            if (!hasCorrectClaim)
            {
                var addResult = await userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    throw new Exception($"{user.UserName} kullanıcısına {claimType} claim'i eklenemedi: {errors}");
                }
            }
        }

        private static async Task CreateClientApplicationAsync(IOpenIddictApplicationManager appManager)
        {
            const string clientId = "sla-angular";

            var existingApp = await appManager.FindByClientIdAsync(clientId);
            if (existingApp != null)
            {
                return;
            }

            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "SLA Angular UI",
                RedirectUris =
                {
                    new Uri("http://localhost:4200/auth/callback")
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,

                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,

                    Permissions.ResponseTypes.Code,

                    Permissions.Prefixes.Scope + Scopes.OpenId,
                    Permissions.Prefixes.Scope + Scopes.Profile,
                    Permissions.Prefixes.Scope + Scopes.OfflineAccess,
                    Permissions.Prefixes.Scope + "incidents_api"
                }
            });
        }
    }
}