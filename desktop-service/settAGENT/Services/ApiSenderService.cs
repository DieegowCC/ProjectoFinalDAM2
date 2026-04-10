using System.Text;
using System.Text.Json;
using settAGENT.Models;

namespace settAGENT.Services
{
    public class ApiSenderService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiSenderService> _logger;
        private const string Endpoint = "http://IP_DEL_UBUNTU:5000/api/snapshots";

        public ApiSenderService(ILogger<ApiSenderService> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task SendAsync(ActivitySnapshot snapshot, CancellationToken ct)
        {
            try
            {
                string json = JsonSerializer.Serialize(snapshot);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(Endpoint, content, ct);

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Snapshot enviado correctamente.");
                else
                    _logger.LogWarning("La API respondió con error: {Code}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el snapshot.");
            }
        }
    }
}