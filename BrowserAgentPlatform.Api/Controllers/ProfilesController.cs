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
    private readonly ProfileLifecycleService _profileLifecycleService;

    public ProfilesController(AppDbContext db, IsolationPolicyService isolationPolicyService, AuditService auditService, ProfileLifecycleService profileLifecycleService)
    {
        _db = db;
        _isolationPolicyService = isolationPolicyService;
        _auditService = auditService;
        _profileLifecycleService = profileLifecycleService;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var profiles = await _db.BrowserProfiles.OrderByDescending(x => x.Id).ToListAsync();
        return Ok(profiles.Select(MapListItem));
    }

    [HttpGet("state-board")]
    public async Task<IActionResult> StateBoard([FromQuery] int take = 12)
    {
        take = Math.Clamp(take, 1, 50);
        var profiles = await _db.BrowserProfiles
            .OrderByDescending(x => x.LastUsedAt ?? x.LastStartedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync();

        var byLifecycle = await _db.BrowserProfiles
            .GroupBy(x => x.LifecycleState ?? "created")
            .Select(g => new { lifecycle = g.Key, count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            total = await _db.BrowserProfiles.CountAsync(),
            active = await _db.BrowserProfiles.CountAsync(x => x.LifecycleState == "running" || x.LifecycleState == "leased"),
            broken = await _db.BrowserProfiles.CountAsync(x => x.LifecycleState == "broken"),
            ready = await _db.BrowserProfiles.CountAsync(x => x.LifecycleState == "ready" || x.Status == "idle"),
            byLifecycle,
            items = profiles.Select(MapListItem)
        });
    }

    [HttpGet("{id:long}/state-panel")]
    public async Task<IActionResult> StatePanel(long id)
    {
        var profile = await _db.BrowserProfiles.FindAsync(id);
        if (profile is null) return NotFound();
        return Ok(MapListItem(profile));
    }

    [HttpPost]
    public async Task<IActionResult> Create(BrowserProfileRequest request)
    {
        var profile = new BrowserProfile
        {
            Name = request.Name ?? "",
            OwnerAgentId = request.OwnerAgentId,
            ProxyId = request.ProxyId,
            FingerprintTemplateId = request.FingerprintTemplateId,
            LocalProfilePath = request.LocalProfilePath ?? "",
            StorageRootPath = request.StorageRootPath ?? "",
            DownloadRootPath = request.DownloadRootPath ?? "",
            StartupArgsJson = request.StartupArgsJson ?? "[]",
            IsolationPolicyJson = request.IsolationPolicyJson ?? "{}",
            IsolationLevel = request.IsolationLevel ?? "standard",
            WorkspaceKey = request.WorkspaceKey ?? $"profile_{Guid.NewGuid():N}"[..16],
            ProfileRootPath = request.ProfileRootPath ?? request.LocalProfilePath ?? "",
            ArtifactRootPath = request.ArtifactRootPath ?? "",
            TempRootPath = request.TempRootPath ?? "",
            LifecycleState = string.IsNullOrWhiteSpace(request.LifecycleState) ? "created" : request.LifecycleState!
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

        profile.Name = request.Name ?? "";
        profile.OwnerAgentId = request.OwnerAgentId;
        profile.ProxyId = request.ProxyId;
        profile.FingerprintTemplateId = request.FingerprintTemplateId;
        profile.LocalProfilePath = request.LocalProfilePath ?? "";
        profile.StorageRootPath = request.StorageRootPath ?? "";
        profile.DownloadRootPath = request.DownloadRootPath ?? "";
        profile.StartupArgsJson = request.StartupArgsJson ?? "[]";
        profile.IsolationPolicyJson = request.IsolationPolicyJson ?? "{}";
        profile.IsolationLevel = request.IsolationLevel ?? "standard";
        profile.WorkspaceKey = request.WorkspaceKey ?? profile.WorkspaceKey;
        profile.ProfileRootPath = request.ProfileRootPath ?? request.LocalProfilePath ?? profile.ProfileRootPath;
        profile.ArtifactRootPath = request.ArtifactRootPath ?? profile.ArtifactRootPath;
        profile.TempRootPath = request.TempRootPath ?? profile.TempRootPath;
        if (!string.IsNullOrWhiteSpace(request.LifecycleState)) profile.LifecycleState = request.LifecycleState!;

        await _db.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var profile = await _db.BrowserProfiles.FindAsync(id);
        if (profile is null) return NotFound();

        _db.BrowserProfiles.Remove(profile);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
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
            PayloadJson = JsonSerializer.Serialize(new { profileId = profile.Id })
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
            PayloadJson = JsonSerializer.Serialize(new { profileId = profile.Id, headed = request.Headed })
        });

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id:long}/unlock")]
    public async Task<IActionResult> Unlock(long id)
    {
        var locks = await _db.BrowserProfileLocks
            .Where(x => x.ProfileId == id && (x.Status == "reserved" || x.Status == "leased"))
            .ToListAsync();

        foreach (var item in locks)
            item.Status = "released";

        var profile = await _db.BrowserProfiles.FindAsync(id);
        if (profile is not null)
            await _profileLifecycleService.MarkUnlockedAsync(profile.Id);
        else
            await _db.SaveChangesAsync();
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

        return Ok(new
        {
            ok = result.Ok,
            errors = result.Errors,
            warnings = result.Warnings,
            effectivePolicyJson = result.EffectivePolicyJson
        });
    }

    private static BrowserProfileListItem MapListItem(BrowserProfile x) => new(
        x.Id, x.Name, x.OwnerAgentId, x.ProxyId, x.FingerprintTemplateId, x.Status, x.IsolationLevel,
        x.LocalProfilePath, x.StorageRootPath, x.DownloadRootPath, x.StartupArgsJson, x.IsolationPolicyJson, x.RuntimeMetaJson,
        x.WorkspaceKey, x.ProfileRootPath, x.ArtifactRootPath, x.TempRootPath, x.LifecycleState,
        x.LastUsedAt, x.LastIsolationCheckAt, x.LastStartedAt, x.LastStoppedAt, x.LastRebuildAt, x.CreatedAt
    );
}
