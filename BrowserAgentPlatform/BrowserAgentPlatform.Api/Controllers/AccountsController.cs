using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using BrowserAgentPlatform.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AccountsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.Accounts
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Platform,
                x.Username,
                x.Status,
                x.BrowserProfileId,
                x.CredentialJson,
                x.MetadataJson,
                x.CreatedAt
            })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AccountRequest request)
    {
        if (request.BrowserProfileId.HasValue)
        {
            var profileExists = await _db.BrowserProfiles.AnyAsync(x => x.Id == request.BrowserProfileId.Value);
            if (!profileExists) return BadRequest("绑定的 BrowserProfile 不存在。");
        }

        var item = new Account
        {
            Name = request.Name,
            Platform = request.Platform,
            Username = request.Username,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status,
            BrowserProfileId = request.BrowserProfileId,
            CredentialJson = string.IsNullOrWhiteSpace(request.CredentialJson) ? "{}" : request.CredentialJson!,
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson!
        };
        _db.Accounts.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, AccountRequest request)
    {
        if (request.BrowserProfileId.HasValue)
        {
            var profileExists = await _db.BrowserProfiles.AnyAsync(x => x.Id == request.BrowserProfileId.Value);
            if (!profileExists) return BadRequest("绑定的 BrowserProfile 不存在。");
        }

        var item = await _db.Accounts.FindAsync(id);
        if (item is null) return NotFound();

        item.Name = request.Name;
        item.Platform = request.Platform;
        item.Username = request.Username;
        item.Status = string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status;
        item.BrowserProfileId = request.BrowserProfileId;
        item.CredentialJson = string.IsNullOrWhiteSpace(request.CredentialJson) ? "{}" : request.CredentialJson!;
        item.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson!;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var item = await _db.Accounts.FindAsync(id);
        if (item is null) return NotFound();

        var hasTaskBinding = await _db.Tasks.AnyAsync(x => x.AccountId == id);
        if (hasTaskBinding)
            return BadRequest("当前账号仍被任务绑定，请先解除任务绑定后再删除。");

        _db.Accounts.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
