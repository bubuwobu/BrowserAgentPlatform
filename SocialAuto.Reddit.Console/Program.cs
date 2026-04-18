using System.Text.Json;
using Microsoft.Playwright;

var (config, configBaseDir) = await LoadConfigAsync(args);
NormalizeConfigPaths(config, configBaseDir);

var random = new Random();
var endAt = DateTime.UtcNow.AddMinutes(config.RunMinutes);

Console.WriteLine($"[BOOT] Reddit bot start. RunMinutes={config.RunMinutes}, BaseUrl={config.BaseUrl}");
Console.WriteLine($"[BOOT] mode: keyword-first; openPostProb={config.OpenRandomPostProbability}, randomLikeProb={config.LikeProbability}, keywordLikeProb={config.KeywordLikeProbability}");

using var playwright = await Playwright.CreateAsync();
await using var context = await playwright.Chromium.LaunchPersistentContextAsync(
    config.ProfileDir,
    new BrowserTypeLaunchPersistentContextOptions
    {
        Headless = config.Headless,
        Locale = config.Isolation.Locale,
        TimezoneId = config.Isolation.TimezoneId,
        UserAgent = string.IsNullOrWhiteSpace(config.Isolation.UserAgent) ? null : config.Isolation.UserAgent,
        ViewportSize = new ViewportSize { Width = config.Isolation.ViewportWidth, Height = config.Isolation.ViewportHeight }
    }
);

context.SetDefaultTimeout(12000);
context.SetDefaultNavigationTimeout(45000);

var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
await BootstrapLoginAsync(context, page, config);

var cycle = 0;
while (DateTime.UtcNow < endAt)
{
    cycle++;
    Console.WriteLine($"[CYCLE {cycle}] url={page.Url}");

    await EnsureFeedAsync(page, config);
    await page.Mouse.WheelAsync(0, random.Next(config.Scroll.MinDeltaY, config.Scroll.MaxDeltaY + 1));
    await page.WaitForTimeoutAsync(random.Next(config.Scroll.MinPauseMs, config.Scroll.MaxPauseMs + 1));

    var openedPost = false;
    if (ShouldAct(config.OpenRandomPostProbability, random))
    {
        openedPost = await TryRandomClickAsync(page, config.Selectors.PostEntryLinks, "OPEN_POST", random);
        if (openedPost)
        {
            await page.WaitForTimeoutAsync(random.Next(1000, 2200));
        }
    }

    var postText = await ReadPostTextAsync(page, config.Selectors.PostTextSelectors);
    var matchedRule = MatchKeywordRule(postText, config.KeywordRules);

    if (matchedRule is not null)
    {
        Console.WriteLine($"[KEYWORD] matched='{matchedRule.Keyword}'");
    }

    var shouldLike = matchedRule is not null
        ? ShouldAct(config.KeywordLikeProbability, random)
        : ShouldAct(config.LikeProbability, random);

    if (shouldLike)
    {
        await TryRandomClickAsync(page, config.Selectors.LikeButtons, matchedRule is null ? "LIKE_RANDOM" : "LIKE_KEYWORD", random);
    }

    var shouldComment = matchedRule is not null
        ? ShouldAct(config.KeywordCommentProbability, random)
        : ShouldAct(config.CommentProbability, random);

    if (shouldComment)
    {
        await TryCommentAsync(page, config, random, matchedRule, postText);
    }

    var waitMs = random.Next(config.MinWaitMs, config.MaxWaitMs + 1);
    Console.WriteLine($"[CYCLE {cycle}] wait={waitMs}ms");
    await page.WaitForTimeoutAsync(waitMs);

    if (openedPost && ShouldAct(0.65, random))
    {
        await EnsureFeedAsync(page, config);
    }
}

Console.WriteLine("[DONE] Reddit bot completed.");
if (config.Login.ExportCookiesOnExit)
{
    await SaveCookiesAsync(context, config.Login.ExportCookieFilePath);
}

