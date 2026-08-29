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

    /*
     * IMPORTANTE:
     *
     * Coloca aquí el Resource ID REAL del recurso
     * comparativo de gastos 2017-2021 que estés utilizando.
     *
     * No reutilices el Resource ID 2022-2026.
     */
    private const string ResourceId2017_2021 =
        "0e2469d8-5872-4bc2-a5bc-91ee01c99df8";

    private const int Limit = 100;

    public MefService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.Timeout =
            TimeSpan.FromSeconds(120);
    }

    // ============================================================
    // OBTENER EVOLUCIÓN 2017-2021
    // ============================================================

    public async Task<List<EvolucionPresupuesto>>
        ObtenerEvolucionAsync(
            string? filtro = null,
            CancellationToken cancellationToken = default)
    {
        filtro =
            LimpiarFiltro(filtro);

        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("MEF - CONSULTA 2017-2021");
        Console.WriteLine(
            $"Filtro: '{filtro}'");
        Console.WriteLine(
            $"Resource ID: {ResourceId2017_2021}");
        Console.WriteLine("==========================================");

        if (
            ResourceId2017_2021 ==
            "COLOCA_AQUI_EL_RESOURCE_ID_2017_2021")
        {
            throw new InvalidOperationException(
                "Debes colocar el Resource ID del dataset 2017-2021 en MefService.cs.");
        }

        try
        {
            var url =
                $"{BaseUrl}datastore_search" +
                $"?resource_id={Uri.EscapeDataString(ResourceId2017_2021)}" +
                $"&limit={Limit}";

            /*
             * El filtro se realiza mediante q solamente cuando
             * existe una entidad explícita.
             */
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                url +=
                    $"&q={Uri.EscapeDataString(filtro)}";
            }

            Console.WriteLine();
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("MEF - DATASTORE SEARCH");
            Console.WriteLine("------------------------------------------");

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

                return [];
            }

            var registros =
                ExtraerRegistros(json);

            Console.WriteLine(
                $"Registros recibidos: {registros.Count}");

            if (registros.Count == 0)
            {
                return [];
            }

            /*
             * Cada objeto recibido representa una fila
             * del dataset 2017-2021.
             *
             * No existe una columna ANIO.
             * Los cinco años están dentro del mismo registro.
             */
            var resultados =
                registros
                    .Select(MapearRegistro)
                    .Where(x =>
                        x is not null)
                    .Select(x => x!)
                    .ToList();

            Console.WriteLine(
                $"Registros convertidos: {resultados.Count}");

            return resultados;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                "La consulta al MEF fue cancelada.");

            return [];
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(
                $"Error HTTP del MEF: {ex.Message}");

            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error procesando MEF: {ex}");

            return [];
        }
    }

    // ============================================================
    // MAPEAR REGISTRO
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

            SecEjecut =
                ObtenerTexto(
                    registro,
                    "SEC_EJEC"),

            Ejecutora =
                ObtenerTexto(
                    registro,
                    "EJECUTORA"),

            EjecutoraNombre =
                ObtenerTexto(
                    registro,
                    "EJECUTORA_NOMBRE"),

            // ====================================================
            // UBICACIÓN
            // ====================================================

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
                    "PROGRAMA_PPT0",
                    "PROGRAMA_PPTO"),

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
            // CLASIFICACIÓN FUNCIONAL
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
                    "MONTO_PIA_2017"),

            Pim2017 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIM_2017"),

            Certificado2017 =
                ObtenerDecimal(
                    registro,
                    "MONTO_CERTIFICADO_2017"),

            ComprometidoAnual2017 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_ANUAL_2017"),

            Comprometido2017 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_2017"),

            Devengado2017 =
                ObtenerDecimal(
                    registro,
                    "MONTO_DEVENGADO_2017"),

            Girado2017 =
                ObtenerDecimal(
                    registro,
                    "MONTO_GIRADO_2017"),

            // ====================================================
            // 2018
            // ====================================================

            Pia2018 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIA_2018"),

            Pim2018 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIM_2018"),

            Certificado2018 =
                ObtenerDecimal(
                    registro,
                    "MONTO_CERTIFICADO_2018"),

            ComprometidoAnual2018 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_ANUAL_2018"),

            Comprometido2018 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_2018"),

            Devengado2018 =
                ObtenerDecimal(
                    registro,
                    "MONTO_DEVENGADO_2018"),

            Girado2018 =
                ObtenerDecimal(
                    registro,
                    "MONTO_GIRADO_2018"),

            // ====================================================
            // 2019
            // ====================================================

            Pia2019 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIA_2019"),

            Pim2019 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIM_2019"),

            Certificado2019 =
                ObtenerDecimal(
                    registro,
                    "MONTO_CERTIFICADO_2019"),

            ComprometidoAnual2019 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_ANUAL_2019"),

            Comprometido2019 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_2019"),

            Devengado2019 =
                ObtenerDecimal(
                    registro,
                    "MONTO_DEVENGADO_2019"),

            Girado2019 =
                ObtenerDecimal(
                    registro,
                    "MONTO_GIRADO_2019"),

            // ====================================================
            // 2020
            // ====================================================

            Pia2020 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIA_2020"),

            Pim2020 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIM_2020"),

            Certificado2020 =
                ObtenerDecimal(
                    registro,
                    "MONTO_CERTIFICADO_2020"),

            ComprometidoAnual2020 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_ANUAL_2020"),

            Comprometido2020 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_2020"),

            Devengado2020 =
                ObtenerDecimal(
                    registro,
                    "MONTO_DEVENGADO_2020"),

            Girado2020 =
                ObtenerDecimal(
                    registro,
                    "MONTO_GIRADO_2020"),

            // ====================================================
            // 2021
            // ====================================================

            Pia2021 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIA_2021"),

            Pim2021 =
                ObtenerDecimal(
                    registro,
                    "MONTO_PIM_2021"),

            Certificado2021 =
                ObtenerDecimal(
                    registro,
                    "MONTO_CERTIFICADO_2021"),

            ComprometidoAnual2021 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_ANUAL_2021"),

            Comprometido2021 =
                ObtenerDecimal(
                    registro,
                    "MONTO_COMPROMETIDO_2021"),

            Devengado2021 =
                ObtenerDecimal(
                    registro,
                    "MONTO_DEVENGADO_2021"),

            Girado2021 =
                ObtenerDecimal(
                    registro,
                    "MONTO_GIRADO_2021")
        };
    }

    // ============================================================
    // EXTRAER RECORDS
    // ============================================================

    private static List<Dictionary<string, JsonElement>>
        ExtraerRegistros(
            string json)
    {
        var resultado =
            new List<Dictionary<string, JsonElement>>();

        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

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

    private static decimal
        ObtenerDecimal(
            Dictionary<string, JsonElement> registro,
            string campo)
    {
        if (
            !registro.TryGetValue(
                campo,
                out var valor))
        {
            return 0m;
        }

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

        var texto =
            valor.ToString()?.Trim();

        if (
            string.IsNullOrWhiteSpace(
                texto))
        {
            return 0m;
        }

        /*
         * Los campos del diccionario del MEF son text.
         * Permitimos tanto 12345.67 como 12,345.67.
         */
        texto =
            texto.Replace(
                ",",
                "");

        if (
            decimal.TryParse(
                texto,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var resultado))
        {
            return resultado;
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
        if (
            string.IsNullOrWhiteSpace(
                filtro))
        {
            return "";
        }

        return filtro.Trim();
    }
}