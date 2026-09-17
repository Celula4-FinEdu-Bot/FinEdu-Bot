using System.Text.Json;

namespace src.Models;

public sealed class MefRawResponse
{
    public MefRawResult? Result { get; set; }
}

public sealed class MefRawResult
{
    public int Total { get; set; }

    public List<Dictionary<string, JsonElement>> Records { get; set; } = [];
}