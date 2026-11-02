using System.Globalization;

namespace src.Models;

public sealed class QuePasoHoyIndicador
{
    private static readonly CultureInfo CulturaPeru =
        CultureInfo.GetCultureInfo("es-PE");

    public DateOnly Fecha { get; init; }

    public string Indicador { get; init; } = string.Empty;

    public string Titulo { get; init; } = string.Empty;

    public decimal? Valor { get; init; }

    public string? Detalle { get; init; }

    public bool Destacado { get; init; }

    public string RangoAnios { get; init; } = string.Empty;

    public DateTimeOffset ActualizadoEn { get; init; }

    // Convención del flujo de n8n: los indicadores "_pct" son porcentajes; el resto, soles.
    public bool EsPorcentaje =>
        Indicador.EndsWith("_pct", StringComparison.OrdinalIgnoreCase);

    // =========================================================
    // PROPIEDADES DISPLAY
    // =========================================================

    public string ValorDisplay
    {
        get
        {
            if (Valor is null)
                return "—";

            return EsPorcentaje
                ? $"{Valor.Value.ToString("N1", CulturaPeru)} %"
                : $"S/ {Valor.Value.ToString("N2", CulturaPeru)}";
        }
    }
}

public sealed class QuePasoHoyResumen
{
    // Perú no tiene horario de verano: UTC-5 todo el año.
    private static readonly TimeSpan HoraPeru = TimeSpan.FromHours(-5);

    public DateOnly? Fecha { get; init; }

    public string RangoAnios { get; init; } = "2017-2021";

    public QuePasoHoyIndicador? Destacado { get; init; }

    public IReadOnlyList<QuePasoHoyIndicador> Otros { get; init; } = [];

    public DateTimeOffset? ActualizadoEn { get; init; }

    public bool TieneDatos => Destacado is not null || Otros.Count > 0;

    public string ActualizadoEnDisplay =>
        ActualizadoEn is null
            ? string.Empty
            : ActualizadoEn.Value
                .ToOffset(HoraPeru)
                .ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-PE"));

    public static QuePasoHoyResumen Vacio { get; } = new();
}
