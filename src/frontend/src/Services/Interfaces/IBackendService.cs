using src.Models;

namespace src.Interfaces;

public interface IBackendService
{
    Task<NlqResponse> ConsultarAsync(
        string chatInput,
        string sessionId,
        CancellationToken cancellationToken = default);
}