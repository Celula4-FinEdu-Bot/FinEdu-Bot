using src.Models;

namespace src.Interfaces;


public interface INlqService
{
    Task<NlqResponse> ProcessAsync(
        NlqRequest request,
        CancellationToken cancellationToken = default);
}