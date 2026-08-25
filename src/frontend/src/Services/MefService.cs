using System.Globalization;
using System.Text;
using System.Text.Json;

namespace src.Services;

public sealed class MefService
{
    private readonly HttpClient _httpClient;

    private const string BaseUrl =
        "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/";

    // ============================================================
    // DATASET 2022-2026
    // ============================================================

    private const string Resource2022_2026 =
        "510bae6d-3d37-4fb2-af35-a40ce01715f4";

    // ============================================================
    // DATASET 2017-2021
    // ============================================================

    private const string Resource2017_2021 =
        "0e2469d8-5872-4bc2-a5bc-91ee01c99df8";

    public MefService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ============================================================
    // EVOLUCIÓN
    // ============================================================

    public async Task<List<MefEvolucionDto>> ObtenerEvolucionAsync(
        string? filtro = null,
        CancellationToken cancellationToken = default)
    {
        filtro = LimpiarFiltro(filtro);

        Console.WriteLine("======================================");
        Console.WriteLine("MEF - EVOLUCIÓN PRESUPUESTARIA");
        Console.WriteLine($"Filtro: {filtro}");
        Console.WriteLine("======================================");

        // ========================================================
        // 1. DATASET 2022-2026
        // ========================================================

        var registros2022 =
            await BuscarRegistrosSqlAsync(
                Resource2022_2026,
                filtro,
                2022,
                2026,
                cancellationToken);

        if (registros2022.Count > 0)
        {
            Console.WriteLine(
                $"MEF 2022-2026: {registros2022.Count} registros encontrados.");

            var resultado2022 =
                ConstruirEvolucion2022_2026(
                    registros2022);

            if (resultado2022.Any(TieneDatos))
            {
                Console.WriteLine(
                    "Se utilizará el dataset 2022-2026.");

                return resultado2022;
            }
        }

        // ========================================================
        // 2. DATASET 2017-2021
        // ========================================================

        Console.WriteLine(
            "No se encontraron datos válidos en 2022-2026.");

        Console.WriteLine(
            "Intentando dataset comparativo 2017-2021...");

        var registros2017 =
            await BuscarRegistrosSqlAsync(
                Resource2017_2021,
                filtro,
                2017,
                2021,
                cancellationToken);

        if (registros2017.Count > 0)
        {
            Console.WriteLine(
                $"MEF 2017-2021: {registros2017.Count} registros encontrados.");

            var resultado2017 =
                ConstruirEvolucion2017_2021(
                    registros2017);

            if (resultado2017.Any(TieneDatos))
            {
                Console.WriteLine(
                    "Se utilizará el dataset 2017-2021.");

                return resultado2017;
            }
        }

        // ========================================================
        // 3. SIN DATOS
        // ========================================================

        Console.WriteLine(
            "No se encontraron datos en ninguno de los datasets.");

        return [];
    }

    // ============================================================
    // CONSULTA SQL AL MEF
    // ============================================================

