using System.Text.Json;
using System.Text.Json.Serialization;

namespace src.Models;

public sealed class OeceNlqResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    // Flexible because different OECE query types return different columns.
    [JsonPropertyName("records")]
    public List<JsonElement> Records { get; set; } = [];

    [JsonPropertyName("fallbackUsed")]
    public bool FallbackUsed { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}