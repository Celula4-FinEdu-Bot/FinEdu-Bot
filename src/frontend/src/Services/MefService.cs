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
        // 1. BUSCAR EN 2022-2026
        // ========================================================

        var registros2022 = await BuscarRegistrosAsync(
            Resource2022_2026,
            filtro,
            cancellationToken);

        if (registros2022.Count > 0)
        {
            Console.WriteLine(
                $"MEF 2022-2026: {registros2022.Count} registros encontrados.");

            var resultado2022 =
                ConstruirEvolucion2022_2026(registros2022);

            if (resultado2022.Any(TieneDatos))
            {
                Console.WriteLine(
                    "Se utilizará el dataset 2022-2026.");

                return resultado2022;
            }
        }

        // ========================================================
        // 2. SI NO HAY DATOS, BUSCAR 2017-2021
        // ========================================================

        Console.WriteLine(
            "No se encontraron datos válidos en 2022-2026.");

        Console.WriteLine(
            "Intentando dataset comparativo 2017-2021...");

        var registros2017 = await BuscarRegistrosAsync(
            Resource2017_2021,
            filtro,
            cancellationToken);

        if (registros2017.Count > 0)
        {
            Console.WriteLine(
                $"MEF 2017-2021: {registros2017.Count} registros encontrados.");

            var resultado2017 =
                ConstruirEvolucion2017_2021(registros2017);

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
    // BUSCAR REGISTROS
    // ============================================================

    private async Task<List<Dictionary<string, JsonElement>>>
        BuscarRegistrosAsync(
            string resourceId,
            string? filtro,
            CancellationToken cancellationToken)
    {
        var url =
            $"{BaseUrl}datastore_search" +
            $"?resource_id={Uri.EscapeDataString(resourceId)}" +
            $"&limit=32000";

        try
        {
            // ====================================================
            // PRIMER INTENTO: q=
            // ====================================================

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var urlConFiltro =
                    url +
                    $"&q={Uri.EscapeDataString(filtro)}";

                Console.WriteLine("URL MEF con filtro:");
                Console.WriteLine(urlConFiltro);

                using var response =
                    await _httpClient.GetAsync(
                        urlConFiltro,
                        cancellationToken);

                var json =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var registros =
                        ExtraerRegistros(json);

                    if (registros.Count > 0)
                    {
                        return registros;
                    }
                }

                Console.WriteLine(
                    $"MEF q= no encontró registros. HTTP {(int)response.StatusCode}");
            }

            // ====================================================
            // SEGUNDO INTENTO: DATASET COMPLETO
            // ====================================================

            Console.WriteLine(
                "Consultando registros generales del recurso MEF...");

            using var responseGeneral =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            var jsonGeneral =
                await responseGeneral.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!responseGeneral.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"MEF respondió HTTP {(int)responseGeneral.StatusCode}");

                Console.WriteLine(jsonGeneral);

                return [];
            }

            var todos =
                ExtraerRegistros(jsonGeneral);

            Console.WriteLine(
                $"MEF devolvió {todos.Count} registros.");

            if (string.IsNullOrWhiteSpace(filtro))
            {
                return todos;
            }

            return FiltrarRegistros(
                todos,
                filtro);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                "La consulta al MEF fue cancelada.");

            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error consultando MEF: {ex.Message}");

            return [];
        }
    }

    // ============================================================
    // EXTRAER REGISTROS
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        ExtraerRegistros(string json)
    {
        try
        {
            using var document =
                JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(
                    "result",
                    out var result))
            {
                return [];
            }

            if (!result.TryGetProperty(
                    "records",
                    out var records))
            {
                return [];
            }

            var lista =
                new List<Dictionary<string, JsonElement>>();

            foreach (var record in records.EnumerateArray())
            {
                var diccionario =
                    new Dictionary<string, JsonElement>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (var propiedad in record.EnumerateObject())
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
                $"Error procesando JSON MEF: {ex.Message}");

            return [];
        }
    }

    // ============================================================
    // FILTRAR REGISTROS
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        FiltrarRegistros(
            List<Dictionary<string, JsonElement>> registros,
            string filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro))
        {
            return registros;
        }

        var texto =
            Normalizar(filtro);

        var palabras =
            texto
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 3)
                .ToArray();

        string[] campos =
        [
            "EJECUTORA_NOMBRE",
            "NIVEL_GOBIERNO_NOMBRE",
            "SECTOR_NOMBRE",
            "PLIEGO_NOMBRE",

            "DEPARTAMENTO_EJECUTORA_NOMBRE",
            "PROVINCIA_EJECUTORA_NOMBRE",
            "DISTRITO_EJECUTORA_NOMBRE",

            "PROGRAMA_PPTO_NOMBRE",
            "PRODUCTO_PROYECTO_NOMBRE",
            "ACTIVIDAD_ACCION_OBRA_NOMBRE",

            "FUNCION_NOMBRE",
            "DIVISION_FUNCIONAL_NOMBRE",
            "GRUPO_FUNCIONAL_NOMBRE",

            "CATEGORIA_GASTO_NOMBRE",
            "GENERICA_NOMBRE",
            "SUBGENERICA_NOMBRE",
            "SUBGENERICA_DET_NOMBRE",

            "ESPECIFICA_NOMBRE",
            "ESPECIFICA_DET_NOMBRE"
        ];

        return registros
            .Where(registro =>
            {
                foreach (var campo in campos)
                {
                    if (!registro.TryGetValue(
                            campo,
                            out var valor))
                    {
                        continue;
                    }

                    if (valor.ValueKind !=
                        JsonValueKind.String)
                    {
                        continue;
                    }

                    var contenido =
                        Normalizar(
                            valor.GetString() ?? "");

                    // Coincidencia de frase
                    if (contenido.Contains(texto))
                    {
                        return true;
                    }

                    // Coincidencia de palabras
                    if (palabras.Length > 0 &&
                        palabras.All(
                            contenido.Contains))
                    {
                        return true;
                    }
                }

                return false;
            })
            .ToList();
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

        for (int anio = 2022; anio <= 2026; anio++)
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

        for (int anio = 2017; anio <= 2021; anio++)
        {
            resultado.Add(
                new MefEvolucionDto
                {
                    Anio = anio,

                    Pia =
                        SumarCampo(
                            registros,
                            $"MONTO_PIA_{anio}"),

                    Pim =
                        SumarCampo(
                            registros,
                            $"MONTO_PIM_{anio}"),

                    Devengado =
                        SumarCampo(
                            registros,
                            $"MONTO_DEVENGADO_{anio}")
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

            total += ConvertirDecimal(valor);
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
    // VERIFICAR SI TIENE DATOS
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
            .ToUpperInvariant()
            .Normalize(NormalizationForm.FormD)
            .Where(c =>
                CharUnicodeInfo.GetUnicodeCategory(c)
                != UnicodeCategory.NonSpacingMark)
            .Aggregate(
                "",
                (actual, c) => actual + c)
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