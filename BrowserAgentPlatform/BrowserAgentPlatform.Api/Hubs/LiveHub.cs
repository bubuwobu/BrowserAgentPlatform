using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BrowserAgentPlatform.Api.Hubs;

[Authorize]
public class LiveHub : Hub
{
    public Task JoinRun(long runId) => Groups.AddToGroupAsync(Context.ConnectionId, $"run:{runId}");
    public Task JoinProfile(long profileId) => Groups.AddToGroupAsync(Context.ConnectionId, $"profile:{profileId}");
    public Task JoinAgent(long agentId) => Groups.AddToGroupAsync(Context.ConnectionId, $"agent:{agentId}");
}
