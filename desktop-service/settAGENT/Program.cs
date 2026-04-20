using Serilog;
using settAGENT.Models;
using settAGENT.Services;
namespace settAGENT
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. Configuramos el Logger al principio de todo
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information() // Nivel mínimo de mensajes a guardar
                .WriteTo.File(
                    path: "logs/log-.txt",         // Carpeta "logs" dentro de donde está el .exe
                    rollingInterval: RollingInterval.Day, // Crea un archivo nuevo CADA DÍA
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            try
            {
                Log.Information("Iniciando el servicio del Agente...");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "El servicio se detuvo inesperadamente.");
            }
            finally
            {
                Log.CloseAndFlush(); // Asegura que se guarden los últimos mensajes al cerrar
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService() // Configuración para servicio de Windows
                .UseSerilog()        // <--- IMPORTANTE: Le decimos al Host que use nuestro Log
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<AgentSettings>(hostContext.Configuration.GetSection("AgentSettings"));
                    services.AddSingleton<ApiSenderService>();
                    services.AddHostedService<Worker>();
                });
    }
}