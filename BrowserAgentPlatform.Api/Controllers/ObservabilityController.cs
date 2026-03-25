using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/observability")]
public class ObservabilityController : ControllerBase
{
    private readonly ObservabilityService _observabilityService;
    private readonly AppDbContext _db;

    public ObservabilityController(ObservabilityService observabilityService, AppDbContext db)
    {
        _observabilityService = observabilityService;
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var overview = await _observabilityService.GetOverviewAsync(cancellationToken);
        return Ok(overview);
    }

    [HttpGet("audit-events")]
    public async Task<IActionResult> AuditEvents([FromQuery] int take = 200, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 1000);
        var events = await _db.Set<BrowserAgentPlatform.Api.Data.Entities.AuditEvent>()
            .OrderByDescending(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return Ok(events);
    }
}
