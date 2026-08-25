using System.Text;
using src.Models;

namespace src.Services;

public sealed class NlqService
{
    private readonly MefService _mefService;

    public NlqService(MefService mefService)
    {
        _mefService = mefService;
    }

    // ============================================================
    // PROCESAR CONSULTA
    // ============================================================

    public async Task<NlqResponse> ProcesarAsync(
        string pregunta,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new NlqResponse
            {
                Success = false,
                Intent = "NoReconocido",
                Message = "Escribe una consulta."
            };
        }

        var texto =
            NormalizarTexto(pregunta);

        Console.WriteLine("======================================");
        Console.WriteLine("NLQ - CONSULTA");
        Console.WriteLine($"Pregunta: {pregunta}");
        Console.WriteLine($"Normalizada: {texto}");
        Console.WriteLine("======================================");

        // ========================================================
        // EVOLUCIÓN PRESUPUESTARIA
        // ========================================================

        if (EsConsultaEvolucion(texto))
        {
            var entidad =
                ExtraerEntidad(pregunta);

            Console.WriteLine("======================================");
            Console.WriteLine("NLQ - EVOLUCIÓN");
            Console.WriteLine($"Pregunta: {pregunta}");
            Console.WriteLine(
                $"Entidad detectada: '{entidad}'");
            Console.WriteLine("======================================");

            var datos =
                await _mefService.ObtenerEvolucionAsync(
                    entidad,
                    cancellationToken);

            var datosValidos =
                datos
                    .Where(x =>
                        x.Pia != 0 ||
                        x.Pim != 0 ||
                        x.Devengado != 0)
                    .OrderBy(x => x.Anio)
                    .ToList();

            if (datosValidos.Count > 0)
            {
                var evolucion =
                    datosValidos
                        .Select(x =>
                        {
                            decimal porcentaje = 0;

                            if (x.Pim > 0)
                            {
                                porcentaje =
                                    (x.Devengado / x.Pim) *
                                    100;
                            }

                            return new EvolucionPresupuesto
                            {
                                Anio = x.Anio,

                                Entidad =
                                    string.IsNullOrWhiteSpace(entidad)
                                        ? null
                                        : entidad,

                                PresupuestoInicial =
                                    x.Pia,

                                PresupuestoModificado =
                                    x.Pim,

                                MontoEjecutado =
                                    x.Devengado,

                                PorcentajeEjecucion =
                                    porcentaje
                            };
                        })
                        .ToList();

                var primerAnio =
                    evolucion.Min(x => x.Anio);

                var ultimoAnio =
                    evolucion.Max(x => x.Anio);

                var descripcionEntidad =
                    string.IsNullOrWhiteSpace(entidad)
                        ? "del presupuesto público"
                        : $"de {entidad}";

                return new NlqResponse
                {
                    Success = true,

                    Intent =
                        "EvolucionPresupuesto",

                    Message =
                        $"Se encontraron datos {descripcionEntidad} " +
                        $"para el período {primerAnio}-{ultimoAnio}.",

                    Evolucion =
                        evolucion
                };
            }

            return new NlqResponse
            {
                Success = false,

                Intent =
                    "EvolucionPresupuesto",

                Message =
                    string.IsNullOrWhiteSpace(entidad)
                        ? "El MEF no devolvió datos presupuestarios para la consulta realizada."
                        : $"No se encontraron datos presupuestarios para '{entidad}'.",

                Evolucion = []
            };
        }

        // ========================================================
        // PROYECTOS
        // ========================================================

        if (ContieneAlguno(
                texto,
                "proyecto",
                "proyectos",
                "categorias",
                "mayor presupuesto",
                "mayor ejecucion"))
        {
            return new NlqResponse
            {
                Success = true,

                Intent =
                    "Proyectos",

                Message =
                    "La consulta corresponde al análisis de proyectos y categorías presupuestarias."
            };
        }

        // ========================================================
        // CONTRATACIONES
        // ========================================================

        if (ContieneAlguno(
                texto,
                "contratacion",
                "contrataciones",
                "contrato",
                "contratos",
                "licitacion",
                "licitaciones",
                "oece"))
        {
            return new NlqResponse
            {
                Success = true,

                Intent =
                    "Contrataciones",

                Message =
                    "Esta consulta debe ser atendida por el microfrontend OECE."
            };
        }

