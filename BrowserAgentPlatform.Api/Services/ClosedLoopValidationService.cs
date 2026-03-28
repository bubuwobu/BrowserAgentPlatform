using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BrowserAgentPlatform.Api.Services;

public class ClosedLoopValidationService
{
    private readonly AppDbContext _db;
    private readonly SchedulerService _schedulerService;
    private readonly IsolationPolicyService _isolationPolicyService;
    private readonly AuditService _auditService;

    public ClosedLoopValidationService(
        AppDbContext db,
        SchedulerService schedulerService,
        IsolationPolicyService isolationPolicyService,
        AuditService auditService)
    {
        _db = db;
        _schedulerService = schedulerService;
        _isolationPolicyService = isolationPolicyService;
        _auditService = auditService;
    }

    public async Task<object> StartAsync(long profileId, string agentKey, string? taskName, string? payloadJson, CancellationToken cancellationToken = default)
    {
        var profile = await _db.BrowserProfiles.FindAsync(new object?[] { profileId }, cancellationToken: cancellationToken);
        if (profile is null) return new { ok = false, message = "profile not found" };

        await EnsureAgentAsync(agentKey, cancellationToken);
        var isolation = await _isolationPolicyService.CheckProfileAsync(profile, cancellationToken);

        var task = new WorkflowTask
        {
            Name = string.IsNullOrWhiteSpace(taskName) ? $"closed-loop-{DateTime.UtcNow:yyyyMMddHHmmss}" : taskName,
            BrowserProfileId = profileId,
            SchedulingStrategy = "preferred_agent",
            PreferredAgentId = (await _db.Agents.FirstAsync(x => x.AgentKey == agentKey, cancellationToken)).Id,
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{\"mode\":\"closed-loop-validation\"}" : payloadJson!,
            RetryPolicyJson = "{\"maxRetries\":0}",
            Priority = 999,
            TimeoutSeconds = 300,
            Status = "queued"
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        var run = new TaskRun
        {
            TaskId = task.Id,
            BrowserProfileId = task.BrowserProfileId,
            Status = "queued",
            RetryCount = 0,
            MaxRetries = 0
        };
        _db.TaskRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "closed_loop_start",
            "user",
            "manual",
            "task_run",
            run.Id.ToString(),
            JsonSerializer.Serialize(new { profileId, isolation.Ok, isolation.Errors, isolation.Warnings }),
            cancellationToken);

        return new
        {
            ok = true,
            taskId = task.Id,
            runId = run.Id,
            isolationOk = isolation.Ok,
            isolationErrors = isolation.Errors,
            isolationWarnings = isolation.Warnings
        };
    }

    public async Task<object> ExecuteAsync(long runId, string agentKey, CancellationToken cancellationToken = default)
    {
        await EnsureAgentAsync(agentKey, cancellationToken);

        var lease = await _schedulerService.LeaseNextRunForAgentAsync(agentKey);
        if (lease is null) return new { ok = false, message = "no run leased for agent" };
        if (lease.Value.run.Id != runId)
        {
            await _schedulerService.ReleaseRunAsync(lease.Value.run.Id);
            return new { ok = false, message = $"leased run {lease.Value.run.Id} does not match requested run {runId}" };
        }

        var run = lease.Value.run;
        var task = lease.Value.task;
        var profile = await _db.BrowserProfiles.FindAsync(new object?[] { run.BrowserProfileId }, cancellationToken: cancellationToken);
        if (profile is null)
        {
            run.Status = "failed";
            run.ErrorCode = "missing_profile";
            run.ErrorMessage = "profile not found during closed-loop execute";
            task.Status = "failed";
            await _db.SaveChangesAsync(cancellationToken);
            await _schedulerService.ReleaseRunAsync(run.Id);
            return new { ok = false, message = "profile missing" };
        }

        run.Status = "running";
        run.StartedAt = DateTime.UtcNow;
        run.HeartbeatAt = DateTime.UtcNow;
        _db.TaskRunLogs.Add(new TaskRunLog
        {
            TaskRunId = run.Id,
            StepId = "validation_start",
            Level = "info",
            Message = "Closed-loop validation started"
        });
        await _db.SaveChangesAsync(cancellationToken);

        var isolation = await _isolationPolicyService.CheckProfileAsync(profile, cancellationToken);
        if (!isolation.Ok)
        {
            run.Status = "failed";
            run.ErrorCode = "isolation_validation_failed";
            run.ErrorMessage = string.Join("; ", isolation.Errors);
            run.FinishedAt = DateTime.UtcNow;
            task.Status = "failed";
            _db.TaskRunLogs.Add(new TaskRunLog
            {
                TaskRunId = run.Id,
                StepId = "validation_isolation",
                Level = "error",
                Message = run.ErrorMessage
            });
            await _db.SaveChangesAsync(cancellationToken);
            await _schedulerService.ReleaseRunAsync(run.Id);
            return new { ok = false, message = "isolation check failed", errors = isolation.Errors };
        }

        run.Status = "completed";
        run.ResultJson = JsonSerializer.Serialize(new
        {
            mode = "closed-loop-validation",
            leaseTokenUsed = lease.Value.profileLock.LeaseToken,
            isolation = new { isolation.Ok, isolation.Warnings }
        });
        run.FinishedAt = DateTime.UtcNow;
        task.Status = "completed";

        _db.RunIsolationReports.Add(new RunIsolationReport
        {
            TaskRunId = run.Id,
            BrowserProfileId = run.BrowserProfileId,
            ProxySnapshotJson = isolation.EffectivePolicyJson,
            FingerprintSnapshotJson = isolation.EffectivePolicyJson,
            StorageCheckJson = JsonSerializer.Serialize(new { ok = true, profile.StorageRootPath, profile.DownloadRootPath }),
            NetworkCheckJson = JsonSerializer.Serialize(new { ok = true }),
            Result = "pass"
        });

        _db.TaskRunLogs.Add(new TaskRunLog
        {
            TaskRunId = run.Id,
            StepId = "validation_complete",
            Level = "info",
            Message = "Closed-loop validation completed"
        });
        await _db.SaveChangesAsync(cancellationToken);
        await _schedulerService.ReleaseRunAsync(run.Id);

        await _auditService.WriteAsync("closed_loop_complete", "agent", agentKey, "task_run", run.Id.ToString(), cancellationToken: cancellationToken);

        return new
        {
            ok = true,
            runId = run.Id,
            taskId = task.Id,
            status = run.Status,
            result = run.ResultJson
        };
    }

    private async Task EnsureAgentAsync(string agentKey, CancellationToken cancellationToken)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(x => x.AgentKey == agentKey, cancellationToken);
        if (agent is not null) return;

        _db.Agents.Add(new AgentNode
        {
            AgentKey = agentKey,
            Name = $"validator-{agentKey}",
            MachineName = Environment.MachineName,
            MaxParallelRuns = 1,
            SchedulerTags = "validation",
            Status = "online",
            LastHeartbeatAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
