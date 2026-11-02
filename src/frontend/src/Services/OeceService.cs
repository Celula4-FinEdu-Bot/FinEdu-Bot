using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;

namespace src.Services;

public sealed class OeceService
{
    private readonly IWebHostEnvironment _environment;

    private const string FileName =
        "OSCE_PAC2018_0.xlsx";

    private const int DefaultLimit = 100;


    // ============================================================
    // RUTA DEL EXCEL
    // ============================================================

    private string GetExcelPath()
    {
        /*
         * ContentRootPath:
         *
         * FinEdu-Bot-FrontEnd-Zamudio
         *   └── src
         *       └── frontend
         *           └── src
         *
         * Desde aquí:
         *
         * ../../../
         *
         * nos lleva a:
         *
         * FinEdu-Bot-FrontEnd-Zamudio
         */

        var repositoryRoot =
            Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    "..",
                    "..",
                    ".."));

        return Path.Combine(
            repositoryRoot,
            "src",
            "database",
            "seeders",
            FileName);
    }


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public OeceService(
        IWebHostEnvironment environment)
    {
        _environment = environment;
    }


    // ============================================================
    // BUSCAR
    // ============================================================

    public Task<OeceSearchResult> BuscarAsync(
        string? texto = null,
        int? anio = null,
        string? departamento = null,
        string? entidad = null,
        string? tipoProceso = null,
        string? objetoContractual = null,
        int? mes = null,
        int limite = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path =
            GetExcelPath();

        Console.WriteLine();
        Console.WriteLine(
            "==========================================");

        Console.WriteLine(
            "OECE - CONSULTA EXCEL");

        Console.WriteLine(
            $"Archivo: {path}");

        Console.WriteLine(
            "==========================================");


        if (!File.Exists(path))
        {
            return Task.FromResult(
                new OeceSearchResult
                {
                    Success = false,

                    Message =
                        $"No se encontró el archivo Excel OECE: {path}"
                });
        }


        try
        {
            using var document =
                SpreadsheetDocument.Open(
                    path,
                    false);

            var workbookPart =
                document.WorkbookPart;

            if (workbookPart == null)
            {
                return Task.FromResult(
                    new OeceSearchResult
                    {
                        Success = false,
                        Message =
                            "El archivo Excel no contiene información de workbook."
                    });
            }


            var worksheetPart =
                GetFirstWorksheetPart(
                    workbookPart);

            if (worksheetPart == null)
            {
                return Task.FromResult(
                    new OeceSearchResult
                    {
                        Success = false,
                        Message =
                            "No se encontró ninguna hoja de cálculo en el archivo OECE."
                    });
            }


            var sharedStrings =
                workbookPart
                    .SharedStringTablePart?
                    .SharedStringTable;


            var rows =
                worksheetPart
                    .Worksheet
                    .GetFirstChild<SheetData>()?
                    .Elements<Row>();


            if (rows == null)
            {
                return Task.FromResult(
                    new OeceSearchResult
                    {
                        Success = false,
                        Message =
                            "La hoja de cálculo no contiene filas."
                    });
            }


            // ====================================================
            // CABECERA
            // ====================================================

            var firstRow =
                rows.FirstOrDefault();

            if (firstRow == null)
            {
                return Task.FromResult(
                    new OeceSearchResult
                    {
                        Success = false,
                        Message =
                            "El Excel no contiene cabecera."
                    });
            }


            var headers =
                ReadRow(
                    firstRow,
                    sharedStrings);


            NormalizeHeaders(
                headers);


            Console.WriteLine(
                "Cabeceras encontradas:");

            foreach (var header in headers)
            {
                Console.WriteLine(
                    $" - [{header}]");
            }


            var indexes =
                BuildHeaderIndexes(
                    headers);


            ValidateRequiredColumns(
                indexes);


            // ====================================================
            // RESULTADOS
            // ====================================================

            var resultado =
                new List<OeceRecord>();

            var total =
                0;


            // ====================================================
            // LEER FILAS
            // ====================================================

            foreach (
                var row
                in rows.Skip(1))
            {
                cancellationToken.ThrowIfCancellationRequested();


                var values =
                    ReadRow(
                        row,
                        sharedStrings);


                var record =
                    MapRecord(
                        values,
                        indexes);


                if (record == null)
                {
                    continue;
                }


                if (
                    !Matches(
                        record,
                        texto,
                        anio,
                        departamento,
                        entidad,
                        tipoProceso,
                        objetoContractual,
                        mes))
                {
                    continue;
                }


                total++;


                if (
                    resultado.Count <
                    limite)
                {
                    resultado.Add(
                        record);
                }
            }


            Console.WriteLine(
                $"Coincidencias encontradas: {total}");

            Console.WriteLine(
                $"Registros mostrados: {resultado.Count}");


            return Task.FromResult(
                new OeceSearchResult
                {
                    Success = true,

                    Message =
                        total > 0
                            ? $"Se encontraron {total} coincidencia(s)."
                            : "No se encontraron contrataciones.",

                    Total =
                        total,

                    Records =
                        resultado
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR OECE: {ex}");

            return Task.FromResult(
                new OeceSearchResult
                {
                    Success = false,

                    Message =
                        $"Error leyendo el archivo Excel OECE: {ex.Message}"
                });
        }
    }


    // ============================================================
    // OBTENER PRIMERA HOJA
    // ============================================================

    private static WorksheetPart?
        GetFirstWorksheetPart(
            WorkbookPart workbookPart)
    {
        var sheets =
            workbookPart
                .Workbook
                .Sheets?
                .Elements<Sheet>();


        if (sheets == null)
        {
            return null;
        }


        foreach (var sheet in sheets)
        {
            if (string.IsNullOrWhiteSpace(
                    sheet.Id?.Value))
            {
                continue;
            }


            var relationshipId =
                sheet.Id!.Value;


            return workbookPart
                .GetPartById(
                    relationshipId)
                as WorksheetPart;
        }


        return null;
    }


    // ============================================================
    // LEER FILA
    // ============================================================

    private static List<string>
        ReadRow(
            Row row,
            SharedStringTable? sharedStrings)
    {
        var result =
            new List<string>();


        var cells =
            row.Elements<Cell>()
               .ToList();


        if (cells.Count == 0)
        {
            return result;
        }


        var maximumColumn =
            cells
                .Select(
                    cell =>
                        GetColumnIndex(
                            cell.CellReference?.Value))
                .DefaultIfEmpty(0)
                .Max();


        for (
            var index = 0;
            index <= maximumColumn;
            index++)
        {
            var cell =
                cells.FirstOrDefault(
                    c =>
                        GetColumnIndex(
                            c.CellReference?.Value)
                        == index);


            result.Add(
                cell == null
                    ? ""
                    : GetCellValue(
                        cell,
                        sharedStrings));
        }


        return result;
    }


    // ============================================================
    // VALOR DE CELDA
    // ============================================================

    private static string GetCellValue(
    Cell cell,
    SharedStringTable? sharedStrings)
    {
        var value =
            cell.CellValue?.Text ??
            cell.InnerText ??
            "";

        if (cell.DataType == null)
        {
            return value;
        }

        // El atributo XML de DataType usa valores como:
        // s  = SharedString
        // b  = Boolean
        // inlineStr = InlineString

        var dataType =
            cell.DataType.InnerText;

        // ============================================================
        // SHARED STRING
        // ============================================================

        if (dataType == "s")
        {
            if (
                int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var sharedIndex) &&
                sharedStrings != null)
            {
                var item =
                    sharedStrings
                        .Elements<SharedStringItem>()
                        .ElementAtOrDefault(
                            sharedIndex);

                return item?.InnerText ?? "";
            }

            return value;
        }

        // ============================================================
        // BOOLEAN
        // ============================================================

        if (dataType == "b")
        {
            return value == "1"
                ? "TRUE"
                : "FALSE";
        }

        // ============================================================
        // INLINE STRING
        // ============================================================

        if (dataType == "inlineStr")
        {
            return
                cell.InlineString?.InnerText
                ?? value;
        }

        return value;
    }




    // ============================================================
    // INDICE DE COLUMNA
    // ============================================================

    private static int
        GetColumnIndex(
            string? cellReference)
    {
        if (
            string.IsNullOrWhiteSpace(
                cellReference))
        {
            return 0;
        }


        var column =
            0;


        foreach (
            var character
            in cellReference)
        {
            if (
                !char.IsLetter(
                    character))
            {
                break;
            }


            column =
                column * 26 +
                (
                    char.ToUpperInvariant(
                        character)
                    - 'A'
                    + 1
                );
        }


        return column - 1;
    }


    // ============================================================
    // NORMALIZAR CABECERAS
    // ============================================================

    private static void
        NormalizeHeaders(
            List<string> headers)
    {
        for (
            var i = 0;
            i < headers.Count;
            i++)
        {
            headers[i] =
                headers[i]
                    .Trim()
                    .Trim('\uFEFF')
                    .Trim();
        }
    }


    // ============================================================
    // INDICES
    // ============================================================

    private static Dictionary<string, int>
        BuildHeaderIndexes(
            List<string> headers)
    {
        var result =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);


        for (
            var i = 0;
            i < headers.Count;
            i++)
        {
            var name =
                NormalizeHeader(
                    headers[i]);


            result[name] =
                i;
        }


        return result;
    }


    private static string
        NormalizeHeader(
            string header)
    {
        return header
            .Trim()
            .Trim('\uFEFF')
            .ToLowerInvariant();
    }


    // ============================================================
    // VALIDAR COLUMNAS
    // ============================================================

    private static void
        ValidateRequiredColumns(
            Dictionary<string, int> indexes)
    {
        var required =
            new[]
            {
                "año",
                "codigoentidad",
                "ruc_entidad",
                "entidad",
                "version",
                "n_referencia",
                "descripcion_proceso",
                "tipoprocesoseleccion",
                "objetocontractual",
                "n_item",
                "descripcion_item",
                "cantidad",
                "unidad_medida",
                "departamento",
                "provincia",
                "distrito",
                "codigoubigeo_item",
                "departamento_ent",
                "provincia_ent",
                "distrito_ent",
                "codigoubigeo_ent",
                "mes_previsto",
                "fecha_publicacion"
            };


        foreach (var column in required)
        {
            if (!indexes.ContainsKey(column))
            {
                throw new InvalidOperationException(
                    $"La columna requerida '{column}' " +
                    "no existe en el Excel OECE.");
            }
        }
    }


    // ============================================================
    // MAPEAR REGISTRO
    // ============================================================

    private static OeceRecord?
        MapRecord(
            List<string> values,
            Dictionary<string, int> indexes)
    {
        if (values.Count == 0)
        {
            return null;
        }


        return new OeceRecord
        {
            Anio =
                GetInt(
                    values,
                    indexes,
                    "año"),


            CodigoEntidad =
                GetString(
                    values,
                    indexes,
                    "codigoentidad"),


            RucEntidad =
                GetString(
                    values,
                    indexes,
                    "ruc_entidad"),


            Entidad =
                GetString(
                    values,
                    indexes,
                    "entidad"),


            Version =
                GetString(
                    values,
                    indexes,
                    "version"),


            NumeroReferencia =
                GetString(
                    values,
                    indexes,
                    "n_referencia"),


            DescripcionProceso =
                GetString(
                    values,
                    indexes,
                    "descripcion_proceso"),


            TipoProcesoSeleccion =
                GetString(
                    values,
                    indexes,
                    "tipoprocesoseleccion"),


            ObjetoContractual =
                GetString(
                    values,
                    indexes,
                    "objetocontractual"),


            NumeroItem =
                GetString(
                    values,
                    indexes,
                    "n_item"),


            DescripcionItem =
                GetString(
                    values,
                    indexes,
                    "descripcion_item"),


            Cantidad =
                GetDecimal(
                    values,
                    indexes,
                    "cantidad"),


            UnidadMedida =
                GetString(
                    values,
                    indexes,
                    "unidad_medida"),


            Departamento =
                GetString(
                    values,
                    indexes,
                    "departamento"),


            Provincia =
                GetString(
                    values,
                    indexes,
                    "provincia"),


            Distrito =
                GetString(
                    values,
                    indexes,
                    "distrito"),


            CodigoUbigeoItem =
                GetString(
                    values,
                    indexes,
                    "codigoubigeo_item"),


            DepartamentoEntidad =
                GetString(
                    values,
                    indexes,
                    "departamento_ent"),


            ProvinciaEntidad =
                GetString(
                    values,
                    indexes,
                    "provincia_ent"),


            DistritoEntidad =
                GetString(
                    values,
                    indexes,
                    "distrito_ent"),


            CodigoUbigeoEntidad =
                GetString(
                    values,
                    indexes,
                    "codigoubigeo_ent"),


            MesPrevisto =
                GetInt(
                    values,
                    indexes,
                    "mes_previsto"),


            FechaPublicacion =
                GetString(
                    values,
                    indexes,
                    "fecha_publicacion")
        };
    }


    // ============================================================
    // FILTROS
    // ============================================================

    private static bool
        Matches(
            OeceRecord record,
            string? texto,
            int? anio,
            string? departamento,
            string? entidad,
            string? tipoProceso,
            string? objetoContractual,
            int? mes)
    {
        if (
            anio.HasValue &&
            record.Anio != anio)
        {
            return false;
        }


        if (
            mes.HasValue &&
            record.MesPrevisto != mes)
        {
            return false;
        }


        if (
            !Contains(
                record.Departamento,
                departamento))
        {
            return false;
        }


        if (
            !Contains(
                record.Entidad,
                entidad))
        {
            return false;
        }


        if (
            !Contains(
                record.TipoProcesoSeleccion,
                tipoProceso))
        {
            return false;
        }


        if (
            !Contains(
                record.ObjetoContractual,
                objetoContractual))
        {
            return false;
        }


        if (
            !string.IsNullOrWhiteSpace(
                texto))
        {
            var encontrado =
                Contains(
                    record.Entidad,
                    texto)

                || Contains(
                    record.DescripcionProceso,
                    texto)

                || Contains(
                    record.TipoProcesoSeleccion,
                    texto)

                || Contains(
                    record.ObjetoContractual,
                    texto)

                || Contains(
                    record.DescripcionItem,
                    texto)

                || Contains(
                    record.Departamento,
                    texto)

                || Contains(
                    record.Provincia,
                    texto)

                || Contains(
                    record.Distrito,
                    texto);


            if (!encontrado)
            {
                return false;
            }
        }


        return true;
    }


    private static bool
        Contains(
            string? value,
            string? filter)
    {
        if (
            string.IsNullOrWhiteSpace(
                filter))
        {
            return true;
        }


        if (
            string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }


        return value.Contains(
            filter,
            StringComparison.OrdinalIgnoreCase);
    }


    // ============================================================
    // STRING
    // ============================================================

    private static string?
        GetString(
            List<string> values,
            Dictionary<string, int> indexes,
            string column)
    {
        var normalized =
            NormalizeHeader(
                column);


        if (
            !indexes.TryGetValue(
                normalized,
                out var index))
        {
            return null;
        }


        if (
            index < 0 ||
            index >= values.Count)
        {
            return null;
        }


        var value =
            values[index]
                .Trim();


        return string.IsNullOrWhiteSpace(
            value)
            ? null
            : value;
    }


    // ============================================================
    // INT
    // ============================================================

    private static int?
        GetInt(
            List<string> values,
            Dictionary<string, int> indexes,
            string column)
    {
        var value =
            GetString(
                values,
                indexes,
                column);


        if (
            string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }


        return int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : null;
    }


    // ============================================================
    // DECIMAL
    // ============================================================

    private static decimal?
        GetDecimal(
            List<string> values,
            Dictionary<string, int> indexes,
            string column)
    {
        var value =
            GetString(
                values,
                indexes,
                column);


        if (
            string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }


        value =
            value.Replace(
                ",",
                "");


        return decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : null;
    }
}


