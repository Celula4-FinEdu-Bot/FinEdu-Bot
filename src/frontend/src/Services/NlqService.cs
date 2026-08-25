using src.Models;

namespace src.Services;

public sealed class NlqService
{
    private readonly MefService _mefService;

    public NlqService(MefService mefService)
    {
        _mefService = mefService;
    }

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

        // ========================================================
        // EVOLUCIÓN PRESUPUESTARIA
        // ========================================================

        if (ContieneAlguno(
                texto,
                "evolucion",
                "presupuesto por ano",
                "presupuesto anual",
                "ejecucion por ano",
                "evolucion del presupuesto",
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
                "presupuesto 2026"))
        {
            var entidad =
                ExtraerEntidad(pregunta);

            Console.WriteLine("======================================");
            Console.WriteLine("NLQ - EVOLUCIÓN");
            Console.WriteLine($"Pregunta: {pregunta}");
            Console.WriteLine($"Filtro entidad: {entidad}");
            Console.WriteLine("======================================");

            var datos =
                await _mefService.ObtenerEvolucionAsync(
                    entidad,
                    cancellationToken);

            // ====================================================
            // SI HAY DATOS
            // ====================================================

            if (datos.Count > 0)
            {
                var datosValidos =
                    datos
                        .Where(x =>
                            x.Pia != 0 ||
                            x.Pim != 0 ||
                            x.Devengado != 0)
                        .ToList();

                if (datosValidos.Count > 0)
                {
                    var primerAnio =
                        datosValidos.Min(x => x.Anio);

                    var ultimoAnio =
                        datosValidos.Max(x => x.Anio);

                    return new NlqResponse
                    {
                        Success = true,

                        Intent =
                            "EvolucionPresupuesto",

                        Message =
                            $"Se encontraron datos presupuestarios para el período {primerAnio}-{ultimoAnio}.",

                        Evolucion =
                            datosValidos
                                .Select(x =>
                                {
                                    var porcentaje =
                                        x.Pim > 0
                                            ? (x.Devengado / x.Pim) * 100
                                            : 0;

                                    return new EvolucionPresupuesto
                                    {
                                        Anio = x.Anio,

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
                                .ToList()
                    };
                }
            }

            // ====================================================
            // SIN DATOS
            // ====================================================

            return new NlqResponse
            {
                Success = false,

                Intent =
                    "EvolucionPresupuesto",

                Message =
                    "No se encontraron datos en los datasets del MEF para la consulta realizada.",

                Evolucion =
                    []
            };
        }

        // ========================================================
        // PROYECTOS
        // ========================================================

        if (ContieneAlguno(
                texto,
                "proyectos",
                "proyecto",
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
                "contrataciones",
                "contratos",
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
                "Prueba con: \"¿Cuál fue la evolución del presupuesto entre 2022 y 2026?\""
        };
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
        return texto
            .Trim()
            .ToLowerInvariant()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u");
    }

    // ============================================================
    // EXTRAER ENTIDAD
    // ============================================================

    private static string ExtraerEntidad(
        string pregunta)
    {
        var texto =
            pregunta.Trim();

        string[] palabrasAEliminar =
        [
            "evolución",
            "evolucion",
            "presupuesto",
            "gasto",
            "gastos",
            "ejecución",
            "ejecucion",
            "presupuestaria",
            "presupuestario",

            "entre",
            "del",
            "de",
            "la",
            "el",
            "los",
            "las",

            "2017",
            "2018",
            "2019",
            "2020",
            "2021",
            "2022",
            "2023",
            "2024",
            "2025",
            "2026",

            "2017-2021",
            "2022-2026",

            "año",
            "anos",
            "años"
        ];

        foreach (var palabra in palabrasAEliminar)
        {
            texto =
                texto.Replace(
                    palabra,
                    "",
                    StringComparison.OrdinalIgnoreCase);
        }

        // Limpieza adicional
        texto =
            texto
                .Replace("  ", " ")
                .Replace(" - ", " ")
                .Trim();

        return texto;
    }
}