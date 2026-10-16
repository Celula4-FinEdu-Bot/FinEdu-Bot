using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.Configuration;
using src.Models;
using src.Services;
using Xunit;

namespace FinEdu.Tests.Services;

public class BackendServiceTests : TestBase
{
    private readonly BackendService _backendService;

    public BackendServiceTests()
    {
        _backendService = new BackendService(HttpClient, Configuration);
    }

    [Fact]
    public async Task ConsultarAsync_ValidRequest_ReturnsSuccessfulResponse()
    {
        // Arrange
        var expectedResponse = new NlqResponse
        {
            Success = true,
            Intent = "ConsultaPuntual",
            Message = "Consulta exitosa",
            Output = "Resultado de la consulta",
            Risk = "SAFE",
            Evolucion = new List<EvolucionPresupuesto>
            {
                new EvolucionPresupuesto
                {
                    EjecutoraNombre = "MUNICIPALIDAD DISTRITAL DE MONZON",
                    NivelGobiernoNombre = "Gobierno Local",
                    DepartamentoEjecutoraNombre = "HUANUCO",
                    Pia2021 = 1000000,
                    Pim2021 = 1200000,
                    Devengado2021 = 900000
                }
            },
            TotalRegistros = 1,
            PaginaActual = 1,
            TamanioPagina = 10,
            TotalPaginas = 1
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        SetupBackendServiceMock(responseJson);

        // Act
        var result = await _backendService.ConsultarAsync(
            "¿Cuál fue el presupuesto de la Municipalidad de Monzón en 2021?",
            "test-session-123",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Intent.Should().Be("ConsultaPuntual");
        result.Message.Should().Be("Consulta exitosa");
        result.Output.Should().Be("Resultado de la consulta");
        result.Risk.Should().Be("SAFE");
        result.Evolucion.Should().HaveCount(1);
        result.Evolucion[0].EjecutoraNombre.Should().Be("MUNICIPALIDAD DISTRITAL DE MONZON");
        result.TotalRegistros.Should().Be(1);
    }

    [Fact]
    public async Task ConsultarAsync_EmptyChatInput_ReturnsErrorResponse()
    {
        // Arrange
        SetupBackendServiceMock("{}", HttpStatusCode.BadRequest);

        // Act
        var result = await _backendService.ConsultarAsync(
            "",
            "test-session-123",
            CancellationToken.None);

        // Assert - The service doesn't validate empty input, it sends to backend
        // Backend returns 400, so we should get an error response
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("ErrorBackend");
    }

    [Fact]
    public async Task ConsultarAsync_BackendReturns500_ReturnsErrorResponse()
    {
        // Arrange
        SetupBackendServiceMock("Internal Server Error", HttpStatusCode.InternalServerError);

        // Act
        var result = await _backendService.ConsultarAsync(
            "Consulta de prueba",
            "test-session-123",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("ErrorBackend");
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task ConsultarAsync_BackendReturnsInvalidJson_ReturnsErrorResponse()
    {
        // Arrange
        SetupBackendServiceMock("invalid json{", HttpStatusCode.OK);

        // Act
        var result = await _backendService.ConsultarAsync(
            "Consulta de prueba",
            "test-session-123",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("RespuestaInvalida");
        result.Message.Should().Contain("formato JSON inválido");
    }

    [Fact]
    public async Task ConsultarAsync_NullResponseFromBackend_ReturnsErrorResponse()
    {
        // Arrange - Empty string response
        SetupBackendServiceMock("", HttpStatusCode.OK);

        // Act
        var result = await _backendService.ConsultarAsync(
            "Consulta de prueba",
            "test-session-123",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("RespuestaInvalida");
        result.Message.Should().Contain("vacía");
    }

    [Fact]
    public async Task ConsultarAsync_BackendUrlNotConfigured_ReturnsConfigError()
    {
        // Arrange
        var configWithoutBackend = new ConfigurationBuilder().Build();
        var service = new BackendService(HttpClient, configWithoutBackend);

        // Act
        var result = await service.ConsultarAsync(
            "Consulta de prueba",
            "test-session-123",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("ErrorConfiguracion");
        result.Message.Should().Contain("No está configurada la URL del backend");
    }

    [Fact]
    public async Task ConsultarAsync_HttpRequestException_ReturnsErrorResponse()
    {
        // Arrange - Use a mock that throws HttpRequestException
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "http://localhost:3000/api/chat")
            .Throw(new HttpRequestException("Connection failed"));
        
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost/");
        
        var configWithUrl = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Backend:BaseUrl"] = "http://localhost:3000"
            })
            .Build();
        
        var service = new BackendService(httpClient, configWithUrl);

        // Act
        var result = await service.ConsultarAsync(
            "Consulta de prueba",
            "test-session-123",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Intent.Should().Be("ErrorBackend");
        result.Message.Should().Contain("No se pudo conectar");
    }

    [Fact]
    public async Task ConsultarAsync_CancellationToken_RespectsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var expectedResponse = new NlqResponse { Success = true, Intent = "Test" };
        var responseJson = JsonSerializer.Serialize(expectedResponse);
        SetupBackendServiceMock(responseJson);

        // Act & Assert - Should throw OperationCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _backendService.ConsultarAsync(
                "Consulta de prueba",
                "test-session-123",
                cts.Token);
        });
    }

    [Fact]
    public async Task ConsultarAsync_SendsCorrectRequestFormat()
    {
        // Arrange
        var capturedRequest = string.Empty;
        MockHttp.When(HttpMethod.Post, "http://localhost:3000/api/chat")
            .Respond(req =>
            {
                capturedRequest = req.Content?.ReadAsStringAsync().Result ?? "";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new NlqResponse { Success = true }))
                };
            });

        // Act
        await _backendService.ConsultarAsync(
            "Mi consulta de prueba",
            "session-abc-123",
            CancellationToken.None);

        // Assert
        capturedRequest.Should().Contain("Mi consulta de prueba");
        capturedRequest.Should().Contain("session-abc-123");
        capturedRequest.Should().Contain("chatInput");
        capturedRequest.Should().Contain("sessionId");
    }
}