using System.Text.Json;
using System.Text.Json.Serialization;

namespace src.Models;

public sealed class MefDataResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("result")]
    public MefResult Result { get; set; } = new();
}

public sealed class MefResult
{
    [JsonPropertyName("include_total")]
    public JsonElement IncludeTotal { get; set; }

    [JsonPropertyName("limit")]
    public JsonElement Limit { get; set; }

    [JsonPropertyName("records")]
    public List<Dictionary<string, JsonElement>> Records { get; set; } = [];

    [JsonPropertyName("total")]
    public JsonElement Total { get; set; }
}