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
[Route("api/live")]
public class LiveController : ControllerBase
{
    private readonly AppDbContext _db;
    public LiveController(AppDbContext db) => _db = db;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var data = new
        {
            agents = await _db.Agents.CountAsync(),
            profiles = await _db.BrowserProfiles.CountAsync(),
            queued = await _db.TaskRuns.CountAsync(x => x.Status == "queued"),
            running = await _db.TaskRuns.CountAsync(x => x.Status == "running"),
            completed = await _db.TaskRuns.CountAsync(x => x.Status == "completed")
        };
        return Ok(data);
    }
}
