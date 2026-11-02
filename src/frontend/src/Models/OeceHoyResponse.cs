using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace src.Models;
public sealed class OeceHoyResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = "";
    [JsonPropertyName("fecha")] public string Fecha { get; set; } = "";
    [JsonPropertyName("modo")] public string Modo { get; set; } = "historico";
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("limitado")] public bool Limitado { get; set; }
    [JsonPropertyName("records")] public List<OeceHoyEvento> Records { get; set; } = [];
}

public sealed class OeceHoyEvento
{
    [JsonPropertyName("fecha")] public string Fecha { get; set; } = "";
    [JsonPropertyName("anio")] public int Anio { get; set; }
    [JsonPropertyName("tipo")] public string Tipo { get; set; } = "";
    [JsonPropertyName("entidad")] public string Entidad { get; set; } = "";
    [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = "";
    [JsonPropertyName("monto")] public decimal? Monto { get; set; }
    [JsonPropertyName("referencia")] public string? Referencia { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("tabla")] public string Tabla { get; set; } = "";
}
