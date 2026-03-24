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
        if (!doc.RootElement.TryGetProperty("commands", out var commands)) return new();
        return commands.EnumerateArray().ToList();
    }

    public async Task<AgentPullResponse?> PullAsync()
    {
        var response = await _http.PostAsync($"api/agents/pull/{_options.AgentKey}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentPullResponse>(_json);
    }

    public Task ReportProgressAsync(AgentProgressRequest request) => _http.PostAsJsonAsync("api/agents/report-progress", request);

    public Task ReportCompleteAsync(AgentCompleteRequest request) => _http.PostAsJsonAsync("api/agents/report-complete", request);
}
