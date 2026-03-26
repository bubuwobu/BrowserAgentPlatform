using System.Text.Json;
using BrowserAgentPlatform.Agent.Contracts;
using BrowserAgentPlatform.Agent.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace BrowserAgentPlatform.Agent.Services;

public class TaskExecutor
{
    private readonly PlatformApiClient _api;
    private readonly ProfileRuntimeManager _profiles;
    private readonly AgentOptions _options;

    public TaskExecutor(PlatformApiClient api, ProfileRuntimeManager profiles, IOptions<AgentOptions> options)
    {
        _api = api;
        _profiles = profiles;
        _options = options.Value;
    }

    public async Task ExecuteAsync(long taskRunId, long profileId, string leaseToken, string payloadJson)
    {
        string status = "completed";
        var result = new Dictionary<string, object?>();
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var startupArgsJson = doc.RootElement.TryGetProperty("startupArgsJson", out var argsJson) ? argsJson.GetRawText() : "[]";
            var fingerprintJson = doc.RootElement.TryGetProperty("fingerprint", out var fpJson) ? fpJson.GetRawText() : "{}";
            var proxyJson = doc.RootElement.TryGetProperty("proxy", out var pxJson) ? pxJson.GetRawText() : null;
            var steps = doc.RootElement.GetProperty("steps").EnumerateArray().ToList();
            var edges = doc.RootElement.TryGetProperty("edges", out var edgesProp) ? edgesProp.EnumerateArray().ToList() : new List<JsonElement>();

            var context = await _profiles.GetOrLaunchAsync(profileId, startupArgsJson, fingerprintJson, proxyJson, _options.RunHeaded);
            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            var stepMap = steps.ToDictionary(x => x.GetProperty("id").GetString()!, x => x);
            var edgeMap = edges.GroupBy(x => x.GetProperty("source").GetString()!).ToDictionary(g => g.Key!, g => g.ToList());

            string? currentId = steps.FirstOrDefault().GetProperty("id").GetString();
            int guard = 0;

            while (!string.IsNullOrWhiteSpace(currentId) && guard++ < 500)
            {
                var step = stepMap[currentId];
                var type = step.GetProperty("type").GetString() ?? "";
                var data = step.TryGetProperty("data", out var d) ? d : default;
                var label = data.ValueKind != JsonValueKind.Undefined && data.TryGetProperty("label", out var labelEl) ? labelEl.GetString() : currentId;

                await _api.ReportProgressAsync(new AgentProgressRequest
                {
                    TaskRunId = taskRunId,
                    Status = "running",
                    CurrentStepId = currentId,
                    CurrentStepLabel = label ?? currentId,
                    CurrentUrl = page.Url,
                    Message = $"Executing {type}",
                    PreviewBase64 = await CaptureAsync(page),
                    LeaseToken = leaseToken,
                    HeartbeatAt = DateTime.UtcNow
                });

                switch (type)
                {
                    case "open":
                        await page.GotoAsync(data.GetProperty("url").GetString()!);
                        break;
                    case "click":
                        await page.ClickAsync(data.GetProperty("selector").GetString()!);
                        break;
                    case "type":
                        await page.FillAsync(data.GetProperty("selector").GetString()!, data.GetProperty("value").GetString() ?? "");
                        break;
                    case "wait_for_element":
                        await page.WaitForSelectorAsync(data.GetProperty("selector").GetString()!, new() { Timeout = data.TryGetProperty("timeout", out var timeout) ? timeout.GetInt32() : 10000 });
                        break;
                    case "wait_for_timeout":
                        await page.WaitForTimeoutAsync(data.TryGetProperty("timeout", out var ms) ? ms.GetInt32() : 1000);
                        break;
                    case "hover":
                        await page.HoverAsync(data.GetProperty("selector").GetString()!);
                        break;
                    case "select":
                        await page.SelectOptionAsync(data.GetProperty("selector").GetString()!, data.GetProperty("value").GetString()!);
                        break;
                    case "upload_file":
                        await page.SetInputFilesAsync(data.GetProperty("selector").GetString()!, data.GetProperty("filePath").GetString()!);
                        break;
                    case "scroll":
                        await page.EvaluateAsync("window.scrollBy(0, arguments[0])", data.TryGetProperty("deltaY", out var dy) ? dy.GetInt32() : 600);
                        break;
                    case "execute_js":
                        var jsResult = await page.EvaluateAsync<object>(data.GetProperty("script").GetString()!);
                        result[currentId] = jsResult;
                        break;
                    case "extract_text":
                        var text = await page.TextContentAsync(data.GetProperty("selector").GetString()!);
                        result[currentId] = text;
                        break;
                    case "loop":
                        var count = data.TryGetProperty("count", out var cnt) ? cnt.GetInt32() : 1;
                        result[$"{currentId}_loopRemaining"] = count;
                        break;
                    case "branch":
                        // branch resolution happens on edges below
                        break;
                    case "end_success":
                        status = "completed";
                        currentId = null;
                        continue;
                    case "end_fail":
                        status = "failed";
                        currentId = null;
                        continue;
                }

                currentId = ResolveNext(currentId, type, data, result, edgeMap);
            }

            await _api.ReportCompleteAsync(new AgentCompleteRequest
            {
                TaskRunId = taskRunId,
                Status = status,
                ResultJson = JsonSerializer.Serialize(result),
                FinalPreviewBase64 = await CaptureAsync(page),
                LeaseToken = leaseToken
            });
        }
        catch (Exception ex)
        {
            await _api.ReportCompleteAsync(new AgentCompleteRequest
            {
                TaskRunId = taskRunId,
                Status = "failed",
                ResultJson = JsonSerializer.Serialize(new { error = ex.Message }),
                LeaseToken = leaseToken,
                ErrorCode = "executor_error",
                ErrorMessage = ex.Message
            });
        }
    }

    private static string? ResolveNext(string currentId, string type, JsonElement data, Dictionary<string, object?> result, Dictionary<string, List<JsonElement>> edgeMap)
    {
        if (!edgeMap.TryGetValue(currentId, out var edges) || edges.Count == 0) return null;
        if (type == "if_text_contains")
        {
            var sourceKey = data.GetProperty("sourceStepId").GetString()!;
            var keyword = data.GetProperty("keyword").GetString() ?? "";
            var text = result.TryGetValue(sourceKey, out var obj) ? obj?.ToString() ?? "" : "";
            var handle = text.Contains(keyword, StringComparison.OrdinalIgnoreCase) ? "true" : "false";
            return edges.FirstOrDefault(x => (x.TryGetProperty("sourceHandle", out var h) ? h.GetString() : "") == handle).GetProperty("target").GetString();
        }
        if (type == "branch")
        {
            var mode = data.TryGetProperty("mode", out var modeEl) ? modeEl.GetString() : "first";
            if (mode == "random")
            {
                var index = Random.Shared.Next(edges.Count);
                return edges[index].GetProperty("target").GetString();
            }
            return edges.First().GetProperty("target").GetString();
        }
        if (type == "loop")
        {
            var key = $"{currentId}_loopRemaining";
            var remaining = result.TryGetValue(key, out var obj) ? Convert.ToInt32(obj) : 0;
            if (remaining > 1)
            {
                result[key] = remaining - 1;
                var loopEdge = edges.FirstOrDefault(x => (x.TryGetProperty("sourceHandle", out var h) ? h.GetString() : "") == "loop");
                if (loopEdge.ValueKind != JsonValueKind.Undefined) return loopEdge.GetProperty("target").GetString();
            }
            var doneEdge = edges.FirstOrDefault(x => (x.TryGetProperty("sourceHandle", out var h) ? h.GetString() : "") == "done");
            if (doneEdge.ValueKind != JsonValueKind.Undefined) return doneEdge.GetProperty("target").GetString();
        }
        return edges.First().GetProperty("target").GetString();
    }

    private static async Task<string?> CaptureAsync(IPage page)
    {
        try
        {
            var bytes = await page.ScreenshotAsync(new() { Type = ScreenshotType.Png });
            return Convert.ToBase64String(bytes);
        }
        catch
        {
            return null;
        }
    }
}
