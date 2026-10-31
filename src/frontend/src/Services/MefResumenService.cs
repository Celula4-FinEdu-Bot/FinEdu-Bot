using Npgsql;
using src.Models;

namespace src.Services;

public class MefResumenService
{
    private readonly IConfiguration _configuracion;

    public MefResumenService(IConfiguration configuracion)
    {
        _configuracion = configuracion;
    }

    public List<ResumenMef> ObtenerRegistros()
    {
        var connectionString = _configuracion
            .GetConnectionString("DefaultConnection");



        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No se encontró la conexión a PostgreSQL."
            );
        }

        var registros = new List<ResumenMef>();

        using var conexion = new NpgsqlConnection(connectionString);

        conexion.Open();

        const string consulta = @"
            SELECT
                nivel_gobierno,
                sector,
                pliego,
                pliego_nombre,
                anio,
                pia,
                pim,
                devengado
            FROM public.mef_resumen_pliego_anual
            ORDER BY pliego_nombre, anio;
        ";

        using var comando = new NpgsqlCommand(consulta, conexion);
        using var lector = comando.ExecuteReader();

        while (lector.Read())
        {
            var registro = new ResumenMef
            {
                NivelGobierno = lector.GetString(0),
                Sector = lector.GetString(1),
                Pliego = lector.GetString(2),
                PliegoNombre = lector.GetString(3),

                Anio = lector.GetInt16(4),

                Pia = lector.GetDecimal(5),
                Pim = lector.GetDecimal(6),
                Devengado = lector.GetDecimal(7)
            };

            registros.Add(registro);
        }

        return registros;
    }
}
