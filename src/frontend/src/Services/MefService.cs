using System.Globalization;
using System.Text.Json;
using src.Models;

namespace src.Services;

public sealed class MefService
{
    private readonly HttpClient _httpClient;

    private const string BaseUrl =
        "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/";

    private const string ResourceId2017_2021 =
        "0e2469d8-5872-4bc2-a5bc-91ee01c99df8";

    private const int DefaultPageSize = 20;

    public MefService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.Timeout =
            TimeSpan.FromSeconds(120);
    }

    // ============================================================
    // CONSULTA NORMAL - COMPATIBILIDAD
    // ============================================================

    public async Task<List<EvolucionPresupuesto>>
        ObtenerEvolucionAsync(
            string? filtro = null,
            CancellationToken cancellationToken = default)
    {
        var pagina =
            await ObtenerEvolucionPaginaAsync(
                filtro,
                1,
                DefaultPageSize,
                cancellationToken);

        return pagina.Records;
    }

    // ============================================================
    // CONSULTA PAGINADA
    // ============================================================

    public async Task<MefPageResult>
        ObtenerEvolucionPaginaAsync(
            string? filtro,
            int pagina,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        if (pageSize < 1)
        {
            pageSize = DefaultPageSize;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        filtro =
            LimpiarFiltro(filtro);

        var offset =
            (pagina - 1) * pageSize;

        Console.WriteLine();
        Console.WriteLine(
            "==========================================");
        Console.WriteLine(
            "MEF - CONSULTA 2017-2021");
        Console.WriteLine(
            $"Filtro: '{filtro}'");
        Console.WriteLine(
            $"Página: {pagina}");
        Console.WriteLine(
            $"Tamaño página: {pageSize}");
        Console.WriteLine(
            $"Offset: {offset}");
        Console.WriteLine(
            $"Resource ID: {ResourceId2017_2021}");
        Console.WriteLine(
            "==========================================");

        try
        {
            var url =
                $"{BaseUrl}datastore_search" +
                $"?resource_id={Uri.EscapeDataString(ResourceId2017_2021)}" +
                $"&limit={pageSize}" +
                $"&offset={offset}";

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                url +=
                    $"&q={Uri.EscapeDataString(filtro)}";
            }

            Console.WriteLine();
            Console.WriteLine(
                "------------------------------------------");
            Console.WriteLine(
                "MEF - DATASTORE SEARCH");
            Console.WriteLine(
                "------------------------------------------");

            Console.WriteLine(url);

            using var response =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            var json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            Console.WriteLine();
            Console.WriteLine(
                $"HTTP MEF: {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    "Respuesta de error del MEF:");

                Console.WriteLine(json);

                return new MefPageResult
                {
                    Records = [],
                    Total = 0,
                    Page = pagina,
                    PageSize = pageSize
                };
            }

            using var document =
                JsonDocument.Parse(json);

            var root =
                document.RootElement;

            var registros =
                ExtraerRegistros(
                    root);

            var total =
                ExtraerTotal(
                    root,
                    registros.Count,
                    offset);

            Console.WriteLine(
                $"Registros recibidos: {registros.Count}");

            Console.WriteLine(
                $"Total reportado por MEF: {total}");

            if (registros.Count > 0)
            {
                MostrarClavesPresupuesto(
                    registros[0]);

                MostrarValoresPresupuesto(
                    registros[0]);
            }

            var resultados =
                registros
                    .Select(MapearRegistro)
                    .Where(x => x != null)
                    .Select(x => x!)
                    .ToList();

            Console.WriteLine(
                $"Registros convertidos: {resultados.Count}");

            return new MefPageResult
            {
                Records =
                    resultados,

                Total =
                    total,

                Page =
                    pagina,

                PageSize =
                    pageSize
            };
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                "La consulta al MEF fue cancelada.");

            throw;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(
                $"Error HTTP MEF: {ex.Message}");

            return new MefPageResult
            {
                Records = [],
                Total = 0,
                Page = pagina,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error procesando MEF: {ex}");

            return new MefPageResult
            {
                Records = [],
                Total = 0,
                Page = pagina,
                PageSize = pageSize
            };
        }
    }

    // ============================================================
    // TOTAL
    // ============================================================

    private static int ExtraerTotal(
        JsonElement root,
        int cantidadPagina,
        int offset)
    {
        if (
            root.TryGetProperty(
                "result",
                out var result) &&
            result.ValueKind ==
                JsonValueKind.Object)
        {
            if (
                result.TryGetProperty(
                    "total",
                    out var total) &&
                total.ValueKind ==
                    JsonValueKind.Number &&
                total.TryGetInt32(
                    out var totalInt))
            {
                return totalInt;
            }
        }

        if (
            root.TryGetProperty(
                "result",
                out result) &&
            result.ValueKind ==
                JsonValueKind.Object &&
            result.TryGetProperty(
                "count",
                out var count) &&
            count.ValueKind ==
                JsonValueKind.Number &&
            count.TryGetInt32(
                out var countInt))
        {
            return offset + countInt;
        }

        return offset + cantidadPagina;
    }

    // ============================================================
    // MOSTRAR CLAVES
    // ============================================================

    private static void MostrarClavesPresupuesto(
        Dictionary<string, JsonElement> registro)
    {
        Console.WriteLine();
        Console.WriteLine(
            "------------------------------------------");
        Console.WriteLine(
            "MEF - CLAVES PRESUPUESTARIAS RECIBIDAS");
        Console.WriteLine(
            "------------------------------------------");

        foreach (var clave in registro.Keys)
        {
            var nombre =
                clave.ToUpperInvariant();

            if (
                nombre.Contains("PIA") ||
                nombre.Contains("PIM") ||
                nombre.Contains("DEVENGADO") ||
                nombre.Contains("GIRADO") ||
                nombre.Contains("CERTIFICADO") ||
                nombre.Contains("COMPROMETIDO"))
            {
                Console.WriteLine(
                    $"[{clave}]");
            }
        }
    }

    // ============================================================
    // MOSTRAR VALORES CRUDOS
    // ============================================================

    private static void MostrarValoresPresupuesto(
        Dictionary<string, JsonElement> registro)
    {
        Console.WriteLine();
        Console.WriteLine(
            "------------------------------------------");
        Console.WriteLine(
            "MEF - VALORES PRESUPUESTARIOS RECIBIDOS");
        Console.WriteLine(
            "------------------------------------------");

        var claves =
            registro.Keys
                .Where(
                    k =>
                    k.Contains(
                        "PIA",
                        StringComparison.OrdinalIgnoreCase) ||
                    k.Contains(
                        "PIM",
                        StringComparison.OrdinalIgnoreCase) ||
                    k.Contains(
                        "DEVENGADO",
                        StringComparison.OrdinalIgnoreCase))
                .Take(15);

        foreach (var clave in claves)
        {
            Console.WriteLine(
                $"{clave} = {registro[clave]}");
        }
    }

    // ============================================================
    // MAPEAR
    // ============================================================

    private static EvolucionPresupuesto?
        MapearRegistro(
            Dictionary<string, JsonElement> registro)
    {
        if (registro.Count == 0)
        {
            return null;
        }

        return new EvolucionPresupuesto
        {
            // ====================================================
            // IDENTIFICACIÓN
            // ====================================================

            KeyValue =
                ObtenerTexto(
                    registro,
                    "KEY_VALUE"),

            NivelGobierno =
                ObtenerTexto(
                    registro,
                    "NIVEL_GOBIERNO"),

            NivelGobiernoNombre =
                ObtenerTexto(
                    registro,
                    "NIVEL_GOBIERNO_NOMBRE"),

            Sector =
                ObtenerTexto(
                    registro,
                    "SECTOR"),

            SectorNombre =
                ObtenerTexto(
                    registro,
                    "SECTOR_NOMBRE"),

            Pliego =
                ObtenerTexto(
                    registro,
                    "PLIEGO"),

            PliegoNombre =
                ObtenerTexto(
                    registro,
                    "PLIEGO_NOMBRE"),

            Ejecutora =
                ObtenerTexto(
                    registro,
                    "EJECUTORA"),

            EjecutoraNombre =
                ObtenerTexto(
                    registro,
                    "EJECUTORA_NOMBRE"),

            SecEjecut =
                ObtenerTexto(
                    registro,
                    "SEC_EJEC"),

            DepartamentoEjecutora =
                ObtenerTexto(
                    registro,
                    "DEPARTAMENTO_EJECUTORA"),

            DepartamentoEjecutoraNombre =
                ObtenerTexto(
                    registro,
                    "DEPARTAMENTO_EJECUTORA_NOMBRE"),

            ProvinciaEjecutora =
                ObtenerTexto(
                    registro,
                    "PROVINCIA_EJECUTORA"),

            ProvinciaEjecutoraNombre =
                ObtenerTexto(
                    registro,
                    "PROVINCIA_EJECUTORA_NOMBRE"),

            DistritoEjecutora =
                ObtenerTexto(
                    registro,
                    "DISTRITO_EJECUTORA"),

            DistritoEjecutoraNombre =
                ObtenerTexto(
                    registro,
                    "DISTRITO_EJECUTORA_NOMBRE"),

            // ====================================================
            // PROGRAMA
            // ====================================================

            ProgramaPpto =
                ObtenerTexto(
                    registro,
                    "PROGRAMA_PPTO",
                    "PROGRAMA_PPT0"),

            ProgramaPptoNombre =
                ObtenerTexto(
                    registro,
                    "PROGRAMA_PPTO_NOMBRE"),

            TipoActProy =
                ObtenerTexto(
                    registro,
                    "TIPO_ACT_PROY"),

            TipoActProyNombre =
                ObtenerTexto(
                    registro,
                    "TIPO_ACT_PROY_NOMBRE"),

            ProductoProyecto =
                ObtenerTexto(
                    registro,
                    "PRODUCTO_PROYECTO"),

            ProductoProyectoNombre =
                ObtenerTexto(
                    registro,
                    "PRODUCTO_PROYECTO_NOMBRE"),

            ActividadAccionObra =
                ObtenerTexto(
                    registro,
                    "ACTIVIDAD_ACCION_OBRA"),

            ActividadAccionObraNombre =
                ObtenerTexto(
                    registro,
                    "ACTIVIDAD_ACCION_OBRA_NOMBRE"),

            // ====================================================
            // CLASIFICACIÓN
            // ====================================================

            Funcion =
                ObtenerTexto(
                    registro,
                    "FUNCION"),

            FuncionNombre =
                ObtenerTexto(
                    registro,
                    "FUNCION_NOMBRE"),

            DivisionFuncional =
                ObtenerTexto(
                    registro,
                    "DIVISION_FUNCIONAL"),

            DivisionFuncionalNombre =
                ObtenerTexto(
                    registro,
                    "DIVISION_FUNCIONAL_NOMBRE"),

            GrupoFuncional =
                ObtenerTexto(
                    registro,
                    "GRUPO_FUNCIONAL"),

            GrupoFuncionalNombre =
                ObtenerTexto(
                    registro,
                    "GRUPO_FUNCIONAL_NOMBRE"),

            Meta =
                ObtenerTexto(
                    registro,
                    "META"),

            MetaNombre =
                ObtenerTexto(
                    registro,
                    "META_NOMBRE"),

            DepartamentoMeta =
                ObtenerTexto(
                    registro,
                    "DEPARTAMENTO_META"),

            DepartamentoMetaNombre =
                ObtenerTexto(
                    registro,
                    "DEPARTAMENTO_META_NOMBRE"),

            // ====================================================
            // FINANCIAMIENTO
            // ====================================================

            FuenteFinanciamiento =
                ObtenerTexto(
                    registro,
                    "FUENTE_FINANCIAMIENTO"),

            FuenteFinanciamientoNombre =
                ObtenerTexto(
                    registro,
                    "FUENTE_FINANCIAMIENTO_NOMBRE"),

            Rubro =
                ObtenerTexto(
                    registro,
                    "RUBRO"),

            RubroNombre =
                ObtenerTexto(
                    registro,
                    "RUBRO_NOMBRE"),

            TipoRecurso =
                ObtenerTexto(
                    registro,
                    "TIPO_RECURSO"),

            TipoRecursoNombre =
                ObtenerTexto(
                    registro,
                    "TIPO_RECURSO_NOMBRE"),

            // ====================================================
            // GASTO
            // ====================================================

            CategoriaGasto =
                ObtenerNullableInt(
                    registro,
                    "CATEGORIA_GASTO"),

            CategoriaGastoNombre =
                ObtenerTexto(
                    registro,
                    "CATEGORIA_GASTO_NOMBRE"),

            TipoTransaccion =
                ObtenerNullableInt(
                    registro,
                    "TIPO_TRANSACCION"),

            Generica =
                ObtenerNullableInt(
                    registro,
                    "GENERICA"),

            GenericaNombre =
                ObtenerTexto(
                    registro,
                    "GENERICA_NOMBRE"),

            Subgenerica =
                ObtenerNullableInt(
                    registro,
                    "SUBGENERICA"),

            SubgenericaNombre =
                ObtenerTexto(
                    registro,
                    "SUBGENERICA_NOMBRE"),

            SubgenericaDet =
                ObtenerNullableInt(
                    registro,
                    "SUBGENERICA_DET"),

            SubgenericaDetNombre =
                ObtenerTexto(
                    registro,
                    "SUBGENERICA_DET_NOMBRE"),

            Especifica =
                ObtenerNullableInt(
                    registro,
                    "ESPECIFICA"),

            EspecificaNombre =
                ObtenerTexto(
                    registro,
                    "ESPECIFICA_NOMBRE"),

            EspecificaDet =
                ObtenerNullableInt(
                    registro,
                    "ESPECIFICA_DET"),

            EspecificaDetNombre =
                ObtenerTexto(
                    registro,
                    "ESPECIFICA_DET_NOMBRE"),

            // ====================================================
            // 2017
            // ====================================================

            Pia2017 =
                ObtenerDecimal(
                    registro,
                    "PIA_2017",
                    "MONTO_PIA_2017",
                    "PIA2017"),

            Pim2017 =
                ObtenerDecimal(
                    registro,
                    "PIM_2017",
                    "MONTO_PIM_2017",
                    "PIM2017"),

            Certificado2017 =
                ObtenerDecimal(
                    registro,
                    "CERTIFICADO_2017",
                    "MONTO_CERTIFICADO_2017",
                    "CERTIFICADO2017"),

            ComprometidoAnual2017 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_ANUAL_2017",
                    "MONTO_COMPROMETIDO_ANUAL_2017",
                    "COMPROMETIDOANUAL2017"),

            Comprometido2017 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_2017",
                    "MONTO_COMPROMETIDO_2017",
                    "COMPROMETIDO2017"),

            Devengado2017 =
                ObtenerDecimal(
                    registro,
                    "DEVENGADO_2017",
                    "MONTO_DEVENGADO_2017",
                    "DEVENGADO2017"),

            Girado2017 =
                ObtenerDecimal(
                    registro,
                    "GIRADO_2017",
                    "MONTO_GIRADO_2017",
                    "GIRADO2017"),

            // ====================================================
            // 2018
            // ====================================================

            Pia2018 =
                ObtenerDecimal(
                    registro,
                    "PIA_2018",
                    "MONTO_PIA_2018",
                    "PIA2018"),

            Pim2018 =
                ObtenerDecimal(
                    registro,
                    "PIM_2018",
                    "MONTO_PIM_2018",
                    "PIM2018"),

            Certificado2018 =
                ObtenerDecimal(
                    registro,
                    "CERTIFICADO_2018",
                    "MONTO_CERTIFICADO_2018",
                    "CERTIFICADO2018"),

            ComprometidoAnual2018 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_ANUAL_2018",
                    "MONTO_COMPROMETIDO_ANUAL_2018",
                    "COMPROMETIDOANUAL2018"),

            Comprometido2018 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_2018",
                    "MONTO_COMPROMETIDO_2018",
                    "COMPROMETIDO2018"),

            Devengado2018 =
                ObtenerDecimal(
                    registro,
                    "DEVENGADO_2018",
                    "MONTO_DEVENGADO_2018",
                    "DEVENGADO2018"),

            Girado2018 =
                ObtenerDecimal(
                    registro,
                    "GIRADO_2018",
                    "MONTO_GIRADO_2018",
                    "GIRADO2018"),

            // ====================================================
            // 2019
            // ====================================================

            Pia2019 =
                ObtenerDecimal(
                    registro,
                    "PIA_2019",
                    "MONTO_PIA_2019",
                    "PIA2019"),

            Pim2019 =
                ObtenerDecimal(
                    registro,
                    "PIM_2019",
                    "MONTO_PIM_2019",
                    "PIM2019"),

            Certificado2019 =
                ObtenerDecimal(
                    registro,
                    "CERTIFICADO_2019",
                    "MONTO_CERTIFICADO_2019",
                    "CERTIFICADO2019"),

            ComprometidoAnual2019 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_ANUAL_2019",
                    "MONTO_COMPROMETIDO_ANUAL_2019",
                    "COMPROMETIDOANUAL2019"),

            Comprometido2019 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_2019",
                    "MONTO_COMPROMETIDO_2019",
                    "COMPROMETIDO2019"),

            Devengado2019 =
                ObtenerDecimal(
                    registro,
                    "DEVENGADO_2019",
                    "MONTO_DEVENGADO_2019",
                    "DEVENGADO2019"),

            Girado2019 =
                ObtenerDecimal(
                    registro,
                    "GIRADO_2019",
                    "MONTO_GIRADO_2019",
                    "GIRADO2019"),

            // ====================================================
            // 2020
            // ====================================================

            Pia2020 =
                ObtenerDecimal(
                    registro,
                    "PIA_2020",
                    "MONTO_PIA_2020",
                    "PIA2020"),

            Pim2020 =
                ObtenerDecimal(
                    registro,
                    "PIM_2020",
                    "MONTO_PIM_2020",
                    "PIM2020"),

            Certificado2020 =
                ObtenerDecimal(
                    registro,
                    "CERTIFICADO_2020",
                    "MONTO_CERTIFICADO_2020",
                    "CERTIFICADO2020"),

            ComprometidoAnual2020 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_ANUAL_2020",
                    "MONTO_COMPROMETIDO_ANUAL_2020",
                    "COMPROMETIDOANUAL2020"),

            Comprometido2020 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_2020",
                    "MONTO_COMPROMETIDO_2020",
                    "COMPROMETIDO2020"),

            Devengado2020 =
                ObtenerDecimal(
                    registro,
                    "DEVENGADO_2020",
                    "MONTO_DEVENGADO_2020",
                    "DEVENGADO2020"),

            Girado2020 =
                ObtenerDecimal(
                    registro,
                    "GIRADO_2020",
                    "MONTO_GIRADO_2020",
                    "GIRADO2020"),

            // ====================================================
            // 2021
            // ====================================================

            Pia2021 =
                ObtenerDecimal(
                    registro,
                    "PIA_2021",
                    "MONTO_PIA_2021",
                    "PIA2021"),

            Pim2021 =
                ObtenerDecimal(
                    registro,
                    "PIM_2021",
                    "MONTO_PIM_2021",
                    "PIM2021"),

            Certificado2021 =
                ObtenerDecimal(
                    registro,
                    "CERTIFICADO_2021",
                    "MONTO_CERTIFICADO_2021",
                    "CERTIFICADO2021"),

            ComprometidoAnual2021 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_ANUAL_2021",
                    "MONTO_COMPROMETIDO_ANUAL_2021",
                    "COMPROMETIDOANUAL2021"),

            Comprometido2021 =
                ObtenerDecimal(
                    registro,
                    "COMPROMETIDO_2021",
                    "MONTO_COMPROMETIDO_2021",
                    "COMPROMETIDO2021"),

            Devengado2021 =
                ObtenerDecimal(
                    registro,
                    "DEVENGADO_2021",
                    "MONTO_DEVENGADO_2021",
                    "DEVENGADO2021"),

            Girado2021 =
                ObtenerDecimal(
                    registro,
                    "GIRADO_2021",
                    "MONTO_GIRADO_2021",
                    "GIRADO2021")
        };
    }

    // ============================================================
    // EXTRAER REGISTROS
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        ExtraerRegistros(
            JsonElement root)
    {
        var resultado =
            new List<Dictionary<string, JsonElement>>();

        JsonElement records;

        if (
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
        else if (
            root.TryGetProperty(
                "records",
                out var recordsDirectos))
        {
            records =
                recordsDirectos;
        }
        else
        {
            return resultado;
        }

        if (
            records.ValueKind !=
            JsonValueKind.Array)
        {
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

            resultado.Add(
                diccionario);
        }

        return resultado;
    }

    // ============================================================
    // TEXTO
    // ============================================================

    private static string?
        ObtenerTexto(
            Dictionary<string, JsonElement> registro,
            params string[] campos)
    {
        foreach (var campo in campos)
        {
            if (
                !registro.TryGetValue(
                    campo,
                    out var valor))
            {
                continue;
            }

            if (
                valor.ValueKind ==
                JsonValueKind.Null)
            {
                continue;
            }

            var texto =
                valor.ToString()?.Trim();

            if (
                !string.IsNullOrWhiteSpace(
                    texto))
            {
                return texto;
            }
        }

        return null;
    }

    // ============================================================
    // DECIMAL
    // ============================================================

    private static decimal ObtenerDecimal(
        Dictionary<string, JsonElement> registro,
        params string[] campos)
    {
        foreach (var campo in campos)
        {
            if (
                !registro.TryGetValue(
                    campo,
                    out var valor))
            {
                continue;
            }

            if (
                valor.ValueKind ==
                JsonValueKind.Null)
            {
                continue;
            }

            if (
                valor.ValueKind ==
                JsonValueKind.Number &&
                valor.TryGetDecimal(
                    out var numero))
            {
                return numero;
            }

            var texto =
                valor.ToString()?.Trim();

            if (
                string.IsNullOrWhiteSpace(
                    texto))
            {
                continue;
            }

            /*
             * Caso 1:
             *
             * 12345.67
             */

            if (
                decimal.TryParse(
                    texto,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var resultado))
            {
                return resultado;
            }

            /*
             * Caso 2:
             *
             * 12.345,67
             */

            if (
                decimal.TryParse(
                    texto,
                    NumberStyles.Any,
                    CultureInfo.GetCultureInfo(
                        "es-PE"),
                    out resultado))
            {
                return resultado;
            }

            /*
             * Caso 3:
             *
             * 12,345.67
             */

            var normalizado =
                texto.Replace(
                    ",",
                    "");

            if (
                decimal.TryParse(
                    normalizado,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out resultado))
            {
                return resultado;
            }
        }

        return 0m;
    }

    // ============================================================
    // INT
    // ============================================================

    private static int?
        ObtenerNullableInt(
            Dictionary<string, JsonElement> registro,
            string campo)
    {
        var texto =
            ObtenerTexto(
                registro,
                campo);

        if (
            string.IsNullOrWhiteSpace(
                texto))
        {
            return null;
        }

        return int.TryParse(
            texto,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var resultado)
            ? resultado
            : null;
    }

    // ============================================================
    // LIMPIAR FILTRO
    // ============================================================

    private static string
        LimpiarFiltro(
            string? filtro)
    {
        return
            string.IsNullOrWhiteSpace(
                filtro)
                ? ""
                : filtro.Trim();
    }
}


// =================================================================
// RESULTADO PAGINADO
// =================================================================

public sealed class MefPageResult
{
    public List<EvolucionPresupuesto> Records { get; set; } = [];

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages
    {
        get
        {
            if (PageSize <= 0)
            {
                return 1;
            }

            return Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)Total /
                    PageSize));
        }
    }
}