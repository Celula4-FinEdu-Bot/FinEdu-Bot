namespace src.Models;

public class EvolucionPresupuesto
{
    public int Anio { get; set; }

    public string? Entidad { get; set; }

    public decimal PresupuestoInicial { get; set; }

    public decimal PresupuestoModificado { get; set; }

    public decimal MontoEjecutado { get; set; }

    public decimal PorcentajeEjecucion { get; set; }

    public string AnioDisplay =>
        Anio.ToString();

    public string PresupuestoInicialDisplay =>
        PresupuestoInicial.ToString("N2");

    public string PresupuestoModificadoDisplay =>
        PresupuestoModificado.ToString("N2");

    public string MontoEjecutadoDisplay =>
        MontoEjecutado.ToString("N2");

    public string PorcentajeEjecucionDisplay =>
        $"{PorcentajeEjecucion:N2}%";
}