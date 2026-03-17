using Microsoft.EntityFrameworkCore;
using SlaMonitor.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=/app/data/slamonitor.db"));


builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ui", p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ui");

app.MapControllers();

app.Run();