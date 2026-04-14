using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using settAPI.Data;
using settAPI.Hubs;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);      

builder.Services.AddControllers();                                       
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(ConfigurarBaseDatos);

builder.Services.AddCors(ConfigurarCors);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())                                     
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();
app.Run();
app.MapHub<MonitoringHub>("/hubs/monitoring");   // expone el hub en esta URL para que el frontend se conecte

void ConfigurarBaseDatos(DbContextOptionsBuilder options)
{
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    options.UseNpgsql(connectionString);
}

void ConfigurarCors(CorsOptions options)
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
}