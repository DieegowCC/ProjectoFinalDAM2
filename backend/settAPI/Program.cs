using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using System.Text;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using settAPI.Data;
using settAPI.Hubs;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();                                       
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(ConfigurarSwagger);  // configura Swagger con soporte JWT

builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(ConfigurarBaseDatos);

builder.Services.AddCors(ConfigurarCors);

builder.Services.AddAuthentication(ConfigurarAuthentication).AddJwtBearer(ConfigurarJwtBearer);

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthentication();    // debe ir siempre antes de UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");   // expone el hub en esta URL para que el frontend se conecte
app.Run();

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

void ConfigurarAuthentication(AuthenticationOptions options)
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;     // esquema por defecto para autenticar
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;        // esquema por defecto para redirigir si no autenticado
}

void ConfigurarJwtBearer(JwtBearerOptions options)
{
    string key = builder.Configuration["Jwt:Key"]!;                             // clave secreta del appsettings.json

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,                                                      // verifica que el emisor del token es correcto
        ValidateAudience = true,                                                    // verifica que el receptor del token es correcto
        ValidateLifetime = true,                                                    // verifica que el token no ha expirado
        ValidateIssuerSigningKey = true,                                            // verifica que la firma del token es válida
        ValidIssuer = builder.Configuration["Jwt:Issuer"],                          // emisor válido definido en appsettings.json
        ValidAudience = builder.Configuration["Jwt:Issuer"],                        // receptor válido (mismo que emisor en este caso)
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))    // clave con la que se firmó el token

    };
}

void ConfigurarSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    OpenApiSecurityScheme securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",                 // nombre del header HTTP
        Type = SecuritySchemeType.Http,         // tipo de esquema HTTP
        Scheme = "Bearer",                      // esquema Bearer
        In = ParameterLocation.Header,          // el token va en el header
        Description = "Introduce el token JWT"
    };

    OpenApiSecurityScheme schemeReference = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"                       // referencia al esquema definido arriba
        }
    };

    OpenApiSecurityRequirement securityRequirement = new OpenApiSecurityRequirement
    {
        { schemeReference, new List<string>() }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(securityRequirement);
}