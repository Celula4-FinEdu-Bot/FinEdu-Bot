using src.Models;

namespace src.Interfaces;

public interface IMeFService
{
    Task<IReadOnlyList<PresupuestoMensualDto>>
        ObtenerEvolucionMensualAsync(
            int? anio = null,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProyectoPresupuestoDto>>
        ObtenerProyectosAsync(
            int? anio = null,
            CancellationToken cancellationToken = default);

    Task<int> ObtenerTotalRegistrosAsync(
        CancellationToken cancellationToken = default);
}