    private async Task<List<Dictionary<string, JsonElement>>>
        BuscarRegistrosSqlAsync(
            string resourceId,
            string? filtro,
            int anioInicial,
            int anioFinal,
            CancellationToken cancellationToken)
    {
        try
        {
            // ----------------------------------------------------
            // Si no existe filtro no hacemos una consulta masiva.
            // ----------------------------------------------------

            if (string.IsNullOrWhiteSpace(filtro))
            {
                Console.WriteLine(
                    "No se especificó una entidad para la consulta MEF.");

                return [];
            }

            var filtroNormalizado =
                Normalizar(filtro);

            // ----------------------------------------------------
            // Escapamos las comillas simples para SQL.
            // ----------------------------------------------------

            var filtroSql =
                filtroNormalizado
                    .Replace(
                        "'",
                        "''",
                        StringComparison.Ordinal);

            // ----------------------------------------------------
            // Campos que utilizaremos para localizar la entidad.
            //
            // Se utilizan los nombres reales del dataset recibido.
            // ----------------------------------------------------

            var condiciones =
                new StringBuilder();

            condiciones.Append(
                "(");

            condiciones.Append(
                $"UPPER(\"EJECUTORA_NOMBRE\") LIKE '%{filtroSql}%'");

            condiciones.Append(
                $" OR UPPER(\"PLIEGO_NOMBRE\") LIKE '%{filtroSql}%'");

            condiciones.Append(
                $" OR UPPER(\"NIVEL_GOBIERNO_NOMBRE\") LIKE '%{filtroSql}%'");

            condiciones.Append(
                $" OR UPPER(\"DEPARTAMENTO_EJECUTORA_NOMBRE\") LIKE '%{filtroSql}%'");

            condiciones.Append(
                ")");

            // ----------------------------------------------------
            // Solo solicitamos las columnas necesarias.
            //
            // Esto evita traer las decenas de columnas del recurso.
            // ----------------------------------------------------

            var columnas =
                new StringBuilder();

            columnas.Append(
                "\"EJECUTORA_NOMBRE\",");

            columnas.Append(
                "\"PLIEGO_NOMBRE\",");

            columnas.Append(
                "\"NIVEL_GOBIERNO_NOMBRE\",");

            columnas.Append(
                "\"DEPARTAMENTO_EJECUTORA_NOMBRE\"");

            for (int anio = anioInicial;
                 anio <= anioFinal;
                 anio++)
            {
                columnas.Append(
                    $",\"PIA_{anio}\"");

                columnas.Append(
                    $",\"PIM_{anio}\"");

                columnas.Append(
                    $",\"DEVENGADO_{anio}\"");
            }

            // ----------------------------------------------------
            // Consulta SQL.
            // ----------------------------------------------------

            var sql =
                $"SELECT {columnas} " +
                $"FROM \"{resourceId}\" " +
                $"WHERE {condiciones} " +
                $"LIMIT 32000";

            var url =
                $"{BaseUrl}datastore_search_sql" +
                $"?sql={Uri.EscapeDataString(sql)}";

            Console.WriteLine("======================================");
            Console.WriteLine("MEF - CONSULTA SQL");
            Console.WriteLine(url);
            Console.WriteLine("======================================");

            using var response =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            var json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"MEF SQL respondió HTTP {(int)response.StatusCode}");

                Console.WriteLine(json);

                return [];
            }

            var registros =
                ExtraerRegistrosSql(json);

            Console.WriteLine(
                $"MEF SQL devolvió {registros.Count} registros.");

