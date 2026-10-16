using FluentAssertions;
using src.Models;
using src.Services;
using Xunit;

namespace FinEdu.Tests.Models;

public class NlqResponseTests
{
    [Fact]
    public void NlqResponse_DefaultValues_AreCorrect()
    {
        // Act
        var response = new NlqResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.Intent.Should().BeNull();
        response.Message.Should().BeNull();
        response.Output.Should().BeNull();
        response.Risk.Should().BeNull();
        response.Evolucion.Should().BeEmpty();
        response.Presupuestos.Should().BeEmpty();
        response.Proyectos.Should().BeEmpty();
        response.Contrataciones.Should().BeEmpty();
        response.TotalRegistros.Should().Be(0);
        response.PaginaActual.Should().Be(1);
        response.TamanioPagina.Should().Be(20);
        response.TotalPaginas.Should().Be(1);
    }

    [Fact]
    public void NlqResponse_CanBeSerializedAndDeserialized()
    {
        // Arrange
        var original = new NlqResponse
        {
            Success = true,
            Intent = "TestIntent",
            Message = "Test message",
            Output = "Test output",
            Risk = "SAFE",
            Evolucion = new List<EvolucionPresupuesto>
            {
                new EvolucionPresupuesto { EjecutoraNombre = "Entidad 1" }
            },
            TotalRegistros = 1,
            PaginaActual = 1,
            TamanioPagina = 10,
            TotalPaginas = 1
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(original);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<NlqResponse>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Success.Should().BeTrue();
        deserialized.Intent.Should().Be("TestIntent");
        deserialized.Message.Should().Be("Test message");
        deserialized.Output.Should().Be("Test output");
        deserialized.Risk.Should().Be("SAFE");
        deserialized.Evolucion.Should().HaveCount(1);
        deserialized.TotalRegistros.Should().Be(1);
    }
}

public class NlqRequestTests
{
    [Fact]
    public void NlqRequest_DefaultValues_AreCorrect()
    {
        // Act
        var request = new NlqRequest();

        // Assert
        request.Query.Should().BeEmpty();
    }
}

public class NlqInterpretacionTests
{
    [Fact]
    public void NlqInterpretacion_DefaultValues_AreCorrect()
    {
        // Act
        var interpretation = new NlqInterpretacion();

        // Assert
        interpretation.Intent.Should().Be("no_reconocido");
        interpretation.Entidad.Should().BeNull();
        interpretation.NivelGobierno.Should().BeNull();
        interpretation.Departamento.Should().BeNull();
        interpretation.Provincia.Should().BeNull();
        interpretation.Distrito.Should().BeNull();
        interpretation.AnioInicio.Should().BeNull();
        interpretation.AnioFin.Should().BeNull();
        interpretation.Metricas.Should().BeEmpty();
    }
}

public class EvolucionPresupuestoTests
{
    [Fact]
    public void EvolucionPresupuesto_PorcentajeEjecucion_CalculatesCorrectly()
    {
        // Arrange
        var record = new EvolucionPresupuesto
        {
            Pim2021 = 1000,
            Devengado2021 = 750
        };

        // Act
        var porcentaje = record.PorcentajeEjecucion;

        // Assert
        porcentaje.Should().Be(75);
    }

    [Fact]
    public void EvolucionPresupuesto_PorcentajeEjecucion_ZeroWhenPimIsZero()
    {
        // Arrange
        var record = new EvolucionPresupuesto
        {
            Pim2021 = 0,
            Devengado2021 = 100
        };

        // Act
        var porcentaje = record.PorcentajeEjecucion;

        // Assert
        porcentaje.Should().Be(0);
    }

    [Fact]
    public void EvolucionPresupuesto_DisplayProperties_FormatCorrectly()
    {
        // Arrange
        var record = new EvolucionPresupuesto
        {
            Pia2021 = 1234567.89m,
            Pim2021 = 2345678.90m,
            Devengado2021 = 1111111.11m
        };

        // Act & Assert - Use invariant culture for consistent formatting
        var culture = new System.Globalization.CultureInfo("es-PE");
        record.PresupuestoInicialDisplay.Should().Be(record.Pia2021.ToString("N2", culture));
        record.PresupuestoModificadoDisplay.Should().Be(record.Pim2021.ToString("N2", culture));
        record.MontoEjecutadoDisplay.Should().Be(record.Devengado2021.ToString("N2", culture));
        record.PorcentajeEjecucionDisplay.Should().Contain("%");
    }

