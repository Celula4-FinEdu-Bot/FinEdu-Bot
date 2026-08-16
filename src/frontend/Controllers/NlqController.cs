using FinEduBot.Frontend.Models;
using FinEduBot.Frontend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinEduBot.Frontend.Controllers;

[ApiController]
[Route("api/nlq")]
public sealed class NlqController : ControllerBase
{
    private readonly INlqService _nlqService;

    public NlqController(INlqService nlqService)
    {
        _nlqService = nlqService;
    }

    [HttpPost]
    public async Task<ActionResult<NlqResponse>> Query(
        [FromBody] NlqRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(
                new
                {
                    message =
                        "La consulta no puede estar vacía."
                });
        }

        var response =
            await _nlqService.ProcessAsync(
                request,
                cancellationToken);

        return Ok(response);
    }
}