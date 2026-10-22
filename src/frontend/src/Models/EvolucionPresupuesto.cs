namespace src.Models;

public sealed class EvolucionPresupuesto
{
    // Identificación
    public string? KeyValue { get; set; }

    public string? NivelGobierno { get; set; }
    public string? NivelGobiernoNombre { get; set; }

    public string? Sector { get; set; }
    public string? SectorNombre { get; set; }

    public string? Pliego { get; set; }
    public string? PliegoNombre { get; set; }

    public string? Ejecutora { get; set; }
    public string? EjecutoraNombre { get; set; }

    public string? SecEjecut { get; set; }

    public string? DepartamentoEjecutora { get; set; }
    public string? DepartamentoEjecutoraNombre { get; set; }

    public string? ProvinciaEjecutora { get; set; }
    public string? ProvinciaEjecutoraNombre { get; set; }

    public string? DistritoEjecutora { get; set; }
    public string? DistritoEjecutoraNombre { get; set; }

    // Programa presupuestal
    public string? ProgramaPpto { get; set; }
    public string? ProgramaPptoNombre { get; set; }

    public string? TipoActProy { get; set; }
    public string? TipoActProyNombre { get; set; }

    public string? ProductoProyecto { get; set; }
    public string? ProductoProyectoNombre { get; set; }

    public string? ActividadAccionObra { get; set; }
    public string? ActividadAccionObraNombre { get; set; }

    // Clasificación funcional
    public string? Funcion { get; set; }
    public string? FuncionNombre { get; set; }

    public string? DivisionFuncional { get; set; }
    public string? DivisionFuncionalNombre { get; set; }

    public string? GrupoFuncional { get; set; }
    public string? GrupoFuncionalNombre { get; set; }

    public string? Meta { get; set; }
    public string? MetaNombre { get; set; }

    public string? DepartamentoMeta { get; set; }
    public string? DepartamentoMetaNombre { get; set; }

    // Financiamiento
    public string? FuenteFinanciamiento { get; set; }
    public string? FuenteFinanciamientoNombre { get; set; }

    public string? Rubro { get; set; }
    public string? RubroNombre { get; set; }

    public string? TipoRecurso { get; set; }
    public string? TipoRecursoNombre { get; set; }

    // Clasificación del gasto
    public int? CategoriaGasto { get; set; }
    public string? CategoriaGastoNombre { get; set; }

    public int? TipoTransaccion { get; set; }

    public int? Generica { get; set; }
    public string? GenericaNombre { get; set; }

    public int? Subgenerica { get; set; }
    public string? SubgenericaNombre { get; set; }

    public int? SubgenericaDet { get; set; }
    public string? SubgenericaDetNombre { get; set; }

    public int? Especifica { get; set; }
    public string? EspecificaNombre { get; set; }

    public int? EspecificaDet { get; set; }
    public string? EspecificaDetNombre { get; set; }


   // =========================================================
    // PRESUPUESTO 2017
    // =========================================================

   public decimal Pia2017 { get; set; }
   public decimal Pim2017 { get; set; }
   public decimal Certificado2017 { get; set; }
   public decimal ComprometidoAnual2017 { get; set; }
   public decimal Comprometido2017 { get; set; }
   public decimal Devengado2017 { get; set; }
   public decimal Girado2017 { get; set; }


   // =========================================================
    // PRESUPUESTO 2018
    // =========================================================

   public decimal Pia2018 { get; set; }
   public decimal Pim2018 { get; set; }
   public decimal Certificado2018 { get; set; }
   public decimal ComprometidoAnual2018 { get; set; }
   public decimal Comprometido2018 { get; set; }
   public decimal Devengado2018 { get; set; }
   public decimal Girado2018 { get; set; }



   // =========================================================
    // PRESUPUESTO 2019
    // =========================================================

   public decimal Pia2019 { get; set; }
   public decimal Pim2019 { get; set; }
   public decimal Certificado2019 { get; set; }
   public decimal ComprometidoAnual2019 { get; set; }
   public decimal Comprometido2019 { get; set; }
   public decimal Devengado2019 { get; set; }
   public decimal Girado2019 { get; set; }

   // =========================================================
    // PRESUPUESTO 2020
    // =========================================================

