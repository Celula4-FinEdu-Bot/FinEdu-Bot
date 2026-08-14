using FinEduBot.Frontend.Models;

namespace FinEduBot.Frontend.Services.Interfaces;

public interface IOeceService
{
    Task<IReadOnlyList<ContratacionDto>>
        ObtenerContratacionesAsync(
            CancellationToken cancellationToken = default);

    Task<ContratacionDto?>
        ObtenerPorOcidAsync(
            string ocid,
            CancellationToken cancellationToken = default);
}