static async Task<(RedditBotConfig Config, string ConfigBaseDir)> LoadConfigAsync(string[] args)
{
    var candidates = new List<string>();
    if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
    {
        candidates.Add(args[0]);
    }

    candidates.Add(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "SocialAuto.Reddit.Console", "appsettings.json"));

    var configPath = candidates.FirstOrDefault(File.Exists);
    if (string.IsNullOrWhiteSpace(configPath))
    {
        Console.WriteLine("[WARN] appsettings.json not found, using built-in defaults.");
        return (new RedditBotConfig(), Directory.GetCurrentDirectory());
    }

    Console.WriteLine($"[BOOT] Using config: {configPath}");
    var config = JsonSerializer.Deserialize<RedditBotConfig>(
                     await File.ReadAllTextAsync(configPath),
                     new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                 ) ?? new RedditBotConfig();

    return (config, Path.GetDirectoryName(configPath) ?? Directory.GetCurrentDirectory());
}

static void NormalizeConfigPaths(RedditBotConfig config, string configBaseDir)
{
    config.ProfileDir = ResolvePath(configBaseDir, config.ProfileDir);
    config.Login.CookieFilePath = ResolvePath(configBaseDir, config.Login.CookieFilePath);
    config.Login.ExportCookieFilePath = ResolvePath(configBaseDir, config.Login.ExportCookieFilePath);
}

static string ResolvePath(string configBaseDir, string path)
{
    if (string.IsNullOrWhiteSpace(path)) return path;
    return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(configBaseDir, path));
}

static bool ShouldAct(double probability, Random random)
{
    var normalized = Math.Clamp(probability, 0, 1);
    return random.NextDouble() <= normalized;
}

static async Task<bool> TryRandomClickAsync(IPage page, List<string> selectors, string label, Random random)
{
    foreach (var selector in selectors)
    {
        try
        {
            var locator = page.Locator(selector);
            var count = await locator.CountAsync();
            if (count == 0) continue;

            var shuffled = Enumerable.Range(0, count).OrderBy(_ => random.Next()).ToList();
            foreach (var index in shuffled)
            {
                var target = locator.Nth(index);
                if (!await target.IsVisibleAsync()) continue;
                await target.ScrollIntoViewIfNeededAsync();
                await page.WaitForTimeoutAsync(random.Next(180, 600));
                await target.ClickAsync(new LocatorClickOptions { Delay = random.Next(35, 140) });
                Console.WriteLine($"[{label}] clicked selector={selector}, index={index}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{label}] selector failed: {selector}, err={ex.Message}");
        }
    }

    Console.WriteLine($"[{label}] no target clicked.");
    return false;
}

static async Task EnsureFeedAsync(IPage page, RedditBotConfig config)
{
    try
    {
        if (!page.Url.Contains("/comments/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await page.GotoAsync(config.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 });
        Console.WriteLine("[NAV] returned to feed page.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NAV] failed to return feed: {ex.Message}");
    }
}

static async Task BootstrapLoginAsync(IBrowserContext context, IPage page, RedditBotConfig config)
{
    if (string.Equals(config.Login.Mode, "cookie_bootstrap", StringComparison.OrdinalIgnoreCase))
    {
        var cookiePath = config.Login.CookieFilePath;
        if (File.Exists(cookiePath))
        {
            var cookies = await LoadCookiesAsync(cookiePath);
            if (cookies.Count > 0)
            {
                await context.ClearCookiesAsync();
                await context.AddCookiesAsync(cookies);
                Console.WriteLine($"[LOGIN] Loaded cookies: {cookies.Count} from {cookiePath}");
            }
        }
        else
        {
            Console.WriteLine($"[LOGIN] Cookie file not found: {cookiePath}, fallback to manual page open.");
        }
    }

    await GoWithRetryAsync(page, config.BaseUrl);
    await ReportLoginStateAsync(page);

    if (config.Login.WaitForManualConfirm)
    {
        Console.WriteLine("[LOGIN] 请在浏览器窗口完成手工登录，然后按回车继续...");
        Console.ReadLine();
    }
}

static async Task GoWithRetryAsync(IPage page, string url, int maxAttempts = 3)
{
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });
            return;
        }
        catch when (attempt < maxAttempts)
        {
            await page.WaitForTimeoutAsync(900 * attempt);
            Console.WriteLine($"[NAV] retry {attempt}/{maxAttempts} for {url}");
        }
    }
}

