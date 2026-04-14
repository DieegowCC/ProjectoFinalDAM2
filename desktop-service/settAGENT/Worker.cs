using Microsoft.Extensions.Options;
using settAGENT.Collectors;
using settAGENT.Models;
using settAGENT.Services;

namespace settAGENT
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ApiSenderService _apiSender;
        private readonly AgentSettings _settings;

        private readonly string _macAddress;
        private readonly string _hostname;
        private readonly string _windowsUsername;

        private static readonly TimeSpan CollectionInterval = TimeSpan.FromSeconds(10);

        public Worker(ILogger<Worker> logger, ApiSenderService apiSender, IOptions<AgentSettings> settings)
        {
            _logger = logger;
            _apiSender = apiSender;
            _settings = settings.Value;

            _macAddress = SystemInfoCollector.GetMacAddress();
            _hostname = SystemInfoCollector.GetHostname();
            _windowsUsername = SystemInfoCollector.GetWindowsUsername();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Agente iniciado. MAC: {Mac} | Host: {Host} | User: {User}",
                _macAddress, _hostname, _windowsUsername);

            TimeSpan interval = TimeSpan.FromSeconds(_settings.CollectionIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ActivitySnapshot snapshot = CollectSnapshot();

                    _logger.LogInformation(
                        "Snapshot | Activo: {Active} | Proceso: {Process} | Ventana: {Window}",
                        snapshot.IsUserActive,
                        snapshot.ActiveProcessName,
                        snapshot.ActiveWindowTitle
                    );

                    await _apiSender.SendAsync(snapshot, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante la recopilación.");
                }

                await Task.Delay(CollectionInterval, stoppingToken);
            }
        }

        private ActivitySnapshot CollectSnapshot()
        {
            var (windowTitle, processName) = ActiveWindowCollector.GetActiveWindow();

            return new ActivitySnapshot
            {
                MacAddress = _macAddress,
                Hostname = _hostname,
                WindowsUsername = _windowsUsername,
                ActiveWindowTitle = windowTitle,
                ActiveProcessName = processName,
                IsUserActive = ActivityCollector.IsUserActive(_settings.InactivityThresholdMinutes),
                CapturedAt = DateTime.UtcNow
            };
        }
    }
}