   public decimal Pia2020 { get; set; }
   public decimal Pim2020 { get; set; }
   public decimal Certificado2020 { get; set; }
   public decimal ComprometidoAnual2020 { get; set; }
   public decimal Comprometido2020 { get; set; }
   public decimal Devengado2020 { get; set; }
   public decimal Girado2020 { get; set; }



   // =========================================================
    // PRESUPUESTO 2021
    // =========================================================

   public decimal Pia2021 { get; set; }
   public decimal Pim2021 { get; set; }
   public decimal Certificado2021 { get; set; }
   public decimal ComprometidoAnual2021 { get; set; }
   public decimal Comprometido2021 { get; set; }
   public decimal Devengado2021 { get; set; }
   public decimal Girado2021 { get; set; }




    // =========================================================
    // PRESUPUESTO 2022
    // =========================================================

    public decimal Pia2022 { get; set; }
    public decimal Pim2022 { get; set; }
    public decimal Certificado2022 { get; set; }
    public decimal ComprometidoAnual2022 { get; set; }
    public decimal Comprometido2022 { get; set; }
    public decimal Devengado2022 { get; set; }
    public decimal Girado2022 { get; set; }

    // =========================================================
    // PRESUPUESTO 2023
    // =========================================================

    public decimal Pia2023 { get; set; }
    public decimal Pim2023 { get; set; }
    public decimal Certificado2023 { get; set; }
    public decimal ComprometidoAnual2023 { get; set; }
    public decimal Comprometido2023 { get; set; }
    public decimal Devengado2023 { get; set; }
    public decimal Girado2023 { get; set; }

    // =========================================================
    // PRESUPUESTO 2024
    // =========================================================

    public decimal Pia2024 { get; set; }
    public decimal Pim2024 { get; set; }
    public decimal Certificado2024 { get; set; }
    public decimal ComprometidoAnual2024 { get; set; }
    public decimal Comprometido2024 { get; set; }
    public decimal Devengado2024 { get; set; }
    public decimal Girado2024 { get; set; }

    // =========================================================
    // PRESUPUESTO 2025
    // =========================================================

    public decimal Pia2025 { get; set; }
    public decimal Pim2025 { get; set; }
    public decimal Certificado2025 { get; set; }
    public decimal ComprometidoAnual2025 { get; set; }
    public decimal Comprometido2025 { get; set; }
    public decimal Devengado2025 { get; set; }
    public decimal Girado2025 { get; set; }

    // =========================================================
    // PRESUPUESTO 2026
    // =========================================================

    public decimal Pia2026 { get; set; }
    public decimal Pim2026 { get; set; }
    public decimal Certificado2026 { get; set; }
    public decimal ComprometidoAnual2026 { get; set; }
    public decimal Comprometido2026 { get; set; }
    public decimal Devengado2026 { get; set; }
    public decimal Girado2026 { get; set; }

    // =========================================================
    // PROPIEDADES PARA LA VISTA
    // =========================================================

    /// <summary>
    /// PIA correspondiente al período seleccionado/principal.
    /// Por defecto utilizamos 2026.
    /// </summary>
    public decimal PresupuestoInicial =>
    Pia2021;

public decimal PresupuestoModificado =>
    Pim2021;

public decimal MontoEjecutado =>
    Devengado2021;

public decimal PorcentajeEjecucion
{
    get
    {
        if (Pim2021 <= 0)
            return 0;

        return (Devengado2021 / Pim2021) * 100;
    }
}
    // =========================================================
    // PROPIEDADES DISPLAY
    // =========================================================

    public string PresupuestoInicialDisplay =>
        FormatearMoneda(PresupuestoInicial);

    public string PresupuestoModificadoDisplay =>
        FormatearMoneda(PresupuestoModificado);

    public string MontoEjecutadoDisplay =>
        FormatearMoneda(MontoEjecutado);

    public string PorcentajeEjecucionDisplay =>
        $"{PorcentajeEjecucion:N2}%";

    private static string FormatearMoneda(decimal valor)
    {
        return valor.ToString(
            "N2",
            System.Globalization.CultureInfo.GetCultureInfo("es-PE"));
    }
}