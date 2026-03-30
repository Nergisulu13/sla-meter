using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SlaMonitor.Auth.Data;
using SlaMonitor.Auth.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlite("Data Source=auth.db");
    options.UseOpenIddict();
});

builder.Services
    .AddIdentity<AuthUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<AuthDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token");

        options.AllowAuthorizationCodeFlow();

        options.RegisterScopes(
            Scopes.OpenId,
            Scopes.Profile,
            "incidents_api");

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.DisableAccessTokenEncryption();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await SeedDataAsync(app);

app.Run();

static async Task SeedDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.EnsureCreatedAsync();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();
    var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    var user = await userManager.FindByNameAsync("admin");
    if (user is null)
    {
        user = new AuthUser
        {
            UserName = "admin",
            Email = "admin@test.com",
            Tenant = "default",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, "Admin123!");
    }

    if (await appManager.FindByClientIdAsync("sla-angular") is null)
    {
        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "sla-angular",
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
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OpenId,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + "incidents_api"
            }
        });
    }
}