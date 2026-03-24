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
        var tasks = await _db.Tasks.OrderByDescending(x => x.Id).ToListAsync();
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkflowTaskRequest request)
    {
        var task = new WorkflowTask
        {
            Name = request.Name,
            BrowserProfileId = request.BrowserProfileId,
            SchedulingStrategy = request.SchedulingStrategy,
            PreferredAgentId = request.PreferredAgentId,
            PayloadJson = request.PayloadJson,
            Priority = request.Priority,
            Status = "queued"
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpGet("runs")]
    public Task<List<TaskRun>> Runs() => _db.TaskRuns.OrderByDescending(x => x.Id).Take(100).ToListAsync();

    [HttpGet("runs/{runId:long}")]
    public async Task<IActionResult> RunDetail(long runId)
    {
        var run = await _db.TaskRuns.FindAsync(runId);
        if (run is null) return NotFound();
        var logs = await _db.TaskRunLogs.Where(x => x.TaskRunId == runId).OrderBy(x => x.Id).ToListAsync();
        var artifacts = await _db.BrowserArtifacts.Where(x => x.TaskRunId == runId).OrderByDescending(x => x.Id).ToListAsync();
        return Ok(new { run, logs, artifacts });
    }
}
