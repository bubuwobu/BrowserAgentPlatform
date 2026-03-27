using BrowserAgentPlatform.Api.Models;
using BrowserAgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/validation/closed-loop")]
public class ValidationController : ControllerBase
{
    private readonly ClosedLoopValidationService _closedLoopValidationService;

    public ValidationController(ClosedLoopValidationService closedLoopValidationService)
    {
        _closedLoopValidationService = closedLoopValidationService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(ClosedLoopStartRequest request, CancellationToken cancellationToken)
    {
        var result = await _closedLoopValidationService.StartAsync(
            request.ProfileId,
            request.AgentKey,
            request.TaskName,
            request.PayloadJson,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute(ClosedLoopExecuteRequest request, CancellationToken cancellationToken)
    {
        var result = await _closedLoopValidationService.ExecuteAsync(request.RunId, request.AgentKey, cancellationToken);
        return Ok(result);
    }
}
