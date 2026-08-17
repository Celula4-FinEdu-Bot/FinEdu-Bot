namespace FinEduBot.Frontend.Models;

public sealed class PresupuestoMensual
{
    public int Anio { get; set; }

    public int Mes { get; set; }

    public string NombreMes =>
        Mes switch
        {
            1 => "Enero",
            2 => "Febrero",
            3 => "Marzo",
            4 => "Abril",
            5 => "Mayo",
            6 => "Junio",
            7 => "Julio",
            8 => "Agosto",
            9 => "Septiembre",
            10 => "Octubre",
            11 => "Noviembre",
            12 => "Diciembre",
            _ => $"Mes {Mes}"
        };

    public decimal PIA { get; set; }

    public decimal PIM { get; set; }

    public decimal Ejecutado { get; set; }

    public decimal PorcentajeEjecucion =>
        PIM <= 0
            ? 0
            : Math.Round(Ejecutado / PIM * 100, 2);
}