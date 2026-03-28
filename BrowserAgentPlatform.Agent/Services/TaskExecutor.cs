using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Text;
using BrowserAgentPlatform.Agent.Contracts;
using BrowserAgentPlatform.Agent.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace BrowserAgentPlatform.Agent.Services;

public class TaskExecutor
{
    private static readonly HttpClient CommentAiHttpClient = new();
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
            var assertions = doc.RootElement.TryGetProperty("assertions", out var assertionsProp) && assertionsProp.ValueKind == JsonValueKind.Array
                ? assertionsProp.EnumerateArray().ToList()
                : new List<JsonElement>();
            if (steps.Count == 0)
            {
                throw new InvalidOperationException("Payload steps is empty.");
            }

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
                        await HumanTypeAsync(
                            page,
                            data.GetProperty("selector").GetString()!,
                            data.GetProperty("value").GetString() ?? "",
                            data.TryGetProperty("minKeyDelayMs", out var minTypeDelay) ? minTypeDelay.GetInt32() : 35,
                            data.TryGetProperty("maxKeyDelayMs", out var maxTypeDelay) ? maxTypeDelay.GetInt32() : 160,
                            data.TryGetProperty("typoRate", out var typeTypoRate) ? typeTypoRate.GetDouble() : 0.02,
                            data.TryGetProperty("backspaceRate", out var typeBackspaceRate) ? typeBackspaceRate.GetDouble() : 0.02
                        );
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

