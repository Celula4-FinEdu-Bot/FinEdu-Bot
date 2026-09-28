using Microsoft.Extensions.Configuration;
using RichardSzalay.MockHttp;
using System.Net.Http;

namespace FinEdu.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly MockHttpMessageHandler MockHttp;
    protected readonly HttpClient HttpClient;
    protected readonly IConfiguration Configuration;

    protected TestBase()
    {
        MockHttp = new MockHttpMessageHandler();
        HttpClient = MockHttp.ToHttpClient();
        HttpClient.BaseAddress = new Uri("http://localhost/");

        // Setup configuration
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Backend:BaseUrl"] = "http://localhost:3000"
            });
        Configuration = configBuilder.Build();
    }

    public void Dispose()
    {
        MockHttp.Dispose();
        HttpClient.Dispose();
    }

    protected void SetupBackendServiceMock(string responseJson, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        MockHttp.When(HttpMethod.Post, "http://localhost:3000/api/chat")
            .Respond(statusCode, "application/json", responseJson);
    }

    protected void SetupMefServiceMock(string responseJson, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        MockHttp.When(HttpMethod.Get, "https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/datastore_search*")
            .Respond(statusCode, "application/json", responseJson);
    }
}