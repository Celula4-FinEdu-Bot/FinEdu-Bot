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

        var texto = NormalizarTexto(pregunta);

        Console.WriteLine();
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
            var entidad = ExtraerEntidad(pregunta);

            Console.WriteLine();
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

            if (datos.Count > 0)
            {
                var primerAnio =
                    datos.Min(x => x.Anio);

                var ultimoAnio =
                    datos.Max(x => x.Anio);

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
                        datos
                };
            }

            return new NlqResponse
            {
                Success = false,

                Intent =
                    "EvolucionPresupuesto",

                Message =
                    string.IsNullOrWhiteSpace(entidad)
                        ? "El MEF no devolvió datos presupuestarios para el período consultado."
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

            "evolucion de presupuesto",

            "evolucion presupuestaria",

            "presupuesto por ano",

            "presupuesto anual",

            "ejecucion por ano",

            "ejecucion anual",

            "presupuesto entre",

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

        foreach (var caracter in normalizado)
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
        var textoOriginal =
            pregunta.Trim();

        var normalizado =
            NormalizarTexto(textoOriginal);

        // ========================================================
        // CASOS CONOCIDOS
        // ========================================================

        var entidadesConocidas =
            new[]
            {
                "ministerio de defensa",
                "ministerio de salud",
                "ministerio de educacion",
                "ministerio de economia y finanzas",
                "ministerio del interior",
                "ministerio de interior",
                "ministerio de transportes y comunicaciones",
                "ministerio de desarrollo e inclusion social",

                "municipalidad de lima",
                "municipalidad metropolitana de lima",

                "gobierno regional"
            };

        foreach (var entidad in entidadesConocidas)
        {
            if (normalizado.Contains(
                    entidad,
                    StringComparison.OrdinalIgnoreCase))
            {
                return entidad;
            }
        }

        // ========================================================
        // ELIMINAR ESTRUCTURA DE LA PREGUNTA
        // ========================================================

        var texto =
            normalizado;

        var frasesAEliminar =
            new[]
            {
                "cual fue la evolucion del presupuesto",
                "cual fue la evolucion de presupuesto",

                "evolucion del presupuesto",
                "evolucion de presupuesto",

                "evolucion presupuestaria",

                "muestrame la evolucion del presupuesto",
                "muestrame el presupuesto",

                "dame la evolucion del presupuesto",
                "dame el presupuesto",

                "presupuesto entre",
                "presupuesto del",
                "presupuesto de",

                "ejecucion entre",
                "ejecucion del",
                "ejecucion de",

                "ejecucion presupuestaria",

                "presupuestario",
                "presupuestaria",
                "presupuesto",

                "gasto",
                "gastos"
            };

        foreach (var frase in frasesAEliminar)
        {
            texto =
                texto.Replace(
                    frase,
                    " ",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ========================================================
        // QUITAR AÑOS
        // ========================================================

        for (var anio = 2012; anio <= 2030; anio++)
        {
            texto =
                texto.Replace(
                    anio.ToString(),
                    " ",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ========================================================
        // QUITAR PALABRAS TEMPORALES
        // ========================================================

        var palabrasTemporales =
            new[]
            {
                "entre",
                "hasta",
                "desde",
                "durante",
                "periodo",
                "period",
                "ano",
                "anos",
                "año",
                "años"
            };

        foreach (var palabra in palabrasTemporales)
        {
            texto =
                texto.Replace(
                    palabra,
                    " ",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ========================================================
        // QUITAR CONECTORES
        // ========================================================

        var conectores =
            new[]
            {
                "y",
                "el",
                "la",
                "los",
                "las",
                "de",
                "del",
                "al",
                "un",
                "una"
            };

        var palabras =
            texto
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(
                    palabra =>
                        !conectores.Contains(
                            palabra,
                            StringComparer.OrdinalIgnoreCase))
                .ToList();

        // ========================================================
        // LIMPIEZA
        // ========================================================

        texto =
            string.Join(
                " ",
                palabras);

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

        texto =
            texto.Trim();

        // ========================================================
        // EVITAR ENTIDADES FALSAS
        // ========================================================

        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        if (texto.Length <= 2)
        {
            return "";
        }

        return texto;
    }
}