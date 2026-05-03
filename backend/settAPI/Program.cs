using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using settAPI.Data;
using settAPI.Hubs;
using System.Text;
using System.Text.Json;

// =============================================================================
// Punto de entrada de la API.
// Este archivo configura todos los servicios (BBDD, JWT, CORS, SignalR, Swagger),
// arranca la app, aplica migraciones, siembra datos iniciales y limpia datos
// huérfanos antes de empezar a aceptar peticiones.
// =============================================================================

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. REGISTRO DE SERVICIOS
// -----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(ConfigurarSwagger);                      // Swagger con soporte JWT
builder.Services.AddSignalR().AddJsonProtocol(ConfigurarSignalR);       // SignalR en camelCase
builder.Services.AddDbContext<AppDbContext>(ConfigurarBaseDatos);       // PostgreSQL via EF Core
builder.Services.AddCors(ConfigurarCors);                               // Permite llamadas desde el frontend
builder.Services.AddAuthentication(ConfigurarAuthentication)            // JWT Bearer
       .AddJwtBearer(ConfigurarJwtBearer);

WebApplication app = builder.Build();

// -----------------------------------------------------------------------------
// 2. PIPELINE HTTP
// El orden importa: CORS → Authentication → Authorization → Endpoints.
// -----------------------------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthentication();    // siempre antes de UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring").AllowAnonymous();   // hub público para el dashboard

// -----------------------------------------------------------------------------
// 3. ARRANQUE DE LA BBDD: migrar + sembrar + limpiar huérfanos
// Creamos un scope manual porque DbContext es Scoped y aquí estamos fuera de
// una petición HTTP.
// -----------------------------------------------------------------------------
IServiceScope scope = app.Services.CreateScope();
IServiceProvider provider = scope.ServiceProvider;
AppDbContext dbContext = provider.GetRequiredService<AppDbContext>();

dbContext.Database.Migrate();   // crea las tablas si no existen y aplica migraciones pendientes
DbSeeder.Seed(dbContext);       // crea admin/admin y workers de demo si la BBDD está vacía

// Cleanup de runs anteriores: si el agente (o la API) se apagaron a la brava
// pueden quedar sesiones, periodos o actividades sin cerrar (ended_at = null).
// Las cerramos con el timestamp actual para que las stats no se vuelvan locas.
DateTime nowUtc = DateTime.UtcNow;

List<settAPI.Classes.WorkSession> orphanSessions = dbContext.WorkSessions
    .Where(s => s.ended_at == null)
    .ToList();
foreach (settAPI.Classes.WorkSession s in orphanSessions)
{
    s.ended_at = nowUtc;
    s.total_minutes = (int)(nowUtc - s.started_at).TotalMinutes;
}

List<settAPI.Classes.ActivityPeriod> orphanPeriods = dbContext.ActivityPeriods
    .Where(p => p.period_end == null)
    .ToList();
foreach (settAPI.Classes.ActivityPeriod p in orphanPeriods)
    p.period_end = nowUtc;

List<settAPI.Classes.AppActivity> orphanActivities = dbContext.AppActivities
    .Where(a => a.ended_at == null)
    .ToList();
foreach (settAPI.Classes.AppActivity a in orphanActivities)
    a.ended_at = nowUtc;

if (orphanSessions.Count > 0 || orphanPeriods.Count > 0 || orphanActivities.Count > 0)
{
    dbContext.SaveChanges();
    Console.WriteLine($"[Cleanup] Cerradas {orphanSessions.Count} sesiones, {orphanPeriods.Count} periodos y {orphanActivities.Count} actividades huerfanas.");
}

scope.Dispose();    // liberamos el scope manual

app.Run();          // arranca el servidor HTTP y se queda escuchando

// =============================================================================
// FUNCIONES DE CONFIGURACIÓN
// (van debajo del app.Run porque en C# top-level statements el cuerpo principal
//  se compila como Main; las funciones locales se pueden declarar después.)
// =============================================================================

// Configura EF Core para conectar a PostgreSQL usando la cadena de conexión
// definida en appsettings.json → ConnectionStrings:DefaultConnection.
void ConfigurarBaseDatos(DbContextOptionsBuilder options)
{
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    options.UseNpgsql(connectionString);
}

// Permite que el frontend (Next.js en localhost:3000) llame a esta API
// enviando cookies/tokens. Sin esto, el navegador bloquearía las llamadas.
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

// Indica que el esquema de autenticación por defecto es JWT Bearer.
// "Authenticate" = cómo identificamos al usuario en cada petición.
// "Challenge"   = qué hacemos si no está autenticado (devolver 401).
void ConfigurarAuthentication(AuthenticationOptions options)
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}

// Configura cómo se valida cada JWT que llega en el header Authorization.
// La clave secreta y los datos de issuer/audience están en appsettings.json.
void ConfigurarJwtBearer(JwtBearerOptions options)
{
    string key = builder.Configuration["Jwt:Key"]!;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,                                                      // ¿quién emitió el token?
        ValidateAudience = true,                                                    // ¿para quién es?
        ValidateLifetime = true,                                                    // ¿no ha expirado?
        ValidateIssuerSigningKey = true,                                            // ¿la firma es válida?
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Issuer"],                        // mismo valor que el issuer
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
}

// Añade a Swagger un botón "Authorize" para meter un token JWT y probar
// endpoints protegidos desde el navegador.
void ConfigurarSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    OpenApiSecurityScheme securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT"
    };

    OpenApiSecurityScheme schemeReference = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    OpenApiSecurityRequirement securityRequirement = new OpenApiSecurityRequirement
    {
        { schemeReference, new List<string>() }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(securityRequirement);
}

// Hace que SignalR mande las propiedades en camelCase ("workerId" en vez de
// "WorkerId") para que el cliente JavaScript del frontend las reciba con el
// mismo formato que ya usa con fetch + JSON.
void ConfigurarSignalR(JsonHubProtocolOptions options)
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
}
