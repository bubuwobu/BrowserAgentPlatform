using System.Net.Http.Json;
using System.Text.Json;
using BrowserAgentPlatform.Agent.Contracts;
using BrowserAgentPlatform.Agent.Models;
using Microsoft.Extensions.Options;

namespace BrowserAgentPlatform.Agent.Services;

public class PlatformApiClient
{
    private readonly HttpClient _http;
    private readonly AgentOptions _options;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public PlatformApiClient(HttpClient http, IOptions<AgentOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
        if (!_http.DefaultRequestHeaders.Contains("x-agent-key"))
        {
            _http.DefaultRequestHeaders.Add("x-agent-key", _options.AgentKey);
        }
    }

    public Task RegisterAsync() => _http.PostAsJsonAsync("api/agents/register", new AgentRegisterRequest
    {
        AgentKey = _options.AgentKey,
        Name = _options.Name,
        MachineName = _options.MachineName,
        MaxParallelRuns = _options.MaxParallelRuns,
        SchedulerTags = _options.SchedulerTags
    });

    public async Task<List<JsonElement>> HeartbeatAsync(int currentRuns)
    {
        var response = await _http.PostAsJsonAsync("api/agents/heartbeat", new AgentHeartbeatRequest
        {
            AgentKey = _options.AgentKey,
            CurrentRuns = currentRuns
        });

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!doc.RootElement.TryGetProperty("commands", out var commands) || commands.ValueKind != JsonValueKind.Array)
            return new();

        var result = new List<JsonElement>();
        foreach (var item in commands.EnumerateArray())
        {
            result.Add(item.Clone());
        }

        return result;
    }

    public async Task<AgentPullResponse?> PullAsync()
    {
        var response = await _http.PostAsync($"api/agents/pull/{_options.AgentKey}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentPullResponse>(_json);
    }

    public async Task ReportProgressAsync(AgentProgressRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/agents/report-progress", request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"report-progress failed: {(int)response.StatusCode} {body}");
        }
    }

    public async Task ReportCompleteAsync(AgentCompleteRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/agents/report-complete", request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"report-complete failed: {(int)response.StatusCode} {body}");
        }
    }
}
