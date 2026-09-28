using System.Text.Json;
using System.Net;
using FluentAssertions;
using RichardSzalay.MockHttp;
using src.Models;
using src.Services;
using Xunit;

namespace FinEdu.Tests.Services;

public class MefServiceTests : TestBase
{
    private readonly MefService _mefService;

    public MefServiceTests()
    {
        _mefService = new MefService(HttpClient);
    }

    [Fact]
    public async Task ObtenerEvolucionPaginaAsync_ValidResponse_ReturnsMappedData()
    {
        // Arrange
        var mefResponse = new
        {
            result = new
            {
                total = 100,
                records = new[]
                {
                    new
                    {
                        EJECUTORA_NOMBRE = "MUNICIPALIDAD DISTRITAL DE MONZON",
                        NIVEL_GOBIERNO_NOMBRE = "Gobierno Local",
                        DEPARTAMENTO_EJECUTORA_NOMBRE = "HUANUCO",
                        PIA_2017 = "1000000",
                        PIM_2017 = "1200000",
                        DEVENGADO_2017 = "900000",
                        GIRADO_2017 = "800000",
                        CERTIFICADO_2017 = "850000",
                        COMPROMETIDO_ANUAL_2017 = "950000",
                        COMPROMETIDO_2017 = "920000",
                        PIA_2018 = "1100000",
                        PIM_2018 = "1300000",
                        DEVENGADO_2018 = "1000000",
                        GIRADO_2018 = "900000",
                        CERTIFICADO_2018 = "950000",
                        COMPROMETIDO_ANUAL_2018 = "1050000",
                        COMPROMETIDO_2018 = "1020000",
                        PIA_2019 = "1200000",
                        PIM_2019 = "1400000",
                        DEVENGADO_2019 = "1100000",
                        GIRADO_2019 = "1000000",
                        CERTIFICADO_2019 = "1050000",
                        COMPROMETIDO_ANUAL_2019 = "1150000",
                        COMPROMETIDO_2019 = "1120000",
                        PIA_2020 = "1300000",
                        PIM_2020 = "1500000",
                        DEVENGADO_2020 = "1200000",
                        GIRADO_2020 = "1100000",
                        CERTIFICADO_2020 = "1150000",
                        COMPROMETIDO_ANUAL_2020 = "1250000",
                        COMPROMETIDO_2020 = "1220000",
                        PIA_2021 = "1400000",
                        PIM_2021 = "1600000",
                        DEVENGADO_2021 = "1300000",
                        GIRADO_2021 = "1200000",
                        CERTIFICADO_2021 = "1250000",
                        COMPROMETIDO_ANUAL_2021 = "1350000",
                        COMPROMETIDO_2021 = "1320000"
                    }
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(mefResponse);
        SetupMefServiceMock(responseJson);

        // Act
        var result = await _mefService.ObtenerEvolucionPaginaAsync(
            null,
            1,
            20,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Records.Should().HaveCount(1);
        result.Total.Should().Be(100);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);

        var record = result.Records[0];
        record.EjecutoraNombre.Should().Be("MUNICIPALIDAD DISTRITAL DE MONZON");
        record.NivelGobiernoNombre.Should().Be("Gobierno Local");
        record.DepartamentoEjecutoraNombre.Should().Be("HUANUCO");
        record.Pia2017.Should().Be(1000000);
        record.Pim2017.Should().Be(1200000);
        record.Devengado2017.Should().Be(900000);
        record.Pia2021.Should().Be(1400000);
        record.Pim2021.Should().Be(1600000);
        record.Devengado2021.Should().Be(1300000);
    }

    [Fact]
    public async Task ObtenerEvolucionPaginaAsync_EmptyRecords_ReturnsEmptyList()
    {
        // Arrange
        var mefResponse = new
        {
            result = new
            {
                total = 0,
                records = Array.Empty<object>()
            }
        };

        var responseJson = JsonSerializer.Serialize(mefResponse);
        SetupMefServiceMock(responseJson);

        // Act
        var result = await _mefService.ObtenerEvolucionPaginaAsync(
            "EntidadInexistente",
            1,
            20,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Records.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task ObtenerEvolucionPaginaAsync_HttpError_ReturnsEmptyResult()
    {
        // Arrange
        MockHttp.When(HttpMethod.Get, "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/datastore_search*")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{ \"error\": \"Server error\" }");

        // Act
        var result = await _mefService.ObtenerEvolucionPaginaAsync(
            null,
            1,
            20,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Records.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task ObtenerEvolucionPaginaAsync_InvalidJson_ReturnsEmptyResult()
    {
        // Arrange
        MockHttp.When(HttpMethod.Get, "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/datastore_search*")
            .Respond(HttpStatusCode.OK, "application/json", "invalid json{");

        // Act
        var result = await _mefService.ObtenerEvolucionPaginaAsync(
            null,
            1,
            20,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Records.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task ObtenerEvolucionPaginaAsync_PaginationParameters_Respected()
    {
        // Arrange
        var mefResponse = new
        {
            result = new
            {
                total = 100,
                records = new[]
                {
                    new { EJECUTORA_NOMBRE = "Entidad 1" },
                    new { EJECUTORA_NOMBRE = "Entidad 2" }
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(mefResponse);
        
        MockHttp.When(HttpMethod.Get, "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/datastore_search*")
            .Respond(req =>
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri!.Query);
                query["limit"].Should().Be("5");
                query["offset"].Should().Be("10");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson)
                };
            });

        // Act
        var result = await _mefService.ObtenerEvolucionPaginaAsync(
            null,
            3, // page 3
            5, // page size 5
            CancellationToken.None);

        // Assert
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task ObtenerEvolucionPaginaAsync_PageSizeLimitedTo100()
    {
        // Arrange
        var mefResponse = new { result = new { total = 100, records = Array.Empty<object>() } };
        var responseJson = JsonSerializer.Serialize(mefResponse);

        MockHttp.When(HttpMethod.Get, "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/datastore_search*")
            .Respond(req =>
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri!.Query);
                query["limit"].Should().Be("100"); // Should be capped at 100
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson)
                };
            });

        // Act
        var result = await _mefService.ObtenerEvolucionPaginaAsync(
            null,
            1,
            200, // Request 200, should be capped to 100
            CancellationToken.None);

        // Assert
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task ObtenerEvolucionPaginaAsync_FilterParameter_SentCorrectly()
    {
        // Arrange
        var mefResponse = new { result = new { total = 1, records = new[] { new { EJECUTORA_NOMBRE = "MUNICIPALIDAD" } } } };
        var responseJson = JsonSerializer.Serialize(mefResponse);

        MockHttp.When(HttpMethod.Get, "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/datastore_search*")
            .Respond(req =>
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri!.Query);
                query["q"].Should().Be("Monzón");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson)
                };
            });

        // Act
        await _mefService.ObtenerEvolucionPaginaAsync(
            "Monzón",
            1,
            20,
            CancellationToken.None);
    }

    [Fact]
    public async Task ObtenerEvolucionAsync_CallsPagedMethod()
    {
        // Arrange
        var mefResponse = new
        {
            result = new
            {
                total = 2,
                records = new[]
                {
                    new { EJECUTORA_NOMBRE = "Entidad 1" },
                    new { EJECUTORA_NOMBRE = "Entidad 2" }
                }
            }
        };
        var responseJson = JsonSerializer.Serialize(mefResponse);
        SetupMefServiceMock(responseJson);

        // Act
        var result = await _mefService.ObtenerEvolucionAsync(null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ObtenerEvolucionPaginaAsync_NullFilter_TreatedAsEmpty()
    {
        // Arrange
        var mefResponse = new { result = new { total = 1, records = new[] { new { EJECUTORA_NOMBRE = "Entidad" } } } };
        var responseJson = JsonSerializer.Serialize(mefResponse);

        MockHttp.When(HttpMethod.Get, "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/datastore_search*")
            .Respond(req =>
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri!.Query);
                query["q"].Should().Be("");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson)
                };
            });

        // Act
        await _mefService.ObtenerEvolucionPaginaAsync(
            null,
            1,
            20,
            CancellationToken.None);
    }
}