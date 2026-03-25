using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using BrowserAgentPlatform.Api.Models;
using BrowserAgentPlatform.Api.Services;
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
    public TasksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var tasks = await _db.Tasks
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                Name = x.Name ?? "",
                x.BrowserProfileId,
                SchedulingStrategy = x.SchedulingStrategy ?? "profile_owner",
                x.PreferredAgentId,
                PayloadJson = x.PayloadJson ?? "{}",
                x.Priority,
                x.TimeoutSeconds,
                RetryPolicyJson = x.RetryPolicyJson ?? "{}",
                Status = x.Status ?? "queued",
                x.CreatedAt
            })
            .ToListAsync();
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkflowTaskRequest request)
    {
        if (request.BrowserProfileId <= 0)
        {
            return BadRequest("请选择有效的 BrowserProfile。");
        }

        var profile = await _db.BrowserProfiles.FindAsync(request.BrowserProfileId);
        if (profile is null)
        {
            return BadRequest($"BrowserProfile 不存在：{request.BrowserProfileId}");
        }

        if (request.SchedulingStrategy == "preferred_agent" && !request.PreferredAgentId.HasValue)
        {
            return BadRequest("schedulingStrategy=preferred_agent 时必须选择 preferredAgent。");
        }

        if (request.SchedulingStrategy == "profile_owner" && !profile.OwnerAgentId.HasValue)
        {
            return BadRequest("当前 Profile 未绑定 OwnerAgent，不能使用 profile_owner 策略。可改为 least_loaded 或先绑定 OwnerAgent。");
        }

        var task = new WorkflowTask
        {
            Name = request.Name,
            BrowserProfileId = request.BrowserProfileId,
            SchedulingStrategy = request.SchedulingStrategy,
            PreferredAgentId = request.PreferredAgentId,
            PayloadJson = request.PayloadJson,
            Priority = request.Priority,
            TimeoutSeconds = request.TimeoutSeconds,
            RetryPolicyJson = request.RetryPolicyJson,
            Status = "queued"
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpGet("runs")]
    public async Task<IActionResult> Runs()
    {
        var runs = await _db.TaskRuns
            .OrderByDescending(x => x.Id)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.TaskId,
                x.BrowserProfileId,
                x.AssignedAgentId,
                LeaseToken = x.LeaseToken ?? "",
                Status = x.Status ?? "queued",
                x.RetryCount,
                x.MaxRetries,
                CurrentStepId = x.CurrentStepId ?? "",
                CurrentStepLabel = x.CurrentStepLabel ?? "",
                CurrentUrl = x.CurrentUrl ?? "",
                ResultJson = x.ResultJson ?? "{}",
                ErrorCode = x.ErrorCode ?? "",
                ErrorMessage = x.ErrorMessage ?? "",
                LastPreviewPath = x.LastPreviewPath ?? "",
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
}
