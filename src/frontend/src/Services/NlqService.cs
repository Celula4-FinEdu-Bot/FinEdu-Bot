using System.Text;
using src.Models;

namespace src.Services;

public sealed class NlqService
{
    private readonly MefService _mefService;

    public NlqService(
        MefService mefService)
    {
        _mefService =
            mefService;
    }

    // ============================================================
    // PROCESAR CONSULTA
    // ============================================================

    public async Task<NlqResponse>
        ProcesarAsync(
            string pregunta,
            CancellationToken cancellationToken = default)
    {
        if (
            string.IsNullOrWhiteSpace(
                pregunta))
        {
            return new NlqResponse
            {
                Success = false,
                Intent = "ConsultaVacia",
                Message =
                    "Escribe una consulta."
            };
        }

        var texto =
            Normalizar(
                pregunta);

        Console.WriteLine();
        Console.WriteLine(
            "==========================================");

        Console.WriteLine(
            "NLQ - CONSULTA");

        Console.WriteLine(
            $"Pregunta: {pregunta}");

        Console.WriteLine(
            $"Normalizada: {texto}");

        Console.WriteLine(
            "==========================================");


        // ========================================================
        // EVOLUCIÓN / PRESUPUESTO
        // ========================================================

        if (
            EsConsultaPresupuesto(
                texto))
        {
            var entidad =
                ExtraerEntidad(
                    pregunta);

            Console.WriteLine(
                $"Entidad detectada: '{entidad}'");

            var datos =
                await _mefService
                    .ObtenerEvolucionAsync(
                        entidad,
                        cancellationToken);

            if (
                datos.Count == 0)
            {
                return new NlqResponse
                {
                    Success = false,

                    Intent =
                        "EvolucionPresupuesto",

                    Message =
                        string.IsNullOrWhiteSpace(
                            entidad)
                            ? "No se encontraron registros para la consulta en el período 2017-2021."
                            : $"No se encontraron registros para '{entidad}' en el período 2017-2021.",

                    Evolucion = []
                };
            }

            return new NlqResponse
            {
                Success = true,

                Intent =
                    "EvolucionPresupuesto",

                Message =
                    string.IsNullOrWhiteSpace(
                        entidad)
                        ? "Se encontraron registros presupuestarios del período 2017-2021."
                        : $"Se encontraron registros presupuestarios para '{entidad}' entre 2017 y 2021.",

                Evolucion =
                    datos
            };
        }


        // ========================================================
        // PROYECTOS
        // ========================================================

        if (
            ContieneAlguno(
                texto,

                "proyecto",
                "proyectos",

                "producto",

                "actividad",

                "obra",

                "meta"
            ))
        {
            return new NlqResponse
            {
                Success = true,

                Intent =
                    "Proyectos",

                Message =
                    "La consulta corresponde a información de proyectos, productos, actividades, obras o metas."
            };
        }


        // ========================================================
        // CLASIFICACIÓN DEL GASTO
        // ========================================================

        if (
            ContieneAlguno(
                texto,

                "generica",

                "subgenerica",

                "especifica",

                "categoria de gasto",

                "categoria del gasto"
            ))
        {
            return new NlqResponse
            {
                Success = true,

                Intent =
                    "ClasificacionGasto",

                Message =
                    "La consulta corresponde a la clasificación presupuestaria del gasto."
            };
        }


        // ========================================================
        // FINANCIAMIENTO
        // ========================================================

        if (
            ContieneAlguno(
                texto,

                "fuente de financiamiento",

                "fuente financiamiento",

                "rubro",

                "tipo de recurso"
            ))
        {
            return new NlqResponse
            {
                Success = true,

                Intent =
                    "Financiamiento",

                Message =
                    "La consulta corresponde a información de financiamiento presupuestario."
            };
        }


        // ========================================================
        // CONTRATACIONES
        // ========================================================

        if (
            ContieneAlguno(
                texto,

                "contratacion",

                "contrataciones",

                "contrato",

                "contratos",

                "licitacion",

                "licitaciones",

                "oece"
            ))
        {
            return new NlqResponse
            {
                Success = true,

                Intent =
                    "Contrataciones",

                Message =
                    "Esta consulta corresponde al módulo de contrataciones/OECE."
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
                "Prueba con presupuesto, PIA, PIM, " +
                "devengado, girado o ejecución."
        };
    }


    // ============================================================
    // DETECTAR CONSULTA DE PRESUPUESTO
    // ============================================================

    private static bool
        EsConsultaPresupuesto(
            string texto)
    {
        return ContieneAlguno(
            texto,

            "presupuesto",

            "presupuestario",

            "presupuestaria",

            "evolucion",

            "evolucion del presupuesto",

            "pia",

            "pim",

            "certificado",

            "comprometido",

            "comprometido anual",

            "devengado",

            "girado",

            "ejecucion",

            "ejecución"
        );
    }


    // ============================================================
    // EXTRAER ENTIDAD
    // ============================================================

    private static string
        ExtraerEntidad(
            string pregunta)
    {
        var texto =
            Normalizar(
                pregunta);

        var entidades =
            new[]
            {
                "municipalidad distrital de monzon",

                "municipalidad distrital de cerro azul",

                "region moquegua salud ilo",

                "region moquegua",

                "gobierno regional",

                "gobiernos regionales",

                "gobierno nacional",

                "gobiernos locales",

                "gobierno local"
            };


        foreach (
            var entidad
            in entidades)
        {
            if (
                texto.Contains(
                    entidad,
                    StringComparison.OrdinalIgnoreCase))
            {
                return entidad;
            }
        }


        return "";
    }


    // ============================================================
    // CONTIENE ALGUNO
    // ============================================================

    private static bool
        ContieneAlguno(
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
    // NORMALIZAR
    // ============================================================

    private static string
        Normalizar(
            string texto)
    {
        var valor =
            texto
                .Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD);

        var resultado =
            new StringBuilder();

        foreach (
            var caracter
            in valor)
        {
            var categoria =
                System.Globalization
                    .CharUnicodeInfo
                    .GetUnicodeCategory(
                        caracter);

            if (
                categoria !=
                System.Globalization
                    .UnicodeCategory
                    .NonSpacingMark)
            {
                resultado.Append(
                    caracter);
            }
        }

        return resultado
            .ToString()
            .Normalize(
                NormalizationForm.FormC);
    }
}