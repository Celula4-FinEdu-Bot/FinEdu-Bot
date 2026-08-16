using FinEduBot.Frontend.Models;
using FinEduBot.Frontend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinEduBot.Frontend.Controllers;

[ApiController]
[Route("api/mef")]
public sealed class MefController : ControllerBase
{
    private readonly IMeFService _mefService;

    public MefController(IMeFService mefService)
    {
        _mefService = mefService;
    }

    [HttpGet("presupuesto-mensual")]
    public async Task<ActionResult<IReadOnlyList<PresupuestoMensualDto>>>
        PresupuestoMensual(
            [FromQuery] int? anio,
            CancellationToken cancellationToken)
    {
        var result =
            await _mefService
                .ObtenerEvolucionMensualAsync(
                    anio,
                    cancellationToken);

        return Ok(result);
    }

    [HttpGet("proyectos")]
    public async Task<ActionResult<IReadOnlyList<ProyectoPresupuestoDto>>>
        Proyectos(
            [FromQuery] int? anio,
            CancellationToken cancellationToken)
    {
        var result =
            await _mefService
                .ObtenerProyectosAsync(
                    anio,
                    cancellationToken);

        return Ok(result);
    }

    [HttpGet("total")]
    public async Task<ActionResult<int>>
        Total(CancellationToken cancellationToken)
    {
        var total =
            await _mefService
                .ObtenerTotalRegistrosAsync(
                    cancellationToken);

        return Ok(total);
    }
}