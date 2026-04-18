using System.Text.Json;
using Microsoft.Playwright;

var (config, configBaseDir) = await LoadConfigAsync(args);
NormalizeConfigPaths(config, configBaseDir);

var random = new Random();
var endAt = DateTime.UtcNow.AddMinutes(config.RunMinutes);

Console.WriteLine($"[BOOT] Instagram bot start. RunMinutes={config.RunMinutes}, BaseUrl={config.BaseUrl}");
Console.WriteLine($"[BOOT] Isolation reserved: enabled={config.Isolation.Enabled}, fingerprint={config.Isolation.FingerprintPreset}, behavior={config.Isolation.BehaviorPreset}");

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

var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
await BootstrapLoginAsync(context, page, config);

var cycle = 0;
while (DateTime.UtcNow < endAt)
{
    cycle++;
    Console.WriteLine($"[CYCLE {cycle}] url={page.Url}");

    await page.Mouse.WheelAsync(0, random.Next(config.Scroll.MinDeltaY, config.Scroll.MaxDeltaY + 1));
    await page.WaitForTimeoutAsync(random.Next(config.Scroll.MinPauseMs, config.Scroll.MaxPauseMs + 1));

    if (ShouldAct(config.LikeProbability, random))
    {
        await TryRandomClickAsync(page, config.Selectors.LikeButtons, "LIKE", random);
    }

    if (ShouldAct(config.CommentProbability, random))
    {
        await TryCommentAsync(page, config, random);
    }

    var waitMs = random.Next(config.MinWaitMs, config.MaxWaitMs + 1);
    Console.WriteLine($"[CYCLE {cycle}] wait={waitMs}ms");
    await page.WaitForTimeoutAsync(waitMs);
}

Console.WriteLine("[DONE] Instagram bot completed.");
if (config.Login.ExportCookiesOnExit)
{
    await SaveCookiesAsync(context, config.Login.ExportCookieFilePath);
}

static async Task<(InstagramBotConfig Config, string ConfigBaseDir)> LoadConfigAsync(string[] args)
{
    var candidates = new List<string>();
    if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
    {
        candidates.Add(args[0]);
    }

    candidates.Add(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "SocialAuto.Instagram.Console", "appsettings.json"));

    var configPath = candidates.FirstOrDefault(File.Exists);
    if (string.IsNullOrWhiteSpace(configPath))
    {
        Console.WriteLine("[WARN] appsettings.json not found, using built-in defaults.");
        return (new InstagramBotConfig(), Directory.GetCurrentDirectory());
    }

    Console.WriteLine($"[BOOT] Using config: {configPath}");
    var config = JsonSerializer.Deserialize<InstagramBotConfig>(
                     await File.ReadAllTextAsync(configPath),
                     new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                 ) ?? new InstagramBotConfig();

    return (config, Path.GetDirectoryName(configPath) ?? Directory.GetCurrentDirectory());
}

static void NormalizeConfigPaths(InstagramBotConfig config, string configBaseDir)
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

static async Task TryRandomClickAsync(IPage page, List<string> selectors, string label, Random random)
{
    foreach (var selector in selectors)
    {
        try
        {
            var locator = page.Locator(selector);
            var count = await locator.CountAsync();
            if (count == 0) continue;
            var index = random.Next(0, count);
            var target = locator.Nth(index);
            if (!await target.IsVisibleAsync()) continue;
            await target.ScrollIntoViewIfNeededAsync();
            await page.WaitForTimeoutAsync(random.Next(200, 700));
            await target.ClickAsync(new LocatorClickOptions { Delay = random.Next(30, 140) });
            Console.WriteLine($"[{label}] clicked selector={selector}, index={index}");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{label}] selector failed: {selector}, err={ex.Message}");
        }
    }

    Console.WriteLine($"[{label}] no target clicked.");
}

