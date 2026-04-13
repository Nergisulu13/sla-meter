using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
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

            string[] roles = { "Admin", "Operator", "Viewer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await CreateUserAsync(userManager, "admin", "Admin123!", "Admin");
            await CreateUserAsync(userManager, "operator", "Operator123!", "Operator");
            await CreateUserAsync(userManager, "viewer", "Viewer123!", "Viewer");

            // 🔥 BURASI ÇOK KRİTİK
            await CreateClientApplicationAsync(appManager);
        }

        private static async Task CreateUserAsync(
            UserManager<AuthUser> userManager,
            string username,
            string password,
            string role)
        {
            var user = await userManager.FindByNameAsync(username);

            if (user == null)
            {
                user = new AuthUser
                {
                    UserName = username,
                    Email = $"{username}@local.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"{username} oluşturulamadı: {errors}");
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