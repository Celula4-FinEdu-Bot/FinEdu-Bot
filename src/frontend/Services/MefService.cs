using System.Globalization;
using System.Net;
using System.Text.Json;
using FinEduBot.Frontend.Models;
using FinEduBot.Frontend.Services.Interfaces;

namespace FinEduBot.Frontend.Services;

public sealed class MefService : IMeFService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MefService> _logger;

    private static readonly string[] AnioFields =
    [
        "ANIO",
        "AÑO",
        "YEAR",
        "ANO"
    ];

    private static readonly string[] MesFields =
    [
        "MES",
        "MONTH",
        "MES_NOMBRE"
    ];

    private static readonly string[] PresupuestoFields =
    [
        "PIM",
        "PIA",
        "PRESUPUESTO",
        "MONTO_PIM",
        "MONTO_PIA",
        "PRESUPUESTO_INSTITUCIONAL_MODIFICADO"
    ];

    private static readonly string[] EjecutadoFields =
    [
        "DEVENGADO",
        "EJECUTADO",
        "EJECUCION",
        "MONTO_DEVENGADO",
        "DEVENGADO_ACUMULADO"
    ];

    private static readonly string[] CategoriaFields =
    [
        "CATEGORIA",
        "CATEGORIA_GASTO",
        "GENERICA_NOMBRE",
        "GENERICA",
        "TIPO_GASTO",
        "PRODUCTO_NOMBRE",
        "PROGRAMA_PRESUPUESTAL"
    ];

    private static readonly string[] ProyectoFields =
    [
        "PROYECTO",
        "PROYECTO_NOMBRE",
        "NOMBRE_PROYECTO",
        "ACTIVIDAD_NOMBRE",
        "PRODUCTO_NOMBRE"
    ];

    public MefService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MefService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> ObtenerTotalRegistrosAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetDataAsync(
            limit: 1,
            offset: 0,
            cancellationToken);

        return response.Result.Total;
    }

    public async Task<IReadOnlyList<PresupuestoMensualDto>>
        ObtenerEvolucionMensualAsync(
            int? anio = null,
            CancellationToken cancellationToken = default)
    {
        var response = await GetDataAsync(
            limit: 1000,
            offset: 0,
            cancellationToken);

        var grouped = new Dictionary<(int Anio, int Mes), PresupuestoMensualDto>();

        foreach (var record in response.Result.Records)
        {
            var row = NormalizeRecord(record);

            var rowAnio = GetInt(row, AnioFields);

            if (anio.HasValue && rowAnio != anio.Value)
            {
                continue;
            }

            if (rowAnio <= 0)
            {
                continue;
            }

            var mes = GetMonth(row);

            if (mes <= 0 || mes > 12)
            {
                continue;
            }

            var presupuesto =
                GetDecimal(row, PresupuestoFields);

            var ejecutado =
                GetDecimal(row, EjecutadoFields);

            var key = (rowAnio, mes);

            if (!grouped.TryGetValue(key, out var current))
            {
                current = new PresupuestoMensualDto
                {
                    Anio = rowAnio,
                    Mes = mes,
                    NombreMes = CultureInfo
                        .GetCultureInfo("es-PE")
                        .DateTimeFormat
                        .GetMonthName(mes),

                    Presupuesto = 0,
                    Ejecutado = 0
                };

                grouped[key] = current;
            }

            current.Presupuesto += presupuesto;
            current.Ejecutado += ejecutado;
        }

        return grouped
            .Values
            .OrderBy(x => x.Anio)
            .ThenBy(x => x.Mes)
            .ToList();
    }

    public async Task<IReadOnlyList<ProyectoPresupuestoDto>>
        ObtenerProyectosAsync(
            int? anio = null,
            CancellationToken cancellationToken = default)
    {
        var response = await GetDataAsync(
            limit: 1000,
            offset: 0,
            cancellationToken);

        var grouped =
            new Dictionary<string, ProyectoPresupuestoDto>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var record in response.Result.Records)
        {
            var row = NormalizeRecord(record);

            var rowAnio = GetInt(row, AnioFields);

            if (anio.HasValue &&
                rowAnio != 0 &&
                rowAnio != anio.Value)
            {
                continue;
            }

            var categoria =
                GetString(row, CategoriaFields);

            var proyecto =
                GetString(row, ProyectoFields);

            if (string.IsNullOrWhiteSpace(categoria))
            {
                categoria = "Sin categoría";
            }

            if (string.IsNullOrWhiteSpace(proyecto))
            {
                proyecto = "Sin proyecto";
            }

            var presupuesto =
                GetDecimal(row, PresupuestoFields);

            var ejecutado =
                GetDecimal(row, EjecutadoFields);

            var key =
                $"{categoria}|{proyecto}";

            if (!grouped.TryGetValue(key, out var current))
            {
                current = new ProyectoPresupuestoDto
                {
                    Categoria = categoria,
                    Proyecto = proyecto
                };

                grouped[key] = current;
            }

            current.Presupuesto += presupuesto;
            current.Ejecutado += ejecutado;
        }

        return grouped
            .Values
            .Where(x =>
                x.Presupuesto > 0 ||
                x.Ejecutado > 0)
            .OrderByDescending(x => x.Ejecutado)
            .Take(20)
            .ToList();
    }

    private async Task<MefDataResponse> GetDataAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var client =
            _httpClientFactory.CreateClient("Mef");

        var resourceId =
            _configuration["Mef:ResourceId"];

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new InvalidOperationException(
                "Mef:ResourceId no está configurado.");
        }

        var url =
            $"datastore_search" +
            $"?resource_id={Uri.EscapeDataString(resourceId)}" +
            $"&limit={limit}" +
            $"&offset={offset}";

        _logger.LogInformation(
            "Consultando MEF. ResourceId={ResourceId}, Limit={Limit}, Offset={Offset}",
            resourceId,
            limit,
            offset);

        using var response =
            await client.GetAsync(
                url,
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "MEF respondió HTTP {StatusCode}: {Body}",
                response.StatusCode,
                body);

            throw new HttpRequestException(
                $"MEF respondió HTTP {(int)response.StatusCode} " +
                $"({response.StatusCode}).");
        }

        var result =
            JsonSerializer.Deserialize<MefDataResponse>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result is null)
        {
            throw new InvalidOperationException(
                "No fue posible interpretar la respuesta del MEF.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                "La API del MEF indicó que la consulta no fue exitosa.");
        }

        return result;
    }

    private static Dictionary<string, JsonElement>
        NormalizeRecord(
            Dictionary<string, JsonElement> record)
    {
        return record.ToDictionary(
            pair => NormalizeKey(pair.Key),
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeKey(
        string key)
    {
        return key
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "_")
            .Replace("-", "_");
    }

    private static string GetString(
        Dictionary<string, JsonElement> row,
        IEnumerable<string> fields)
    {
        foreach (var field in fields)
        {
            var key = NormalizeKey(field);

            if (!row.TryGetValue(key, out var value))
            {
                continue;
            }

            var result =
                JsonElementToString(value);

            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }

        return string.Empty;
    }

    private static int GetInt(
        Dictionary<string, JsonElement> row,
        IEnumerable<string> fields)
    {
        var value =
            GetString(row, fields);

        if (int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var result))
        {
            return result;
        }

        if (decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var decimalResult))
        {
            return (int)decimalResult;
        }

        return 0;
    }

    private static decimal GetDecimal(
        Dictionary<string, JsonElement> row,
        IEnumerable<string> fields)
    {
        foreach (var field in fields)
        {
            var key = NormalizeKey(field);

            if (!row.TryGetValue(key, out var value))
            {
                continue;
            }

            var text =
                JsonElementToString(value);

            if (TryParseDecimal(text, out var result))
            {
                return result;
            }
        }

        return 0;
    }

    private static bool TryParseDecimal(
        string value,
        out decimal result)
    {
        value = value.Trim();

        if (decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out result))
        {
            return true;
        }

        if (decimal.TryParse(
            value,
            NumberStyles.Any,
            new CultureInfo("es-PE"),
            out result))
        {
            return true;
        }

        result = 0;
        return false;
    }

    private static int GetMonth(
        Dictionary<string, JsonElement> row)
    {
        foreach (var field in MesFields)
        {
            var key = NormalizeKey(field);

            if (!row.TryGetValue(key, out var value))
            {
                continue;
            }

            var text =
                JsonElementToString(value)
                    .Trim()
                    .ToLowerInvariant();

            if (int.TryParse(
                text,
                out var numericMonth) &&
                numericMonth >= 1 &&
                numericMonth <= 12)
            {
                return numericMonth;
            }

            var months =
                CultureInfo.GetCultureInfo("es-PE")
                    .DateTimeFormat
                    .MonthNames;

            for (var index = 0;
                 index < 12;
                 index++)
            {
                if (months[index]
                    .StartsWith(
                        text,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return index + 1;
                }
            }
        }

        return 0;
    }

    private static string JsonElementToString(
        JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String =>
                value.GetString() ?? string.Empty,

            JsonValueKind.Number =>
                value.ToString(),

            JsonValueKind.True =>
                "true",

            JsonValueKind.False =>
                "false",

            JsonValueKind.Null =>
                string.Empty,

            _ =>
                value.ToString()
        };
    }
}