using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using settAPI.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);     // Configura los servicios y el pipeline de la aplicación
                                                                        // (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup)
builder.Services.AddControllers();                                       // Añade soporte para controladores MVC (endpoints de la API)
                                                                         // (https://learn.microsoft.com/en-us/aspnet/core/web-api)
builder.Services.AddEndpointsApiExplorer();                              // Necesario para que Swagger descubra los endpoints automáticamente
builder.Services.AddSwaggerGen();                                        // (https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger)
builder.Services.AddSignalR();                                           // Registra SignalR para comunicación en tiempo real por WebSocket
                                                                         // (https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
builder.Services.AddDbContext<AppDbContext>(ConfigurarBaseDatos);        // Registra AppDbContext con PostgreSQL usando la connection string de appsettings.json
                                                                         // (https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration)
builder.Services.AddCors(ConfigurarCors);                                // Registra la política CORS para permitir peticiones del frontend Next.js
                                                                         // (https://learn.microsoft.com/en-us/aspnet/core/security/cors)
WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())                                     // Swagger solo se activa en desarrollo, nunca en producción
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();                                               // Redirige HTTP a HTTPS automáticamente
                                                                         // (https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl)
app.UseCors("AllowFrontend");                                            // Activa la política CORS — debe ir antes de UseAuthorization
app.UseAuthorization();                                                  // Middleware de autorización (JWT se configurará aquí más adelante)
                                                                         // (https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction)
app.MapControllers();                                                    // Mapea las rutas a los controladores automáticamente
app.Run();                                                               // Arranca el servidor y empieza a escuchar peticiones

void ConfigurarBaseDatos(DbContextOptionsBuilder options)                // Configura EF Core para usar PostgreSQL
                                                                         // (https://www.npgsql.org/efcore)
{
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    options.UseNpgsql(connectionString);
}

void ConfigurarCors(CorsOptions options)                                 // Define la política CORS: permite peticiones desde el frontend en localhost:3000
                                                                         // AllowCredentials es necesario para que SignalR funcione correctamente
                                                                         // (https://learn.microsoft.com/en-us/aspnet/core/security/cors)
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
}