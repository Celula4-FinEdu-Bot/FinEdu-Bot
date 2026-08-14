using System.Text.RegularExpressions;
using FinEduBot.Frontend.Models;
using FinEduBot.Frontend.Services.Interfaces;

namespace FinEduBot.Frontend.Services;

public sealed class NlqService : INlqService
{
    private readonly IMeFService _mefService;

    public NlqService(IMeFService mefService)
    {
        _mefService = mefService;
    }

    public async Task<NlqResponse> ProcessAsync(
        NlqRequest request,
        CancellationToken cancellationToken = default)
    {
        var query =
            request.Query
                .Trim()
                .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(query))
        {
            return new NlqResponse
            {
                Query = request.Query,
                Intent = "unknown",
                Message = "Escribe una consulta."
            };
        }

        var year = ExtractYear(query);

        if (IsProjectQuery(query))
        {
            var projects =
                await _mefService
                    .ObtenerProyectosAsync(
                        year,
                        cancellationToken);

            return new NlqResponse
            {
                Query = request.Query,
                Intent = "proyectos",
                Message =
                    "Estas son las categorías y proyectos con mayor ejecución.",
                Proyectos = projects
            };
        }

        if (IsBudgetQuery(query))
        {
            var budget =
                await _mefService
                    .ObtenerEvolucionMensualAsync(
                        year,
                        cancellationToken);

            return new NlqResponse
            {
                Query = request.Query,
                Intent = "presupuesto",
                Message =
                    "Esta es la evolución mensual del presupuesto.",
                PresupuestoMensual = budget
            };
        }

        return new NlqResponse
        {
            Query = request.Query,
            Intent = "unknown",
            Message =
                "No pude identificar la consulta. " +
                "Puedes preguntar por presupuesto, " +
                "ejecución, categorías o proyectos."
        };
    }

    private static bool IsBudgetQuery(
        string query)
    {
        return ContainsAny(
            query,
            "presupuesto",
            "evolución",
            "evolucion",
            "mensual",
            "mes",
            "gasto",
            "ejecución mensual",
            "ejecucion mensual");
    }

    private static bool IsProjectQuery(
        string query)
    {
        return ContainsAny(
            query,
            "proyecto",
            "proyectos",
            "categoría",
            "categorias",
            "categorías",
            "mayor ejecución",
            "mayor ejecucion",
            "más ejecución",
            "mas ejecucion");
    }

    private static bool ContainsAny(
        string value,
        params string[] terms)
    {
        return terms.Any(
            value.Contains);
    }

    private static int? ExtractYear(
        string query)
    {
        var match =
            Regex.Match(
                query,
                @"\b(20\d{2})\b");

        if (!match.Success)
        {
            return null;
        }

        if (int.TryParse(
            match.Value,
            out var year))
        {
            return year;
        }

        return null;
    }
}