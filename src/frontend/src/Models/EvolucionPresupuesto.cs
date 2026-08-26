namespace src.Models;

public class EvolucionPresupuesto
{
    public int Anio { get; set; }

    public string? Entidad { get; set; }

    public decimal PresupuestoInicial { get; set; }

    public decimal PresupuestoModificado { get; set; }

    public decimal MontoEjecutado { get; set; }

    public decimal PorcentajeEjecucion { get; set; }

    // ============================================================
    // COMPATIBILIDAD CON NlqService / MefService
    // ============================================================

    public decimal Pia
    {
        get => PresupuestoInicial;
        set => PresupuestoInicial = value;
    }

    public decimal Pim
    {
        get => PresupuestoModificado;
        set => PresupuestoModificado = value;
    }

    public decimal Devengado
    {
        get => MontoEjecutado;
        set => MontoEjecutado = value;
    }

    // ============================================================
    // DISPLAY
    // ============================================================

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