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
    // DATASETS
    // ============================================================

    private const string Resource2022_2026 =
        "510bae6d-3d37-4fb2-af35-a40ce01715f4";

    private const string Resource2017_2021 =
        "0e2469d8-5872-4bc2-a5bc-91ee01c99df8";

    public MefService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        // El endpoint del MEF puede ser lento.
        // 120 segundos nos da margen, pero la consulta
        // ahora estará filtrada por PLIEGO.
        _httpClient.Timeout =
            TimeSpan.FromSeconds(120);
    }

    // ============================================================
    // EVOLUCIÓN
    // ============================================================

    public async Task<List<MefEvolucionDto>> ObtenerEvolucionAsync(
        string? filtro = null,
        CancellationToken cancellationToken = default)
    {
        filtro = LimpiarFiltro(filtro);

        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("MEF - EVOLUCIÓN PRESUPUESTARIA");
        Console.WriteLine($"Filtro recibido: '{filtro}'");
        Console.WriteLine("======================================");

        if (string.IsNullOrWhiteSpace(filtro))
        {
            Console.WriteLine(
                "No se recibió una entidad para consultar MEF.");

            return [];
        }

        // --------------------------------------------------------
        // 2022 - 2026
        // --------------------------------------------------------

        var resultado2022 =
            await BuscarDatasetAsync(
                Resource2022_2026,
                filtro,
                2022,
                2026,
                cancellationToken);

        Console.WriteLine(
            $"MEF 2022-2026 -> {resultado2022.Count} registros.");

        if (resultado2022.Count > 0)
        {
            var evolucion =
                ConstruirEvolucion(
                    resultado2022,
                    2022,
                    2026);

            if (evolucion.Any(TieneDatos))
            {
                return evolucion;
            }
        }

        // --------------------------------------------------------
        // NO hacemos automáticamente 2017-2021 para una pregunta
        // 2022-2026.
        //
        // Este método solo utiliza 2017-2021 si posteriormente
        // se solicita explícitamente un período de ese rango.
        // --------------------------------------------------------

        Console.WriteLine(
            "No se encontraron datos válidos para 2022-2026.");

        return [];
    }

    // ============================================================
    // BUSCAR DATASET
    // ============================================================

    private async Task<List<Dictionary<string, JsonElement>>>
    BuscarDatasetAsync(
        string resourceId,
        string filtro,
        int anioInicial,
        int anioFinal,
        CancellationToken cancellationToken)
{
    try
    {
        var entidad = ResolverEntidad(filtro);

        Console.WriteLine();
        Console.WriteLine("--------------------------------------");
        Console.WriteLine("MEF - RESOLUCIÓN DE ENTIDAD");
        Console.WriteLine($"Filtro: {filtro}");
        Console.WriteLine($"PLIEGO: {entidad.Pliego}");
        Console.WriteLine($"Nombre MEF: {entidad.Nombre}");
        Console.WriteLine("--------------------------------------");

        if (string.IsNullOrWhiteSpace(entidad.Pliego))
        {
            Console.WriteLine(
                $"No se pudo resolver el PLIEGO para: {filtro}");

            return [];
        }

        // Campos necesarios para la evolución
        var fields = new List<string>
        {
            "PLIEGO",
            "PLIEGO_NOMBRE",
            "EJECUTORA_NOMBRE"
        };

        for (var anio = anioInicial;
             anio <= anioFinal;
             anio++)
        {
            fields.Add($"PIA_{anio}");
            fields.Add($"PIM_{anio}");
            fields.Add($"DEVENGADO_{anio}");
        }

        var fieldsParam = string.Join(",", fields);

        // IMPORTANTE:
        // El MEF espera filters como JSON.
        var filtersObject =
            new Dictionary<string, string>
            {
                ["PLIEGO"] = entidad.Pliego
            };

        var filtersJson =
            JsonSerializer.Serialize(filtersObject);

        var url =
            $"{BaseUrl}datastore_search" +
            $"?resource_id={Uri.EscapeDataString(resourceId)}" +
            $"&filters={Uri.EscapeDataString(filtersJson)}" +
            $"&fields={Uri.EscapeDataString(fieldsParam)}" +
            $"&limit=1000" +
            $"&offset=0";

        Console.WriteLine();
        Console.WriteLine("--------------------------------------");
        Console.WriteLine("MEF - DATASTORE SEARCH");
        Console.WriteLine($"Dataset: {resourceId}");
        Console.WriteLine($"PLIEGO: {entidad.Pliego}");
        Console.WriteLine($"Entidad: {entidad.Nombre}");
        Console.WriteLine($"Años: {anioInicial}-{anioFinal}");
        Console.WriteLine("--------------------------------------");

        using var response =
            await _httpClient.GetAsync(
                url,
                cancellationToken);

        var json =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        Console.WriteLine(
            $"HTTP MEF: {(int)response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(
                "El MEF respondió con error:");

            Console.WriteLine(json);

            return [];
        }

        Console.WriteLine(
            $"Respuesta MEF recibida: {json.Length} caracteres.");

        var registros =
            ExtraerRegistros(json);

        Console.WriteLine(
            $"Registros recibidos del MEF: {registros.Count}");

        return registros;
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
    // RESOLVER ENTIDAD
    // ============================================================

    private static MefEntidad ResolverEntidad(
    string filtro)
{
    var normalizado =
        Normalizar(filtro);

    // ============================================================
    // DEFENSA
    // PLIEGO 026
    // ============================================================

    if (
        normalizado.Contains(
            "MINISTERIO DE DEFENSA") ||
        normalizado == "DEFENSA" ||
        normalizado.Contains(
            "M DE DEFENSA"))
    {
        return new MefEntidad
        {
            Pliego = "026",
            Nombre = "M. DE DEFENSA"
        };
    }

    // ============================================================
    // SALUD
    // PLIEGO 011
    // ============================================================

    if (
        normalizado.Contains(
            "MINISTERIO DE SALUD") ||
        normalizado == "SALUD" ||
        normalizado.Contains(
            "M DE SALUD"))
    {
        return new MefEntidad
        {
            Pliego = "011",
            Nombre = "M. DE SALUD"
        };
    }

    // ============================================================
    // EDUCACIÓN
    // PLIEGO 010
    // ============================================================

    if (
        normalizado.Contains(
            "MINISTERIO DE EDUCACION") ||
        normalizado == "EDUCACION")
    {
        return new MefEntidad
        {
            Pliego = "010",
            Nombre = "M. DE EDUCACION"
        };
    }

    // ============================================================
    // ECONOMÍA
    // PLIEGO 009
    // ============================================================

    if (
        normalizado.Contains(
            "MINISTERIO DE ECONOMIA") ||
        normalizado.Contains(
            "ECONOMIA Y FINANZAS"))
    {
        return new MefEntidad
        {
            Pliego = "009",
            Nombre = "M. DE ECONOMIA Y FINANZAS"
        };
    }

    // ============================================================
    // INTERIOR
    // PLIEGO 007
    // ============================================================

    if (
        normalizado.Contains(
            "MINISTERIO DEL INTERIOR") ||
        normalizado == "INTERIOR")
    {
        return new MefEntidad
        {
            Pliego = "007",
            Nombre = "M. DEL INTERIOR"
        };
    }

    // ============================================================
    // TRANSPORTES
    // ============================================================

    if (
        normalizado.Contains(
            "MINISTERIO DE TRANSPORTES") ||
        normalizado.Contains(
            "TRANSPORTES Y COMUNICACIONES"))
    {
        return new MefEntidad
        {
            Pliego = "036",
            Nombre =
                "M. DE TRANSPORTES Y COMUNICACIONES"
        };
    }

    // ============================================================
    // MIDIS
    // ============================================================

    if (
        normalizado.Contains(
            "MINISTERIO DE DESARROLLO E INCLUSION SOCIAL") ||
        normalizado.Contains(
            "DESARROLLO E INCLUSION SOCIAL"))
    {
        return new MefEntidad
        {
            Pliego = "040",
            Nombre =
                "MINISTERIO DE DESARROLLO E INCLUSION SOCIAL"
        };
    }

    // ============================================================
    // DESCONOCIDO
    // ============================================================

    return new MefEntidad
    {
        Pliego = "",
        Nombre = filtro
    };
}

    // ============================================================
// EXTRAER REGISTROS
// ============================================================

private static List<Dictionary<string, JsonElement>>
    ExtraerRegistros(string json)
{
    var resultado =
        new List<Dictionary<string, JsonElement>>();

    try
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Console.WriteLine(
                "MEF devolvió una respuesta vacía.");

            return resultado;
        }

        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            Console.WriteLine(
                $"Respuesta MEF inesperada. " +
                $"Tipo raíz: {root.ValueKind}");

            return resultado;
        }

        // --------------------------------------------------------
        // Verificar propiedad records
        // --------------------------------------------------------

        if (!root.TryGetProperty(
                "records",
                out var records))
        {
            Console.WriteLine(
                "La respuesta MEF no contiene la propiedad 'records'.");

            return resultado;
        }

        // --------------------------------------------------------
        // CASO 1:
        // records = null
        // --------------------------------------------------------

        if (records.ValueKind == JsonValueKind.Null ||
            records.ValueKind == JsonValueKind.Undefined)
        {
            Console.WriteLine(
                "MEF devolvió records=null.");

            // Mostrar información útil para diagnóstico.
            if (root.TryGetProperty(
                    "success",
                    out var success))
            {
                Console.WriteLine(
                    $"MEF success: {success}");
            }

            if (root.TryGetProperty(
                    "sucess",
                    out var sucess))
            {
                Console.WriteLine(
                    $"MEF sucess: {sucess}");
            }

            if (root.TryGetProperty(
                    "result",
                    out var result))
            {
                Console.WriteLine(
                    $"MEF result: {result}");
            }

            return resultado;
        }

        // --------------------------------------------------------
        // CASO 2:
        // records no es un array
        // --------------------------------------------------------

        if (records.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine(
                $"MEF devolvió 'records' con tipo " +
                $"{records.ValueKind}, no Array.");

            Console.WriteLine(
                $"records: {records}");

            return resultado;
        }

        // --------------------------------------------------------
        // CASO 3:
        // records es un array
        // --------------------------------------------------------

        foreach (var record in records.EnumerateArray())
        {
            if (record.ValueKind != JsonValueKind.Object)
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
                resultado.Add(diccionario);
            }
        }

        Console.WriteLine(
            $"ExtraerRegistros: {resultado.Count} registros.");

        return resultado;
    }
    catch (JsonException ex)
    {
        Console.WriteLine(
            $"JSON inválido del MEF: {ex.Message}");

        Console.WriteLine(
            $"Respuesta recibida: {json}");

        return resultado;
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"Error procesando respuesta MEF: {ex.Message}");

        return resultado;
    }
}
    

    // ============================================================
    // CONSTRUIR EVOLUCIÓN
    // ============================================================

    private static List<MefEvolucionDto>
        ConstruirEvolucion(
            List<Dictionary<string, JsonElement>> registros,
            int anioInicial,
            int anioFinal)
    {
        var resultado =
            new List<MefEvolucionDto>();

        for (
            int anio = anioInicial;
            anio <= anioFinal;
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
    // SUMAR
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
            }

            if (
                valor.ValueKind ==
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

                if (
                    decimal.TryParse(
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
            // 0
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
    // LIMPIAR
    // ============================================================

    private static string LimpiarFiltro(
        string? filtro)
    {
        return string.IsNullOrWhiteSpace(filtro)
            ? ""
            : filtro.Trim();
    }

    // ============================================================
    // NORMALIZAR
    // ============================================================

    private static string Normalizar(
    string? texto)
{
    if (string.IsNullOrWhiteSpace(texto))
    {
        return "";
    }

    var normalizado =
        texto
            .Trim()
            .ToUpperInvariant()
            .Normalize(
                NormalizationForm.FormD);

    var resultado =
        new StringBuilder();

    foreach (var caracter in normalizado)
    {
        var categoria =
            CharUnicodeInfo.GetUnicodeCategory(
                caracter);

        if (
            categoria !=
            UnicodeCategory.NonSpacingMark)
        {
            resultado.Append(caracter);
        }
    }

    return resultado
        .ToString()
        .Normalize(
            NormalizationForm.FormC)
        .Replace(".", "")
        .Replace(",", "")
        .Replace("-", " ")
        .Replace("_", " ")
        .Replace("  ", " ")
        .Trim();
}
}

// ================================================================
// ENTIDAD MEF
// ================================================================

public sealed class MefEntidad
{
    public string Pliego { get; set; } = "";

    public string Nombre { get; set; } = "";
}

// ================================================================
// DTO EVOLUCIÓN
// ================================================================

public sealed class MefEvolucionDto
{
    public int Anio { get; set; }

    public decimal Pia { get; set; }

    public decimal Pim { get; set; }

    public decimal Devengado { get; set; }
}