static async Task ReportLoginStateAsync(IPage page)
{
    var url = page.Url;
    var loginIndicators = new[]
    {
        "a[href*='/login']",
        "input[name='username']",
        "button[type='submit']"
    };

    var hasLoginUi = false;
    foreach (var selector in loginIndicators)
    {
        var loc = page.Locator(selector);
        if (await loc.CountAsync() > 0 && await loc.First.IsVisibleAsync())
        {
            hasLoginUi = true;
            break;
        }
    }

    if (url.Contains("/login", StringComparison.OrdinalIgnoreCase) || hasLoginUi)
    {
        Console.WriteLine($"[LOGIN] 未检测到登录态，当前页面可能仍需登录。url={url}");
        Console.WriteLine("[LOGIN] 请检查 cookie 是否过期，或是否缺少关键 cookie（例如 reddit_session、csrf_token）。");
    }
    else
    {
        Console.WriteLine($"[LOGIN] 已进入非登录页，疑似登录成功。url={url}");
    }
}

static async Task<List<Cookie>> LoadCookiesAsync(string cookiePath)
{
    var text = await File.ReadAllTextAsync(cookiePath);
    using var doc = JsonDocument.Parse(text);
    var root = doc.RootElement;
    var source = root.ValueKind == JsonValueKind.Array
        ? root
        : (root.TryGetProperty("cookies", out var cookiesEl) ? cookiesEl : default);

    var result = new List<Cookie>();
    if (source.ValueKind != JsonValueKind.Array) return result;

    foreach (var item in source.EnumerateArray())
    {
        if (!item.TryGetProperty("name", out var nameEl) || !item.TryGetProperty("value", out var valueEl))
            continue;

        var cookie = new Cookie
        {
            Name = nameEl.GetString() ?? "",
            Value = valueEl.GetString() ?? "",
            Url = item.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null,
            Domain = item.TryGetProperty("domain", out var domainEl) ? domainEl.GetString() : null,
            Path = item.TryGetProperty("path", out var pathEl) ? pathEl.GetString() : "/",
            Expires = item.TryGetProperty("expires", out var expEl) && expEl.TryGetDouble(out var exp) ? (float?)exp : null,
            HttpOnly = item.TryGetProperty("httpOnly", out var httpOnlyEl) && httpOnlyEl.GetBoolean(),
            Secure = item.TryGetProperty("secure", out var secureEl) && secureEl.GetBoolean(),
            SameSite = item.TryGetProperty("sameSite", out var sameSiteEl) ? ParseSameSite(sameSiteEl.GetString()) : null
        };

        if (string.IsNullOrWhiteSpace(cookie.Url) && string.IsNullOrWhiteSpace(cookie.Domain))
        {
            cookie.Url = "https://www.reddit.com";
        }

        NormalizeRedditCookie(cookie);
        result.Add(cookie);
    }

    return result;
}

