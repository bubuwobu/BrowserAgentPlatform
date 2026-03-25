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
[Authorize]
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IsolationPolicyService _isolationPolicyService;
    private readonly AuditService _auditService;
    public ProfilesController(AppDbContext db, IsolationPolicyService isolationPolicyService, AuditService auditService)
    {
        _db = db;
        _isolationPolicyService = isolationPolicyService;
        _auditService = auditService;
    }

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
            StorageRootPath = request.StorageRootPath,
            DownloadRootPath = request.DownloadRootPath,
            StartupArgsJson = request.StartupArgsJson,
            IsolationPolicyJson = request.IsolationPolicyJson,
            IsolationLevel = request.IsolationLevel
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
        profile.StorageRootPath = request.StorageRootPath;
        profile.DownloadRootPath = request.DownloadRootPath;
        profile.StartupArgsJson = request.StartupArgsJson;
        profile.IsolationPolicyJson = request.IsolationPolicyJson;
        profile.IsolationLevel = request.IsolationLevel;
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
        await _auditService.WriteAsync("profile_test_open", "user", User.Identity?.Name ?? "unknown", "profile", profile.Id.ToString());
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
        await _auditService.WriteAsync("profile_takeover", "user", User.Identity?.Name ?? "unknown", "profile", profile.Id.ToString(), $"{{\"headed\":{request.Headed.ToString().ToLowerInvariant()}}}");
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
        await _auditService.WriteAsync("profile_unlock", "user", User.Identity?.Name ?? "unknown", "profile", id.ToString(), $"{{\"released\":{locks.Count}}}");
        return Ok(new { ok = true, released = locks.Count });
    }

    [HttpPost("{id:long}/isolation-check")]
    public async Task<IActionResult> IsolationCheck(long id, CancellationToken cancellationToken)
    {
        var profile = await _db.BrowserProfiles.FindAsync(new object?[] { id }, cancellationToken: cancellationToken);
        if (profile is null) return NotFound();

        var result = await _isolationPolicyService.CheckProfileAsync(profile, cancellationToken);
        profile.LastIsolationCheckAt = DateTime.UtcNow;
        profile.RuntimeMetaJson = JsonSerializer.Serialize(new
        {
            lastIsolationCheck = new
            {
                at = profile.LastIsolationCheckAt,
                result.Ok,
                result.Errors,
                result.Warnings
            }
        });
        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("profile_isolation_check", "user", User.Identity?.Name ?? "unknown", "profile", profile.Id.ToString(), JsonSerializer.Serialize(new { result.Ok, result.Errors, result.Warnings }), cancellationToken);

        return Ok(new
        {
            ok = result.Ok,
            errors = result.Errors,
            warnings = result.Warnings,
            effectivePolicyJson = result.EffectivePolicyJson
        });
    }
}
