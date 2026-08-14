using FinEduBot.Frontend.Models;

namespace FinEduBot.Frontend.Services.Interfaces;

public interface INlqService
{
    Task<NlqResponse> ProcessAsync(
        NlqRequest request,
        CancellationToken cancellationToken = default);
}