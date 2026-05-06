using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SlaMonitor.Auth.Data;
using SlaMonitor.Auth.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

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
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            await EnsureTenantsAsync(db);

            await CreateOrUpdateUserAsync(userManager, "admin", "Admin123!", "Admin");
            await CreateOrUpdateUserAsync(userManager, "operator", "Operator123!", "Operator");
            await CreateOrUpdateUserAsync(userManager, "viewer", "Viewer123!", "Viewer");
            await CreateOrUpdateUserAsync(userManager, "superadmin", "Admin123!", "SuperAdmin");

            await CreateClientApplicationAsync(appManager);
        }

        private static async Task EnsureTenantsAsync(AuthDbContext db)
        {
            var tenants = new List<Tenant>
            {
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Eclit", DisplayName = "Eclit", IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Paris", DisplayName = "Paris", IsActive = true },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Huawei", DisplayName = "Huawei", IsActive = true },
                new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Ohio", DisplayName = "Ohio", IsActive = true },
                new() { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "UAE", DisplayName = "UAE", IsActive = true },
                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Preprod Ireland", DisplayName = "Preprod Ireland", IsActive = true }
            };

            foreach (var tenant in tenants)
            {
                var existing = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenant.Id);

                if (existing == null)
                {
                    db.Tenants.Add(tenant);
                }
                else
                {
                    existing.Name = tenant.Name;
                    existing.DisplayName = tenant.DisplayName;
                    existing.IsActive = tenant.IsActive;
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task CreateOrUpdateUserAsync(
            UserManager<AuthUser> userManager,
            string username,
            string password,
            string role)
        {
            var email = $"{username}@local.com";
            var user = await userManager.Users.FirstOrDefaultAsync(x => x.UserName == username);

            if (user == null)
            {
                user = new AuthUser
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true,
                    TenantId = null
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"{username} oluşturulamadı: {errors}");
                }
            }
            else
            {
                var changed = false;

                if (user.TenantId != null)
                {
                    user.TenantId = null;
                    changed = true;
                }

                if (user.Email != email)
                {
                    user.Email = email;
                    changed = true;
                }

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    changed = true;
                }

                if (changed)
                {
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                        throw new Exception($"{username} güncellenemedi: {errors}");
                    }
                }
            }

            var currentRoles = await userManager.GetRolesAsync(user);

            if (!currentRoles.Contains(role) || currentRoles.Count != 1)
            {
                if (currentRoles.Any())
                {
                    var removeRolesResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeRolesResult.Succeeded)
                    {
                        var errors = string.Join(", ", removeRolesResult.Errors.Select(e => e.Description));
                        throw new Exception($"{username} eski rolleri silinemedi: {errors}");
                    }
                }

                var addRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    throw new Exception($"{username} rol atanamadı: {errors}");
                }
            }
        }

        private static async Task CreateClientApplicationAsync(IOpenIddictApplicationManager appManager)
        {
            const string clientId = "sla-angular";

            var existingApp = await appManager.FindByClientIdAsync(clientId);
            if (existingApp != null)
                return;

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