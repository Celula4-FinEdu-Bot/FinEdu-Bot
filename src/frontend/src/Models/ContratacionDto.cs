namespace src.Models;

public sealed class ContratacionDto
{
    public string Ocid { get; set; } = string.Empty;

    public string Entidad { get; set; } = string.Empty;

    public string Empresa { get; set; } = string.Empty;

    public string Objeto { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }
}