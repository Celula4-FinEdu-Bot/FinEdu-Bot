namespace src.Models;

public class Contratacion
{
    public int Id { get; set; }

    public string Ocid { get; set; } = string.Empty;

    public string Entidad { get; set; } = string.Empty;

    public string Empresa { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public DateTime Fecha { get; set; }
}