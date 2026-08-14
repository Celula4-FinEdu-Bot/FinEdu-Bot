using FinEduBot.Frontend.Models;

namespace FinEduBot.Frontend.Services.Interfaces;

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