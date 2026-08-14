namespace FinEduBot.Frontend.Models;

public sealed class PresupuestoMensualDto
{
    public int Anio { get; set; }

    public int Mes { get; set; }

    public string NombreMes { get; set; } = string.Empty;

    public decimal Presupuesto { get; set; }

    public decimal Ejecutado { get; set; }

    public decimal PorcentajeEjecucion =>
        Presupuesto <= 0
            ? 0
            : Math.Round(
                Ejecutado / Presupuesto * 100,
                2);
}