namespace FinEduBot.Frontend.Models;

public sealed class NlqResponse
{
    public string Query { get; set; } = string.Empty;

    public string Intent { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<PresupuestoMensualDto>
        PresupuestoMensual { get; set; } = [];

    public IReadOnlyList<ProyectoPresupuestoDto>
        Proyectos { get; set; } = [];
}