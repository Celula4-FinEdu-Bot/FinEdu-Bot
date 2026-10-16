using System.Text.Json;
using FluentAssertions;
using Moq;
using src.Interfaces;
using src.Models;
using src.Services;
using Xunit;

namespace FinEdu.Tests.Services;

public class NlqServiceTests : TestBase
{
    private readonly NlqService _nlqService;
    private readonly Mock<IBackendService> _mockBackendService;

    public NlqServiceTests()
    {
        _mockBackendService = new Mock<IBackendService>();
        _nlqService = new NlqService(_mockBackendService.Object);
    }

    [Fact]
    public async Task ProcesarAsync_ValidQuestion_ReturnsBackendResponse()
    {
        // Arrange
        var expectedResponse = new NlqResponse
        {
            Success = true,
            Intent = "ConsultaPuntual",
            Message = "Datos encontrados",
            Output = "Resultado procesado",
            Risk = "SAFE",
            Evolucion = new List<EvolucionPresupuesto>
            {
                new EvolucionPresupuesto
                {
                    EjecutoraNombre = "MUNICIPALIDAD DISTRITAL DE CHAMBARA",
                    Pia2021 = 500000,
                    Pim2021 = 600000,
                    Devengado2021 = 450000
                }
            },
            TotalRegistros = 1
        };

        _mockBackendService
            .Setup(x => x.ConsultarAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _nlqService.ProcesarAsync(
            "¿Cuál fue el presupuesto de la Municipalidad de Chambara en 2021?",
            1,
            10,
            "custom-session-id",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Intent.Should().Be("ConsultaPuntual");
        result.Evolucion.Should().HaveCount(1);
        result.Evolucion[0].EjecutoraNombre.Should().Be("MUNICIPALIDAD DISTRITAL DE CHAMBARA");

        _mockBackendService.Verify(
            x => x.ConsultarAsync(
                "¿Cuál fue el presupuesto de la Municipalidad de Chambara en 2021?",
                "custom-session-id",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_EmptyQuestion_ReturnsErrorResponse()
    {
        // Act
        var result = await _nlqService.ProcesarAsync(
            "",
            1,
            10,
            "session-id",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("ConsultaVacia");
        result.Message.Should().Be("Escribe una consulta.");

        _mockBackendService.Verify(
            x => x.ConsultarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcesarAsync_WhitespaceQuestion_ReturnsErrorResponse()
    {
        // Act
        var result = await _nlqService.ProcesarAsync(
            "   ",
            1,
            10,
            "session-id",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("ConsultaVacia");
    }

    [Fact]
    public async Task ProcesarAsync_NullSessionId_GeneratesNewGuid()
    {
        // Arrange
        var expectedResponse = new NlqResponse { Success = true, Intent = "Test" };
        _mockBackendService
            .Setup(x => x.ConsultarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _nlqService.ProcesarAsync(
            "Consulta de prueba",
            1,
            10,
            null,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockBackendService.Verify(
            x => x.ConsultarAsync(
                "Consulta de prueba",
                It.Is<string>(s => !string.IsNullOrEmpty(s) && s.Length == 32), // Guid.NewGuid().ToString("N") = 32 chars
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_TrimsQuestion()
    {
        // Arrange
        var expectedResponse = new NlqResponse { Success = true, Intent = "Test" };
        _mockBackendService
            .Setup(x => x.ConsultarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _nlqService.ProcesarAsync(
            "  Consulta con espacios  ",
            1,
            10,
            "session-id",
            CancellationToken.None);

        // Assert
        _mockBackendService.Verify(
            x => x.ConsultarAsync(
                "Consulta con espacios",
                "session-id",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_BackendReturnsError_PropagatesError()
    {
        // Arrange
        var errorResponse = new NlqResponse
        {
            Success = false,
            Intent = "ErrorBackend",
            Message = "Error en el backend"
        };
        _mockBackendService
            .Setup(x => x.ConsultarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResponse);

        // Act
        var result = await _nlqService.ProcesarAsync(
            "Consulta de prueba",
            1,
            10,
            "session-id",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("ErrorBackend");
        result.Message.Should().Be("Error en el backend");
    }

    [Fact]
    public async Task ProcesarAsync_PassesPaginationParameters()
    {
        // Arrange
        var expectedResponse = new NlqResponse
        {
            Success = true,
            Intent = "EvolucionPresupuesto",
            Evolucion = new List<EvolucionPresupuesto>(),
            TotalRegistros = 50,
            PaginaActual = 3,
            TamanioPagina = 20,
            TotalPaginas = 3
        };
        _mockBackendService
            .Setup(x => x.ConsultarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _nlqService.ProcesarAsync(
            "Evolución 2017-2021",
            3,
            20,
            "session-id",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PaginaActual.Should().Be(3);
        result.TamanioPagina.Should().Be(20);
        result.TotalRegistros.Should().Be(50);
    }

    [Fact]
    public async Task ProcesarAsync_CancellationToken_PropagatedToBackend()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockBackendService
            .Setup(x => x.ConsultarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _nlqService.ProcesarAsync(
                "Consulta de prueba",
                1,
                10,
                "session-id",
                cts.Token);
        });
    }
}