static void NormalizeRedditCookie(Cookie cookie)
{
    if (!string.IsNullOrWhiteSpace(cookie.Domain))
    {
        return;
    }

    if (!Uri.TryCreate(cookie.Url, UriKind.Absolute, out var uri))
    {
        return;
    }

    if (!uri.Host.EndsWith("reddit.com", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    cookie.Domain = ".reddit.com";
    cookie.Path = string.IsNullOrWhiteSpace(cookie.Path) ? "/" : cookie.Path;
    cookie.Url = null;
}

static SameSiteAttribute? ParseSameSite(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;
    return raw.ToLowerInvariant() switch
    {
        "lax" => SameSiteAttribute.Lax,
        "strict" => SameSiteAttribute.Strict,
        "none" => SameSiteAttribute.None,
        _ => null
    };
}

static async Task SaveCookiesAsync(IBrowserContext context, string path)
{
    var cookies = await context.CookiesAsync();
    var dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { cookies }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"[LOGIN] Cookies exported: {path}");
}

static async Task<string> ReadPostTextAsync(IPage page, List<string> textSelectors)
{
    foreach (var selector in textSelectors)
    {
        try
        {
            var nodes = page.Locator(selector);
            var count = await nodes.CountAsync();
            for (var i = 0; i < count; i++)
            {
                var txt = (await nodes.Nth(i).InnerTextAsync()).Trim();
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    return txt;
                }
            }
        }
        catch
        {
            // ignore selector-level failures
        }
    }

    return string.Empty;
}

static KeywordRule? MatchKeywordRule(string postText, List<KeywordRule> rules)
{
    if (string.IsNullOrWhiteSpace(postText) || rules.Count == 0)
    {
        return null;
    }

    foreach (var rule in rules)
    {
        if (!string.IsNullOrWhiteSpace(rule.Keyword) && postText.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase))
        {
            return rule;
        }
    }

    return null;
}

static async Task TryCommentAsync(IPage page, RedditBotConfig config, Random random, KeywordRule? matchedRule, string postText)
{
    try
    {
        if (!page.Url.Contains("/comments/", StringComparison.OrdinalIgnoreCase))
        {
            await TryRandomClickAsync(page, config.Selectors.PostEntryLinks, "OPEN_POST", random);
            await page.WaitForTimeoutAsync(random.Next(900, 1800));
        }

        var commentText = GenerateAiStyleComment(config, matchedRule, random, postText);

        foreach (var inputSelector in config.Selectors.CommentInputs)
        {
            var input = page.Locator(inputSelector).First;
            if (await input.CountAsync() == 0) continue;
            if (!await input.IsVisibleAsync()) continue;

            await input.ClickAsync();
            await input.FillAsync(commentText);
            Console.WriteLine($"[COMMENT] filled: {commentText}");

            foreach (var submitSelector in config.Selectors.CommentSubmitButtons)
            {
                var submit = page.Locator(submitSelector).First;
                if (await submit.CountAsync() == 0) continue;
                if (!await submit.IsVisibleAsync()) continue;
                await submit.ClickAsync();
                Console.WriteLine("[COMMENT] submitted.");
                return;
            }
        }

        Console.WriteLine("[COMMENT] input or submit target not found.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[COMMENT] failed: {ex.Message}");
    }
}

static string GenerateAiStyleComment(RedditBotConfig config, KeywordRule? matchedRule, Random random, string postText)
{
    var starters = new[] { "Nice one", "Love this", "Great share", "Interesting take" };
    var closers = new[] { "thanks for posting.", "this was helpful.", "keep it coming!", "learned something new." };

    if (matchedRule is not null && matchedRule.CommentTemplates.Count > 0)
    {
        return matchedRule.CommentTemplates[random.Next(matchedRule.CommentTemplates.Count)];
    }

    if (config.CommentTemplates.Count > 0)
    {
        var baseComment = config.CommentTemplates[random.Next(config.CommentTemplates.Count)];
        if (!string.IsNullOrWhiteSpace(postText))
        {
            var trimmed = postText.Length > 28 ? postText[..28] + "..." : postText;
            return $"{baseComment} ({trimmed})";
        }

        return baseComment;
    }

    return $"{starters[random.Next(starters.Length)]}, {closers[random.Next(closers.Length)]}";
}

