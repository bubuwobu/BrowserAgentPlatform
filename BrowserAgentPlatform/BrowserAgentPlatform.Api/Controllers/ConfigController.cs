using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using BrowserAgentPlatform.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    public ConfigController(AppDbContext db) => _db = db;

    [HttpGet("proxies")]
    public Task<List<ProxyConfig>> Proxies() => _db.Proxies.OrderByDescending(x => x.Id).ToListAsync();

    [HttpPost("proxies")]
    public async Task<IActionResult> CreateProxy(ProxyUpsertRequest request)
    {
        var item = new ProxyConfig
        {
            Name = request.Name,
            Protocol = request.Protocol,
            Host = request.Host,
            Port = request.Port,
            Username = request.Username,
            Password = request.Password,
            Notes = request.Notes
        };
        _db.Proxies.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("proxies/{id:long}")]
    public async Task<IActionResult> UpdateProxy(long id, ProxyUpsertRequest request)
    {
        var item = await _db.Proxies.FindAsync(id);
        if (item is null) return NotFound();
        item.Name = request.Name;
        item.Protocol = request.Protocol;
        item.Host = request.Host;
        item.Port = request.Port;
        item.Username = request.Username;
        item.Password = request.Password;
        item.Notes = request.Notes;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("proxies/{id:long}")]
    public async Task<IActionResult> DeleteProxy(long id)
    {
        var item = await _db.Proxies.FindAsync(id);
        if (item is null) return NotFound();
        _db.Proxies.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpGet("fingerprints")]
    public Task<List<FingerprintTemplate>> Fingerprints() => _db.FingerprintTemplates.OrderByDescending(x => x.Id).ToListAsync();

    [HttpPost("fingerprints")]
    public async Task<IActionResult> CreateFingerprint(FingerprintTemplateRequest request)
    {
        var item = new FingerprintTemplate { Name = request.Name, ConfigJson = request.ConfigJson };
        _db.FingerprintTemplates.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("fingerprints/{id:long}")]
    public async Task<IActionResult> UpdateFingerprint(long id, FingerprintTemplateRequest request)
    {
        var item = await _db.FingerprintTemplates.FindAsync(id);
        if (item is null) return NotFound();
        item.Name = request.Name;
        item.ConfigJson = request.ConfigJson;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("fingerprints/{id:long}")]
    public async Task<IActionResult> DeleteFingerprint(long id)
    {
        var item = await _db.FingerprintTemplates.FindAsync(id);
        if (item is null) return NotFound();
        _db.FingerprintTemplates.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
