namespace src.Models;

public class Presupuesto
{
    public int Id { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal PresupuestoInicial { get; set; }

    public decimal PresupuestoModificado { get; set; }

    public decimal Ejecutado { get; set; }
}