            return registros;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                "La consulta SQL al MEF fue cancelada.");

            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error consultando MEF mediante SQL: {ex.Message}");

            return [];
        }
    }

    // ============================================================
    // EXTRAER REGISTROS DE CONSULTA SQL
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        ExtraerRegistrosSql(string json)
    {
        try
        {
            using var document =
                JsonDocument.Parse(json);

            // ----------------------------------------------------
            // La respuesta del datastore_search_sql utiliza
            // "result" y dentro "records".
            // ----------------------------------------------------

            if (!document.RootElement.TryGetProperty(
                    "result",
                    out var result))
            {
                Console.WriteLine(
                    "La respuesta del MEF no contiene 'result'.");

                return [];
            }

            if (!result.TryGetProperty(
                    "records",
                    out var records))
            {
                Console.WriteLine(
                    "La respuesta del MEF no contiene 'records'.");

                return [];
            }

            var lista =
                new List<Dictionary<string, JsonElement>>();

            foreach (var record in records.EnumerateArray())
            {
                var diccionario =
                    new Dictionary<string, JsonElement>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (var propiedad
                         in record.EnumerateObject())
                {
                    diccionario[propiedad.Name] =
                        propiedad.Value.Clone();
                }

                lista.Add(diccionario);
            }

            return lista;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error procesando respuesta SQL del MEF: {ex.Message}");

            return [];
        }
    }

    // ============================================================
    // CONSTRUIR 2022-2026
    // ============================================================

    private static List<MefEvolucionDto>
        ConstruirEvolucion2022_2026(
            List<Dictionary<string, JsonElement>> registros)
    {
        var resultado =
            new List<MefEvolucionDto>();

        for (int anio = 2022;
             anio <= 2026;
             anio++)
        {
            resultado.Add(
                new MefEvolucionDto
                {
                    Anio = anio,

                    Pia =
                        SumarCampo(
                            registros,
                            $"PIA_{anio}"),

                    Pim =
                        SumarCampo(
                            registros,
                            $"PIM_{anio}"),

                    Devengado =
                        SumarCampo(
                            registros,
                            $"DEVENGADO_{anio}")
                });
        }

        return resultado;
    }

    // ============================================================
    // CONSTRUIR 2017-2021
    // ============================================================

    private static List<MefEvolucionDto>
        ConstruirEvolucion2017_2021(
            List<Dictionary<string, JsonElement>> registros)
    {
        var resultado =
            new List<MefEvolucionDto>();

        for (int anio = 2017;
             anio <= 2021;
             anio++)
        {
            resultado.Add(
                new MefEvolucionDto
                {
                    Anio = anio,

                    Pia =
                        SumarCampo(
                            registros,
                            $"PIA_{anio}"),

                    Pim =
                        SumarCampo(
                            registros,
                            $"PIM_{anio}"),

                    Devengado =
                        SumarCampo(
                            registros,
                            $"DEVENGADO_{anio}")
                });
        }

        return resultado;
    }

    // ============================================================
    // SUMAR CAMPO
    // ============================================================

    private static decimal SumarCampo(
        List<Dictionary<string, JsonElement>> registros,
        string campo)
    {
        decimal total = 0;

        foreach (var registro in registros)
        {
            if (!registro.TryGetValue(
                    campo,
                    out var valor))
            {
                continue;
            }

            total +=
                ConvertirDecimal(valor);
        }

        return total;
    }

    // ============================================================
    // CONVERTIR DECIMAL
    // ============================================================

    private static decimal ConvertirDecimal(
        JsonElement valor)
    {
        try
        {
            if (valor.ValueKind ==
                JsonValueKind.Number)
            {
                if (valor.TryGetDecimal(
                        out var numero))
                {
                    return numero;
                }
            }

            if (valor.ValueKind ==
                JsonValueKind.String)
            {
                var texto =
                    valor.GetString();

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return 0;
                }

                texto =
                    texto
                        .Replace(",", "")
                        .Trim();

                if (decimal.TryParse(
                        texto,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var numero))
                {
                    return numero;
                }
            }
        }
        catch
        {
            // Valor no numérico.
        }

        return 0;
    }

    // ============================================================
    // VERIFICAR DATOS
    // ============================================================

    private static bool TieneDatos(
        MefEvolucionDto x)
    {
        return
            x.Pia != 0 ||
            x.Pim != 0 ||
            x.Devengado != 0;
    }

    // ============================================================
    // LIMPIAR FILTRO
    // ============================================================

    private static string LimpiarFiltro(
        string? filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro))
        {
            return "";
        }

        return filtro.Trim();
    }

    // ============================================================
    // NORMALIZAR
    // ============================================================

    private static string Normalizar(
        string texto)
    {
        return texto
            .Trim()
            .ToUpperInvariant()
            .Normalize(
                NormalizationForm.FormD)
            .Where(
                c =>
                    CharUnicodeInfo.GetUnicodeCategory(c)
                    != UnicodeCategory.NonSpacingMark)
            .Aggregate(
                "",
                (actual, c) =>
                    actual + c)
            .Normalize(
                NormalizationForm.FormC);
    }
}

// ================================================================
// DTO
// ================================================================

public sealed class MefEvolucionDto
{
    public int Anio { get; set; }

    public decimal Pia { get; set; }

    public decimal Pim { get; set; }

    public decimal Devengado { get; set; }
}