            if (status == "completed" && assertions.Count > 0)
            {
                var assertionSummary = EvaluateAssertions(assertions, result);
                result["assertions"] = assertionSummary;
                if (!(assertionSummary.TryGetValue("allPassed", out var allPassedObj) && allPassedObj is bool allPassed && allPassed))
                {
                    status = "failed";
                }
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

    private static Dictionary<string, object?> EvaluateAssertions(List<JsonElement> assertions, Dictionary<string, object?> result)
    {
        var failures = new List<string>();
        var passCount = 0;
        var resultNode = JsonNode.Parse(JsonSerializer.Serialize(result));
        foreach (var assertion in assertions)
        {
            var type = assertion.TryGetProperty("type", out var typeEl) ? (typeEl.GetString() ?? "") : "";
            var label = assertion.TryGetProperty("label", out var labelEl) ? (labelEl.GetString() ?? type) : type;
            bool passed;
            string message;

            switch (type)
            {
                case "step_exists":
                    {
                        var stepId = assertion.TryGetProperty("stepId", out var stepIdEl) ? (stepIdEl.GetString() ?? "") : "";
                        passed = !string.IsNullOrWhiteSpace(stepId) && result.ContainsKey(stepId);
                        message = passed ? "ok" : $"step `{stepId}` not found in results";
                        break;
                    }
                case "text_contains":
                    {
                        var sourceStepId = assertion.TryGetProperty("sourceStepId", out var sourceEl) ? (sourceEl.GetString() ?? "") : "";
                        var expected = assertion.TryGetProperty("expected", out var expectedEl) ? (expectedEl.GetString() ?? "") : "";
                        var actual = result.TryGetValue(sourceStepId, out var sourceObj) ? (sourceObj?.ToString() ?? "") : "";
                        passed = !string.IsNullOrWhiteSpace(expected) && actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
                        message = passed ? "ok" : $"expected `{expected}` in step `{sourceStepId}`, actual `{actual}`";
                        break;
                    }
                case "number_range":
                    {
                        var sourcePath = assertion.TryGetProperty("sourcePath", out var pathEl) ? (pathEl.GetString() ?? "") : "";
                        var min = assertion.TryGetProperty("min", out var minEl) ? minEl.GetDouble() : double.MinValue;
                        var max = assertion.TryGetProperty("max", out var maxEl) ? maxEl.GetDouble() : double.MaxValue;
                        var numberValue = TryGetNumberByPath(resultNode, sourcePath);
                        passed = numberValue.HasValue && numberValue.Value >= min && numberValue.Value <= max;
                        message = passed ? "ok" : $"value `{numberValue?.ToString() ?? "null"}` not in [{min},{max}] for path `{sourcePath}`";
                        break;
                    }
                default:
                    passed = false;
                    message = $"unsupported assertion type: {type}";
                    break;
            }

            if (passed) passCount++;
            else failures.Add($"{label}: {message}");
        }

        return new Dictionary<string, object?>
        {
            ["total"] = assertions.Count,
            ["passed"] = passCount,
            ["failed"] = failures.Count,
            ["allPassed"] = failures.Count == 0,
            ["failures"] = failures
        };
    }

    private static double? TryGetNumberByPath(JsonNode? node, string path)
    {
        if (node is null || string.IsNullOrWhiteSpace(path)) return null;
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        JsonNode? current = node;
        foreach (var part in parts)
        {
            current = current?[part];
            if (current is null) return null;
        }

        if (current is JsonValue value)
        {
            if (value.TryGetValue<double>(out var d)) return d;
            if (value.TryGetValue<int>(out var i)) return i;
            if (value.TryGetValue<long>(out var l)) return l;
        }
        return null;
    }

    private async Task<object> ExecuteTiktokMockSessionAsync(IPage page, JsonElement data)
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
        var behaviorProfile = data.TryGetProperty("behaviorProfile", out var profileEl) ? (profileEl.GetString() ?? "balanced") : "balanced";
        var preset = GetBehaviorPreset(behaviorProfile);
        var watchPattern = data.TryGetProperty("watchPattern", out var watchPatternEl) ? (watchPatternEl.GetString() ?? preset.watchPattern) : preset.watchPattern;
        var commentStyle = data.TryGetProperty("commentStyle", out var commentStyleEl) ? (commentStyleEl.GetString() ?? preset.commentStyle) : preset.commentStyle;
        var commentProvider = data.TryGetProperty("commentProvider", out var providerEl) ? (providerEl.GetString() ?? _options.CommentAi.Provider) : _options.CommentAi.Provider;

        var typingMinDelayMs = data.TryGetProperty("typingMinDelayMs", out var typeMinEl) ? Math.Clamp(typeMinEl.GetInt32(), 15, 600) : preset.typingMinDelayMs;
        var typingMaxDelayMs = data.TryGetProperty("typingMaxDelayMs", out var typeMaxEl) ? Math.Clamp(typeMaxEl.GetInt32(), typingMinDelayMs, 800) : preset.typingMaxDelayMs;
        var typoRate = data.TryGetProperty("typingTypoRate", out var typoEl) ? Math.Clamp(typoEl.GetDouble(), 0, 0.3) : preset.typoRate;
        var backspaceRate = data.TryGetProperty("typingBackspaceRate", out var backspaceEl) ? Math.Clamp(backspaceEl.GetDouble(), 0, 0.4) : preset.backspaceRate;
        var commentCooldownMinMs = data.TryGetProperty("commentCooldownMinMs", out var ccMinEl) ? Math.Clamp(ccMinEl.GetInt32(), 500, 10000) : preset.commentCooldownMinMs;
        var commentCooldownMaxMs = data.TryGetProperty("commentCooldownMaxMs", out var ccMaxEl) ? Math.Clamp(ccMaxEl.GetInt32(), commentCooldownMinMs, 20000) : preset.commentCooldownMaxMs;

        var likeKeywords = ParseKeywords(data, "likeByKeywords");
        var commentKeywords = ParseKeywords(data, "commentByKeywords");

        var videoTarget = Random.Shared.Next(minVideos, maxVideos + 1);
        var likeTarget = Math.Min(videoTarget, Random.Shared.Next(minLikes, maxLikes + 1));
        var commentTarget = Math.Min(videoTarget, Random.Shared.Next(minComments, maxComments + 1));
        var liked = 0;
        var commented = 0;
        var watched = 0;
        var anomalyCount = 0;
        var watchDurations = new List<int>();
        var typedCharCount = 0;
        var typedTimeMs = 0;
        var commentHistory = new List<string>();

        await page.GotoAsync($"{baseUrl}/login");
        await page.WaitForSelectorAsync("[data-testid='login-form']", new() { Timeout = 15000 });
        typedTimeMs += await HumanTypeAsync(page, "[data-testid='username-input']", username, typingMinDelayMs, typingMaxDelayMs, typoRate, backspaceRate);
        typedCharCount += username.Length;
        typedTimeMs += await HumanTypeAsync(page, "[data-testid='password-input']", password, typingMinDelayMs, typingMaxDelayMs, typoRate, backspaceRate);
        typedCharCount += password.Length;
        await page.WaitForTimeoutAsync(Random.Shared.Next(120, 680));
        await page.ClickAsync("[data-testid='login-submit']");
        await page.WaitForURLAsync(url => url.Contains("/feed"), new() { Timeout = 15000 });
        await page.WaitForSelectorAsync("[data-testid='tt-video-card'].active", new() { Timeout = 15000 });

        for (var i = 0; i < videoTarget; i++)
        {
            await page.WaitForSelectorAsync("[data-testid='tt-video-card'].active", new() { Timeout = 10000 });
            var active = page.Locator("[data-testid='tt-video-card'].active");
            var caption = (await active.Locator("[data-testid='tt-caption']").TextContentAsync())?.Trim() ?? "";

            var watchMs = CalculateWatchMs(i, videoTarget, minWatchMs, maxWatchMs, watchPattern);
            watchDurations.Add(watchMs);
            await page.WaitForTimeoutAsync(watchMs);
            watched++;

            if (Random.Shared.NextDouble() < 0.18)
            {
                await page.Mouse.WheelAsync(0, Random.Shared.Next(120, 520));
                await page.WaitForTimeoutAsync(Random.Shared.Next(80, 360));
            }

            var likeBias = likeKeywords.Count == 0 || MatchesAnyKeyword(caption, likeKeywords) ? preset.likeBiasHit : preset.likeBiasMiss;
            var shouldLike = liked < likeTarget &&
                (((likeTarget - liked) >= (videoTarget - i - 1)) || Random.Shared.NextDouble() < likeBias);
            if (shouldLike)
            {
                await active.Locator("[data-testid='tt-like-btn']").ClickAsync();
                liked++;
                await page.WaitForTimeoutAsync(Random.Shared.Next(140, 480));
            }

            var commentBias = commentKeywords.Count == 0 || MatchesAnyKeyword(caption, commentKeywords) ? preset.commentBiasHit : preset.commentBiasMiss;
            var shouldComment = commented < commentTarget &&
                (((commentTarget - commented) >= (videoTarget - i - 1)) || Random.Shared.NextDouble() < commentBias);
            if (shouldComment)
            {
                await active.Locator("[data-testid='tt-comment-toggle']").ClickAsync();
                await page.WaitForSelectorAsync("[data-testid='tt-comment-panel']", new() { Timeout = 5000 });
                var commentText = await GenerateAiLikeCommentAsync(page, active, commentStyle, commentHistory, commentProvider);
                typedTimeMs += await HumanTypeAsync(page, "[data-testid='tt-comment-input']", commentText, typingMinDelayMs, typingMaxDelayMs, typoRate, backspaceRate);
                typedCharCount += commentText.Length;
                await page.ClickAsync("[data-testid='tt-comment-submit']");
                commentHistory.Add(commentText);
                commented++;
                await page.WaitForTimeoutAsync(Random.Shared.Next(commentCooldownMinMs, commentCooldownMaxMs + 1));
                var closeBtn = page.Locator("[data-testid='tt-comment-close']");
                if (await closeBtn.CountAsync() > 0) await closeBtn.ClickAsync();
            }

            if (watchMs < minWatchMs + 300) anomalyCount++;
            if (i < videoTarget - 1)
            {
                await page.ClickAsync("[data-testid='tt-nav-next']");
                await page.WaitForTimeoutAsync(Random.Shared.Next(380, 960));
            }
        }

        var duplicateComments = commentHistory
            .GroupBy(x => x.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Sum(x => x.Count() - 1);

        return new
        {
            mode = "tiktok_mock_session",
            baseUrl,
            watchedVideos = watched,
            watchedTarget = videoTarget,
            likedVideos = liked,
            likeTarget,
            commentedVideos = commented,
            commentTarget,
            behaviorMetrics = new
            {
                avgWatchMs = watchDurations.Count == 0 ? 0 : watchDurations.Average(),
                watchPattern,
                behaviorProfile,
                commentProvider,
                avgTypingDelayMs = typedCharCount == 0 ? 0 : (double)typedTimeMs / typedCharCount,
                typingChars = typedCharCount,
                commentDuplicateRate = commentHistory.Count == 0 ? 0 : (double)duplicateComments / commentHistory.Count,
                anomalyRate = watched == 0 ? 0 : (double)anomalyCount / watched
            }
        };
    }

    private static (string watchPattern, string commentStyle, int typingMinDelayMs, int typingMaxDelayMs, double typoRate, double backspaceRate, int commentCooldownMinMs, int commentCooldownMaxMs, double likeBiasHit, double likeBiasMiss, double commentBiasHit, double commentBiasMiss) GetBehaviorPreset(string profile)
    {
        return profile switch
        {
            "aggressive" => ("engaged", "emoji_light", 20, 95, 0.01, 0.01, 900, 2600, 0.78, 0.46, 0.72, 0.4),
            "conservative" => ("fatigue", "short", 60, 230, 0.03, 0.03, 2600, 8200, 0.58, 0.22, 0.48, 0.18),
            _ => ("engaged", "friendly", 35, 170, 0.025, 0.02, 2200, 7200, 0.65, 0.35, 0.6, 0.3)
        };
    }

    private static int CalculateWatchMs(int index, int total, int minMs, int maxMs, string pattern)
    {
        if (maxMs <= minMs) return minMs;
        var progress = total <= 1 ? 0.5 : (double)index / (total - 1);
        var span = maxMs - minMs;
        return pattern switch
        {
            "explore" => minMs + (int)(span * (0.2 + 0.8 * Random.Shared.NextDouble() * (1 - progress * 0.6))),
            "fatigue" => minMs + (int)(span * Math.Max(0.15, 0.85 - progress * 0.55 + Random.Shared.NextDouble() * 0.2)),
            _ => minMs + (int)(span * (0.35 + 0.55 * Math.Abs(Math.Sin((progress + Random.Shared.NextDouble() * 0.15) * Math.PI))))
        };
    }

    private static List<string> ParseKeywords(JsonElement data, string propertyName)
    {
        if (!data.TryGetProperty(propertyName, out var keywordsEl) || keywordsEl.ValueKind != JsonValueKind.Array) return new List<string>();
        return keywordsEl.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => (x.GetString() ?? "").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static bool MatchesAnyKeyword(string text, List<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(text) || keywords.Count == 0) return false;
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<int> HumanTypeAsync(IPage page, string selector, string text, int minDelayMs, int maxDelayMs, double typoRate, double backspaceRate)
    {
        minDelayMs = Math.Clamp(minDelayMs, 10, 600);
        maxDelayMs = Math.Clamp(maxDelayMs, minDelayMs, 800);
        typoRate = Math.Clamp(typoRate, 0, 0.4);
        backspaceRate = Math.Clamp(backspaceRate, 0, 0.5);

        await page.ClickAsync(selector);
        await page.FillAsync(selector, "");
        var totalDelay = 0;
        var chars = text.ToCharArray();
        foreach (var c in chars)
        {
            var delay = Random.Shared.Next(minDelayMs, maxDelayMs + 1);
            await page.Keyboard.TypeAsync(c.ToString(), new() { Delay = delay });
            totalDelay += delay;

            if (char.IsLetterOrDigit(c) && Random.Shared.NextDouble() < typoRate)
            {
                var typoChar = char.ToLowerInvariant(c) == 'a' ? "s" : "a";
                var typoDelay = Random.Shared.Next(minDelayMs, maxDelayMs + 1);
                await page.Keyboard.TypeAsync(typoChar, new() { Delay = typoDelay });
                totalDelay += typoDelay;
                if (Random.Shared.NextDouble() < backspaceRate)
                {
                    await page.Keyboard.PressAsync("Backspace");
                    totalDelay += Random.Shared.Next(40, 120);
                }
            }

            if (Random.Shared.NextDouble() < 0.09)
            {
                var thinkDelay = Random.Shared.Next(120, 900);
                await page.WaitForTimeoutAsync(thinkDelay);
                totalDelay += thinkDelay;
            }
        }
        return totalDelay;
    }

    private async Task<string> GenerateAiLikeCommentAsync(IPage page, ILocator activeCard, string style, List<string> commentHistory, string provider)
    {
        var caption = (await activeCard.Locator("[data-testid='tt-caption']").TextContentAsync())?.Trim() ?? "";
        var comments = await page.Locator("[data-testid='tt-comment-panel'] .tt-comment-text").AllTextContentsAsync();
        var cleaned = comments
            .Select(x => Regex.Replace(x ?? "", "\\s+", " ").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(8)
            .ToList();

        var seed = cleaned.OrderByDescending(x => x.Length).FirstOrDefault() ?? "这个内容很有意思";
        if (caption.Length > 32) caption = caption[..32];
        if (seed.Length > 28) seed = seed[..28];

        var styleText = style switch
        {
            "question" => $"这个点很有启发，{seed}，你们平时也会这样做吗？",
            "short" => $"赞同，{seed}，有收获。",
            "emoji_light" => $"看完很有共鸣，{seed}，这个细节我很喜欢 🙂",
            _ => $"看完很有共鸣，{seed}。{(string.IsNullOrWhiteSpace(caption) ? "节奏感很好，支持一下～" : $"“{caption}”这个点我也很喜欢，支持！")}"
        };

        var llmText = await TryGenerateCommentByLlmAsync(provider, style, caption, cleaned, commentHistory);
        if (!string.IsNullOrWhiteSpace(llmText))
        {
            styleText = llmText;
        }

        if (commentHistory.Any(x => x.Equals(styleText, StringComparison.OrdinalIgnoreCase)))
        {
            return $"{styleText}（受教了）";
        }
        return styleText;
    }

    private async Task<string?> TryGenerateCommentByLlmAsync(string provider, string style, string caption, List<string> recentComments, List<string> history)
    {
        var normalizedProvider = (provider ?? "").Trim().ToLowerInvariant();
        if (normalizedProvider is not ("openai" or "deepseek")) return null;
        if (string.IsNullOrWhiteSpace(_options.CommentAi.ApiKey)) return null;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(_options.CommentAi.TimeoutSeconds, 3, 40)));
            var prompt = $"""
你是短视频评论助手。根据视频标题和已有评论，生成一条中文评论。
风格: {style}
视频文案: {caption}
已有评论: {string.Join(" | ", recentComments.Take(6))}
历史已发评论(避免重复): {string.Join(" | ", history.TakeLast(8))}
要求: 15-45字，口语化，不要敏感词，不要完全重复历史评论。
""";
            var body = JsonSerializer.Serialize(new
            {
                model = string.IsNullOrWhiteSpace(_options.CommentAi.Model)
                    ? (normalizedProvider == "deepseek" ? "deepseek-chat" : "gpt-4o-mini")
                    : _options.CommentAi.Model,
                messages = new object[]
                {
                    new { role = "system", content = "你是一个真实用户风格的短评生成器。" },
                    new { role = "user", content = prompt }
                },
                temperature = 0.8
            });

            var endpoint = string.IsNullOrWhiteSpace(_options.CommentAi.Endpoint)
                ? (normalizedProvider == "deepseek"
                    ? "https://api.deepseek.com/v1/chat/completions"
                    : "https://api.openai.com/v1/chat/completions")
                : _options.CommentAi.Endpoint;
            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.CommentAi.ApiKey);
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var res = await CommentAiHttpClient.SendAsync(req, cts.Token);
            if (!res.IsSuccessStatusCode) return null;
            var text = await res.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(text);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(content)) return null;
            return Regex.Replace(content, "\\s+", " ").Trim();
        }
        catch
        {
            return null;
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
            return GetTargetByHandleOrFirst(edges, handle);
        }
        if (type == "branch")
        {
            var mode = data.TryGetProperty("mode", out var modeEl) ? modeEl.GetString() : "first";
            if (mode == "random")
            {
                var index = Random.Shared.Next(edges.Count);
                return edges[index].GetProperty("target").GetString();
            }
            return GetTargetByHandleOrFirst(edges, null);
        }
        if (type == "loop")
        {
            var key = $"{currentId}_loopRemaining";
            var remaining = result.TryGetValue(key, out var obj) ? Convert.ToInt32(obj) : 0;
            if (remaining > 1)
            {
                result[key] = remaining - 1;
                var loopTarget = GetTargetByHandleOrFirst(edges, "loop", fallbackToFirst: false);
                if (!string.IsNullOrWhiteSpace(loopTarget)) return loopTarget;
            }
            var doneTarget = GetTargetByHandleOrFirst(edges, "done", fallbackToFirst: false);
            if (!string.IsNullOrWhiteSpace(doneTarget)) return doneTarget;
        }
        return GetTargetByHandleOrFirst(edges, null);
    }

    private static string? GetTargetByHandleOrFirst(List<JsonElement> edges, string? expectedHandle, bool fallbackToFirst = true)
    {
        if (edges.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(expectedHandle))
        {
            var matchedEdge = edges.FirstOrDefault(x => (x.TryGetProperty("sourceHandle", out var h) ? h.GetString() : "") == expectedHandle);
            if (matchedEdge.ValueKind != JsonValueKind.Undefined && matchedEdge.TryGetProperty("target", out var matchedTarget))
            {
                return matchedTarget.GetString();
            }
        }

        if (!fallbackToFirst) return null;
        return edges[0].TryGetProperty("target", out var firstTarget) ? firstTarget.GetString() : null;
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
