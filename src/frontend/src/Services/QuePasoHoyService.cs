using Npgsql;
using src.Models;

namespace src.Services;

public sealed class QuePasoHoyService
{
    private const string Sql = """
        SELECT fecha, indicador, titulo, valor, detalle,
               destacado, rango_anios, actualizado_en
        FROM que_paso_hoy
        WHERE fecha = (SELECT MAX(fecha) FROM que_paso_hoy)
        ORDER BY destacado DESC, indicador
        """;

    private readonly string? _connectionString;
    private readonly ILogger<QuePasoHoyService> _logger;

    public QuePasoHoyService(
        IConfiguration configuration,
        ILogger<QuePasoHoyService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection");

        _logger = logger;
    }

    // ============================================================
    // FILAS DE LA FECHA MÁS RECIENTE
    // ============================================================

    public async Task<QuePasoHoyResumen> ObtenerUltimoAsync(
        CancellationToken cancellationToken = default)
    {
        var filas = new List<QuePasoHoyIndicador>();

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(Sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                filas.Add(new QuePasoHoyIndicador
                {
                    Fecha = reader.GetFieldValue<DateOnly>(0),
                    Indicador = reader.GetString(1),
                    Titulo = reader.GetString(2),
                    Valor = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    Detalle = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Destacado = reader.GetBoolean(5),
                    RangoAnios = reader.GetString(6),
                    ActualizadoEn = reader.GetFieldValue<DateTimeOffset>(7)
                });
            }
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // La tabla la crea el flujo de n8n; si aún no corrió, no hay datos.
            _logger.LogWarning("La tabla que_paso_hoy todavía no existe");
            return QuePasoHoyResumen.Vacio;
        }

        if (filas.Count == 0)
            return QuePasoHoyResumen.Vacio;

        var destacado = filas.FirstOrDefault(f => f.Destacado);

        return new QuePasoHoyResumen
        {
            Fecha = filas[0].Fecha,
            RangoAnios = (destacado ?? filas[0]).RangoAnios,
            Destacado = destacado,
            Otros = filas.Where(f => !ReferenceEquals(f, destacado)).ToList(),
            ActualizadoEn = filas.Max(f => f.ActualizadoEn)
        };
    }
}
