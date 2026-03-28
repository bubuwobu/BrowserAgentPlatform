using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using BrowserAgentPlatform.Api.Models;
using BrowserAgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    public ConfigController(AppDbContext db) => _db = db;

    [HttpGet("proxies")]
    public Task<List<ProxyConfig>> Proxies() => _db.Proxies.OrderByDescending(x => x.Id).ToListAsync();

    [HttpPost("proxies")]
    public async Task<IActionResult> UpsertProxy(ProxyUpsertRequest request)
    {
        var item = new ProxyConfig
        {
            Name = request.Name,
            Protocol = request.Protocol,
            Host = request.Host,
            Port = request.Port,
            Username = request.Username,
            Password = request.Password,
            Notes = request.Notes
        };
        _db.Proxies.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpGet("fingerprints")]
    public Task<List<FingerprintTemplate>> Fingerprints() => _db.FingerprintTemplates.OrderByDescending(x => x.Id).ToListAsync();

    [HttpPost("fingerprints")]
    public async Task<IActionResult> UpsertFingerprint(FingerprintTemplateRequest request)
    {
        var item = new FingerprintTemplate
        {
            Name = request.Name,
            ConfigJson = request.ConfigJson
        };
        _db.FingerprintTemplates.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("demo/reset-reseed")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ResetAndReseedDemo(CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var demoTaskIds = await _db.Tasks
            .Where(x => x.Name.StartsWith("DEMO Task "))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var demoRunIds = await _db.TaskRuns
            .Where(x => demoTaskIds.Contains(x.TaskId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var removed = new Dictionary<string, int>();

        if (demoRunIds.Count > 0)
        {
            var runLogs = await _db.TaskRunLogs.Where(x => demoRunIds.Contains(x.TaskRunId)).ToListAsync(cancellationToken);
            _db.TaskRunLogs.RemoveRange(runLogs);
            removed["taskRunLogs"] = runLogs.Count;

            var artifacts = await _db.BrowserArtifacts.Where(x => demoRunIds.Contains(x.TaskRunId)).ToListAsync(cancellationToken);
            _db.BrowserArtifacts.RemoveRange(artifacts);
            removed["browserArtifacts"] = artifacts.Count;

            var isolationReports = await _db.RunIsolationReports.Where(x => demoRunIds.Contains(x.TaskRunId)).ToListAsync(cancellationToken);
            _db.RunIsolationReports.RemoveRange(isolationReports);
            removed["runIsolationReports"] = isolationReports.Count;

            var runLocks = await _db.BrowserProfileLocks.Where(x => x.TaskRunId.HasValue && demoRunIds.Contains(x.TaskRunId.Value)).ToListAsync(cancellationToken);
            _db.BrowserProfileLocks.RemoveRange(runLocks);
            removed["browserProfileLocksByRun"] = runLocks.Count;
        }

        if (demoTaskIds.Count > 0)
        {
            var taskRuns = await _db.TaskRuns.Where(x => demoTaskIds.Contains(x.TaskId)).ToListAsync(cancellationToken);
            _db.TaskRuns.RemoveRange(taskRuns);
            removed["taskRuns"] = taskRuns.Count;

            var tasks = await _db.Tasks.Where(x => demoTaskIds.Contains(x.Id)).ToListAsync(cancellationToken);
            _db.Tasks.RemoveRange(tasks);
            removed["tasks"] = tasks.Count;
        }

        var demoProfileIds = await _db.BrowserProfiles
            .Where(x => x.Name.StartsWith("DEMO Profile "))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (demoProfileIds.Count > 0)
        {
            var profileLocks = await _db.BrowserProfileLocks.Where(x => demoProfileIds.Contains(x.ProfileId)).ToListAsync(cancellationToken);
            _db.BrowserProfileLocks.RemoveRange(profileLocks);
            removed["browserProfileLocksByProfile"] = profileLocks.Count;

            var profiles = await _db.BrowserProfiles.Where(x => demoProfileIds.Contains(x.Id)).ToListAsync(cancellationToken);
            _db.BrowserProfiles.RemoveRange(profiles);
            removed["profiles"] = profiles.Count;
        }

        var demoAgents = await _db.Agents.Where(x => x.AgentKey.StartsWith("demo-agent-")).ToListAsync(cancellationToken);
        _db.Agents.RemoveRange(demoAgents);
        removed["agents"] = demoAgents.Count;

        var demoProxies = await _db.Proxies.Where(x => x.Name.StartsWith("Demo ")).ToListAsync(cancellationToken);
        _db.Proxies.RemoveRange(demoProxies);
        removed["proxies"] = demoProxies.Count;

        var demoAuditEvents = await _db.AuditEvents
            .Where(x => x.EventType == "demo_seed" || (x.ActorType == "system" && x.ActorId == "db_seeder"))
            .ToListAsync(cancellationToken);
        _db.AuditEvents.RemoveRange(demoAuditEvents);
        removed["auditEvents"] = demoAuditEvents.Count;

        await _db.SaveChangesAsync(cancellationToken);
        await DbSeeder.SeedAsync(_db);
        await tx.CommitAsync(cancellationToken);

        var actor = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "unknown";
        return Ok(new
        {
            ok = true,
            message = "Demo data has been reset and reseeded.",
            actor,
            removed
        });
    }
}
