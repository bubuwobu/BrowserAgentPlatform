using BrowserAgentPlatform.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BrowserAgentPlatform.Api.Services;

public class LiveHubNotifier
{
    private readonly IHubContext<LiveHub> _hub;
    public LiveHubNotifier(IHubContext<LiveHub> hub) => _hub = hub;

    public Task PublishRunUpdateAsync(long runId, object payload)
        => _hub.Clients.Group($"run:{runId}").SendAsync("runUpdate", payload);

    public Task PublishProfileUpdateAsync(long profileId, object payload)
        => _hub.Clients.Group($"profile:{profileId}").SendAsync("profileUpdate", payload);

    public Task PublishAgentUpdateAsync(long agentId, object payload)
        => _hub.Clients.Group($"agent:{agentId}").SendAsync("agentUpdate", payload);
}
