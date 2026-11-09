using System.Net.Http.Json;
using System.Text.Json;
using src.Models;
namespace src.Services;
public sealed class OeceHoyService(HttpClient http, IConfiguration config)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    public static DateOnly HoyPeru() => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/Lima")));
    public async Task<OeceHoyResponse> ConsultarAsync(DateOnly fecha, string modo, string tipo, string entidad, CancellationToken ct = default)
    {
        if (fecha.Year < 2017 || fecha.Year > 2026) throw new ArgumentOutOfRangeException(nameof(fecha));
        if (modo is not ("historico" or "exacto")) throw new ArgumentException("Modo no válido");
        if (tipo is not ("TODOS" or "PENALIDAD" or "SANCION" or "INHABILITACION" or "PRONUNCIAMIENTO" or "INSCRIPCION")) throw new ArgumentException("Tipo no válido");
        var webhook = config["OeceHoy:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhook) || !Uri.TryCreate(webhook, UriKind.Absolute, out var target) || target.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Configura OeceHoy__WebhookUrl con la URL HTTPS de n8n.");
        using var request = new HttpRequestMessage(HttpMethod.Post, target)
        {
            Content = JsonContent.Create(new { fecha = fecha.ToString("yyyy-MM-dd"), modo, tipo, entidad = (entidad ?? "").Trim() })
        };
        var apiKey = config["OeceHoy:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("x-oece-key", apiKey);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"n8n respondió HTTP {(int)response.StatusCode}");
        var result = await response.Content.ReadFromJsonAsync<OeceHoyResponse>(JsonOpts, ct);
        return result ?? throw new InvalidOperationException("n8n devolvió una respuesta vacía");
    }
}
