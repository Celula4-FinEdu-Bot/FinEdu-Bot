using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using src.Services;
using Xunit;

namespace FinEdu.Tests.Services;

public class OeceServiceTests : TestBase
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly OeceService _oeceService;

    public OeceServiceTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(x => x.ContentRootPath).Returns(@"C:\Test\Project\src\frontend\src");
        
        _oeceService = new OeceService(_mockEnvironment.Object);
    }

    [Fact]
    public async Task BuscarAsync_FileNotFound_ReturnsErrorResult()
    {
        // Arrange - default mock returns non-existent path

        // Act
        var result = await _oeceService.BuscarAsync(
            texto: "Monzón",
            cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No se encontró el archivo Excel OECE");
        result.Records.Should().BeEmpty();
        result.Total.Should().Be(0);
    }
}