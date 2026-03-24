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
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly AppDbContext _db;
    public TemplatesController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<List<TaskTemplate>> List() => _db.TaskTemplates.OrderByDescending(x => x.Id).ToListAsync();

    [HttpPost]
    public async Task<IActionResult> Create(TaskTemplateRequest request)
    {
        var item = new TaskTemplate { Name = request.Name, DefinitionJson = request.DefinitionJson };
        _db.TaskTemplates.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }
}
