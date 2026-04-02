using Microsoft.AspNetCore.Identity;
using SlaMonitor.Auth.Models;

namespace SlaMonitor.Auth
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<AuthUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

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
    }
}