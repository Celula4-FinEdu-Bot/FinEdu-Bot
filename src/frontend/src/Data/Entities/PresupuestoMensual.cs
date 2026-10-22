namespace src.Data.Entities;

public sealed class PresupuestoMensual
{
    public int Id { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public string NombreMes { get; set; } = string.Empty;

    public decimal Presupuesto { get; set; }

    public decimal Ejecutado { get; set; }
}