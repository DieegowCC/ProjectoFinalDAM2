using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using settAGENT.Models;

namespace settAGENT.Services
{
    // Servicio que encapsula TODAS las llamadas HTTP del agente a la API.
    //
    // El resto del agente (Worker.cs) llama solo a estos métodos públicos
    // — no toca HttpClient directamente. Así el código del Worker se centra
    // en la lógica (¿ha cambiado la ventana? ¿está idle?) y no en JSON.
    //
    // Todos los métodos devuelven null si la llamada falla, en lugar de tirar
    // excepción. El Worker decide qué hacer ante un null (normalmente loguear
    // y seguir intentándolo en el siguiente tick).
    public class ApiSenderService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiSenderService> _logger;
        private readonly string _baseUrl;

        public ApiSenderService(ILogger<ApiSenderService> logger, IOptions<AgentSettings> settings)
        {
            _httpClient = new HttpClient();
            _logger = logger;
            _baseUrl = settings.Value.ApiUrl;
        }

        // ==================== SESIÓN ====================

        /// <summary>
        /// POST /api/worksessions — abre una nueva sesión de trabajo en la API.
        /// Devuelve el id que la API ha asignado a la sesión, o null si falla.
        /// </summary>
        public async Task<int?> OpenSessionAsync(int workerId, CancellationToken ct)
        {
            var body = new { worker_id = workerId };
            var response = await PostAsync("api/worksessions", body, ct);
            return response?.GetProperty("session").GetProperty("id").GetInt32();
        }

        /// <summary>
        /// PUT /api/worksessions/{id}/close — cierra la sesión activa al apagarse el agente.
        /// </summary>
        public async Task CloseSessionAsync(int sessionId, CancellationToken ct)
        {
            await PutAsync($"api/worksessions/{sessionId}/close", ct);
        }

        // ==================== ACTIVIDAD DE APP ====================

        /// <summary>
        /// POST /api/appactivity — registra que el worker ha pasado a usar otra app.
        /// Devuelve el id de la AppActivity creada, o null si falla.
        /// </summary>
        public async Task<int?> OpenAppActivityAsync(int sessionId, int applicationId, CancellationToken ct)
        {
            var body = new { session_id = sessionId, applications_id = applicationId, is_foreground = true };
            var response = await PostAsync("api/appactivity", body, ct);
            return response?.GetProperty("activity").GetProperty("id").GetInt32();
        }

        /// <summary>
        /// PUT /api/appactivity/{id}/close — cierra la actividad anterior antes de abrir otra.
        /// </summary>
        public async Task CloseAppActivityAsync(int activityId, CancellationToken ct)
        {
            await PutAsync($"api/appactivity/{activityId}/close", ct);
        }

        // ==================== PERIODOS ACTIVO / INACTIVO ====================

        /// <summary>
        /// POST /api/activityperiods — abre un periodo nuevo cuando cambia el estado del usuario.
        /// status puede ser "active" (el usuario está tecleando/moviendo el ratón) o "idle".
        /// </summary>
        public async Task<int?> OpenActivityPeriodAsync(int sessionId, string status, CancellationToken ct)
        {
            var body = new { session_id = sessionId, status };
            var response = await PostAsync("api/activityperiods", body, ct);
            return response?.GetProperty("period").GetProperty("id").GetInt32();
        }

        /// <summary>
        /// PUT /api/activityperiods/{id}/close — cierra el periodo actual.
        /// </summary>
        public async Task CloseActivityPeriodAsync(int periodId, CancellationToken ct)
        {
            await PutAsync($"api/activityperiods/{periodId}/close", ct);
        }

        // ==================== WORKER POR HOSTNAME ====================

        /// <summary>
        /// GET /api/workers/by-hostname/{hostname} — busca el id del worker que
        /// corresponde a este PC por su hostname. Lo llamamos UNA vez al arrancar
        /// el agente. Si no encuentra worker (404), devuelve null y el Worker.cs
        /// aborta el arranque.
        /// </summary>
        public async Task<int?> GetWorkerIdByHostnameAsync(string hostname, CancellationToken ct)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/api/workers/by-hostname/{hostname}", ct);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Worker no encontrado en BBDD para hostname {Hostname}", hostname);
                    return null;
                }
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error al buscar worker por hostname: {Code}", response.StatusCode);
                    return null;
                }
                string json = await response.Content.ReadAsStringAsync(ct);
                JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);
                return element.GetProperty("id").GetInt32();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar worker por hostname");
                return null;
            }
        }

        // ==================== APLICACIONES ====================

        /// <summary>
        /// Devuelve el id de la Application asociada al process_name dado.
        /// Si ya existe en el catálogo (GET /api/applications), reutiliza su id;
        /// si no, la crea (POST /api/applications) y devuelve el nuevo id.
        ///
        /// El display_name se deriva del process_name (primera letra en mayúscula)
        /// para que el dashboard muestre "Chrome" en vez del título completo de la
        /// ventana, que puede contener rutas o nombres de archivo.
        ///
        /// Esto evita duplicados cuando varios workers usan la misma app
        /// (todos comparten el mismo registro en la tabla applications).
        /// </summary>
        public async Task<int?> GetOrCreateApplicationAsync(string processName, CancellationToken ct)
        {
            // 1. Pedir el catálogo y buscar si ya está
            HttpResponseMessage getResponse = await _httpClient.GetAsync($"{_baseUrl}/api/applications", ct);
            if (getResponse.IsSuccessStatusCode)
            {
                string json = await getResponse.Content.ReadAsStringAsync(ct);
                List<JsonElement>? apps = JsonSerializer.Deserialize<List<JsonElement>>(json);
                JsonElement? existing = apps?.FirstOrDefault(a =>
                    a.TryGetProperty("process_name", out JsonElement p) &&
                    p.GetString() == processName);

                if (existing.HasValue && existing.Value.ValueKind == JsonValueKind.Object)
                    return existing.Value.GetProperty("id").GetInt32();
            }

            // 2. Si no estaba, crearla.
            // display_name = process_name con la primera letra en mayúscula
            // (p.ej. "chrome" → "Chrome", "Code" → "Code").
            // NO usamos el título de la ventana porque cambia con cada archivo
            // abierto y puede contener rutas largas.
            string displayName = processName.Length > 0
                ? char.ToUpper(processName[0]) + processName[1..]
                : processName;

            object body = new { process_name = processName, display_name = displayName };
            JsonElement? response = await PostAsync("api/applications", body, ct);
            return response?.GetProperty("application").GetProperty("id").GetInt32();
        }

        // ==================== HELPERS PRIVADOS ====================
        // Estos métodos no se exponen fuera del servicio. Centralizan la
        // serialización JSON, la captura de excepciones y los logs para que
        // los métodos públicos de arriba queden cortos y legibles.

        private async Task<JsonElement?> PostAsync(string endpoint, object body, CancellationToken ct)
        {
            try
            {
                string json = JsonSerializer.Serialize(body);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync(ct);
                    return JsonSerializer.Deserialize<JsonElement>(responseJson);
                }

                _logger.LogWarning("API respondió con error {Code} en {Endpoint}", response.StatusCode, endpoint);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al llamar a {Endpoint}", endpoint);
                return null;
            }
        }

        private async Task PutAsync(string endpoint, CancellationToken ct)
        {
            try
            {
                HttpResponseMessage? response = await _httpClient.PutAsync($"{_baseUrl}/{endpoint}", null, ct);
                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("API respondió con error {Code} en {Endpoint}", response.StatusCode, endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al llamar a {Endpoint}", endpoint);
            }
        }
    }
}
