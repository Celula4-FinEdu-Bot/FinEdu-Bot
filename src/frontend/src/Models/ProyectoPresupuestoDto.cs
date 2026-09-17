namespace src.Models;

public sealed class ProyectoPresupuestoDto
{
    public string Categoria { get; set; } = string.Empty;

    public string Proyecto { get; set; } = string.Empty;

    public decimal Presupuesto { get; set; }

    public decimal Ejecutado { get; set; }

    public decimal PorcentajeEjecucion =>
        Presupuesto <= 0
            ? 0
            : Math.Round(
                Ejecutado / Presupuesto * 100,
                2);
}