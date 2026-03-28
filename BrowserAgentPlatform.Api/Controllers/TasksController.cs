using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using BrowserAgentPlatform.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var tasks = await _db.Tasks
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.BrowserProfileId,
                x.AccountId,
                x.SchedulingStrategy,
                x.PreferredAgentId,
                x.Status,
                x.IsEnabled,
                x.ScheduleType,
                x.ScheduleConfigJson,
                x.NextRunAt,
                x.LastRunAt,
                x.PayloadJson,
                x.Priority,
                x.TimeoutSeconds,
                x.RetryPolicyJson,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkflowTaskRequest request)
    {
        var validation = await ValidateRequestAsync(request);
        if (validation is not null) return validation;

        var task = new WorkflowTask
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? "未命名任务" : request.Name!,
            BrowserProfileId = request.BrowserProfileId,
            AccountId = request.AccountId,
            SchedulingStrategy = string.IsNullOrWhiteSpace(request.SchedulingStrategy) ? "least_loaded" : request.SchedulingStrategy!,
            PreferredAgentId = request.PreferredAgentId,
            Status = "queued",
            IsEnabled = request.IsEnabled,
            ScheduleType = string.IsNullOrWhiteSpace(request.ScheduleType) ? "manual" : request.ScheduleType!,
            ScheduleConfigJson = string.IsNullOrWhiteSpace(request.ScheduleConfigJson) ? "{}" : request.ScheduleConfigJson!,
            PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson!,
            Priority = request.Priority,
            TimeoutSeconds = request.TimeoutSeconds.GetValueOrDefault(300) <= 0 ? 300 : request.TimeoutSeconds!.Value,
            RetryPolicyJson = string.IsNullOrWhiteSpace(request.RetryPolicyJson) ? "{\"maxRetries\":1}" : request.RetryPolicyJson!
        };

        if (task.ScheduleType == "daily_window_random")
        {
            task.NextRunAt = DateTime.UtcNow;
        }

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, WorkflowTaskRequest request)
    {
        var validation = await ValidateRequestAsync(request);
        if (validation is not null) return validation;

        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        task.Name = string.IsNullOrWhiteSpace(request.Name) ? "未命名任务" : request.Name!;
        task.BrowserProfileId = request.BrowserProfileId;
        task.AccountId = request.AccountId;
        task.SchedulingStrategy = string.IsNullOrWhiteSpace(request.SchedulingStrategy) ? "least_loaded" : request.SchedulingStrategy!;
        task.PreferredAgentId = request.PreferredAgentId;
        task.IsEnabled = request.IsEnabled;
        task.ScheduleType = string.IsNullOrWhiteSpace(request.ScheduleType) ? "manual" : request.ScheduleType!;
        task.ScheduleConfigJson = string.IsNullOrWhiteSpace(request.ScheduleConfigJson) ? "{}" : request.ScheduleConfigJson!;
        task.PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson!;
        task.Priority = request.Priority;
        task.TimeoutSeconds = request.TimeoutSeconds.GetValueOrDefault(300) <= 0 ? 300 : request.TimeoutSeconds!.Value;
        task.RetryPolicyJson = string.IsNullOrWhiteSpace(request.RetryPolicyJson) ? "{\"maxRetries\":1}" : request.RetryPolicyJson!;
        if (task.ScheduleType == "daily_window_random" && !task.NextRunAt.HasValue)
        {
            task.NextRunAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id:long}/run-now")]
    public async Task<IActionResult> RunNow(long id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        var run = new TaskRun
        {
            TaskId = task.Id,
            BrowserProfileId = task.BrowserProfileId,
            Status = "queued",
            MaxRetries = 1
        };

        task.LastRunAt = DateTime.UtcNow;
        _db.TaskRuns.Add(run);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, runId = run.Id });
    }

    [HttpPost("{id:long}/toggle-enabled")]
    public async Task<IActionResult> ToggleEnabled(long id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        task.IsEnabled = !task.IsEnabled;
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, task.Id, task.IsEnabled });
    }

    [HttpGet("runs")]
    public async Task<IActionResult> Runs()
    {
        var runs = await _db.TaskRuns
            .OrderByDescending(x => x.Id)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.TaskId,
                x.BrowserProfileId,
                x.AssignedAgentId,
                x.LeaseToken,
                x.Status,
                x.RetryCount,
                x.MaxRetries,
                x.CurrentStepId,
                x.CurrentStepLabel,
                x.CurrentUrl,
                x.ResultJson,
                x.ErrorCode,
                x.ErrorMessage,
                x.LastPreviewPath,
                x.CreatedAt,
                x.StartedAt,
                x.HeartbeatAt,
                x.FinishedAt
            })
            .ToListAsync();

        return Ok(runs);
    }

    [HttpGet("runs/{runId:long}")]
    public async Task<IActionResult> RunDetail(long runId)
    {
        var run = await _db.TaskRuns.FindAsync(runId);
        if (run is null) return NotFound();

        var logs = await _db.TaskRunLogs.Where(x => x.TaskRunId == runId).OrderBy(x => x.Id).ToListAsync();
        var artifacts = await _db.BrowserArtifacts.Where(x => x.TaskRunId == runId).OrderByDescending(x => x.Id).ToListAsync();

        return Ok(new { run, logs, artifacts });
    }

    [HttpGet("runs/{runId:long}/isolation-report")]
    public async Task<IActionResult> IsolationReport(long runId)
    {
        var reports = await _db.RunIsolationReports
            .Where(x => x.TaskRunId == runId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return Ok(reports);
    }

    [HttpPost("runs/{runId:long}/replay")]
    public async Task<IActionResult> Replay(long runId)
    {
        var sourceRun = await _db.TaskRuns.FindAsync(runId);
        if (sourceRun is null) return NotFound("source run not found");

        var sourceTask = await _db.Tasks.FindAsync(sourceRun.TaskId);
        if (sourceTask is null) return NotFound("source task not found");

        var replayRun = new TaskRun
        {
            TaskId = sourceTask.Id,
            BrowserProfileId = sourceTask.BrowserProfileId,
            Status = "queued",
            MaxRetries = 1
        };

        _db.TaskRuns.Add(replayRun);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, replayRunId = replayRun.Id });
    }

    private async Task<IActionResult?> ValidateRequestAsync(WorkflowTaskRequest request)
    {
        if (request.BrowserProfileId <= 0)
            return BadRequest("请选择有效的 BrowserProfile。");

        var profile = await _db.BrowserProfiles.FindAsync(request.BrowserProfileId);
        if (profile is null)
            return BadRequest($"BrowserProfile 不存在：{request.BrowserProfileId}");

        if (request.AccountId.HasValue)
        {
            var account = await _db.Accounts.FindAsync(request.AccountId.Value);
            if (account is null)
                return BadRequest("Account 不存在。");

            if (account.BrowserProfileId.HasValue && account.BrowserProfileId.Value != request.BrowserProfileId)
                return BadRequest("当前任务绑定的 BrowserProfile 与账号绑定的 BrowserProfile 不一致。");
        }

        var schedulingStrategy = string.IsNullOrWhiteSpace(request.SchedulingStrategy)
            ? "least_loaded"
            : request.SchedulingStrategy;

        if (schedulingStrategy == "preferred_agent" && !request.PreferredAgentId.HasValue)
            return BadRequest("schedulingStrategy=preferred_agent 时必须选择 preferredAgent。");

        if (schedulingStrategy == "profile_owner" && !profile.OwnerAgentId.HasValue)
            return BadRequest("当前 Profile 未绑定 OwnerAgent，不能使用 profile_owner。");

        return null;
    }
}
