using System.Text.Json;

namespace FinEduBot.Frontend.Models;

public sealed class MefRawResponse
{
    public MefRawResult? Result { get; set; }
}

public sealed class MefRawResult
{
    public int Total { get; set; }

    public List<Dictionary<string, JsonElement>> Records { get; set; } = [];
}