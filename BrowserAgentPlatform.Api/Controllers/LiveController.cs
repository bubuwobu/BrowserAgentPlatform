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
        var agents = await _db.Agents
            .OrderByDescending(x => x.LastHeartbeatAt)
            .Take(10)
            .Select(x => new
            {
                x.Id,
                Name = x.Name ?? "",
                MachineName = x.MachineName ?? "",
                Status = x.Status ?? "offline",
                x.CurrentRuns,
                x.MaxParallelRuns,
                x.LastHeartbeatAt
            })
            .ToListAsync();

        var profiles = await _db.BrowserProfiles
            .OrderByDescending(x => x.LastUsedAt ?? x.LastStartedAt ?? x.CreatedAt)
            .Take(10)
            .Select(x => new
            {
                x.Id,
                Name = x.Name ?? "",
                Status = x.Status ?? "idle",
                x.OwnerAgentId,
                x.ProxyId,
                x.FingerprintTemplateId,
                x.LastUsedAt,
                x.WorkspaceKey,
                x.ProfileRootPath,
                x.ArtifactRootPath,
                x.TempRootPath,
                x.LifecycleState,
                x.RuntimeMetaJson,
                x.LastStartedAt,
                x.LastStoppedAt,
                x.LastIsolationCheckAt
            })
            .ToListAsync();

        var lifecycleCounts = await _db.BrowserProfiles
            .GroupBy(x => x.LifecycleState ?? "created")
            .Select(g => new { lifecycle = g.Key, count = g.Count() })
            .ToListAsync();

        var recentRuns = await _db.TaskRuns
            .OrderByDescending(x => x.Id)
            .Take(8)
            .Select(x => new
            {
                x.Id,
                x.TaskId,
                x.BrowserProfileId,
                Status = x.Status ?? "queued",
                CurrentStepLabel = x.CurrentStepLabel ?? "",
                CurrentUrl = x.CurrentUrl ?? "",
                LastPreviewPath = x.LastPreviewPath ?? "",
                x.CreatedAt,
                x.StartedAt,
                x.FinishedAt
            })
            .ToListAsync();

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
            lifecycleCounts,
            recentRuns,
            recentAgents = agents,
            recentProfiles = profiles
        };

        return Ok(data);
    }
}
