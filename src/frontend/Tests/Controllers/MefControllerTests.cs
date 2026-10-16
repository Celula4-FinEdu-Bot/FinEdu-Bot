using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using src.Controllers;
using src.Interfaces;
using src.Models;
using Xunit;

namespace FinEdu.Tests.Controllers;

public class MefControllerTests
{
    private readonly Mock<IMeFService> _mockMefService;
    private readonly MefController _controller;

    public MefControllerTests()
    {
        _mockMefService = new Mock<IMeFService>();
        _controller = new MefController(_mockMefService.Object);
    }

    [Fact]
    public async Task PresupuestoMensual_ValidRequest_ReturnsOkWithData()
    {
        // Arrange
        var expectedData = new List<PresupuestoMensualDto>
        {
            new PresupuestoMensualDto { Anio = 2021, Mes = 1, NombreMes = "Enero", Presupuesto = 1000000, Ejecutado = 900000 }
        };

        _mockMefService
            .Setup(x => x.ObtenerEvolucionMensualAsync(2021, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.PresupuestoMensual(2021, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedData);
    }

    [Fact]
    public async Task PresupuestoMensual_NoYearProvided_PassesNullToService()
    {
        // Arrange
        var expectedData = new List<PresupuestoMensualDto>();
        _mockMefService
            .Setup(x => x.ObtenerEvolucionMensualAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.PresupuestoMensual(null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        _mockMefService.Verify(
            x => x.ObtenerEvolucionMensualAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Proyectos_ValidRequest_ReturnsOkWithData()
    {
        // Arrange
        var expectedData = new List<ProyectoPresupuestoDto>
        {
            new ProyectoPresupuestoDto { Categoria = "Obras", Proyecto = "Proyecto A", Presupuesto = 5000000, Ejecutado = 4000000 }
        };

        _mockMefService
            .Setup(x => x.ObtenerProyectosAsync(2021, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.Proyectos(2021, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedData);
    }

    [Fact]
    public async Task Total_ValidRequest_ReturnsOkWithCount()
    {
        // Arrange
        _mockMefService
            .Setup(x => x.ObtenerTotalRegistrosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(150);

        // Act
        var result = await _controller.Total(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(150);
    }

    [Fact]
    public async Task Total_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockMefService
            .Setup(x => x.ObtenerTotalRegistrosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("MEF API unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await _controller.Total(CancellationToken.None);
        });
    }
}