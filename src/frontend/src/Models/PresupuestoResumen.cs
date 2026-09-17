namespace src.Models;

public class PresupuestoResumen
{
    public int Anio { get; set; }
    public string? Mes { get; set; }
    public decimal PIA { get; set; }
    public decimal PIM { get; set; }
    public decimal Ejecutado { get; set; }
    public decimal PorcentajeEjecucion { get; set; }
}