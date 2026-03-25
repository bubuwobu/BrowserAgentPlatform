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
    private readonly IsolationPolicyService _isolationPolicyService;
    private readonly AgentRequestSecurityService _agentRequestSecurityService;
    private readonly AuditService _auditService;

    public AgentsController(
        AppDbContext db,
        SchedulerService scheduler,
        ArtifactService artifactService,
        LiveHubNotifier notifier,
        IsolationPolicyService isolationPolicyService,
        AgentRequestSecurityService agentRequestSecurityService,
        AuditService auditService)
    {
        _db = db;
        _scheduler = scheduler;
        _artifactService = artifactService;
        _notifier = notifier;
        _isolationPolicyService = isolationPolicyService;
        _agentRequestSecurityService = agentRequestSecurityService;
        _auditService = auditService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(AgentRegisterRequest request)
    {
        var security = _agentRequestSecurityService.Validate(HttpContext.Request, request.AgentKey);
        if (!security.ok) return Unauthorized(new { ok = false, message = security.reason });

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
        await _auditService.WriteAsync("agent_register", "agent", request.AgentKey, "agent", agent.Id.ToString(), $"{{\"machine\":\"{request.MachineName}\"}}");
        return Ok(agent);
    }

    [AllowAnonymous]
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(AgentHeartbeatRequest request)
    {
        var security = _agentRequestSecurityService.Validate(HttpContext.Request, request.AgentKey);
        if (!security.ok) return Unauthorized(new { ok = false, message = security.reason });

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
        var security = _agentRequestSecurityService.Validate(HttpContext.Request, agentKey);
        if (!security.ok) return Unauthorized(new { ok = false, message = security.reason });

        var result = await _scheduler.LeaseNextRunForAgentAsync(agentKey);
        if (result is null) return Ok(new AgentPullResponse(null, null, null, null, null, null, null, null));

        var profile = await _db.BrowserProfiles.FindAsync(result.Value.run.BrowserProfileId);
        if (profile is null)
        {
            result.Value.run.Status = "failed";
            result.Value.run.ErrorCode = "missing_profile";
            result.Value.run.ErrorMessage = "Profile not found during pull.";
            await _db.SaveChangesAsync();
            await _scheduler.ReleaseRunAsync(result.Value.run.Id);
            await _auditService.WriteAsync("run_pull_rejected", "agent", agentKey, "task_run", result.Value.run.Id.ToString(), "{\"reason\":\"missing_profile\"}");
            return Ok(new AgentPullResponse(null, null, null, null, null, null, null, null));
        }

        var check = await _isolationPolicyService.CheckProfileAsync(profile);
        if (!check.Ok)
        {
            result.Value.run.Status = "failed";
            result.Value.run.ErrorCode = "isolation_validation_failed";
            result.Value.run.ErrorMessage = string.Join("; ", check.Errors);
            _db.TaskRunLogs.Add(new TaskRunLog
            {
                TaskRunId = result.Value.run.Id,
                Level = "error",
                StepId = "isolation_check",
                Message = result.Value.run.ErrorMessage
            });
            await _db.SaveChangesAsync();
            await _scheduler.ReleaseRunAsync(result.Value.run.Id);
            await _auditService.WriteAsync("run_pull_rejected", "agent", agentKey, "task_run", result.Value.run.Id.ToString(), $"{{\"reason\":\"isolation_validation_failed\",\"errors\":{JsonSerializer.Serialize(check.Errors)}}}");
            return Ok(new AgentPullResponse(null, null, null, null, null, null, null, null));
        }

        profile.LastIsolationCheckAt = DateTime.UtcNow;
        profile.RuntimeMetaJson = check.EffectivePolicyJson;
        await _db.SaveChangesAsync();
        await _auditService.WriteAsync("run_pulled", "agent", agentKey, "task_run", result.Value.run.Id.ToString());

        return Ok(new AgentPullResponse(
            result.Value.run.Id,
            result.Value.run.TaskId,
            result.Value.run.BrowserProfileId,
            result.Value.profileLock.LeaseToken,
            result.Value.task.PayloadJson,
            result.Value.task.TimeoutSeconds,
            result.Value.task.RetryPolicyJson,
            check.EffectivePolicyJson
        ));
    }

    [AllowAnonymous]
    [HttpPost("report-progress")]
    public async Task<IActionResult> ReportProgress(AgentProgressRequest request)
    {
        var requestAgentKey = HttpContext.Request.Headers["x-agent-key"].FirstOrDefault() ?? "";
        var security = _agentRequestSecurityService.Validate(HttpContext.Request, requestAgentKey);
        if (!security.ok) return Unauthorized(new { ok = false, message = security.reason });

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
        await _auditService.WriteAsync("run_progress", "agent", requestAgentKey, "task_run", run.Id.ToString(), $"{{\"status\":\"{run.Status}\",\"step\":\"{run.CurrentStepId}\"}}");
        return Ok(new { ok = true });
    }

    [AllowAnonymous]
    [HttpPost("report-complete")]
    public async Task<IActionResult> ReportComplete(AgentCompleteRequest request)
    {
        var requestAgentKey = HttpContext.Request.Headers["x-agent-key"].FirstOrDefault() ?? "";
        var security = _agentRequestSecurityService.Validate(HttpContext.Request, requestAgentKey);
        if (!security.ok) return Unauthorized(new { ok = false, message = security.reason });

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
            using var doc = JsonDocument.Parse(request.IsolationReportJson);
            var root = doc.RootElement;
            string ReadJsonValueAsString(string name, string fallback = "{}")
            {
                if (!root.TryGetProperty(name, out var prop)) return fallback;
                return prop.ValueKind switch
                {
                    JsonValueKind.String => prop.GetString() ?? fallback,
                    JsonValueKind.Object or JsonValueKind.Array => prop.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Number => prop.GetRawText(),
                    _ => fallback
                };
            }

            _db.RunIsolationReports.Add(new RunIsolationReport
            {
                TaskRunId = run.Id,
                BrowserProfileId = run.BrowserProfileId,
                ProxySnapshotJson = ReadJsonValueAsString("proxySnapshotJson"),
                FingerprintSnapshotJson = ReadJsonValueAsString("fingerprintSnapshotJson"),
                StorageCheckJson = ReadJsonValueAsString("storageCheckJson"),
                NetworkCheckJson = ReadJsonValueAsString("networkCheckJson"),
                Result = ReadJsonValueAsString("result", "pass")
            });
        }

        await _db.SaveChangesAsync();
        await _scheduler.ReleaseRunAsync(run.Id);
        await _notifier.PublishRunUpdateAsync(run.Id, new { run.Id, run.Status, run.LastPreviewPath, completed = true });
        await _auditService.WriteAsync("run_complete", "agent", requestAgentKey, "task_run", run.Id.ToString(), $"{{\"status\":\"{run.Status}\"}}");
        return Ok(new { ok = true });
    }

    [Authorize]
    [HttpGet]
    public Task<List<AgentNode>> List() => _db.Agents.OrderByDescending(x => x.LastHeartbeatAt).ToListAsync();
}
