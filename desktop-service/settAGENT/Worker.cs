using Microsoft.Extensions.Options;
using settAGENT.Collectors;
using settAGENT.Models;
using settAGENT.Services;

namespace settAGENT
{
    // Servicio principal del agente (BackgroundService = se ejecuta en bucle
    // hasta que la app pide cancelar). Es el "motor" del agente:
    //
    //   1. Al arrancar resuelve qué Worker representa este PC (por hostname),
    //      abre una WorkSession y un primer ActivityPeriod "active".
    //   2. En bucle, cada N segundos:
    //        - mira qué ventana tiene el foco y, si ha cambiado, cierra la
    //          actividad anterior y abre una nueva.
    //        - mira si el usuario ha tecleado/movido el ratón hace poco y, si
    //          ha cambiado el estado active↔idle, cierra el periodo anterior
    //          y abre uno nuevo.
    //   3. Al cancelarse (Ctrl+C, parada del servicio) cierra todo lo abierto.
    //
    // Toda comunicación con la API pasa por _apiSender (ApiSenderService).
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ApiSenderService _apiSender;
        private readonly AgentSettings _settings;

        // Datos del PC, calculados una sola vez al arrancar.
        private readonly string _macAddress;
        private readonly string _hostname;
        private readonly string _windowsUsername;

        // Estado en memoria que mantenemos entre ticks del bucle.
        private int _currentSessionId;
        private int? _currentActivityId;
        private int? _currentPeriodId;
        private string _lastProcessName = string.Empty;
        private bool _lastIsActive = true;

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

            // -----------------------------------------------------------------
            // 1. RESOLVER WORKER POR HOSTNAME
            // Le preguntamos a la API si existe un Worker registrado con este
            // hostname. Si no, abortamos (el admin tiene que crear el worker
            // antes de que el agente pueda mandar telemetría).
            // -----------------------------------------------------------------
            int? workerId = await _apiSender.GetWorkerIdByHostnameAsync(_hostname, stoppingToken);
            if (workerId == null)
            {
                _logger.LogError("Worker no resuelto para hostname {Hostname}. Abortando.", _hostname);
                return;
            }
            _logger.LogInformation("Worker resuelto por hostname: id={WorkerId}", workerId.Value);

            // -----------------------------------------------------------------
            // 2. ABRIR SESIÓN + PERIODO INICIAL
            // -----------------------------------------------------------------
            int? sessionId = await _apiSender.OpenSessionAsync(workerId.Value, stoppingToken);
            if (sessionId == null)
            {
                _logger.LogError("No se pudo abrir sesion con la API. Abortando.");
                return;
            }
            _currentSessionId = sessionId.Value;
            _logger.LogInformation("Sesion abierta con ID: {SessionId}", _currentSessionId);

            // Asumimos que el usuario está activo al arrancar (luego el bucle
            // lo corregirá si lleva rato sin tocar el teclado).
            _currentPeriodId = await _apiSender.OpenActivityPeriodAsync(_currentSessionId, "active", stoppingToken);

            TimeSpan interval = TimeSpan.FromSeconds(_settings.CollectionIntervalSeconds);

            // -----------------------------------------------------------------
            // 3. BUCLE PRINCIPAL — se ejecuta cada CollectionIntervalSeconds
            //    hasta que se pida cancelar.
            // -----------------------------------------------------------------
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    (string windowTitle, string processName) = ActiveWindowCollector.GetActiveWindow();
                    bool isActive = ActivityCollector.IsUserActive(_settings.InactivityThresholdMinutes);

                    // 3a. CAMBIO DE VENTANA
                    if (string.IsNullOrEmpty(processName))
                    {
                        // Casos raros: bloqueo de pantalla, ventana de sistema sin proceso accesible...
                        _logger.LogWarning("[APP] GetForegroundWindow no detecto ventana activa este tick.");
                    }
                    else if (processName != _lastProcessName)
                    {
                        _logger.LogInformation("[APP] Cambio: {Process} | {Title}", processName, windowTitle);

                        // Cerrar la actividad de la app anterior (si había una)
                        if (_currentActivityId.HasValue)
                            await _apiSender.CloseAppActivityAsync(_currentActivityId.Value, stoppingToken);

                        // Asegurarnos de que la app está en el catálogo y abrir nueva actividad
                        int? appId = await _apiSender.GetOrCreateApplicationAsync(processName, stoppingToken);
                        _logger.LogInformation("[APP] GetOrCreate appId={AppId}", appId?.ToString() ?? "NULL — fallo al crear/obtener app");

                        if (appId.HasValue)
                        {
                            _currentActivityId = await _apiSender.OpenAppActivityAsync(_currentSessionId, appId.Value, stoppingToken);
                            _logger.LogInformation("[APP] OpenActivity activityId={ActivityId}", _currentActivityId?.ToString() ?? "NULL — fallo al abrir actividad");
                        }

                        _lastProcessName = processName;
                    }

                    // 3b. CAMBIO DE ESTADO ACTIVO ↔ INACTIVO
                    if (isActive != _lastIsActive)
                    {
                        string nuevoEstado = isActive ? "active" : "idle";
                        _logger.LogInformation("Cambio de estado: {Estado}", nuevoEstado);

                        if (_currentPeriodId.HasValue)
                            await _apiSender.CloseActivityPeriodAsync(_currentPeriodId.Value, stoppingToken);

                        _currentPeriodId = await _apiSender.OpenActivityPeriodAsync(_currentSessionId, nuevoEstado, stoppingToken);

                        _lastIsActive = isActive;
                    }
                }
                catch (Exception ex)
                {
                    // Capturamos cualquier error del tick para que el bucle no se muera.
                    _logger.LogError(ex, "Error durante la recopilacion.");
                }

                await Task.Delay(interval, stoppingToken);
            }

            // -----------------------------------------------------------------
            // 4. CLEANUP — al cancelar, cerrar todo lo que dejamos abierto
            // Usamos CancellationToken.None porque el stoppingToken ya está
            // cancelado y no queremos que estas llamadas se aborten a medias.
            // -----------------------------------------------------------------
            _logger.LogInformation("Cerrando sesion...");
            if (_currentActivityId.HasValue)
                await _apiSender.CloseAppActivityAsync(_currentActivityId.Value, CancellationToken.None);
            if (_currentPeriodId.HasValue)
                await _apiSender.CloseActivityPeriodAsync(_currentPeriodId.Value, CancellationToken.None);
            await _apiSender.CloseSessionAsync(_currentSessionId, CancellationToken.None);
        }
    }
}
