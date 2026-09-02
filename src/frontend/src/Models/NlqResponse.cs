using System.Text.Json.Serialization;

namespace src.Models;

public class NlqResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("evolucion")]
    public List<EvolucionPresupuesto> Evolucion { get; set; } = [];

    [JsonPropertyName("presupuestos")]
    public List<PresupuestoResumen> Presupuestos { get; set; } = [];

    [JsonPropertyName("proyectos")]
    public List<Proyecto> Proyectos { get; set; } = [];

    [JsonPropertyName("contrataciones")]
    public List<Contratacion> Contrataciones { get; set; } = [];

    // ============================================================
    // PAGINACIÓN
    // ============================================================

    public int TotalRegistros { get; set; }

    public int PaginaActual { get; set; } = 1;

    public int TamanioPagina { get; set; } = 20;

    public int TotalPaginas { get; set; } = 1;
}