    [Fact]
    public void EvolucionPresupuesto_AllYearProperties_Exist()
    {
        // Arrange
        var record = new EvolucionPresupuesto();

        // Act & Assert - Verify all year properties exist and are settable
        record.Pia2017 = 1; record.Pim2017 = 2; record.Certificado2017 = 3; record.ComprometidoAnual2017 = 4; record.Comprometido2017 = 5; record.Devengado2017 = 6; record.Girado2017 = 7;
        record.Pia2018 = 1; record.Pim2018 = 2; record.Certificado2018 = 3; record.ComprometidoAnual2018 = 4; record.Comprometido2018 = 5; record.Devengado2018 = 6; record.Girado2018 = 7;
        record.Pia2019 = 1; record.Pim2019 = 2; record.Certificado2019 = 3; record.ComprometidoAnual2019 = 4; record.Comprometido2019 = 5; record.Devengado2019 = 6; record.Girado2019 = 7;
        record.Pia2020 = 1; record.Pim2020 = 2; record.Certificado2020 = 3; record.ComprometidoAnual2020 = 4; record.Comprometido2020 = 5; record.Devengado2020 = 6; record.Girado2020 = 7;
        record.Pia2021 = 1; record.Pim2021 = 2; record.Certificado2021 = 3; record.ComprometidoAnual2021 = 4; record.Comprometido2021 = 5; record.Devengado2021 = 6; record.Girado2021 = 7;
        record.Pia2022 = 1; record.Pim2022 = 2; record.Certificado2022 = 3; record.ComprometidoAnual2022 = 4; record.Comprometido2022 = 5; record.Devengado2022 = 6; record.Girado2022 = 7;
        record.Pia2023 = 1; record.Pim2023 = 2; record.Certificado2023 = 3; record.ComprometidoAnual2023 = 4; record.Comprometido2023 = 5; record.Devengado2023 = 6; record.Girado2023 = 7;
        record.Pia2024 = 1; record.Pim2024 = 2; record.Certificado2024 = 3; record.ComprometidoAnual2024 = 4; record.Comprometido2024 = 5; record.Devengado2024 = 6; record.Girado2024 = 7;
        record.Pia2025 = 1; record.Pim2025 = 2; record.Certificado2025 = 3; record.ComprometidoAnual2025 = 4; record.Comprometido2025 = 5; record.Devengado2025 = 6; record.Girado2025 = 7;
        record.Pia2026 = 1; record.Pim2026 = 2; record.Certificado2026 = 3; record.ComprometidoAnual2026 = 4; record.Comprometido2026 = 5; record.Devengado2026 = 6; record.Girado2026 = 7;

        record.Pia2026.Should().Be(1);
    }
}

public class PresupuestoResumenTests
{
    [Fact]
    public void PresupuestoResumen_Properties_CanBeSet()
    {
        // Arrange
        var resumen = new PresupuestoResumen
        {
            Anio = 2021,
            Mes = "Enero",
            PIA = 1000000,
            PIM = 1200000,
            Ejecutado = 900000,
            PorcentajeEjecucion = 75
        };

        // Assert
        resumen.Anio.Should().Be(2021);
        resumen.Mes.Should().Be("Enero");
        resumen.PIA.Should().Be(1000000);
        resumen.PIM.Should().Be(1200000);
        resumen.Ejecutado.Should().Be(900000);
        resumen.PorcentajeEjecucion.Should().Be(75);
    }
}

public class ProyectoTests
{
    [Fact]
    public void Proyecto_Properties_CanBeSet()
    {
        // Arrange
        var proyecto = new Proyecto
        {
            Id = 1,
            Nombre = "Proyecto Test",
            Categoria = "Obras",
            Presupuesto = 5000000,
            Ejecutado = 4000000
        };

        // Assert
        proyecto.Id.Should().Be(1);
        proyecto.Nombre.Should().Be("Proyecto Test");
        proyecto.Categoria.Should().Be("Obras");
        proyecto.Presupuesto.Should().Be(5000000);
        proyecto.Ejecutado.Should().Be(4000000);
    }
}

public class ContratacionTests
{
    [Fact]
    public void Contratacion_Properties_CanBeSet()
    {
        // Arrange
        var contratacion = new Contratacion
        {
            Id = 1,
            Ocid = "ocds-123",
            Entidad = "Entidad Test",
            Empresa = "Empresa Test",
            Monto = 100000,
            Fecha = new DateTime(2021, 6, 15)
        };

        // Assert
        contratacion.Id.Should().Be(1);
        contratacion.Ocid.Should().Be("ocds-123");
        contratacion.Entidad.Should().Be("Entidad Test");
        contratacion.Empresa.Should().Be("Empresa Test");
        contratacion.Monto.Should().Be(100000);
        contratacion.Fecha.Should().Be(new DateTime(2021, 6, 15));
    }
}

public class MefPageResultTests
{
    [Fact]
    public void MefPageResult_TotalPages_CalculatesCorrectly()
    {
        // Arrange
        var result = new MefPageResult
        {
            Total = 100,
            Page = 1,
            PageSize = 20
        };

        // Act & Assert
        result.TotalPages.Should().Be(5);
    }