static async Task BootstrapLoginAsync(IBrowserContext context, IPage page, InstagramBotConfig config)
{
    if (string.Equals(config.Login.Mode, "cookie_bootstrap", StringComparison.OrdinalIgnoreCase))
    {
        var cookiePath = config.Login.CookieFilePath;
        if (File.Exists(cookiePath))
        {
            var cookies = await LoadCookiesAsync(cookiePath);
            if (cookies.Count > 0)
            {
                await context.AddCookiesAsync(cookies);
                Console.WriteLine($"[LOGIN] Loaded cookies: {cookies.Count} from {cookiePath}");
            }
        }
        else
        {
            Console.WriteLine($"[LOGIN] Cookie file not found: {cookiePath}, fallback to manual page open.");
        }
    }

    await page.GotoAsync(config.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });
    await ReportLoginStateAsync(page);

    if (config.Login.WaitForManualConfirm)
    {
        Console.WriteLine("[LOGIN] 请在浏览器窗口完成手工登录，然后按回车继续...");
        Console.ReadLine();
    }
}

static async Task ReportLoginStateAsync(IPage page)
{
    var url = page.Url;
    var loginIndicators = new[]
    {
        "input[name='username']",
        "input[name='password']",
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

    if (url.Contains("/accounts/login", StringComparison.OrdinalIgnoreCase) || hasLoginUi)
    {
        Console.WriteLine($"[LOGIN] 未检测到登录态，当前页面可能仍需登录。url={url}");
        Console.WriteLine("[LOGIN] 请检查 cookie 是否过期，或是否缺少关键 cookie（例如 sessionid、csrftoken）。");
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
            cookie.Url = "https://www.instagram.com";
        }

        result.Add(cookie);
    }

    return result;
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

static async Task TryCommentAsync(IPage page, InstagramBotConfig config, Random random)
{
    try
    {
        await TryRandomClickAsync(page, config.Selectors.PostEntryButtons, "OPEN_POST", random);
        await page.WaitForTimeoutAsync(random.Next(1200, 2500));

        var commentText = config.CommentTemplates[random.Next(0, config.CommentTemplates.Count)];

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
                Console.WriteLine($"[COMMENT] submitted.");
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

public class InstagramBotConfig
{
    public string BaseUrl { get; set; } = "https://www.instagram.com/explore/";
    public int RunMinutes { get; set; } = 60;
    public bool Headless { get; set; } = false;
    public string ProfileDir { get; set; } = "./profiles/instagram";
    public double LikeProbability { get; set; } = 0.22;
    public double CommentProbability { get; set; } = 0.08;
    public int MinWaitMs { get; set; } = 25000;
    public int MaxWaitMs { get; set; } = 60000;
    public IsolationOptions Isolation { get; set; } = new();
    public LoginOptions Login { get; set; } = new();
    public ScrollOptions Scroll { get; set; } = new();
    public InstagramSelectors Selectors { get; set; } = new();
    public List<string> CommentTemplates { get; set; } =
    [
        "Love this 🙌",
        "Great shot 🔥",
        "Really nice content, thanks for posting!"
    ];
}

public class LoginOptions
{
    // manual_once | cookie_bootstrap
    public string Mode { get; set; } = "manual_once";
    public string CookieFilePath { get; set; } = "./profiles/instagram/cookies.json";
    public bool WaitForManualConfirm { get; set; } = true;
    public bool ExportCookiesOnExit { get; set; } = true;
    public string ExportCookieFilePath { get; set; } = "./profiles/instagram/cookies.json";
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
    public int MaxDeltaY { get; set; } = 1150;
    public int MinPauseMs { get; set; } = 100;
    public int MaxPauseMs { get; set; } = 420;
}

public class InstagramSelectors
{
    public List<string> LikeButtons { get; set; } =
    [
        "article button:has(svg[aria-label=Like])",
        "button:has(svg[aria-label=Like])"
    ];

    public List<string> PostEntryButtons { get; set; } =
    [
        "article a",
        "a[href*='/p/']"
    ];

    public List<string> CommentInputs { get; set; } =
    [
        "textarea[aria-label='Add a comment…']",
        "textarea"
    ];

    public List<string> CommentSubmitButtons { get; set; } =
    [
        "button:has-text('Post')",
        "button[type='submit']"
    ];
}
