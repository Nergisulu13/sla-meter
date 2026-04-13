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

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlite("Data Source=auth.db");
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

        options.SetIssuer(new Uri("http://localhost:5183/"));

        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.OfflineAccess,
            "incidents_api");

        options.SetAccessTokenLifetime(TimeSpan.FromSeconds(30));
        options.SetRefreshTokenLifetime(TimeSpan.FromSeconds(60));

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
    db.Database.EnsureCreated();

    await IdentitySeed.SeedAsync(scope.ServiceProvider);
}

app.UseStaticFiles();
app.UseRouting();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run("http://localhost:5183");