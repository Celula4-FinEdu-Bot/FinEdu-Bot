using src.Models;

namespace src.Interfaces;


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