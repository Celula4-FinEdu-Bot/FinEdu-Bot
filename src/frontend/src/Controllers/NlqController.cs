using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using src.Interfaces;
using src.Models;


namespace src.Controllers;


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