using BrowserAgentPlatform.Api.Hubs;
using BrowserAgentPlatform.Api.Models;
using BrowserAgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BrowserAgentPlatform.Api.Controllers;

[ApiController]
[Route("api/picker")]
[Authorize]
public class PickerController : ControllerBase
{
    private readonly PickerSessionService _sessions;
    private readonly PickerRecommendationService _recommendationService;
    private readonly IHubContext<PickerHub> _hub;
    private readonly IConfiguration _configuration;

    public PickerController(
        PickerSessionService sessions,
        PickerRecommendationService recommendationService,
        IHubContext<PickerHub> hub,
        IConfiguration configuration)
    {
        _sessions = sessions;
        _recommendationService = recommendationService;
        _hub = hub;
        _configuration = configuration;
    }

    [HttpPost("start")]
    public async Task<ActionResult<PickerStartResponse>> Start([FromBody] PickerStartRequest request)
    {
        var session = _sessions.CreateOrRestore(
            request.ProfileId, request.PageUrl, request.NodeId, request.NodeType,
            request.Continuous, request.ResumeIfExists, out var restored, agentId: null);

        _sessions.MarkStarted(session.SessionId);

        var apiBaseUrl = _configuration["Platform:PublicApiBaseUrl"]
            ?? _configuration["Api:BaseUrl"]
            ?? $"{Request.Scheme}://{Request.Host}";

        var command = new PickerAgentCommand
        {
            CommandType = restored ? "resume_element_picker" : "start_element_picker",
            SessionId = session.SessionId,
            ProfileId = request.ProfileId,
            ApiBaseUrl = apiBaseUrl,
            PageUrl = request.PageUrl,
            Continuous = request.Continuous,
            Resume = restored
        };

        await _hub.Clients.Group($"picker:{session.SessionId}").SendAsync("pickerStarted", command);

        return Ok(new PickerStartResponse
        {
            SessionId = session.SessionId,
            ProfileId = session.ProfileId,
            NodeId = session.NodeId,
            NodeType = session.NodeType,
            Continuous = session.Continuous,
            Restored = restored,
            Status = restored ? "restored" : "started"
        });
    }

    [HttpPost("pause")]
    public async Task<IActionResult> Pause([FromBody] PickerStopRequest request)
    {
        _sessions.Pause(request.SessionId);
        await _hub.Clients.Group($"picker:{request.SessionId}").SendAsync("pickerPaused", new { sessionId = request.SessionId, profileId = request.ProfileId });
        return Ok();
    }

    [HttpPost("resume")]
    public async Task<IActionResult> Resume([FromBody] PickerResumeRequest request)
    {
        if (!_sessions.TryGet(request.SessionId, out var session) || session is null) return NotFound();

        _sessions.MarkStarted(request.SessionId);

        var apiBaseUrl = _configuration["Platform:PublicApiBaseUrl"]
            ?? _configuration["Api:BaseUrl"]
            ?? $"{Request.Scheme}://{Request.Host}";

        var command = new PickerAgentCommand
        {
            CommandType = "resume_element_picker",
            SessionId = request.SessionId,
            ProfileId = request.ProfileId,
            ApiBaseUrl = apiBaseUrl,
            Continuous = request.Continuous || session.Continuous,
            Resume = true,
            PageUrl = session.PageUrl
        };

        await _hub.Clients.Group($"picker:{request.SessionId}").SendAsync("pickerResumed", command);
        return Ok();
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop([FromBody] PickerStopRequest request)
    {
        _sessions.Stop(request.SessionId);
        await _hub.Clients.Group($"picker:{request.SessionId}").SendAsync("pickerStopped", new { commandType = "stop_element_picker", sessionId = request.SessionId, profileId = request.ProfileId });
        return Ok();
    }

    [HttpGet("session/{sessionId}")]
    public ActionResult<PickerSessionDto> GetSession([FromRoute] string sessionId)
    {
        if (!_sessions.TryGet(sessionId, out var session) || session is null) return NotFound();
        return Ok(session);
    }

    [HttpGet("snapshot/{sessionId}")]
    public ActionResult<PickerStateSnapshotDto> Snapshot([FromRoute] string sessionId)
    {
        var snapshot = _sessions.Snapshot(sessionId);
        if (snapshot is null) return NotFound();
        return Ok(snapshot);
    }

    [HttpPost("result")]
    [AllowAnonymous]
    public async Task<IActionResult> Result([FromBody] PickerResultRequest request)
    {
        if (!_sessions.TryGet(request.SessionId, out var session) || session is null) return NotFound();

        _sessions.MarkPicked(request.SessionId);
        var enriched = _recommendationService.Enrich(request, session.PickCount);
        await _hub.Clients.Group($"picker:{request.SessionId}").SendAsync("pickerResult", enriched);

        return Ok(enriched);
    }
}
