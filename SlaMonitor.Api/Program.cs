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
    opt.UseSqlite("Data Source=slamonitor.db")); // 🔥 LOCAL
    
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

builder.Services.AddAuthorization();

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("http://localhost:5183");
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