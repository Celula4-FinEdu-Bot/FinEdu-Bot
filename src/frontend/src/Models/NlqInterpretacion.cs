using System.Text.Json.Serialization;

namespace src.Models;

public sealed class NlqInterpretacion
{
    [JsonPropertyName("intent")]
    public string Intent { get; set; } = "no_reconocido";

    [JsonPropertyName("entidad")]
    public string? Entidad { get; set; }

    [JsonPropertyName("nivel_gobierno")]
    public string? NivelGobierno { get; set; }

    [JsonPropertyName("departamento")]
    public string? Departamento { get; set; }

    [JsonPropertyName("provincia")]
    public string? Provincia { get; set; }

    [JsonPropertyName("distrito")]
    public string? Distrito { get; set; }

    [JsonPropertyName("anio_inicio")]
    public int? AnioInicio { get; set; }

    [JsonPropertyName("anio_fin")]
    public int? AnioFin { get; set; }

    [JsonPropertyName("metricas")]
    public List<string> Metricas { get; set; } = [];
}