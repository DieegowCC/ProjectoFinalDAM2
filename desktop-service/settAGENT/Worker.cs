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

        // Estado en memoria
        private int _currentSessionId;
        private int? _currentActivityId;
        private int? _currentPeriodId;
        private string _lastProcessName = string.Empty;
        private bool _lastIsActive = true;

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

            // 1. Abrir sesión al arrancar
            // worker_id hardcodeado por ahora, hasta definir cómo identificar al worker
            int? sessionId = await _apiSender.OpenSessionAsync(_settings.WorkerId, stoppingToken);
            if (sessionId == null)
            {
                _logger.LogError("No se pudo abrir sesión con la API. Abortando.");
                return;
            }
            _currentSessionId = sessionId.Value;
            _logger.LogInformation("Sesión abierta con ID: {SessionId}", _currentSessionId);

            // 2. Abrir periodo inicial como activo
            _currentPeriodId = await _apiSender.OpenActivityPeriodAsync(_currentSessionId, "active", stoppingToken);

            var interval = TimeSpan.FromSeconds(_settings.CollectionIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var (windowTitle, processName) = ActiveWindowCollector.GetActiveWindow();
                    bool isActive = ActivityCollector.IsUserActive(_settings.InactivityThresholdMinutes);

                    // 3. Detectar cambio de ventana
                    if (processName != _lastProcessName)
                    {
                        _logger.LogInformation("Cambio de ventana: {Process} | {Title}", processName, windowTitle);

                        // Cerrar actividad anterior
                        if (_currentActivityId.HasValue)
                            await _apiSender.CloseAppActivityAsync(_currentActivityId.Value, stoppingToken);

                        // Registrar/obtener aplicación y abrir nueva actividad
                        int? appId = await _apiSender.GetOrCreateApplicationAsync(processName, windowTitle, stoppingToken);
                        if (appId.HasValue)
                            _currentActivityId = await _apiSender.OpenAppActivityAsync(_currentSessionId, appId.Value, stoppingToken);

                        _lastProcessName = processName;
                    }

                    // 4. Detectar cambio de estado activo/inactivo
                    if (isActive != _lastIsActive)
                    {
                        string nuevoEstado = isActive ? "active" : "idle";
                        _logger.LogInformation("Cambio de estado: {Estado}", nuevoEstado);

                        // Cerrar periodo anterior
                        if (_currentPeriodId.HasValue)
                            await _apiSender.CloseActivityPeriodAsync(_currentPeriodId.Value, stoppingToken);

                        // Abrir nuevo periodo
                        _currentPeriodId = await _apiSender.OpenActivityPeriodAsync(_currentSessionId, nuevoEstado, stoppingToken);

                        _lastIsActive = isActive;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante la recopilación.");
                }

                await Task.Delay(interval, stoppingToken);
            }

            // 5. Cerrar todo al apagarse
            _logger.LogInformation("Cerrando sesión...");
            if (_currentActivityId.HasValue)
                await _apiSender.CloseAppActivityAsync(_currentActivityId.Value, CancellationToken.None);
            if (_currentPeriodId.HasValue)
                await _apiSender.CloseActivityPeriodAsync(_currentPeriodId.Value, CancellationToken.None);
            await _apiSender.CloseSessionAsync(_currentSessionId, CancellationToken.None);
        }
    }
}