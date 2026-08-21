using System.Net.Http.Json;
using System.Text.Json;
using src.Models;

namespace src.Services;

public sealed class MefService
{
    private readonly HttpClient _httpClient;

    private const string BaseUrl =
        "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/";
    
    // Resource ID del dataset de comparativo de gasto
    private const string ResourceId = "5f3b3cbe-3955-41cc-8662-1757ebb5cf53";

    public MefService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MefDataResponse?> SearchAsync(
        string resourceId,
        string? query = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"resource_id={Uri.EscapeDataString(resourceId)}",
            $"limit={limit}"
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            parameters.Add(
                $"q={Uri.EscapeDataString(query)}");
        }

        var url =
            $"{BaseUrl}datastore_search?{string.Join("&", parameters)}";

        return await _httpClient.GetFromJsonAsync<MefDataResponse>(
            url,
            cancellationToken);
    }

    public async Task<MefDataResponse?> ExecuteSqlAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"{BaseUrl}datastore_search_sql?sql={Uri.EscapeDataString(sql)}";

        return await _httpClient.GetFromJsonAsync<MefDataResponse>(
            url,
            cancellationToken);
    }

    // Método de conversión de EvolucionPresupuesto a PresupuestoResumen
    public List<PresupuestoResumen> ConvertirEvolucionAResumen(List<EvolucionPresupuesto> evolucion)
{
    return evolucion.Select(e => new PresupuestoResumen
    {
        Anio = e.Anio,
        Mes = "Anual",  // Como son datos anuales
        PIA = e.PresupuestoInicial,
        PIM = e.PresupuestoModificado,
        Ejecutado = e.MontoEjecutado,
        PorcentajeEjecucion = e.PorcentajeEjecucion
    }).ToList();
}

    // NUEVO MÉTODO: Obtener evolución del presupuesto
    public async Task<List<EvolucionPresupuesto>> ObtenerEvolucionAsync(
        string? entidad,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Primero intentamos obtener datos con SQL
            var sql = $@"
                SELECT 
                    KEY_VALUE,
                    EJECUTORA_NOMBRE,
                    NIVEL_GOBIERNO_NOMBRE,
                    DEPARTAMENTO_EJECUTORA_NOMBRE,
                    PROVINCIA_EJECUTORA_NOMBRE,
                    DISTRITO_EJECUTORA_NOMBRE,
                    MONTO_PIA_2012,
                    MONTO_PIM_2012,
                    MONTO_DEVENGADO_2012,
                    MONTO_PIA_2013,
                    MONTO_PIM_2013,
                    MONTO_DEVENGADO_2013,
                    MONTO_PIA_2014,
                    MONTO_PIM_2014,
                    MONTO_DEVENGADO_2014,
                    MONTO_PIA_2015,
                    MONTO_PIM_2015,
                    MONTO_DEVENGADO_2015,
                    MONTO_PIA_2016,
                    MONTO_PIM_2016,
                    MONTO_DEVENGADO_2016
                FROM 
                    ""{ResourceId}""
                WHERE 
                    1=1";

            // Agregar filtro por entidad si se proporciona
            if (!string.IsNullOrWhiteSpace(entidad))
            {
                sql += $" AND LOWER(EJECUTORA_NOMBRE) LIKE LOWER('%{entidad}%')";
            }

            sql += " LIMIT 1000";

            var response = await ExecuteSqlAsync(sql, cancellationToken);
            
            var result = new List<EvolucionPresupuesto>();

            // Verificar que la respuesta sea exitosa y tenga registros
            if (response?.Success == true && response.Result?.Records != null)
            {
                // Agrupar por año y sumar montos
                var evolucionPorAnio = new Dictionary<int, EvolucionPresupuesto>();

                foreach (var record in response.Result.Records)
                {
                    // Procesar años 2012-2016
                    for (int anio = 2012; anio <= 2016; anio++)
                    {
                        if (!evolucionPorAnio.ContainsKey(anio))
                        {
                            evolucionPorAnio[anio] = new EvolucionPresupuesto
                            {
                                Anio = anio,
                                Entidad = entidad ?? "Todas las entidades"
                            };
                        }

                        // Obtener valores para el año actual
                        var piaKey = $"MONTO_PIA_{anio}";
                        var pimKey = $"MONTO_PIM_{anio}";
                        var devengadoKey = $"MONTO_DEVENGADO_{anio}";

                        // Extraer montos del record
                        var pia = ObtenerDecimalDeRecord(record, piaKey);
                        var pim = ObtenerDecimalDeRecord(record, pimKey);
                        var devengado = ObtenerDecimalDeRecord(record, devengadoKey);

                        // Acumular montos
                        evolucionPorAnio[anio].PresupuestoInicial += pia;
                        evolucionPorAnio[anio].PresupuestoModificado += pim;
                        evolucionPorAnio[anio].MontoEjecutado += devengado;
                    }
                }

                result = evolucionPorAnio.Values
                    .OrderBy(x => x.Anio)
                    .ToList();

                // Calcular porcentajes de ejecución para cada año
                foreach (var item in result)
                {
                    item.PorcentajeEjecucion = item.PresupuestoModificado > 0
                        ? Math.Round((item.MontoEjecutado / item.PresupuestoModificado) * 100, 2)
                        : 0m;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            // Log del error
            Console.WriteLine($"Error al obtener evolución: {ex.Message}");
            return new List<EvolucionPresupuesto>();
        }
    }

    // Método alternativo usando SearchAsync
    public async Task<List<MefRecord>> ObtenerRegistrosPorEntidadAsync(
        string? entidad,
        int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = !string.IsNullOrWhiteSpace(entidad)
                ? $"EJECUTORA_NOMBRE:{entidad}"
                : null;

            var response = await SearchAsync(ResourceId, query, limit, cancellationToken);

            var records = new List<MefRecord>();

            if (response?.Success == true && response.Result?.Records != null)
            {
                foreach (var record in response.Result.Records)
                {
                    var mefRecord = new MefRecord();
                    
                    // Mapear propiedades del diccionario al objeto MefRecord
                    MapearPropiedad(record, "KEY_VALUE", v => mefRecord.KeyValue = v?.ToString());
                    MapearPropiedad(record, "EJECUTORA_NOMBRE", v => mefRecord.EjecutoraNombre = v?.ToString());
                    MapearPropiedad(record, "DEPARTAMENTO_EJECUTORA_NOMBRE", v => mefRecord.DepartamentoEjecutoraNombre = v?.ToString());
                    MapearPropiedad(record, "PROVINCIA_EJECUTORA_NOMBRE", v => mefRecord.ProvinciaEjecutoraNombre = v?.ToString());
                    MapearPropiedad(record, "DISTRITO_EJECUTORA_NOMBRE", v => mefRecord.DistritoEjecutoraNombre = v?.ToString());
                    
                    // Mapear montos para cada año
                    for (int anio = 2012; anio <= 2016; anio++)
                    {
                        var piaKey = $"MONTO_PIA_{anio}";
                        var pimKey = $"MONTO_PIM_{anio}";
                        var devengadoKey = $"MONTO_DEVENGADO_{anio}";
                        var giradoKey = $"MONTO_GIRADO_{anio}";
                        var certificadoKey = $"MONTO_CERTIFICADO_{anio}";
                        var comprometidoKey = $"MONTO_COMPROMETIDO_{anio}";

                        MapearPropiedad(record, piaKey, v => AsignarValorMefRecord(anio, "Pia", mefRecord, v));
                        MapearPropiedad(record, pimKey, v => AsignarValorMefRecord(anio, "Pim", mefRecord, v));
                        MapearPropiedad(record, devengadoKey, v => AsignarValorMefRecord(anio, "Devengado", mefRecord, v));
                        MapearPropiedad(record, giradoKey, v => AsignarValorMefRecord(anio, "Girado", mefRecord, v));
                        MapearPropiedad(record, certificadoKey, v => AsignarValorMefRecord(anio, "Certificado", mefRecord, v));
                        MapearPropiedad(record, comprometidoKey, v => AsignarValorMefRecord(anio, "Comprometido", mefRecord, v));
                    }

                    records.Add(mefRecord);
                }
            }

            return records;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener registros: {ex.Message}");
            return new List<MefRecord>();
        }
    }

    // Método auxiliar para obtener decimal de un record
    private decimal ObtenerDecimalDeRecord(Dictionary<string, JsonElement> record, string key)
    {
        if (record.TryGetValue(key, out var value) && value.ValueKind != JsonValueKind.Null)
        {
            if (decimal.TryParse(value.ToString(), out var result))
                return result;
        }
        return 0m;
    }

    private void MapearPropiedad(Dictionary<string, JsonElement> record, string key, Action<JsonElement?> setter)
    {
        if (record.TryGetValue(key, out var value) && value.ValueKind != JsonValueKind.Null)
        {
            setter(value);
        }
    }

    private void AsignarValorMefRecord(int anio, string propiedad, MefRecord record, JsonElement? valor)
    {
        if (!valor.HasValue || valor.Value.ValueKind == JsonValueKind.Null)
            return;

        var monto = decimal.TryParse(valor.Value.ToString(), out var result) ? result : 0m;
        
        switch (anio)
        {
            case 2012:
                if (propiedad == "Pia") record.Pia2012 = monto;
                else if (propiedad == "Pim") record.Pim2012 = monto;
                else if (propiedad == "Devengado") record.Devengado2012 = monto;
                else if (propiedad == "Girado") record.Girado2012 = monto;
                else if (propiedad == "Certificado") record.Certificado2012 = monto;
                else if (propiedad == "Comprometido") record.Comprometido2012 = monto;
                break;
            case 2013:
                if (propiedad == "Pia") record.Pia2013 = monto;
                else if (propiedad == "Pim") record.Pim2013 = monto;
                else if (propiedad == "Devengado") record.Devengado2013 = monto;
                else if (propiedad == "Girado") record.Girado2013 = monto;
                else if (propiedad == "Certificado") record.Certificado2013 = monto;
                else if (propiedad == "Comprometido") record.Comprometido2013 = monto;
                break;
            case 2014:
                if (propiedad == "Pia") record.Pia2014 = monto;
                else if (propiedad == "Pim") record.Pim2014 = monto;
                else if (propiedad == "Devengado") record.Devengado2014 = monto;
                else if (propiedad == "Girado") record.Girado2014 = monto;
                else if (propiedad == "Certificado") record.Certificado2014 = monto;
                else if (propiedad == "Comprometido") record.Comprometido2014 = monto;
                break;
            case 2015:
                if (propiedad == "Pia") record.Pia2015 = monto;
                else if (propiedad == "Pim") record.Pim2015 = monto;
                else if (propiedad == "Devengado") record.Devengado2015 = monto;
                else if (propiedad == "Girado") record.Girado2015 = monto;
                else if (propiedad == "Certificado") record.Certificado2015 = monto;
                else if (propiedad == "Comprometido") record.Comprometido2015 = monto;
                break;
            case 2016:
                if (propiedad == "Pia") record.Pia2016 = monto;
                else if (propiedad == "Pim") record.Pim2016 = monto;
                else if (propiedad == "Devengado") record.Devengado2016 = monto;
                else if (propiedad == "Girado") record.Girado2016 = monto;
                else if (propiedad == "Certificado") record.Certificado2016 = monto;
                else if (propiedad == "Comprometido") record.Comprometido2016 = monto;
                break;
        }
    }
}