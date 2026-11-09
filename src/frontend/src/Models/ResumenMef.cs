namespace src.Models;
public class ResumenMef
{
    public string NivelGobierno { get; set; } = "";
    public string Sector { get; set; } = "";
    public string Pliego { get; set; } = "";
    public string PliegoNombre { get; set; } = "";

    public int Anio { get; set; }

    public decimal Pia { get; set; }
    public decimal Pim { get; set; }
    public decimal Devengado { get; set; }

    public decimal? PorcentajeEjecucion =>
        Pim > 0
            ? Math.Round(Devengado / Pim * 100, 2)
            : null;
}
