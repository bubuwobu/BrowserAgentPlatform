using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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

    public async Task RegisterAsync()
    {
        using var message = BuildSignedJsonRequest(HttpMethod.Post, "api/agents/register", new AgentRegisterRequest
        {
            AgentKey = _options.AgentKey,
            Name = _options.Name,
            MachineName = _options.MachineName,
            MaxParallelRuns = _options.MaxParallelRuns,
            SchedulerTags = _options.SchedulerTags
        });
        using var response = await _http.SendAsync(message);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<JsonElement>> HeartbeatAsync(int currentRuns)
    {
        using var message = BuildSignedJsonRequest(HttpMethod.Post, "api/agents/heartbeat", new AgentHeartbeatRequest
        {
            AgentKey = _options.AgentKey,
            CurrentRuns = currentRuns
        });
        var response = await _http.SendAsync(message);

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
        using var message = BuildSignedRequest(HttpMethod.Post, $"api/agents/pull/{_options.AgentKey}");
        var response = await _http.SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"pull failed: {(int)response.StatusCode} {body}");
        }
        return await response.Content.ReadFromJsonAsync<AgentPullResponse>(_json);
    }

    public async Task ReportProgressAsync(AgentProgressRequest request)
    {
        using var message = BuildSignedJsonRequest(HttpMethod.Post, "api/agents/report-progress", request);
        var response = await _http.SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"report-progress failed: {(int)response.StatusCode} {body}");
        }
    }

    public async Task ReportCompleteAsync(AgentCompleteRequest request)
    {
        using var message = BuildSignedJsonRequest(HttpMethod.Post, "api/agents/report-complete", request);
        var response = await _http.SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"report-complete failed: {(int)response.StatusCode} {body}");
        }
    }

    private HttpRequestMessage BuildSignedJsonRequest(HttpMethod method, string path, object body)
    {
        var msg = BuildSignedRequest(method, path);
        msg.Content = JsonContent.Create(body);
        return msg;
    }

    private HttpRequestMessage BuildSignedRequest(HttpMethod method, string path)
    {
        var msg = new HttpRequestMessage(method, path);
        var secret = _options.AgentSecuritySharedSecret;
        if (string.IsNullOrWhiteSpace(secret)) return msg;

        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");
        var payload = $"{_options.AgentKey}:{ts}:{nonce}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        msg.Headers.Add("x-agent-ts", ts.ToString());
        msg.Headers.Add("x-agent-nonce", nonce);
        msg.Headers.Add("x-agent-signature", signature);
        return msg;
    }
}
