using BrowserAgentPlatform.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/live")]
public class LiveController : ControllerBase
{
    private readonly AppDbContext _db;
    public LiveController(AppDbContext db) => _db = db;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var agents = await _db.Agents.OrderByDescending(x => x.LastHeartbeatAt).Take(10).ToListAsync();
        var profiles = await _db.BrowserProfiles.OrderByDescending(x => x.Id).Take(10).ToListAsync();
        var recentRuns = await _db.TaskRuns.OrderByDescending(x => x.Id).Take(8).ToListAsync();

        var data = new
        {
            agents = await _db.Agents.CountAsync(),
            onlineAgents = await _db.Agents.CountAsync(x => x.Status == "online"),
            profiles = await _db.BrowserProfiles.CountAsync(),
            idleProfiles = await _db.BrowserProfiles.CountAsync(x => x.Status == "idle"),
            queued = await _db.TaskRuns.CountAsync(x => x.Status == "queued"),
            running = await _db.TaskRuns.CountAsync(x => x.Status == "running" || x.Status == "leased"),
            completed = await _db.TaskRuns.CountAsync(x => x.Status == "completed"),
            failed = await _db.TaskRuns.CountAsync(x => x.Status == "failed"),
            recentRuns = recentRuns.Select(x => new
            {
                x.Id,
                x.TaskId,
                x.Status,
                x.CurrentStepLabel,
                x.CurrentUrl,
                x.LastPreviewPath,
                x.CreatedAt,
                x.StartedAt,
                x.FinishedAt
            }),
            recentAgents = agents.Select(x => new
            {
                x.Id,
                x.Name,
                x.MachineName,
                x.Status,
                x.CurrentRuns,
                x.MaxParallelRuns,
                x.LastHeartbeatAt
            }),
            recentProfiles = profiles.Select(x => new
            {
                x.Id,
                x.Name,
                x.Status,
                x.OwnerAgentId,
                x.ProxyId,
                x.FingerprintTemplateId,
                x.LastUsedAt
            })
        };

        return Ok(data);
    }
}