public class RedditBotConfig
{
    public string BaseUrl { get; set; } = "https://www.reddit.com/search/?q=%E4%B8%AD%E5%9B%BD%E7%93%B7%E5%99%A8";
    public int RunMinutes { get; set; } = 60;
    public bool Headless { get; set; } = false;
    public string ProfileDir { get; set; } = "./profiles/reddit";
    public double LikeProbability { get; set; } = 0.08;
    public double KeywordLikeProbability { get; set; } = 0.85;
    public double CommentProbability { get; set; } = 0.01;
    public double KeywordCommentProbability { get; set; } = 0.25;
    public double OpenRandomPostProbability { get; set; } = 0.55;
    public int MinWaitMs { get; set; } = 4000;
    public int MaxWaitMs { get; set; } = 9000;
    public IsolationOptions Isolation { get; set; } = new();
    public LoginOptions Login { get; set; } = new();
    public ScrollOptions Scroll { get; set; } = new();
    public RedditSelectors Selectors { get; set; } = new();
    public List<KeywordRule> KeywordRules { get; set; } =
    [
        new() { Keyword = "中国瓷器", CommentTemplates = ["中国瓷器的纹饰真的太美了。", "这件中国瓷器的细节很有味道。"] },
        new() { Keyword = "青花瓷", CommentTemplates = ["青花瓷的发色非常漂亮，质感很棒。", "这件青花瓷看起来很有年代感。"] },
        new() { Keyword = "景德镇", CommentTemplates = ["景德镇工艺果然名不虚传。", "景德镇这类作品细节处理很讲究。"] },
        new() { Keyword = "汝窑", CommentTemplates = ["汝窑的釉色很温润，太喜欢了。", "这件汝窑风格的作品气质很高级。"] },
        new() { Keyword = "官窑", CommentTemplates = ["官窑器型和釉面都很耐看。", "官窑风格非常经典，收藏价值高。"] }
    ];

    public List<string> CommentTemplates { get; set; } =
    [
        "Nice post! Thanks for sharing.",
        "Interesting perspective 👀",
        "This is helpful, appreciate it."
    ];
}

public class KeywordRule
{
    public string Keyword { get; set; } = string.Empty;
    public List<string> CommentTemplates { get; set; } = [];
}

public class LoginOptions
{
    // manual_once | cookie_bootstrap
    public string Mode { get; set; } = "manual_once";
    public string CookieFilePath { get; set; } = "./profiles/reddit/cookies.json";
    public bool WaitForManualConfirm { get; set; } = true;
    public bool ExportCookiesOnExit { get; set; } = true;
    public string ExportCookieFilePath { get; set; } = "./profiles/reddit/cookies.json";
}

public class IsolationOptions
{
    public bool Enabled { get; set; } = false;
    public string FingerprintPreset { get; set; } = "desktop_chrome_default";
    public string BehaviorPreset { get; set; } = "human_browse_lowfreq";
    public string Locale { get; set; } = "en-US";
    public string TimezoneId { get; set; } = "America/Los_Angeles";
    public string UserAgent { get; set; } = "";
    public int ViewportWidth { get; set; } = 1366;
    public int ViewportHeight { get; set; } = 768;
}

public class ScrollOptions
{
    public int MinDeltaY { get; set; } = 420;
    public int MaxDeltaY { get; set; } = 1200;
    public int MinPauseMs { get; set; } = 100;
    public int MaxPauseMs { get; set; } = 400;
}

public class RedditSelectors
{
    public List<string> LikeButtons { get; set; } =
    [
        "button[aria-label*=upvote i]",
        "button[aria-label*=like i]"
    ];

    public List<string> PostEntryLinks { get; set; } =
    [
        "a[data-click-id='comments']",
        "a[href*='/comments/']"
    ];

    public List<string> PostTextSelectors { get; set; } =
    [
        "h1",
        "[data-test-id='post-content']",
        "article"
    ];

    public List<string> CommentInputs { get; set; } =
    [
        "textarea",
        "div[contenteditable='true']"
    ];

    public List<string> CommentSubmitButtons { get; set; } =
    [
        "button[type='submit']",
        "button:has-text('Comment')"
    ];
}
