using System.Text.Json;
using BrowserAgentPlatform.Agent.Models;
using Microsoft.Extensions.Options;

namespace BrowserAgentPlatform.Agent.Services;

public class AgentWorker : BackgroundService
{
    private readonly PlatformApiClient _api;
    private readonly TaskExecutor _executor;
    private readonly ProfileRuntimeManager _profiles;
    private readonly AgentOptions _options;
    private int _currentRuns = 0;

    public AgentWorker(PlatformApiClient api, TaskExecutor executor, ProfileRuntimeManager profiles, IOptions<AgentOptions> options)
    {
        _api = api;
        _executor = executor;
        _profiles = profiles;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _api.RegisterAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            var commands = await _api.HeartbeatAsync(_currentRuns);
            foreach (var cmd in commands)
            {
                await HandleCommandAsync(cmd);
            }

            if (_currentRuns < _options.MaxParallelRuns)
            {
                var pull = await _api.PullAsync();
                if (pull?.TaskRunId is long taskRunId && pull.ProfileId is long profileId && !string.IsNullOrWhiteSpace(pull.PayloadJson))
                {
                    _currentRuns += 1;
                    _ = Task.Run(async () =>
                    {
                        try { await _executor.ExecuteAsync(taskRunId, profileId, pull.PayloadJson!); }
                        finally { Interlocked.Decrement(ref _currentRuns); }
                    }, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task HandleCommandAsync(JsonElement cmd)
    {
        if (!cmd.TryGetProperty("commandType", out var typeEl)) return;
        var type = typeEl.GetString();
        var profileId = cmd.TryGetProperty("profileId", out var pid) && pid.ValueKind != JsonValueKind.Null ? pid.GetInt64() : 0L;
        switch (type)
        {
            case "test_open_profile":
                await _profiles.GetOrLaunchAsync(profileId, "[]", "{}", null, true);
                break;
            case "takeover_start":
                await _profiles.GetOrLaunchAsync(profileId, "[]", "{}", null, true);
                break;
            case "takeover_stop":
                await _profiles.CloseAsync(profileId);
                break;
        }
    }
}
