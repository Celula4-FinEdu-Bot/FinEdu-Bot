
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

        // Evitamos que una consulta lenta del MEF sea cancelada
        // demasiado rápido por el HttpClient.
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    // ============================================================
    // EVOLUCIÓN PRESUPUESTARIA
    // ============================================================

    public async Task<List<MefEvolucionDto>> ObtenerEvolucionAsync(
        string? filtro = null,
        CancellationToken cancellationToken = default)
    {
        filtro = LimpiarFiltro(filtro);

        Console.WriteLine("======================================");
        Console.WriteLine("MEF - EVOLUCIÓN PRESUPUESTARIA");
        Console.WriteLine($"Filtro recibido: '{filtro}'");
        Console.WriteLine("======================================");

        // --------------------------------------------------------
        // IMPORTANTE:
        // Primero intentamos 2022-2026.
        // Si no encontramos nada, buscamos 2017-2021.
        // --------------------------------------------------------

        var registros2022 = await BuscarRegistrosAsync(
            Resource2022_2026,
            filtro,
            2022,
            2026,
            cancellationToken);

        Console.WriteLine(
            $"MEF 2022-2026 -> {registros2022.Count} registros.");

        if (registros2022.Count > 0)
        {
            var resultado =
                ConstruirEvolucion(
                    registros2022,
                    2022,
                    2026);

            if (resultado.Any(TieneDatos))
            {
                return resultado;
            }
        }

        Console.WriteLine(
            "No hubo datos válidos en 2022-2026.");

        // --------------------------------------------------------
        // SEGUNDO DATASET
        // --------------------------------------------------------

        var registros2017 = await BuscarRegistrosAsync(
            Resource2017_2021,
            filtro,
            2017,
            2021,
            cancellationToken);

        Console.WriteLine(
            $"MEF 2017-2021 -> {registros2017.Count} registros.");

        if (registros2017.Count > 0)
        {
            var resultado =
                ConstruirEvolucion(
                    registros2017,
                    2017,
                    2021);

            if (resultado.Any(TieneDatos))
            {
                return resultado;
            }
        }

        Console.WriteLine(
            "No se encontraron datos en ninguno de los datasets.");

        return [];
    }

    // ============================================================
    // BUSCAR REGISTROS
    //
    // NO UTILIZAMOS datastore_search_sql.
    //
    // Utilizamos el endpoint datastore_search que ya comprobaste
    // que devuelve correctamente records.
    // ============================================================

    private async Task<List<Dictionary<string, JsonElement>>>
        BuscarRegistrosAsync(
            string resourceId,
            string? filtro,
            int anioInicial,
            int anioFinal,
            CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filtro))
            {
                Console.WriteLine(
                    "Filtro vacío. No se ejecutará una consulta masiva.");

                return [];
            }

            var filtroNormalizado =
                Normalizar(filtro);

            Console.WriteLine(
                $"Filtro normalizado: '{filtroNormalizado}'");

            // ----------------------------------------------------
            // IMPORTANTE
            //
            // datastore_search permite usar q.
            //
            // No descargamos millones de registros.
            // Buscamos solamente coincidencias relacionadas con
            // la entidad enviada por el usuario.
            // ----------------------------------------------------

            const int limit = 100;

            var url =
                $"{BaseUrl}datastore_search" +
                $"?resource_id={Uri.EscapeDataString(resourceId)}" +
                $"&q={Uri.EscapeDataString(filtroNormalizado)}" +
                $"&limit={limit}" +
                $"&offset=0";

            Console.WriteLine("--------------------------------------");
            Console.WriteLine("MEF - DATASTORE SEARCH");
            Console.WriteLine($"Dataset: {resourceId}");
            Console.WriteLine($"Filtro: {filtroNormalizado}");
            Console.WriteLine($"URL: {url}");
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
                    "Respuesta de error del MEF:");

                Console.WriteLine(json);

                return [];
            }

            var registros =
                ExtraerRegistros(json);

            Console.WriteLine(
                $"Registros recibidos: {registros.Count}");

            // ----------------------------------------------------
            // datastore_search q hace una búsqueda general.
            //
            // Por seguridad hacemos una segunda validación local
            // sobre los campos que realmente nos interesan.
            // ----------------------------------------------------

            var filtrados =
                FiltrarPorEntidad(
                    registros,
                    filtroNormalizado);

            Console.WriteLine(
                $"Registros después del filtro local: {filtrados.Count}");

            return filtrados;
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
                $"Error HTTP consultando MEF: {ex.Message}");

            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error general consultando MEF: {ex}");

            return [];
        }
    }

    // ============================================================
    // EXTRAER RECORDS
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        ExtraerRegistros(string json)
    {
        try
        {
            using var document =
                JsonDocument.Parse(json);

            var root =
                document.RootElement;

            // ----------------------------------------------------
            // Formato real:
            //
            // {
            //   "records": [...],
            //   "result": {...}
            // }
            // ----------------------------------------------------

            if (!root.TryGetProperty(
                    "records",
                    out var records))
            {
                Console.WriteLine(
                    "La respuesta MEF no contiene 'records'.");

                return [];
            }

            var resultado =
                new List<Dictionary<string, JsonElement>>();

            foreach (var record
                     in records.EnumerateArray())
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

                resultado.Add(diccionario);
            }

            return resultado;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                $"JSON inválido recibido del MEF: {ex.Message}");

            return [];
        }
    }

    // ============================================================
    // FILTRAR ENTIDAD
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        FiltrarPorEntidad(
            List<Dictionary<string, JsonElement>> registros,
            string filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro))
        {
            return registros;
        }

        var resultado =
            new List<Dictionary<string, JsonElement>>();

        foreach (var registro in registros)
        {
            var camposBusqueda = new[]
            {
                ObtenerTexto(
                    registro,
                    "EJECUTORA_NOMBRE"),

                ObtenerTexto(
                    registro,
                    "PLIEGO_NOMBRE"),

                ObtenerTexto(
                    registro,
                    "NIVEL_GOBIERNO_NOMBRE"),

                ObtenerTexto(
                    registro,
                    "DEPARTAMENTO_EJECUTORA_NOMBRE"),

                ObtenerTexto(
                    registro,
                    "PROVINCIA_EJECUTORA_NOMBRE"),

                ObtenerTexto(
                    registro,
                    "DISTRITO_EJECUTORA_NOMBRE"),

                ObtenerTexto(
                    registro,
                    "SECTOR_NOMBRE")
            };

            var coincide =
                camposBusqueda.Any(
                    campo =>
                        !string.IsNullOrWhiteSpace(campo) &&
                        Normalizar(campo).Contains(
                            filtro,
                            StringComparison.OrdinalIgnoreCase));

            if (coincide)
            {
                resultado.Add(registro);
            }
        }

        return resultado;
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
            // Se interpreta como 0.
        }

        return 0;
    }

    // ============================================================
    // OBTENER TEXTO
    // ============================================================

    private static string ObtenerTexto(
        Dictionary<string, JsonElement> registro,
        string campo)
    {
        if (!registro.TryGetValue(
                campo,
                out var valor))
        {
            return "";
        }

        if (valor.ValueKind ==
            JsonValueKind.String)
        {
            return valor.GetString() ?? "";
        }

        return valor.ToString();
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
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        return texto
            .Trim()
            .ToUpperInvariant()
            .Normalize(NormalizationForm.FormD)
            .Where(
                c =>
                    CharUnicodeInfo.GetUnicodeCategory(c)
                    != UnicodeCategory.NonSpacingMark)
            .Aggregate(
                new StringBuilder(),
                (sb, c) => sb.Append(c))
            .ToString()
            .Normalize(NormalizationForm.FormC);
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
