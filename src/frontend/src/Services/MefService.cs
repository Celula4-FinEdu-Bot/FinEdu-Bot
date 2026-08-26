using System.Globalization;
using System.Text;
using System.Text.Json;
using src.Models;

namespace src.Services;

public sealed class MefService
{
    private readonly HttpClient _httpClient;

    private const string BaseUrl =
        "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/";

    // ============================================================
    // DATASET COMPARATIVO DE GASTOS 2022-2026
    // ============================================================

    private const string Resource2022_2026 =
        "510bae6d-3d37-4fb2-af35-a40ce01715f4";

    public MefService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.Timeout =
            TimeSpan.FromSeconds(120);
    }

    // ============================================================
    // OBTENER EVOLUCIÓN
    // ============================================================

    public async Task<List<EvolucionPresupuesto>>
        ObtenerEvolucionAsync(
            string? filtro = null,
            CancellationToken cancellationToken = default)
    {
        filtro =
            LimpiarFiltro(filtro);

        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("MEF - EVOLUCIÓN PRESUPUESTARIA");
        Console.WriteLine(
            $"Filtro recibido: '{filtro}'");
        Console.WriteLine(
            $"Dataset: {Resource2022_2026}");
        Console.WriteLine("======================================");

        try
        {
            var sql =
                ConstruirConsultaSql(filtro);

            Console.WriteLine();
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("MEF - CONSULTA SQL");
            Console.WriteLine("--------------------------------------");
            Console.WriteLine(sql);

            var url =
                $"{BaseUrl}datastore_search_sql" +
                $"?sql={Uri.EscapeDataString(sql)}";

            Console.WriteLine();
            Console.WriteLine(
                $"URL MEF SQL: {url}");

            using var response =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            var json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            Console.WriteLine();
            Console.WriteLine(
                $"HTTP MEF SQL: {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    "MEF respondió con error:");

                Console.WriteLine(json);

                return [];
            }

            Console.WriteLine(
                $"Respuesta MEF recibida: {json.Length} caracteres.");

            var registros =
                ExtraerRegistros(json);

            Console.WriteLine(
                $"Registros SQL recibidos: {registros.Count}");

            if (registros.Count == 0)
            {
                Console.WriteLine(
                    "La consulta SQL no devolvió registros.");

                return [];
            }

            var evolucion =
                ConstruirEvolucion(
                    registros,
                    filtro);

            Console.WriteLine();
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("EVOLUCIÓN CALCULADA");
            Console.WriteLine("--------------------------------------");

            foreach (var item in evolucion)
            {
                Console.WriteLine(
                    $"{item.Anio} | " +
                    $"PIA={item.Pia:N2} | " +
                    $"PIM={item.Pim:N2} | " +
                    $"DEVENGADO={item.Devengado:N2} | " +
                    $"EJECUCIÓN={item.PorcentajeEjecucion:N2}%");
            }

            return evolucion;
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine(
                $"Consulta MEF cancelada: {ex.Message}");

            return [];
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(
                $"Error HTTP MEF: {ex.Message}");

            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error consultando MEF: {ex}");

            return [];
        }
    }

    // ============================================================
    // CONSTRUIR CONSULTA SQL
    // ============================================================

    private static string ConstruirConsultaSql(
        string filtro)
    {
        var columnas =
            new StringBuilder();

        for (var anio = 2022; anio <= 2026; anio++)
        {
            columnas.AppendLine(
                $"COALESCE(SUM(\"PIA_{anio}\"), 0) AS \"PIA_{anio}\",");

            columnas.AppendLine(
                $"COALESCE(SUM(\"PIM_{anio}\"), 0) AS \"PIM_{anio}\",");

            columnas.AppendLine(
                $"COALESCE(SUM(\"DEVENGADO_{anio}\"), 0) AS \"DEVENGADO_{anio}\"" +
                (anio < 2026 ? "," : ""));
        }

        var where =
            ConstruirWhere(filtro);

        var sql =
            $"""
            SELECT
                {columnas}
            FROM "{Resource2022_2026}"
            {where}
            """;

        return sql;
    }

    // ============================================================
    // CONSTRUIR WHERE
    // ============================================================

    private static string ConstruirWhere(
        string filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro))
        {
            Console.WriteLine(
                "MEF: consulta global sin filtro de entidad.");

            return "";
        }

        var normalizado =
            Normalizar(filtro);

        // ========================================================
        // GOBIERNOS REGIONALES
        // ========================================================

        if (
            normalizado.Contains(
                "GOBIERNO REGIONAL",
                StringComparison.OrdinalIgnoreCase) ||
            normalizado == "REGIONAL" ||
            normalizado.Contains(
                "GOBIERNOS REGIONALES",
                StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(
                "MEF: filtro detectado = GOBIERNO REGIONAL");

            return
                """
                WHERE
                    "NIVEL_GOBIERNO" = 'R'
                    OR
                    "NIVEL_GOBIERNO_NOMBRE"
                        ILIKE '%REGIONAL%'
                """;
        }

        // ========================================================
        // MUNICIPALIDAD DE LIMA
        // ========================================================

        if (
            normalizado.Contains(
                "MUNICIPALIDAD DE LIMA",
                StringComparison.OrdinalIgnoreCase) ||
            normalizado.Contains(
                "MUNICIPALIDAD METROPOLITANA DE LIMA",
                StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(
                "MEF: filtro detectado = MUNICIPALIDAD DE LIMA");

            // Municipalidad Metropolitana de Lima
            // tiene presencia presupuestaria asociada a los
            // pliegos 150 y 465.
            return
                """
                WHERE
                    "PLIEGO" IN ('150', '465')
                    OR
                    "PLIEGO_NOMBRE"
                        ILIKE '%MUNICIPALIDAD METROPOLITANA DE LIMA%'
                    OR
                    "EJECUTORA_NOMBRE"
                        ILIKE '%MUNICIPALIDAD METROPOLITANA DE LIMA%'
                """;
        }

        // ========================================================
        // MINISTERIOS
        // ========================================================

        var pliego =
            ResolverPliego(normalizado);

        if (!string.IsNullOrWhiteSpace(pliego))
        {
            Console.WriteLine(
                $"MEF: pliego detectado = {pliego}");

            return
                $"WHERE \"PLIEGO\" = '{pliego}'";
        }

        // ========================================================
        // BÚSQUEDA GENERAL
        // ========================================================

        var termino =
            EscaparSql(filtro);

        Console.WriteLine(
            $"MEF: búsqueda textual = '{filtro}'");

        return
            $"""
            WHERE
                "PLIEGO_NOMBRE" ILIKE '%{termino}%'
                OR
                "EJECUTORA_NOMBRE" ILIKE '%{termino}%'
                OR
                "SECTOR_NOMBRE" ILIKE '%{termino}%'
            """;
    }

    // ============================================================
    // RESOLVER PLIEGO
    // ============================================================

    private static string ResolverPliego(
        string filtro)
    {
        if (
            filtro.Contains(
                "MINISTERIO DE DEFENSA",
                StringComparison.OrdinalIgnoreCase) ||
            filtro == "DEFENSA")
        {
            return "026";
        }

        if (
            filtro.Contains(
                "MINISTERIO DE SALUD",
                StringComparison.OrdinalIgnoreCase) ||
            filtro == "SALUD")
        {
            return "011";
        }

        if (
            filtro.Contains(
                "MINISTERIO DE EDUCACION",
                StringComparison.OrdinalIgnoreCase) ||
            filtro == "EDUCACION")
        {
            return "010";
        }

        if (
            filtro.Contains(
                "MINISTERIO DE ECONOMIA",
                StringComparison.OrdinalIgnoreCase) ||
            filtro.Contains(
                "ECONOMIA Y FINANZAS",
                StringComparison.OrdinalIgnoreCase))
        {
            return "009";
        }

        if (
            filtro.Contains(
                "MINISTERIO DEL INTERIOR",
                StringComparison.OrdinalIgnoreCase) ||
            filtro.Contains(
                "MINISTERIO DE INTERIOR",
                StringComparison.OrdinalIgnoreCase) ||
            filtro == "INTERIOR")
        {
            return "007";
        }

        if (
            filtro.Contains(
                "MINISTERIO DE TRANSPORTES",
                StringComparison.OrdinalIgnoreCase) ||
            filtro.Contains(
                "TRANSPORTES Y COMUNICACIONES",
                StringComparison.OrdinalIgnoreCase))
        {
            return "036";
        }

        if (
            filtro.Contains(
                "MINISTERIO DE DESARROLLO E INCLUSION SOCIAL",
                StringComparison.OrdinalIgnoreCase) ||
            filtro.Contains(
                "DESARROLLO E INCLUSION SOCIAL",
                StringComparison.OrdinalIgnoreCase))
        {
            return "040";
        }

        return "";
    }

    // ============================================================
    // EXTRAER REGISTROS
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        ExtraerRegistros(
            string json)
    {
        var resultado =
            new List<Dictionary<string, JsonElement>>();

        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return resultado;
            }

            using var document =
                JsonDocument.Parse(json);

            var root =
                document.RootElement;

            JsonElement records;

            // ====================================================
            // FORMATO 1
            // records directamente en raíz
            // ====================================================

            if (
                root.TryGetProperty(
                    "records",
                    out var recordsDirectos))
            {
                records =
                    recordsDirectos;
            }

            // ====================================================
            // FORMATO 2
            // result.records
            // ====================================================

            else if (
                root.TryGetProperty(
                    "result",
                    out var result) &&
                result.ValueKind ==
                    JsonValueKind.Object &&
                result.TryGetProperty(
                    "records",
                    out var recordsResult))
            {
                records =
                    recordsResult;
            }
            else
            {
                Console.WriteLine(
                    "La respuesta MEF no contiene records.");

                Console.WriteLine(
                    $"JSON recibido: {json}");

                return resultado;
            }

            if (
                records.ValueKind !=
                JsonValueKind.Array)
            {
                Console.WriteLine(
                    "records no es un arreglo.");

                return resultado;
            }

            foreach (
                var record
                in records.EnumerateArray())
            {
                if (
                    record.ValueKind !=
                    JsonValueKind.Object)
                {
                    continue;
                }

                var diccionario =
                    new Dictionary<string, JsonElement>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (
                    var propiedad
                    in record.EnumerateObject())
                {
                    diccionario[propiedad.Name] =
                        propiedad.Value.Clone();
                }

                if (diccionario.Count > 0)
                {
                    resultado.Add(
                        diccionario);
                }
            }

            return resultado;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                $"JSON inválido del MEF: {ex.Message}");

            return resultado;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error procesando JSON MEF: {ex.Message}");

            return resultado;
        }
    }

    // ============================================================
    // CONSTRUIR EVOLUCIÓN
    // ============================================================

    private static List<EvolucionPresupuesto>
        ConstruirEvolucion(
            List<Dictionary<string, JsonElement>> registros,
            string? entidad)
    {
        var resultado =
            new List<EvolucionPresupuesto>();

        if (registros.Count == 0)
        {
            return resultado;
        }

        var registro =
            registros[0];

        for (var anio = 2022; anio <= 2026; anio++)
        {
            var pia =
                ObtenerDecimal(
                    registro,
                    $"PIA_{anio}");

            var pim =
                ObtenerDecimal(
                    registro,
                    $"PIM_{anio}");

            var devengado =
                ObtenerDecimal(
                    registro,
                    $"DEVENGADO_{anio}");

            var porcentaje =
                pim > 0
                    ? devengado / pim * 100
                    : 0;

            resultado.Add(
                new EvolucionPresupuesto
                {
                    Anio = anio,

                    Entidad =
                        string.IsNullOrWhiteSpace(entidad)
                            ? "Todas las entidades"
                            : entidad,

                    PresupuestoInicial =
                        pia,

                    PresupuestoModificado =
                        pim,

                    MontoEjecutado =
                        devengado,

                    PorcentajeEjecucion =
                        porcentaje
                });
        }

        return resultado
            .OrderBy(
                x => x.Anio)
            .ToList();
    }

    // ============================================================
    // OBTENER DECIMAL
    // ============================================================

    private static decimal ObtenerDecimal(
        Dictionary<string, JsonElement> registro,
        string campo)
    {
        if (
            !registro.TryGetValue(
                campo,
                out var valor))
        {
            return 0;
        }

        try
        {
            if (
                valor.ValueKind ==
                JsonValueKind.Number)
            {
                if (
                    valor.TryGetDecimal(
                        out var numero))
                {
                    return numero;
                }

                if (
                    valor.TryGetDouble(
                        out var numeroDouble))
                {
                    return
                        (decimal)numeroDouble;
                }
            }

            if (
                valor.ValueKind ==
                JsonValueKind.String)
            {
                var texto =
                    valor.GetString();

                if (
                    string.IsNullOrWhiteSpace(
                        texto))
                {
                    return 0;
                }

                texto =
                    texto
                        .Replace(",", "")
                        .Trim();

                if (
                    decimal.TryParse(
                        texto,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var numero))
                {
                    return numero;
                }

                if (
                    decimal.TryParse(
                        texto,
                        NumberStyles.Any,
                        CultureInfo.GetCultureInfo(
                            "es-PE"),
                        out numero))
                {
                    return numero;
                }
            }
        }
        catch
        {
            // El campo se interpreta como cero.
        }

        return 0;
    }

    // ============================================================
    // ESCAPAR SQL
    // ============================================================

    private static string EscaparSql(
        string texto)
    {
        return texto
            .Replace(
                "'",
                "''",
                StringComparison.Ordinal);
    }

    // ============================================================
    // NORMALIZAR
    // ============================================================

    private static string Normalizar(
        string? texto)
    {
        if (
            string.IsNullOrWhiteSpace(
                texto))
        {
            return "";
        }

        return texto
            .Trim()
            .ToUpperInvariant()
            .Replace("Á", "A")
            .Replace("É", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O")
            .Replace("Ú", "U")
            .Replace("Ü", "U")
            .Replace("Ñ", "N");
    }

    // ============================================================
    // LIMPIAR FILTRO
    // ============================================================

    private static string LimpiarFiltro(
        string? filtro)
    {
        if (
            string.IsNullOrWhiteSpace(
                filtro))
        {
            return "";
        }

        var resultado =
            filtro.Trim();

        // Evitamos que un residuo como "y"
        // llegue al MEF como entidad.
        if (
            resultado.Equals(
                "y",
                StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        return resultado;
    }
}