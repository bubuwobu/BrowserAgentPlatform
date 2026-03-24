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
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    public ConfigController(AppDbContext db) => _db = db;

    [HttpGet("proxies")]
    public Task<List<ProxyConfig>> Proxies() => _db.Proxies.OrderByDescending(x => x.Id).ToListAsync();

    [HttpPost("proxies")]
    public async Task<IActionResult> UpsertProxy(ProxyUpsertRequest request)
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

    [HttpGet("fingerprints")]
    public Task<List<FingerprintTemplate>> Fingerprints() => _db.FingerprintTemplates.OrderByDescending(x => x.Id).ToListAsync();

    [HttpPost("fingerprints")]
    public async Task<IActionResult> UpsertFingerprint(FingerprintTemplateRequest request)
    {
        var item = new FingerprintTemplate
        {
            Name = request.Name,
            ConfigJson = request.ConfigJson
        };
        _db.FingerprintTemplates.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }
}
