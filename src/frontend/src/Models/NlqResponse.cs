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

    // Para la nueva funcionalidad
    [JsonPropertyName("evolucion")]
    public List<EvolucionPresupuesto>? Evolucion { get; set; }

    // Para compatibilidad con el componente existente
    [JsonPropertyName("presupuestos")]
    public List<PresupuestoResumen>? Presupuestos { get; set; }

    [JsonPropertyName("proyectos")]
    public List<Proyecto>? Proyectos { get; set; }

    [JsonPropertyName("contrataciones")]
    public List<Contratacion>? Contrataciones { get; set; }
}