using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SlaMonitor.Auth;
using SlaMonitor.Auth.Data;
using SlaMonitor.Auth.Models;

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

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "auth.db");
    Console.WriteLine("AUTH DB PATH => " + dbPath);

    options.UseSqlite($"Data Source={dbPath}");
    options.UseOpenIddict();
});

builder.Services.AddIdentity<AuthUser, IdentityRole>()
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

        options.SetIssuer(new Uri("http://sla-auth:8080/"));

        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.OfflineAccess,
            "incidents_api");

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(5));
        options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(30));

        options.DisableRollingRefreshTokens();
        options.DisableSlidingRefreshTokenExpiration();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.DisableAccessTokenEncryption();

        options.AcceptAnonymousClients();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    Console.WriteLine("MIGRATION BASLIYOR...");
    db.Database.Migrate();
    Console.WriteLine("MIGRATION BITTI.");

    Console.WriteLine("SEED BASLIYOR...");
    await IdentitySeed.SeedAsync(scope.ServiceProvider);
    Console.WriteLine("SEED BITTI.");
}

app.UseStaticFiles();
app.UseRouting();

app.UseCors("frontend");

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();