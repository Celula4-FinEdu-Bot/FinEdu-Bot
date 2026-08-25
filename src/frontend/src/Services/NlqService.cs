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

    // ============================================================
    // NORMALIZAMOS PARA PODER DETECTAR LA ESTRUCTURA
    // ============================================================

    var normalizado =
        NormalizarTexto(texto);

    // ============================================================
    // CASOS CONOCIDOS
    //
    // Es preferible devolver el nombre completo de la entidad.
    // MefService posteriormente lo convierte a PLIEGO.
    // ============================================================

    string[] entidades =
    [
        "ministerio de defensa",
        "ministerio de salud",
        "ministerio de educacion",
        "ministerio de economia y finanzas",
        "ministerio del interior",
        "ministerio de transportes y comunicaciones",
        "ministerio de desarrollo e inclusion social"
    ];

    foreach (var entidad in entidades)
    {
        if (
            normalizado.Contains(
                entidad,
                StringComparison.OrdinalIgnoreCase))
        {
            return entidad;
        }
    }

    // ============================================================
    // SI NO ENCONTRAMOS UNA ENTIDAD CONOCIDA,
    // usamos la extracción anterior como fallback.
    // ============================================================

    string[] frasesAEliminar =
    [
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

    foreach (var frase
             in frasesAEliminar)
    {
        texto =
            texto.Replace(
                frase,
                "",
                StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // QUITAMOS AÑOS
    // ============================================================

    for (
        int anio = 2017;
        anio <= 2026;
        anio++)
    {
        texto =
            texto.Replace(
                anio.ToString(),
                "",
                StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // PALABRAS TEMPORALES
    // ============================================================

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

    foreach (var palabra
             in palabrasTemporales)
    {
        texto =
            texto.Replace(
                palabra,
                "",
                StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // LIMPIEZA
    // ============================================================

    texto =
        texto
            .Replace("¿", "")
            .Replace("?", "")
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace(":", " ")
            .Replace("-", " ");

    while (
        texto.Contains(
            "  ",
            StringComparison.Ordinal))
    {
        texto =
            texto.Replace(
                "  ",
                " ",
                StringComparison.Ordinal);
    }

    return texto.Trim();
}

}