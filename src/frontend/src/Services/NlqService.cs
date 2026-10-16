using src.Interfaces;
using src.Models;

namespace src.Services;

public sealed class NlqService
{
    private readonly IBackendService _backendService;

    public NlqService(IBackendService backendService)
    {
        _backendService = backendService;
    }

    public async Task<NlqResponse> ProcesarAsync(
        string pregunta,
        int pagina = 1,
        int tamanioPagina = 10,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new NlqResponse
            {
                Success = false,
                Intent = "ConsultaVacia",
                Message = "Escribe una consulta."
            };
        }

        sessionId ??= Guid.NewGuid().ToString("N");

        return await _backendService.ConsultarAsync(
            pregunta.Trim(),
            sessionId,
            cancellationToken);
    }
}