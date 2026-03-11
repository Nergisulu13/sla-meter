using Microsoft.EntityFrameworkCore;
using SlaMonitor.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQLite DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=slamonitor.db"));

// CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ui", p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ui");

app.MapControllers();

app.Run();