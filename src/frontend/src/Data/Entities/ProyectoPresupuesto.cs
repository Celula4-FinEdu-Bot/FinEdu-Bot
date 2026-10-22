namespace src.Data.Entities;

public sealed class ProyectoPresupuesto
{
    public int Id { get; set; }

    public string Categoria { get; set; } = string.Empty;

    public string Proyecto { get; set; } = string.Empty;

    public decimal Presupuesto { get; set; }

    public decimal Ejecutado { get; set; }
}