// =================================================================
// RESULTADO
// =================================================================

public sealed class OeceSearchResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = "";

    public int Total { get; set; }

    public List<OeceRecord> Records { get; set; } = [];
}


// =================================================================
// REGISTRO
// =================================================================

public sealed class OeceRecord
{
    public int? Anio { get; set; }

    public string? CodigoEntidad { get; set; }

    public string? RucEntidad { get; set; }

    public string? Entidad { get; set; }

    public string? Version { get; set; }

    public string? NumeroReferencia { get; set; }

    public string? DescripcionProceso { get; set; }

    public string? TipoProcesoSeleccion { get; set; }

    public string? ObjetoContractual { get; set; }

    public string? NumeroItem { get; set; }

    public string? DescripcionItem { get; set; }

    public decimal? Cantidad { get; set; }

    public string? UnidadMedida { get; set; }

    public string? Departamento { get; set; }

    public string? Provincia { get; set; }

    public string? Distrito { get; set; }

    public string? CodigoUbigeoItem { get; set; }

    public string? DepartamentoEntidad { get; set; }

    public string? ProvinciaEntidad { get; set; }

    public string? DistritoEntidad { get; set; }

    public string? CodigoUbigeoEntidad { get; set; }

    public int? MesPrevisto { get; set; }

    public string? FechaPublicacion { get; set; }
}