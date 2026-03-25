using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using BrowserAgentPlatform.Api.Models;
using BrowserAgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SchedulerService _scheduler;
    private readonly ArtifactService _artifactService;
    private readonly LiveHubNotifier _notifier;

    public AgentsController(AppDbContext db, SchedulerService scheduler, ArtifactService artifactService, LiveHubNotifier notifier)
    {
        _db = db;
        _scheduler = scheduler;
        _artifactService = artifactService;
        _notifier = notifier;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(AgentRegisterRequest request)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(x => x.AgentKey == request.AgentKey);
        if (agent is null)
        {
            agent = new AgentNode
            {
                AgentKey = request.AgentKey,
                Name = request.Name,
                MachineName = request.MachineName,
                MaxParallelRuns = request.MaxParallelRuns,
                SchedulerTags = request.SchedulerTags,
                Status = "online",
                LastHeartbeatAt = DateTime.UtcNow
            };
            _db.Agents.Add(agent);
        }
        else
        {
            agent.Name = request.Name;
            agent.MachineName = request.MachineName;
            agent.MaxParallelRuns = request.MaxParallelRuns;
            agent.SchedulerTags = request.SchedulerTags;
            agent.Status = "online";
            agent.LastHeartbeatAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Ok(agent);
    }

    [AllowAnonymous]
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(AgentHeartbeatRequest request)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(x => x.AgentKey == request.AgentKey);
        if (agent is null) return NotFound();
        agent.Status = "online";
        agent.LastHeartbeatAt = DateTime.UtcNow;
        agent.CurrentRuns = request.CurrentRuns;
        await _db.SaveChangesAsync();

        var commands = await _db.AgentCommands
            .Where(x => x.AgentId == agent.Id && x.Status == "pending")
            .OrderBy(x => x.Id)
            .Take(20)
            .ToListAsync();

        return Ok(new { ok = true, commands });
    }

    [AllowAnonymous]
    [HttpPost("pull/{agentKey}")]
    public async Task<IActionResult> Pull(string agentKey)
    {
        var result = await _scheduler.LeaseNextRunForAgentAsync(agentKey);
        if (result is null) return Ok(new AgentPullResponse(null, null, null, null, null, null, null, null));

        var profile = await _db.BrowserProfiles.FindAsync(result.Value.run.BrowserProfileId);
        return Ok(new AgentPullResponse(
            result.Value.run.Id,
            result.Value.run.TaskId,
            result.Value.run.BrowserProfileId,
            result.Value.profileLock.LeaseToken,
            result.Value.task.PayloadJson,
            result.Value.task.TimeoutSeconds,
            result.Value.task.RetryPolicyJson,
            profile?.IsolationPolicyJson
        ));
    }

    [AllowAnonymous]
    [HttpPost("report-progress")]
    public async Task<IActionResult> ReportProgress(AgentProgressRequest request)
    {
        var run = await _db.TaskRuns.FindAsync(request.TaskRunId);
        if (run is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.LeaseToken) || request.LeaseToken != run.LeaseToken)
        {
            return Conflict(new { ok = false, message = "lease token mismatch or expired" });
        }

        run.Status = request.Status;
        run.CurrentStepId = request.CurrentStepId;
        run.CurrentStepLabel = request.CurrentStepLabel;
        run.CurrentUrl = request.CurrentUrl;
        run.HeartbeatAt = request.HeartbeatAt ?? DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.PreviewBase64))
        {
            run.LastPreviewPath = await _artifactService.SavePreviewAsync(run.Id, request.PreviewBase64);
        }

        _db.TaskRunLogs.Add(new TaskRunLog
        {
            TaskRunId = run.Id,
            StepId = request.CurrentStepId,
            Level = request.Status == "failed" ? "error" : "info",
            Message = request.Message
        });
        await _db.SaveChangesAsync();

        await _notifier.PublishRunUpdateAsync(run.Id, new
        {
            run.Id,
            run.Status,
            run.CurrentStepId,
            run.CurrentStepLabel,
            run.CurrentUrl,
            run.LastPreviewPath,
            message = request.Message
        });

        var refreshed = await _scheduler.RefreshLeaseAsync(run.Id, request.LeaseToken);
        if (!refreshed) return Conflict(new { ok = false, message = "lease expired" });
        return Ok(new { ok = true });
    }

    [AllowAnonymous]
    [HttpPost("report-complete")]
    public async Task<IActionResult> ReportComplete(AgentCompleteRequest request)
    {
        var run = await _db.TaskRuns.FindAsync(request.TaskRunId);
        if (run is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.LeaseToken) || request.LeaseToken != run.LeaseToken)
        {
            return Conflict(new { ok = false, message = "lease token mismatch or expired" });
        }

        run.Status = request.Status;
        run.ResultJson = request.ResultJson;
        run.ErrorCode = request.ErrorCode ?? "";
        run.ErrorMessage = request.ErrorMessage ?? "";
        run.FinishedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.FinalPreviewBase64))
        {
            run.LastPreviewPath = await _artifactService.SavePreviewAsync(run.Id, request.FinalPreviewBase64, "final");
        }

        var task = await _db.Tasks.FindAsync(run.TaskId);
        if (task is not null) task.Status = request.Status;

        _db.TaskRunLogs.Add(new TaskRunLog
        {
            TaskRunId = run.Id,
            Level = request.Status == "completed" ? "info" : "error",
            Message = $"Run finished with status: {request.Status}"
        });

        if (!string.IsNullOrWhiteSpace(request.IsolationReportJson))
        {
            var reportJson = request.IsolationReportJson;
            using var doc = JsonDocument.Parse(reportJson);
            var root = doc.RootElement;
            string GetStringOrDefault(string name, string fallback = "{}")
                => root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? fallback : fallback;

            _db.RunIsolationReports.Add(new RunIsolationReport
            {
                TaskRunId = run.Id,
                BrowserProfileId = run.BrowserProfileId,
                ProxySnapshotJson = GetStringOrDefault("proxySnapshotJson"),
                FingerprintSnapshotJson = GetStringOrDefault("fingerprintSnapshotJson"),
                StorageCheckJson = GetStringOrDefault("storageCheckJson"),
                NetworkCheckJson = GetStringOrDefault("networkCheckJson"),
                Result = GetStringOrDefault("result", "pass")
            });
        }

        await _db.SaveChangesAsync();
        await _scheduler.ReleaseRunAsync(run.Id);
        await _notifier.PublishRunUpdateAsync(run.Id, new { run.Id, run.Status, run.LastPreviewPath, completed = true });
        return Ok(new { ok = true });
    }

    [Authorize]
    [HttpGet]
    public Task<List<AgentNode>> List() => _db.Agents.OrderByDescending(x => x.LastHeartbeatAt).ToListAsync();
}
