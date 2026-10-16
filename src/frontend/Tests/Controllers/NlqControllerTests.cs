using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using src.Controllers;
using src.Interfaces;
using src.Models;
using Xunit;

namespace FinEdu.Tests.Controllers;

public class NlqControllerTests
{
    private readonly Mock<INlqService> _mockNlqService;
    private readonly NlqController _controller;

    public NlqControllerTests()
    {
        _mockNlqService = new Mock<INlqService>();
        _controller = new NlqController(_mockNlqService.Object);
    }

    [Fact]
    public async Task Query_ValidRequest_ReturnsOkWithResponse()
    {
        // Arrange
        var request = new NlqRequest { Query = "Consulta de prueba" };
        var expectedResponse = new NlqResponse
        {
            Success = true,
            Intent = "ConsultaPuntual",
            Message = "Datos encontrados",
            Evolucion = new List<EvolucionPresupuesto>
            {
                new EvolucionPresupuesto { EjecutoraNombre = "Entidad Test" }
            }
        };

        _mockNlqService
            .Setup(x => x.ProcessAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Query(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Query_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Query(null!, CancellationToken.None);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Query_EmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var request = new NlqRequest { Query = "" };

        // Act
        var result = await _controller.Query(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Query_WhitespaceQuery_ReturnsBadRequest()
    {
        // Arrange
        var request = new NlqRequest { Query = "   " };

        // Act
        var result = await _controller.Query(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Query_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var request = new NlqRequest { Query = "Consulta de prueba" };
        _mockNlqService
            .Setup(x => x.ProcessAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _controller.Query(request, CancellationToken.None);
        });
    }

    [Fact]
    public async Task Query_CancellationToken_PassedToService()
    {
        // Arrange
        var request = new NlqRequest { Query = "Consulta de prueba" };
        var cts = new CancellationTokenSource();
        var expectedResponse = new NlqResponse { Success = true };

        _mockNlqService
            .Setup(x => x.ProcessAsync(request, cts.Token))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.Query(request, cts.Token);

        // Assert
        _mockNlqService.Verify(
            x => x.ProcessAsync(request, cts.Token),
            Times.Once);
    }
}