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
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProfilesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var profiles = await _db.BrowserProfiles.OrderByDescending(x => x.Id).ToListAsync();
        return Ok(profiles);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BrowserProfileRequest request)
    {
        var profile = new BrowserProfile
        {
            Name = request.Name,
            OwnerAgentId = request.OwnerAgentId,
            ProxyId = request.ProxyId,
            FingerprintTemplateId = request.FingerprintTemplateId,
            LocalProfilePath = request.LocalProfilePath,
            StartupArgsJson = request.StartupArgsJson
        };
        _db.BrowserProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, BrowserProfileRequest request)
    {
        var profile = await _db.BrowserProfiles.FindAsync(id);
        if (profile is null) return NotFound();

        profile.Name = request.Name;
        profile.OwnerAgentId = request.OwnerAgentId;
        profile.ProxyId = request.ProxyId;
        profile.FingerprintTemplateId = request.FingerprintTemplateId;
        profile.LocalProfilePath = request.LocalProfilePath;
        profile.StartupArgsJson = request.StartupArgsJson;
        await _db.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpPost("{id:long}/test-open")]
    public async Task<IActionResult> TestOpen(long id)
    {
        var profile = await _db.BrowserProfiles.FindAsync(id);
        if (profile is null) return NotFound();
        if (!profile.OwnerAgentId.HasValue) return BadRequest("Profile 未绑定 Agent。");

        _db.AgentCommands.Add(new AgentCommand
        {
            AgentId = profile.OwnerAgentId.Value,
            ProfileId = profile.Id,
            CommandType = "test_open_profile",
            PayloadJson = $"{{\"profileId\":{profile.Id}}}"
        });
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id:long}/takeover")]
    public async Task<IActionResult> Takeover(long id, TakeoverRequest request)
    {
        var profile = await _db.BrowserProfiles.FindAsync(id);
        if (profile is null) return NotFound();
        if (!profile.OwnerAgentId.HasValue) return BadRequest("Profile 未绑定 Agent。");

        _db.AgentCommands.Add(new AgentCommand
        {
            AgentId = profile.OwnerAgentId.Value,
            ProfileId = profile.Id,
            CommandType = request.Headed ? "takeover_start" : "takeover_stop",
            PayloadJson = $"{{\"profileId\":{profile.Id},\"headed\":{request.Headed.ToString().ToLowerInvariant()}}}"
        });
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id:long}/unlock")]
    public async Task<IActionResult> Unlock(long id)
    {
        var locks = await _db.BrowserProfileLocks.Where(x => x.ProfileId == id && (x.Status == "reserved" || x.Status == "leased")).ToListAsync();
        foreach (var item in locks) item.Status = "released";
        var profile = await _db.BrowserProfiles.FindAsync(id);
        if (profile is not null) profile.Status = "idle";
        await _db.SaveChangesAsync();
        return Ok(new { ok = true, released = locks.Count });
    }
}
