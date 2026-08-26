using System.Globalization;
using System.Text.Json;
using src.Models;

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

    public MefService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.Timeout =
            TimeSpan.FromSeconds(120);
    }

    // ============================================================
    // OBTENER EVOLUCIÓN
    // ============================================================

    public async Task<List<EvolucionPresupuesto>> ObtenerEvolucionAsync(
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

            // Se continúa: intentaremos muestrear el dataset para identificar registros
        }

        var entidad =
            ResolverEntidad(filtro);

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
                $"No se pudo resolver el PLIEGO para '{filtro}'.");

            Console.WriteLine("Se intentará muestrear el dataset sin filtro de PLIEGO.");
            // Continuar: BuscarRegistrosAsync soporta entidad.Pliego vacío y consultará el dataset sin filters.
        }

        // ========================================================
        // CONSULTA AL DATASET
        // ========================================================

        var registros =
            await BuscarRegistrosAsync(
                Resource2022_2026,
                entidad,
                cancellationToken);

        Console.WriteLine();
        Console.WriteLine(
            $"Registros recibidos del MEF: {registros.Count}");

        if (registros.Count == 0)
        {
            Console.WriteLine(
                "No se encontraron registros para la entidad.");

            return [];
        }

        // ========================================================
        // MOSTRAR ESTRUCTURA REAL DEL DATASET
        // ========================================================

        MostrarEstructura(registros);

        // ========================================================
        // CONSTRUIR EVOLUCIÓN
        // ========================================================

        var evolucion =
            ConstruirEvolucion(
                registros,
                entidad.Nombre);

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
                $"DEVENGADO={item.Devengado:N2}");
        }

        return evolucion;
    }

    // ============================================================
    // BUSCAR REGISTROS
    // ============================================================

    private async Task<List<Dictionary<string, JsonElement>>>
        BuscarRegistrosAsync(
            string resourceId,
            MefEntidad entidad,
            CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("MEF - DATASTORE SEARCH");
            Console.WriteLine($"Dataset: {resourceId}");
            Console.WriteLine($"PLIEGO: {entidad.Pliego}");
            Console.WriteLine($"Entidad: {entidad.Nombre}");
            Console.WriteLine("--------------------------------------");

            // ====================================================
            // PRIMERA CONSULTA:
            // usar filters con PLIEGO si está disponible, sino solicitar una muestra del dataset
            // ====================================================

            string url;

            if (!string.IsNullOrWhiteSpace(entidad.Pliego))
            {
                var filtros = new Dictionary<string, string>
                {
                    ["PLIEGO"] = entidad.Pliego
                };

                var filtersJson =
                    JsonSerializer.Serialize(filtros);

                url =
                    $"{BaseUrl}datastore_search" +
                    $"?resource_id={Uri.EscapeDataString(resourceId)}" +
                    $"&filters={Uri.EscapeDataString(filtersJson)}" +
                    $"&limit=1000";
            }
            else
            {
                // Sin PLIEGO: obtener una muestra corta para inspección manual
                url = $"{BaseUrl}datastore_search" +
                      $"?resource_id={Uri.EscapeDataString(resourceId)}" +
                      $"&limit=50";
            }

            Console.WriteLine(
                $"URL MEF: {url}");

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
                    "MEF respondió con error:");

                Console.WriteLine(json);

                return [];
            }

            Console.WriteLine(
                $"Respuesta MEF recibida: {json.Length} caracteres.");

            var registros =
                ExtraerRegistros(json);

            Console.WriteLine(
                $"Registros extraídos: {registros.Count}");

            // ====================================================
            // SEGUNDO INTENTO:
            // q sobre el PLIEGO
            // ====================================================

            if (registros.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "No hubo resultados usando filters.");
                Console.WriteLine(
                    "Intentando búsqueda mediante q...");

                var urlQ =
                    $"{BaseUrl}datastore_search" +
                    $"?resource_id={Uri.EscapeDataString(resourceId)}" +
                    $"&q={Uri.EscapeDataString(entidad.Pliego)}" +
                    $"&limit=1000";

                Console.WriteLine(
                    $"URL MEF q: {urlQ}");

                using var responseQ =
                    await _httpClient.GetAsync(
                        urlQ,
                        cancellationToken);

                var jsonQ =
                    await responseQ.Content.ReadAsStringAsync(
                        cancellationToken);

                Console.WriteLine(
                    $"HTTP MEF q: {(int)responseQ.StatusCode}");

                if (responseQ.IsSuccessStatusCode)
                {
                    registros =
                        ExtraerRegistros(jsonQ);

                    Console.WriteLine(
                        $"Registros encontrados con q: {registros.Count}");
                }
            }

            // ====================================================
            // FILTRO LOCAL
            // ====================================================

            var filtrados =
                FiltrarPorEntidad(
                    registros,
                    entidad);

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
                return resultado;
            }

            using var document =
                JsonDocument.Parse(json);

            var root =
                document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                Console.WriteLine(
                    $"Raíz JSON inesperada: {root.ValueKind}");

                return resultado;
            }

            if (!root.TryGetProperty(
                    "records",
                    out var records))
            {
                Console.WriteLine(
                    "La respuesta MEF no contiene 'records'.");

                return resultado;
            }

            if (records.ValueKind == JsonValueKind.Null ||
                records.ValueKind == JsonValueKind.Undefined)
            {
                Console.WriteLine(
                    "MEF devolvió records=null.");

                if (root.TryGetProperty(
                        "result",
                        out var result))
                {
                    Console.WriteLine(
                        $"MEF result: {result}");
                }

                return resultado;
            }

            if (records.ValueKind != JsonValueKind.Array)
            {
                Console.WriteLine(
                    $"records tiene tipo {records.ValueKind}.");

                return resultado;
            }

            foreach (var record in records.EnumerateArray())
            {
                if (record.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var diccionario =
                    new Dictionary<string, JsonElement>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (var propiedad
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
    // FILTRAR ENTIDAD
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        FiltrarPorEntidad(
            List<Dictionary<string, JsonElement>> registros,
            MefEntidad entidad)
    {
        if (registros.Count == 0)
        {
            return [];
        }

        var resultado =
            new List<Dictionary<string, JsonElement>>();

        foreach (var registro in registros)
        {
            var pliego =
                ObtenerTexto(registro, "PLIEGO");

            var nombre =
                ObtenerTexto(registro, "PLIEGO_NOMBRE");

            var ejecutora =
                ObtenerTexto(registro, "EJECUTORA_NOMBRE");

            var coincideCodigo =
                NormalizarCodigo(pliego) ==
                NormalizarCodigo(entidad.Pliego);

            var coincideNombre =
                ContieneEntidad(
                    nombre,
                    entidad.Nombre);

            var coincideEjecutora =
                ContieneEntidad(
                    ejecutora,
                    entidad.Nombre);

            if (
                coincideCodigo ||
                coincideNombre ||
                coincideEjecutora)
            {
                resultado.Add(registro);
            }
        }

        return resultado;
    }

    // ============================================================
    // CONSTRUIR EVOLUCIÓN
    // ============================================================

    private static List<EvolucionPresupuesto>
        ConstruirEvolucion(
            List<Dictionary<string, JsonElement>> registros,
            string entidad)
    {
        var resultado =
            new List<EvolucionPresupuesto>();

        // ========================================================
        // Detectamos automáticamente el campo del año
        // ========================================================

        foreach (var grupo in registros)
        {
            var anio =
                ObtenerAnio(grupo);

            if (anio < 2022 || anio > 2026)
            {
                continue;
            }

            var pia =
                ObtenerCampoPresupuesto(
                    grupo,
                    "PIA");

            var pim =
                ObtenerCampoPresupuesto(
                    grupo,
                    "PIM");

            var devengado =
                ObtenerCampoPresupuesto(
                    grupo,
                    "DEVENGADO");

            var existente =
                resultado.FirstOrDefault(
                    x => x.Anio == anio);

            if (existente == null)
            {
                existente =
                    new EvolucionPresupuesto
                    {
                        Anio = anio,
                        Entidad = entidad
                    };

                resultado.Add(existente);
            }

            existente.Pia += pia;
            existente.Pim += pim;
            existente.Devengado += devengado;
        }

        foreach (var item in resultado)
        {
            if (item.Pim > 0)
            {
                item.PorcentajeEjecucion =
                    item.Devengado /
                    item.Pim *
                    100;
            }
        }

        return resultado
            .OrderBy(x => x.Anio)
            .ToList();
    }

    // ============================================================
    // OBTENER AÑO
    // ============================================================

    private static int ObtenerAnio(
        Dictionary<string, JsonElement> registro)
    {
        string[] campos =
        [
            "ANIO",
            "AÑO",
            "ANO",
            "ANO_EJE",
            "ANIO_EJE",
            "AÑO_EJE",
            "EJERCICIO",
            "PERIODO",
            "YEAR"
        ];

        foreach (var campo in campos)
        {
            var texto =
                ObtenerTexto(
                    registro,
                    campo);

            if (int.TryParse(
                    texto,
                    out var anio))
            {
                return anio;
            }
        }

        // Buscar cualquier campo cuyo valor sea un año.
        foreach (var propiedad in registro)
        {
            var texto =
                ObtenerTexto(
                    registro,
                    propiedad.Key);

            if (int.TryParse(
                    texto,
                    out var anio) &&
                anio >= 2012 &&
                anio <= 2030)
            {
                return anio;
            }
        }

        return 0;
    }

    // ============================================================
    // OBTENER CAMPO PRESUPUESTARIO
    // ============================================================

    private static decimal ObtenerCampoPresupuesto(
        Dictionary<string, JsonElement> registro,
        string nombre)
    {
        var candidatos =
            registro.Keys
                .Where(k =>
                    Normalizar(k)
                        .Contains(
                            Normalizar(nombre),
                            StringComparison.OrdinalIgnoreCase))
                .ToList();

        foreach (var campo in candidatos)
        {
            var valor =
                ConvertirDecimal(
                    registro[campo]);

            if (valor != 0)
            {
                return valor;
            }
        }

        return 0;
    }

    // ============================================================
    // MOSTRAR ESTRUCTURA
    // ============================================================

    private static void MostrarEstructura(
        List<Dictionary<string, JsonElement>> registros)
    {
        if (registros.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("--------------------------------------");
        Console.WriteLine("MEF - CAMPOS REALES DEL DATASET");
        Console.WriteLine("--------------------------------------");

        foreach (var campo in registros[0].Keys)
        {
            Console.WriteLine($"Campo: {campo}");
        }

        Console.WriteLine("--------------------------------------");
    }

    // ============================================================
    // RESOLVER ENTIDAD
    // ============================================================

    private static MefEntidad ResolverEntidad(
        string filtro)
    {
        var normalizado =
            Normalizar(filtro);

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

        if (
            normalizado.Contains(
                "MINISTERIO DE ECONOMIA") ||
            normalizado.Contains(
                "ECONOMIA Y FINANZAS"))
        {
            return new MefEntidad
            {
                Pliego = "009",
                Nombre =
                    "M. DE ECONOMIA Y FINANZAS"
            };
        }

        if (
            normalizado.Contains(
                "MINISTERIO DEL INTERIOR") ||
            normalizado.Contains(
                "MINISTERIO DE INTERIOR") ||
            normalizado == "INTERIOR")
        {
            return new MefEntidad
            {
                Pliego = "007",
                Nombre =
                    "M. DEL INTERIOR"
            };
        }

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

        return new MefEntidad
        {
            Pliego = "",
            Nombre = filtro
        };
    }

    // ============================================================
    // CONTIENE ENTIDAD
    // ============================================================

    private static bool ContieneEntidad(
        string? valor,
        string entidad)
    {
        if (
            string.IsNullOrWhiteSpace(valor) ||
            string.IsNullOrWhiteSpace(entidad))
        {
            return false;
        }

        var valorNormalizado =
            Normalizar(valor);

        var entidadNormalizada =
            Normalizar(entidad);

        return
            valorNormalizado.Contains(
                entidadNormalizada,
                StringComparison.OrdinalIgnoreCase)
            ||
            (
                entidadNormalizada == "M DE DEFENSA" &&
                valorNormalizado.Contains(
                    "DEFENSA",
                    StringComparison.OrdinalIgnoreCase)
            );
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

        try
        {
            if (valor.ValueKind ==
                JsonValueKind.String)
            {
                return valor.GetString() ?? "";
            }

            if (valor.ValueKind ==
                JsonValueKind.Number)
            {
                return valor.ToString();
            }

            return "";
        }
        catch
        {
            return "";
        }
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

                if (valor.TryGetDouble(
                        out var numeroDouble))
                {
                    return (decimal)numeroDouble;
                }
            }

            if (valor.ValueKind ==
                JsonValueKind.String)
            {
                var texto =
                    valor.GetString();

                if (string.IsNullOrWhiteSpace(
                        texto))
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

                if (decimal.TryParse(
                        texto,
                        NumberStyles.Any,
                        CultureInfo.GetCultureInfo("es-PE"),
                        out numero))
                {
                    return numero;
                }
            }
        }
        catch
        {
        }

        return 0;
    }

    // ============================================================
    // NORMALIZAR CÓDIGO
    // ============================================================

    private static string NormalizarCodigo(
        string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return "";
        }

        var limpio =
            codigo.Trim();

        if (int.TryParse(
                limpio,
                out var numero))
        {
            return numero.ToString();
        }

        return limpio;
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
        return string.IsNullOrWhiteSpace(filtro)
            ? ""
            : filtro.Trim();
    }

    // ============================================================
    // ENTIDAD
    // ============================================================

    private sealed class MefEntidad
    {
        public string Pliego { get; set; } = "";

        public string Nombre { get; set; } = "";
    }
}