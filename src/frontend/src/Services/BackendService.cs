using System.Net.Http.Json;
using System.Text.Json;
using src.Models;

namespace src.Services;

public sealed class BackendService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public BackendService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<NlqResponse> ConsultarAsync(
        string chatInput,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Backend:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new NlqResponse
            {
                Success = false,
                Intent = "ErrorConfiguracion",
                Message = "No está configurada la URL del backend."
            };
        }

        var url =
            $"{baseUrl.TrimEnd('/')}/api/chat";

        var request = new
        {
            chatInput,
            sessionId
        };

        using var response =
            await _httpClient.PostAsJsonAsync(
                url,
                request,
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new NlqResponse
            {
                Success = false,
                Intent = "ErrorBackend",
                Message =
                    $"El backend respondió con HTTP {(int)response.StatusCode}: {body}"
            };
        }

        var result =
            JsonSerializer.Deserialize<NlqResponse>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return result
            ?? new NlqResponse
            {
                Success = false,
                Intent = "RespuestaInvalida",
                Message =
                    "El backend devolvió una respuesta vacía o inválida."
            };
    }
}