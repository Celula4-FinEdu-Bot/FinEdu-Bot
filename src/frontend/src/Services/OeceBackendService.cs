using System.Net.Http.Json;
using System.Text.Json;
using src.Models;

namespace src.Services;

public sealed class OeceBackendService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OeceBackendService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<OeceNlqResponse> ConsultarAsync(
        string chatInput,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chatInput))
        {
            return Failure("Escribe una pregunta sobre contrataciones OECE.", "consulta_vacia");
        }

        var baseUrl = _configuration["Backend:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Failure("No está configurada la URL del backend Node.js.", "backend_not_configured");
        }

        var url = $"{baseUrl.TrimEnd('/')}/api/oece/query";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                url,
                new
                {
                    chatInput = chatInput.Trim(),
                    sessionId = string.IsNullOrWhiteSpace(sessionId)
                        ? Guid.NewGuid().ToString("N")
                        : sessionId
                },
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            OeceNlqResponse? result = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    result = JsonSerializer.Deserialize<OeceNlqResponse>(body, JsonOptions);
                }
                catch (JsonException)
                {
                    result = null;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                result ??= Failure(
                    $"El backend OECE respondió con HTTP {(int)response.StatusCode}.",
                    "backend_error");

                result.Success = false;
                result.Message ??= $"No se pudo completar la consulta OECE (HTTP {(int)response.StatusCode}).";
                result.FallbackUsed = true;
                return result;
            }

            return result ?? Failure(
                "El backend devolvió una respuesta vacía o un JSON no válido.",
                "respuesta_invalida");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(
                "La consulta OECE excedió el tiempo de espera. Inténtalo nuevamente.",
                "oece_timeout");
        }
        catch (HttpRequestException)
        {
            return Failure(
                "No se pudo conectar con el backend OECE. Verifica su URL y disponibilidad.",
                "backend_no_disponible");
        }
    }

    private static OeceNlqResponse Failure(string message, string intent) => new()
    {
        Success = false,
        Intent = intent,
        Message = message,
        Source = "OECE",
        Total = 0,
        Records = [],
        FallbackUsed = true
    };
}
