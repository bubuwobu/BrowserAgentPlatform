using System.Text.Json;
using System.Text.RegularExpressions;
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
                    case "tiktok_mock_session":
                        result[currentId] = await ExecuteTiktokMockSessionAsync(page, data);
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

    private static async Task<object> ExecuteTiktokMockSessionAsync(IPage page, JsonElement data)
    {
        var baseUrl = data.TryGetProperty("baseUrl", out var baseUrlEl) ? (baseUrlEl.GetString() ?? "http://localhost:3001").TrimEnd('/') : "http://localhost:3001";
        var username = data.TryGetProperty("username", out var uEl) ? (uEl.GetString() ?? "alice") : "alice";
        var password = data.TryGetProperty("password", out var pEl) ? (pEl.GetString() ?? "123456") : "123456";
        var minVideos = data.TryGetProperty("minVideos", out var minV) ? Math.Max(1, minV.GetInt32()) : 3;
        var maxVideos = data.TryGetProperty("maxVideos", out var maxV) ? Math.Max(minVideos, maxV.GetInt32()) : Math.Max(minVideos, 6);
        var minWatchMs = data.TryGetProperty("minWatchMs", out var minW) ? Math.Max(800, minW.GetInt32()) : 2000;
        var maxWatchMs = data.TryGetProperty("maxWatchMs", out var maxW) ? Math.Max(minWatchMs, maxW.GetInt32()) : Math.Max(minWatchMs, 6000);
        var minLikes = data.TryGetProperty("minLikes", out var minL) ? Math.Max(0, minL.GetInt32()) : 0;
        var maxLikes = data.TryGetProperty("maxLikes", out var maxL) ? Math.Max(minLikes, maxL.GetInt32()) : 2;
        var minComments = data.TryGetProperty("minComments", out var minC) ? Math.Max(0, minC.GetInt32()) : 0;
        var maxComments = data.TryGetProperty("maxComments", out var maxC) ? Math.Max(minComments, maxC.GetInt32()) : 2;

        var videoTarget = Random.Shared.Next(minVideos, maxVideos + 1);
        var likeTarget = Math.Min(videoTarget, Random.Shared.Next(minLikes, maxLikes + 1));
        var commentTarget = Math.Min(videoTarget, Random.Shared.Next(minComments, maxComments + 1));
        var liked = 0;
        var commented = 0;
        var watched = 0;

        await page.GotoAsync($"{baseUrl}/login");
        await page.WaitForSelectorAsync("[data-testid='login-form']", new() { Timeout = 15000 });
        await page.FillAsync("[data-testid='username-input']", username);
        await page.FillAsync("[data-testid='password-input']", password);
        await page.ClickAsync("[data-testid='login-submit']");
        await page.WaitForURLAsync(url => url.Contains("/feed"), new() { Timeout = 15000 });
        await page.WaitForSelectorAsync("[data-testid='tt-video-card'].active", new() { Timeout = 15000 });

        for (var i = 0; i < videoTarget; i++)
        {
            await page.WaitForSelectorAsync("[data-testid='tt-video-card'].active", new() { Timeout = 10000 });
            var active = page.Locator("[data-testid='tt-video-card'].active");
            var watchMs = Random.Shared.Next(minWatchMs, maxWatchMs + 1);
            await page.WaitForTimeoutAsync(watchMs);
            watched++;

            if (liked < likeTarget && (((likeTarget - liked) >= (videoTarget - i - 1)) || Random.Shared.NextDouble() > 0.45))
            {
                await active.Locator("[data-testid='tt-like-btn']").ClickAsync();
                liked++;
                await page.WaitForTimeoutAsync(200);
            }

            if (commented < commentTarget && (((commentTarget - commented) >= (videoTarget - i - 1)) || Random.Shared.NextDouble() > 0.5))
            {
                await active.Locator("[data-testid='tt-comment-toggle']").ClickAsync();
                await page.WaitForSelectorAsync("[data-testid='tt-comment-panel']", new() { Timeout = 5000 });
                var commentText = await GenerateAiLikeCommentAsync(page, active);
                await page.FillAsync("[data-testid='tt-comment-input']", commentText);
                await page.ClickAsync("[data-testid='tt-comment-submit']");
                commented++;
                await page.WaitForTimeoutAsync(350);
                var closeBtn = page.Locator("[data-testid='tt-comment-close']");
                if (await closeBtn.CountAsync() > 0) await closeBtn.ClickAsync();
            }

            if (i < videoTarget - 1)
            {
                await page.ClickAsync("[data-testid='tt-nav-next']");
                await page.WaitForTimeoutAsync(650);
            }
        }

        return new
        {
            mode = "tiktok_mock_session",
            baseUrl,
            watchedVideos = watched,
            watchedTarget = videoTarget,
            likedVideos = liked,
            likeTarget,
            commentedVideos = commented,
            commentTarget
        };
    }

    private static async Task<string> GenerateAiLikeCommentAsync(IPage page, ILocator activeCard)
    {
        var caption = (await activeCard.Locator("[data-testid='tt-caption']").TextContentAsync())?.Trim() ?? "";
        var comments = await page.Locator("[data-testid='tt-comment-panel'] .tt-comment-text").AllTextContentsAsync();
        var cleaned = comments
            .Select(x => Regex.Replace(x ?? "", "\\s+", " ").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(5)
            .ToList();

        var seed = cleaned.FirstOrDefault() ?? "这个内容很有意思";
        if (caption.Length > 28) caption = caption[..28];
        if (seed.Length > 24) seed = seed[..24];
        return $"看完很有共鸣，{seed}。{(string.IsNullOrWhiteSpace(caption) ? "节奏感很好，支持一下～" : $"“{caption}”这个点我也很喜欢，支持！")}";
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
