namespace src.Models;

public class Proyecto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Presupuesto { get; set; }

    public decimal Ejecutado { get; set; }
}