    [Fact]
    public void MefPageResult_TotalPages_HandlesZeroPageSize()
    {
        // Arrange
        var result = new MefPageResult
        {
            Total = 100,
            Page = 1,
            PageSize = 0
        };

        // Act & Assert
        result.TotalPages.Should().Be(1); // Should not crash, returns 1
    }

    [Fact]
    public void MefPageResult_TotalPages_RoundsUp()
    {
        // Arrange
        var result = new MefPageResult
        {
            Total = 25,
            Page = 1,
            PageSize = 10
        };

        // Act & Assert
        result.TotalPages.Should().Be(3);
    }
}

public class OeceSearchResultTests
{
    [Fact]
    public void OeceSearchResult_Properties_CanBeSet()
    {
        // Arrange
        var result = new src.Services.OeceSearchResult
        {
            Success = true,
            Message = "Test message",
            Total = 10,
            Records = new List<src.Services.OeceRecord>
            {
                new src.Services.OeceRecord { Anio = 2018, Entidad = "Test" }
            }
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Test message");
        result.Total.Should().Be(10);
        result.Records.Should().HaveCount(1);
    }
}

public class PresupuestoMensualDtoTests
{
    [Fact]
    public void PresupuestoMensualDto_PorcentajeEjecucion_CalculatesCorrectly()
    {
        // Arrange
        var dto = new PresupuestoMensualDto
        {
            Presupuesto = 1000,
            Ejecutado = 750
        };

        // Act
        var porcentaje = dto.PorcentajeEjecucion;

        // Assert
        porcentaje.Should().Be(75);
    }

    [Fact]
    public void PresupuestoMensualDto_PorcentajeEjecucion_ZeroWhenPresupuestoIsZero()
    {
        // Arrange
        var dto = new PresupuestoMensualDto
        {
            Presupuesto = 0,
            Ejecutado = 100
        };

        // Act
        var porcentaje = dto.PorcentajeEjecucion;

        // Assert
        porcentaje.Should().Be(0);
    }
}

public class ProyectoPresupuestoDtoTests
{
    [Fact]
    public void ProyectoPresupuestoDto_Properties_CanBeSet()
    {
        // Arrange
        var dto = new ProyectoPresupuestoDto
        {
            Categoria = "Obras",
            Proyecto = "Proyecto Test",
            Presupuesto = 5000000,
            Ejecutado = 4000000
        };

        // Assert
        dto.Categoria.Should().Be("Obras");
        dto.Proyecto.Should().Be("Proyecto Test");
        dto.Presupuesto.Should().Be(5000000);
        dto.Ejecutado.Should().Be(4000000);
    }
}