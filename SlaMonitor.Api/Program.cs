using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using SlaMonitor.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=slamonitor.db")
);

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DowntimesRead", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx =>
                  ctx.User.Claims.Any(c =>
                      (c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" || c.Type == "role") &&
                      (c.Value == "Admin" || c.Value == "Operator" || c.Value == "Viewer" || c.Value == "SuperAdmin"))));

    options.AddPolicy("DowntimesWrite", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx =>
                  ctx.User.Claims.Any(c =>
                      (c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" || c.Type == "role") &&
                      (c.Value == "Admin" || c.Value == "Operator" || c.Value == "SuperAdmin"))));

    options.AddPolicy("DowntimesDelete", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx =>
                  ctx.User.Claims.Any(c =>
                      (c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" || c.Type == "role") &&
                      (c.Value == "Admin" || c.Value == "SuperAdmin"))));
});

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("http://sla-auth:8080/");
        options.AddAudiences("resource_server");

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();