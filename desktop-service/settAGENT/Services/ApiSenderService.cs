using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using settAGENT.Models;

namespace settAGENT.Services
{
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

        // --- Sesión ---
        public async Task<int?> OpenSessionAsync(int workerId, CancellationToken ct)
        {
            var body = new { worker_id = workerId };
            var response = await PostAsync("api/worksessions", body, ct);
            return response?.GetProperty("session").GetProperty("id").GetInt32();
        }

        public async Task CloseSessionAsync(int sessionId, CancellationToken ct)
        {
            await PutAsync($"api/worksessions/{sessionId}/close", ct);
        }

        // --- Actividad de app ---
        public async Task<int?> OpenAppActivityAsync(int sessionId, int applicationId, CancellationToken ct)
        {
            var body = new { session_id = sessionId, applications_id = applicationId, is_foreground = true };
            var response = await PostAsync("api/appactivity", body, ct);
            return response?.GetProperty("activity").GetProperty("id").GetInt32();
        }

        public async Task CloseAppActivityAsync(int activityId, CancellationToken ct)
        {
            await PutAsync($"api/appactivity/{activityId}/close", ct);
        }

        // --- Periodos activo/inactivo ---
        public async Task<int?> OpenActivityPeriodAsync(int sessionId, string status, CancellationToken ct)
        {
            var body = new { session_id = sessionId, status };
            var response = await PostAsync("api/activityperiods", body, ct);
            return response?.GetProperty("period").GetProperty("id").GetInt32();
        }

        public async Task CloseActivityPeriodAsync(int periodId, CancellationToken ct)
        {
            await PutAsync($"api/activityperiods/{periodId}/close", ct);
        }

        // --- Aplicaciones ---
        public async Task<int?> GetOrCreateApplicationAsync(string processName, string windowTitle, CancellationToken ct)
        {
            // Primero busca si ya existe
            var getResponse = await _httpClient.GetAsync($"{_baseUrl}/api/applications", ct);
            if (getResponse.IsSuccessStatusCode)
            {
                var json = await getResponse.Content.ReadAsStringAsync(ct);
                var apps = JsonSerializer.Deserialize<List<JsonElement>>(json);
                var existing = apps?.FirstOrDefault(a =>
                    a.TryGetProperty("process_name", out var p) &&
                    p.GetString() == processName);

                if (existing.HasValue && existing.Value.ValueKind == JsonValueKind.Object)
                    return existing.Value.GetProperty("id").GetInt32();
            }

            // Si no existe, la crea
            var body = new { process_name = processName, display_name = windowTitle };
            var response = await PostAsync("api/applications", body, ct);
            return response?.GetProperty("application").GetProperty("id").GetInt32();
        }

        // --- Helpers privados ---
        private async Task<JsonElement?> PostAsync(string endpoint, object body, CancellationToken ct)
        {
            try
            {
                string json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(ct);
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
                var response = await _httpClient.PutAsync($"{_baseUrl}/{endpoint}", null, ct);
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