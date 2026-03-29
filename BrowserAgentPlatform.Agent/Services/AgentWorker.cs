using System.Text.Json;
using BrowserAgentPlatform.Agent.Contracts;
using BrowserAgentPlatform.Agent.Models;
using Microsoft.Extensions.Options;

namespace BrowserAgentPlatform.Agent.Services;

public class AgentWorker : BackgroundService
{
    private readonly PlatformApiClient _api;
    private readonly TaskExecutor _executor;
    private readonly ProfileRuntimeManager _profiles;
    private readonly ElementPickerService _picker;
    private readonly AgentOptions _options;
    private int _currentRuns = 0;

    public AgentWorker(
        PlatformApiClient api,
        TaskExecutor executor,
        ProfileRuntimeManager profiles,
        ElementPickerService picker,
        IOptions<AgentOptions> options)
    {
        _api = api;
        _executor = executor;
        _profiles = profiles;
        _picker = picker;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _api.RegisterAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var commands = await _api.HeartbeatAsync(_currentRuns);
                foreach (var cmd in commands)
                {
                    await HandleCommandAsync(cmd.Clone(), stoppingToken);
                }

                if (_currentRuns < _options.MaxParallelRuns)
                {
                    var pull = await _api.PullAsync();
                    if (pull?.TaskRunId is long taskRunId && pull.ProfileId is long profileId)
                    {
                        if (string.IsNullOrWhiteSpace(pull.PayloadJson) || string.IsNullOrWhiteSpace(pull.LeaseToken))
                        {
                            await _api.ReportCompleteAsync(new AgentCompleteRequest
                            {
                                TaskRunId = taskRunId,
                                Status = "failed",
                                LeaseToken = pull.LeaseToken ?? "",
                                ResultJson = "{\"error\":\"missing payload or lease token\"}",
                                ErrorCode = "invalid_pull_payload",
                                ErrorMessage = "missing payload or lease token"
                            });
                        }
                        else
                        {
                            Interlocked.Increment(ref _currentRuns);
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _executor.ExecuteAsync(taskRunId, profileId, pull.LeaseToken!, pull.PayloadJson!);
                                }
                                finally
                                {
                                    Interlocked.Decrement(ref _currentRuns);
                                }
                            }, stoppingToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Agent] main loop ERROR:");
                Console.WriteLine(ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task HandleCommandAsync(JsonElement cmd, CancellationToken cancellationToken)
    {
        try
        {
            if (!cmd.TryGetProperty("commandType", out var typeEl)) return;

            var type = typeEl.GetString() ?? string.Empty;
            var profileId = cmd.TryGetProperty("profileId", out var pid) && pid.ValueKind != JsonValueKind.Null
                ? pid.GetInt64()
                : 0L;

            var payloadJson = cmd.TryGetProperty("payloadJson", out var payloadEl) && payloadEl.ValueKind == JsonValueKind.String
                ? payloadEl.GetString()
                : null;

            Console.WriteLine($"[Agent] Received command: {type}, profileId={profileId}");

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

                case "start_element_picker":
                    await HandleStartElementPickerAsync(profileId, payloadJson, cancellationToken);
                    break;

                case "stop_element_picker":
                    await HandleStopElementPickerAsync(profileId, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Agent] HandleCommandAsync ERROR:");
            Console.WriteLine(ex);
        }
    }

    private async Task HandleStartElementPickerAsync(long profileId, string? payloadJson, CancellationToken cancellationToken)
    {
        string sessionId = string.Empty;
        string? pageUrl = null;
        bool headed = true;

        if (!string.IsNullOrWhiteSpace(payloadJson))
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            sessionId = root.TryGetProperty("sessionId", out var sid) ? (sid.GetString() ?? string.Empty) : string.Empty;
            pageUrl = root.TryGetProperty("pageUrl", out var url) ? url.GetString() : null;
            headed = root.TryGetProperty("headed", out var hd) ? hd.GetBoolean() : true;
        }

        var page = await _profiles.GetOrLaunchPageAsync(profileId, "[]", "{}", null, headed);
        if (!string.IsNullOrWhiteSpace(pageUrl) && (string.IsNullOrWhiteSpace(page.Url) || page.Url == "about:blank"))
        {
            try { await page.GotoAsync(pageUrl); } catch { }
        }

        await _picker.StartPickerAsync(page, _options.ApiBaseUrl, sessionId, profileId, cancellationToken);
        Console.WriteLine($"[Agent] element picker started. profileId={profileId}, sessionId={sessionId}");
    }

    private async Task HandleStopElementPickerAsync(long profileId, CancellationToken cancellationToken)
    {
        if (_profiles.TryGetPage(profileId, out var page) && page is not null)
        {
            await _picker.StopPickerAsync(page);
            Console.WriteLine($"[Agent] element picker stopped. profileId={profileId}");
        }
    }
}