        // ========================================================
        // NO RECONOCIDO
        // ========================================================

        return new NlqResponse
        {
            Success = false,

            Intent =
                "NoReconocido",

            Message =
                "No pude identificar la consulta. " +
                "Prueba con una consulta sobre presupuesto, " +
                "evolución, proyectos o contrataciones."
        };
    }

    // ============================================================
    // DETECTAR EVOLUCIÓN
    // ============================================================

    private static bool EsConsultaEvolucion(
        string texto)
    {
        return ContieneAlguno(
            texto,

            "evolucion",

            "evolucion del presupuesto",

            "presupuesto por ano",

            "presupuesto anual",

            "ejecucion por ano",

            "presupuesto entre",

            "presupuesto 2017",

            "presupuesto 2018",

            "presupuesto 2019",

            "presupuesto 2020",

            "presupuesto 2021",

            "presupuesto 2022",

            "presupuesto 2023",

            "presupuesto 2024",

            "presupuesto 2025",

            "presupuesto 2026"
        );
    }

    // ============================================================
    // DETECTAR PALABRAS
    // ============================================================

    private static bool ContieneAlguno(
        string texto,
        params string[] valores)
    {
        return valores.Any(
            valor =>
                texto.Contains(
                    valor,
                    StringComparison.OrdinalIgnoreCase));
    }

    // ============================================================
    // NORMALIZAR TEXTO
    // ============================================================

    private static string NormalizarTexto(
        string texto)
    {
        var normalizado =
            texto
                .Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD);

        var resultado =
            new StringBuilder();

        foreach (var caracter
                 in normalizado)
        {
            var categoria =
                System.Globalization.CharUnicodeInfo
                    .GetUnicodeCategory(caracter);

            if (categoria !=
                System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(caracter);
            }
        }

        return resultado
            .ToString()
            .Normalize(
                NormalizationForm.FormC);
    }

    // ============================================================
    // EXTRAER ENTIDAD
    // ============================================================

    
private static string ExtraerEntidad(
    string pregunta)
{
    var texto =
        pregunta.Trim();

    // ------------------------------------------------------------
    // Primero quitamos frases completas de la consulta.
    // No eliminamos palabras como "de", porque pueden formar
    // parte del nombre real de una entidad.
    // ------------------------------------------------------------

    string[] frasesAEliminar =
    [
        "¿cuál fue la evolución del presupuesto",
        "¿cual fue la evolucion del presupuesto",

        "cuál fue la evolución del presupuesto",
        "cual fue la evolucion del presupuesto",

        "evolución del presupuesto",
        "evolucion del presupuesto",

        "evolución de presupuesto",
        "evolucion de presupuesto",

        "evolución presupuestaria",
        "evolucion presupuestaria",

        "presupuesto entre",
        "presupuesto del",
        "presupuesto de",

        "ejecución entre",
        "ejecucion entre",

        "ejecución del",
        "ejecucion del",

        "ejecución de",
        "ejecucion de",

        "presupuesto",
        "presupuestario",
        "presupuestaria",

        "gastos",
        "gasto"
    ];

    foreach (var frase in frasesAEliminar)
    {
        texto =
            texto.Replace(
                frase,
                "",
                StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------
    // Eliminamos los años.
    // ------------------------------------------------------------

    for (int anio = 2017; anio <= 2026; anio++)
    {
        texto =
            texto.Replace(
                anio.ToString(),
                "",
                StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------
    // Eliminamos palabras de estructura temporal.
    //
    // NO eliminamos "de", "del", "la", etc.
    // ------------------------------------------------------------

    string[] palabrasTemporales =
    [
        "entre",
        "hasta",
        "desde",
        "año",
        "ano",
        "años",
        "anos"
    ];

    foreach (var palabra in palabrasTemporales)
    {
        texto =
            texto.Replace(
                palabra,
                "",
                StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------
    // Limpieza final
    // ------------------------------------------------------------

    texto =
        texto
            .Replace("¿", "")
            .Replace("?", "")
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace(":", " ")
            .Replace("-", " ")
            .Replace("  ", " ")
            .Trim();

    return texto;
}


}