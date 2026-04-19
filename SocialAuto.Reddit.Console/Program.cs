using System.Text.Json;
using Microsoft.Playwright;

var (config, configBaseDir) = await LoadConfigAsync(args);
NormalizeConfigPaths(config, configBaseDir);

if (HasFlag(args, "--import-browser"))
{
    Console.WriteLine("[BOOT] Importing login state from running Chromium browser...");
    await ImportFromRunningBrowserAsync(config);
    Console.WriteLine("[BOOT] 已从运行中的 Chrome 导入登录态，程序结束。");
    return;
}

var random = new Random();
var endAt = DateTime.UtcNow.AddMinutes(config.RunMinutes);

Console.WriteLine($"[BOOT] Reddit bot start. RunMinutes={config.RunMinutes}, BaseUrl={config.BaseUrl}");
Console.WriteLine($"[BOOT] mode: keyword-first; openPostProb={config.OpenRandomPostProbability}, randomLikeProb={config.LikeProbability}, keywordLikeProb={config.KeywordLikeProbability}");

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(
    new BrowserTypeLaunchOptions
    {
        Headless = config.Headless
    }
);

var contextOptions = BuildBrowserContextOptions(config);
await using var context = await browser.NewContextAsync(contextOptions);
context.SetDefaultTimeout(12000);
context.SetDefaultNavigationTimeout(45000);

var page = await context.NewPageAsync();
Console.WriteLine("[HOTKEY] 控制台命令：save=立即导出当前登录态，import-browser=从正常Chrome导入登录态，status=检查登录态，quit=退出程序");
using var commandCts = new CancellationTokenSource();
var commandLoopTask = StartConsoleCommandLoopAsync(context, page, config, commandCts.Token);
var loginReady = await BootstrapLoginAsync(context, page, config);
if (!loginReady)
{
    Console.WriteLine("[FATAL] Login is still not ready after auto/manual recovery. Stop run to avoid invalid actions.");
    commandCts.Cancel();
    await StopCommandLoopAsync(commandLoopTask);
    return;
}

var cycle = 0;
while (DateTime.UtcNow < endAt)
{
    cycle++;
    var isLoggedIn = await ReportLoginStateAsync(page, verbose: false);
    if (!isLoggedIn)
    {
        Console.WriteLine($"[CYCLE {cycle}] login missing, retry bootstrap once...");
        loginReady = await BootstrapLoginAsync(context, page, config);
        if (!loginReady)
        {
            Console.WriteLine($"[CYCLE {cycle}] skip cycle due to missing login.");
            await page.WaitForTimeoutAsync(2500);
            continue;
        }
    }

    Console.WriteLine($"[CYCLE {cycle}] url={page.Url}");

    await EnsureFeedAsync(page, config);
    await page.Mouse.WheelAsync(0, random.Next(config.Scroll.MinDeltaY, config.Scroll.MaxDeltaY + 1));
    await page.WaitForTimeoutAsync(random.Next(config.Scroll.MinPauseMs, config.Scroll.MaxPauseMs + 1));

    if (ShouldAct(config.OpenRandomPostProbability, random))
    {
        var openedPost = await TryRandomClickAsync(page, config.Selectors.PostEntryLinks, "OPEN_POST", random);
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
        var likeLabel = matchedRule is null ? "LIKE_RANDOM" : "LIKE_KEYWORD";
        var liked = await TryLikeAsync(page, config, random, likeLabel);
        if (liked)
        {
            await SaveActionScreenshotAsync(page, config, likeLabel, matchedRule?.Keyword);
        }
    }

    var shouldComment = matchedRule is not null
        ? ShouldAct(config.KeywordCommentProbability, random)
        : ShouldAct(config.CommentProbability, random);

    if (shouldComment)
    {
        await TryCommentAsync(page, config, random, matchedRule, postText);
    }

    await TryReturnToFeedAsync(page, config);

    var waitMs = random.Next(config.MinWaitMs, config.MaxWaitMs + 1);
    Console.WriteLine($"[CYCLE {cycle}] wait={waitMs}ms");
    await page.WaitForTimeoutAsync(waitMs);
}

Console.WriteLine("[DONE] Reddit bot completed.");
if (config.Login.ExportCookiesOnExit)
{
    await PersistLoginStateForNextRunAsync(context, config);
}

commandCts.Cancel();
await StopCommandLoopAsync(commandLoopTask);

static BrowserNewContextOptions BuildBrowserContextOptions(RedditBotConfig config)
{
    var options = new BrowserNewContextOptions
    {
        Locale = config.Isolation.Locale,
        TimezoneId = config.Isolation.TimezoneId,
        UserAgent = string.IsNullOrWhiteSpace(config.Isolation.UserAgent) ? null : config.Isolation.UserAgent,
        ViewportSize = new ViewportSize
        {
            Width = config.Isolation.ViewportWidth,
            Height = config.Isolation.ViewportHeight
        }
    };

    if (!string.IsNullOrWhiteSpace(config.Login.StorageStateFilePath) && File.Exists(config.Login.StorageStateFilePath))
    {
        options.StorageStatePath = config.Login.StorageStateFilePath;
        Console.WriteLine($"[LOGIN] Using storage state: {config.Login.StorageStateFilePath}");
    }
    else
    {
        Console.WriteLine($"[LOGIN] Storage state not found, fallback to cookie bootstrap: {config.Login.StorageStateFilePath}");
    }

    return options;
}

static bool HasFlag(string[] args, string flag)
{
    return args.Any(x => string.Equals(x, flag, StringComparison.OrdinalIgnoreCase));
}

static async Task<(RedditBotConfig Config, string ConfigBaseDir)> LoadConfigAsync(string[] args)
{
    var candidates = new List<string>();

    var configArg = args.FirstOrDefault(x =>
        !string.IsNullOrWhiteSpace(x) &&
        !x.StartsWith("--", StringComparison.Ordinal));

    if (!string.IsNullOrWhiteSpace(configArg))
    {
        candidates.Add(Path.GetFullPath(configArg));
    }

    static string? FindProjectConfig(string startDir, string projectFolderName)
    {
        if (string.IsNullOrWhiteSpace(startDir) || !Directory.Exists(startDir))
        {
            return null;
        }

        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var nestedProjectConfig = Path.Combine(dir.FullName, projectFolderName, "appsettings.json");
            if (File.Exists(nestedProjectConfig))
            {
                return nestedProjectConfig;
            }

            var sameDirConfig = Path.Combine(dir.FullName, "appsettings.json");
            if (string.Equals(dir.Name, projectFolderName, StringComparison.OrdinalIgnoreCase) && File.Exists(sameDirConfig))
            {
                return sameDirConfig;
            }

            dir = dir.Parent;
        }

        return null;
    }

    var currentDir = Directory.GetCurrentDirectory();
    var baseDir = AppContext.BaseDirectory;

    var projectConfig =
        FindProjectConfig(currentDir, "SocialAuto.Reddit.Console") ??
        FindProjectConfig(baseDir, "SocialAuto.Reddit.Console");

    if (!string.IsNullOrWhiteSpace(projectConfig))
    {
        candidates.Add(projectConfig);
    }

    candidates.Add(Path.Combine(currentDir, "appsettings.json"));
    candidates.Add(Path.Combine(baseDir, "appsettings.json"));

    var configPath = candidates
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(Path.GetFullPath)
        .FirstOrDefault(File.Exists);

    if (string.IsNullOrWhiteSpace(configPath))
    {
        Console.WriteLine("[WARN] appsettings.json not found, using built-in defaults.");
        return (new RedditBotConfig(), currentDir);
    }

    Console.WriteLine($"[BOOT] Using config: {configPath}");
    var config = JsonSerializer.Deserialize<RedditBotConfig>(
                     await File.ReadAllTextAsync(configPath),
                     new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                 ) ?? new RedditBotConfig();

    return (config, Path.GetDirectoryName(configPath) ?? currentDir);
}

static void NormalizeConfigPaths(RedditBotConfig config, string configBaseDir)
{
    config.ProfileDir = ResolvePath(configBaseDir, config.ProfileDir);
    config.Login.CookieFilePath = ResolvePath(configBaseDir, config.Login.CookieFilePath);
    config.Login.ExportCookieFilePath = ResolvePath(configBaseDir, config.Login.ExportCookieFilePath);
    config.Login.StorageStateFilePath = ResolvePath(configBaseDir, config.Login.StorageStateFilePath);
    config.Login.ExportStorageStateFilePath = ResolvePath(configBaseDir, config.Login.ExportStorageStateFilePath);
    config.Evidence.Directory = ResolvePath(configBaseDir, config.Evidence.Directory);
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

static Task StartConsoleCommandLoopAsync(IBrowserContext context, IPage page, RedditBotConfig config, CancellationToken cancellationToken)
{
    return Task.Run(async () =>
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? command;
            try
            {
                command = await Task.Run(Console.ReadLine, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HOTKEY] command loop stopped: {ex.Message}");
                break;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            var normalized = command.Trim().ToLowerInvariant();
            if (normalized is "save" or "export" or "s")
            {
                try
                {
                    await PersistLoginStateForNextRunAsync(context, config);
                    Console.WriteLine("[HOTKEY] 当前登录态已导出到 profiles 目录。下次启动会优先恢复这份登录态。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HOTKEY] 登录态导出失败: {ex.Message}");
                }
            }
            else if (normalized is "import-browser" or "import")
            {
                try
                {
                    await ImportFromRunningBrowserAsync(config);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HOTKEY] 导入运行中浏览器失败: {ex.Message}");
                }
            }
            else if (normalized is "status" or "check")
            {
                try
                {
                    await ReportLoginStateAsync(page);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HOTKEY] status 检查失败: {ex.Message}");
                }
            }
            else if (normalized is "quit" or "exit" or "q")
            {
                Console.WriteLine("[HOTKEY] 收到退出命令，程序将在当前循环结束后退出。");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine($"[HOTKEY] 未识别命令: {command}. 可用命令：save | import-browser | status | quit");
            }
        }
    }, cancellationToken);
}

static async Task StopCommandLoopAsync(Task commandLoopTask)
{
    try
    {
        await commandLoopTask;
    }
    catch (OperationCanceledException)
    {
        // ignore cancellation
    }
    catch
    {
        // ignore background stop failures
    }
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

static async Task<bool> TryLikeAsync(IPage page, RedditBotConfig config, Random random, string label)
{
    await EnsurePostOpenedForLikeAsync(page, config, random);

    for (var attempt = 1; attempt <= config.MaxLikeAttempts; attempt++)
    {
        foreach (var selector in config.Selectors.LikeButtons)
        {
            try
            {
                var buttons = page.Locator(selector);
                var count = await buttons.CountAsync();
                if (count == 0) continue;

                for (var i = 0; i < count; i++)
                {
                    var button = buttons.Nth(i);
                    if (!await button.IsVisibleAsync()) continue;
                    if (await IsAlreadyUpvotedAsync(button)) continue;

                    await button.ScrollIntoViewIfNeededAsync();
                    await page.WaitForTimeoutAsync(random.Next(250, 800));
                    await button.ClickAsync(new LocatorClickOptions { Delay = random.Next(35, 150) });
                    Console.WriteLine($"[{label}] like clicked selector={selector}, index={i}, attempt={attempt}");

                    if (await WaitForLikedIndicatorAsync(page, config.Selectors.LikedStateIndicators))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{label}] selector failed: {selector}, err={ex.Message}");
            }
        }

        await page.WaitForTimeoutAsync(random.Next(600, 1200));
    }

    Console.WriteLine($"[{label}] not confirmed after attempts.");
    return false;
}

static async Task<bool> WaitForLikedIndicatorAsync(IPage page, List<string> likedStateSelectors)
{
    foreach (var selector in likedStateSelectors)
    {
        try
        {
            var loc = page.Locator(selector);
            if (await loc.CountAsync() > 0 && await loc.First.IsVisibleAsync())
            {
                return true;
            }
        }
        catch
        {
            // ignore selector-level failures
        }
    }

    return false;
}

static async Task<bool> IsAlreadyUpvotedAsync(ILocator button)
{
    try
    {
        var pressed = await button.GetAttributeAsync("aria-pressed");
        if (string.Equals(pressed, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }
    catch
    {
        // ignore
    }

    return false;
}

static async Task EnsurePostOpenedForLikeAsync(IPage page, RedditBotConfig config, Random random)
{
    if (page.Url.Contains("/comments/", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var opened = await TryRandomClickAsync(page, config.Selectors.PostEntryLinks, "OPEN_POST_FOR_LIKE", random);
    if (opened)
    {
        await page.WaitForTimeoutAsync(random.Next(900, 1800));
    }
}

static async Task TryReturnToFeedAsync(IPage page, RedditBotConfig config)
{
    if (!config.ReturnToFeedAfterAction)
    {
        return;
    }

    try
    {
        if (!page.Url.Contains("/comments/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.Commit, Timeout = 15000 });
        Console.WriteLine("[NAV] returned to feed by browser back.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NAV] return-to-feed skipped: {ex.Message}");
    }
}

static async Task EnsureFeedAsync(IPage page, RedditBotConfig config)
{
    if (page.Url.Contains("/comments/", StringComparison.OrdinalIgnoreCase))
    {
        await TryReturnToFeedAsync(page, config);
    }
    else if (string.IsNullOrWhiteSpace(page.Url) || page.Url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
    {
        await GoWithRetryAsync(page, config.BaseUrl);
    }
}

static async Task<bool> BootstrapLoginAsync(IBrowserContext context, IPage page, RedditBotConfig config)
{
    var loginOk = await GoAndCheckLoginAsync(page, config);
    if (loginOk)
    {
        await PersistLoginStateForNextRunAsync(context, config);
        return true;
    }

    var bootstrapCookies = new List<Cookie>();
    if (string.Equals(config.Login.Mode, "cookie_bootstrap", StringComparison.OrdinalIgnoreCase))
    {
        var cookiePath = ResolveCookiePathWithFallback(config);
        if (File.Exists(cookiePath))
        {
            bootstrapCookies = await LoadCookiesAsync(cookiePath);
            bootstrapCookies = FilterValidCookies(bootstrapCookies);
            if (ValidateBootstrapCookies(bootstrapCookies))
            {
                await context.ClearCookiesAsync();
                await context.AddCookiesAsync(bootstrapCookies);
                Console.WriteLine($"[LOGIN] Loaded cookies: {bootstrapCookies.Count} from {cookiePath}");
            }
            else
            {
                Console.WriteLine("[LOGIN] Cookie bootstrap skipped: required cookies missing/invalid.");
            }
        }
        else
        {
            Console.WriteLine($"[LOGIN] Cookie file not found: {cookiePath}, fallback to manual page open.");
        }

        loginOk = await StabilizeLoginAsync(context, page, config, bootstrapCookies);
        if (loginOk)
        {
            await PersistLoginStateForNextRunAsync(context, config);
            return true;
        }
    }

    Console.WriteLine("[LOGIN] 请在浏览器窗口中手工完成登录。检测到登录成功后会自动保存 Cookie 和 StorageState。");
    loginOk = await WaitForManualLoginAndAutoPersistAsync(context, page, config);
    return loginOk;
}

static async Task<bool> GoAndCheckLoginAsync(IPage page, RedditBotConfig config)
{
    var navigated = await GoWithRetryAsync(page, config.BaseUrl);
    if (!navigated)
    {
        foreach (var fallbackUrl in config.FallbackBaseUrls)
        {
            if (await GoWithRetryAsync(page, fallbackUrl, 2))
            {
                Console.WriteLine($"[NAV] fallback url succeeded: {fallbackUrl}");
                break;
            }
        }
    }

    await WaitForSafeDomAsync(page);
    return await ReportLoginStateAsync(page);
}

static async Task<bool> WaitForManualLoginAndAutoPersistAsync(IBrowserContext context, IPage page, RedditBotConfig config)
{
    var timeout = TimeSpan.FromMinutes(Math.Max(1, config.Login.ManualLoginDetectTimeoutMinutes));
    var startedAt = DateTime.UtcNow;

    while (DateTime.UtcNow - startedAt < timeout)
    {
        await WaitForSafeDomAsync(page);
        var loggedIn = await ReportLoginStateAsync(page, verbose: false);
        if (loggedIn)
        {
            await PersistLoginStateForNextRunAsync(context, config);
            Console.WriteLine("[LOGIN] 已检测到你手工登录成功，Cookie 和 StorageState 已自动保存到 profiles 目录。");
            return true;
        }

        await page.WaitForTimeoutAsync(2000);
    }

    Console.WriteLine($"[LOGIN] 在 {timeout.TotalMinutes} 分钟内未检测到登录成功。");
    return false;
}

static string ResolveCookiePathWithFallback(RedditBotConfig config)
{
    var fallbackCandidates = new[]
    {
        config.Login.CookieFilePath,
        config.Login.ExportCookieFilePath,
        Path.Combine(Directory.GetCurrentDirectory(), "SocialAuto.Reddit.Console", "default.cookies.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "SocialAuto.Reddit.Console", "profiles", "reddit", "cookies.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "default.cookies.json")
    };

    foreach (var candidate in fallbackCandidates.Where(x => !string.IsNullOrWhiteSpace(x)))
    {
        if (File.Exists(candidate))
        {
            Console.WriteLine($"[LOGIN] Cookie path fallback: {candidate}");
            return candidate;
        }
    }

    return config.Login.CookieFilePath;
}

static async Task ImportFromRunningBrowserAsync(RedditBotConfig config)
{
    using var playwright = await Playwright.CreateAsync();
    var browser = await playwright.Chromium.ConnectOverCDPAsync(config.Login.ImportBrowserCdpEndpoint);
    try
    {
        var context = browser.Contexts.FirstOrDefault();
        if (context is null)
        {
            throw new InvalidOperationException("未找到可用的浏览器上下文，请先用带调试端口的 Chrome 打开并登录 Reddit。");
        }

        await SaveStorageStateAsync(context, config.Login.ExportStorageStateFilePath);
        if (!string.Equals(config.Login.ExportStorageStateFilePath, config.Login.StorageStateFilePath, StringComparison.OrdinalIgnoreCase))
        {
            await SaveStorageStateAsync(context, config.Login.StorageStateFilePath);
        }

        await SaveCookiesAsync(context, config.Login.ExportCookieFilePath);
        if (!string.Equals(config.Login.ExportCookieFilePath, config.Login.CookieFilePath, StringComparison.OrdinalIgnoreCase))
        {
            await SaveCookiesAsync(context, config.Login.CookieFilePath);
        }
    }
    finally
    {
        await browser.DisposeAsync();
    }
}

static async Task PersistLoginStateForNextRunAsync(IBrowserContext context, RedditBotConfig config)
{
    await SaveStorageStateAsync(context, config.Login.ExportStorageStateFilePath);
    if (!string.Equals(config.Login.ExportStorageStateFilePath, config.Login.StorageStateFilePath, StringComparison.OrdinalIgnoreCase))
    {
        await SaveStorageStateAsync(context, config.Login.StorageStateFilePath);
    }

    await SaveCookiesAsync(context, config.Login.ExportCookieFilePath);
    if (!string.Equals(config.Login.ExportCookieFilePath, config.Login.CookieFilePath, StringComparison.OrdinalIgnoreCase))
    {
        await SaveCookiesAsync(context, config.Login.CookieFilePath);
    }
}

static async Task<bool> StabilizeLoginAsync(
    IBrowserContext context,
    IPage page,
    RedditBotConfig config,
    List<Cookie> bootstrapCookies)
{
    const int maxAttempts = 3;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        await WaitForSafeDomAsync(page);
        var loggedIn = await ReportLoginStateAsync(page);
        if (loggedIn)
        {
            return true;
        }

        if (bootstrapCookies.Count > 0)
        {
            await context.ClearCookiesAsync();
            await context.AddCookiesAsync(bootstrapCookies);
        }

        if (attempt < maxAttempts)
        {
            Console.WriteLine($"[LOGIN] retry login bootstrap {attempt}/{maxAttempts}");
            await page.WaitForTimeoutAsync(1000 * attempt);
            await GoWithRetryAsync(page, config.BaseUrl, 2);
        }
    }

    return false;
}

static async Task<bool> GoWithRetryAsync(IPage page, string url, int maxAttempts = 3)
{
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Commit, Timeout = 60000 });
            return true;
        }
        catch when (attempt < maxAttempts)
        {
            await page.WaitForTimeoutAsync(900 * attempt);
            Console.WriteLine($"[NAV] retry {attempt}/{maxAttempts} for {url}");
        }
    }

    Console.WriteLine($"[NAV] failed after {maxAttempts} attempts: {url}");
    return false;
}

static async Task<bool> ReportLoginStateAsync(IPage page, bool verbose = true)
{
    var url = page.Url;
    var loginIndicators = new[]
    {
        "a[href*='/login']",
        "input[name='username']",
        "input[name='password']",
        "button[type='submit']"
    };

    var hasLoginUi = false;
    foreach (var selector in loginIndicators)
    {
        if (await IsSelectorVisibleWithRetryAsync(page, selector))
        {
            hasLoginUi = true;
            break;
        }
    }

    if (url.Contains("/login", StringComparison.OrdinalIgnoreCase) || hasLoginUi)
    {
        if (verbose)
        {
            Console.WriteLine($"[LOGIN] 未检测到登录态，当前页面可能仍需登录。url={url}");
            Console.WriteLine("[LOGIN] 请检查 storageState/cookie 是否过期，或是否缺少关键 cookie（例如 reddit_session）。");
        }
        return false;
    }

    if (verbose)
    {
        Console.WriteLine($"[LOGIN] 登录态已就绪。url={url}");
    }

    return true;
}

static async Task WaitForSafeDomAsync(IPage page)
{
    try
    {
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 10000 });
    }
    catch (PlaywrightException ex)
    {
        Console.WriteLine($"[NAV] wait domcontentloaded skipped: {ex.Message}");
    }
}

static async Task<bool> IsSelectorVisibleWithRetryAsync(IPage page, string selector)
{
    for (var attempt = 1; attempt <= 2; attempt++)
    {
        try
        {
            var loc = page.Locator(selector);
            return await loc.CountAsync() > 0 && await loc.First.IsVisibleAsync();
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Execution context was destroyed", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[NAV] execution context changed while checking selector '{selector}', retry={attempt}");
            await page.WaitForTimeoutAsync(300 * attempt);
        }
    }

    return false;
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
        var name = ReadJsonAsString(item, "name");
        var value = ReadJsonAsString(item, "value");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            continue;

        var cookie = new Cookie
        {
            Name = name,
            Value = value,
            Url = ReadJsonAsString(item, "url"),
            Domain = ReadJsonAsString(item, "domain"),
            Path = ReadJsonAsString(item, "path") ?? "/",
            Expires = ReadJsonAsFloat(item, "expires"),
            HttpOnly = ReadJsonAsBool(item, "httpOnly"),
            Secure = ReadJsonAsBool(item, "secure"),
            SameSite = ParseSameSite(ReadJsonAsString(item, "sameSite"))
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

static string? ReadJsonAsString(JsonElement parent, string propertyName)
{
    if (!parent.TryGetProperty(propertyName, out var el))
    {
        return null;
    }

    return el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => null,
        _ => el.GetRawText().Trim('"')
    };
}

static float? ReadJsonAsFloat(JsonElement parent, string propertyName)
{
    if (!parent.TryGetProperty(propertyName, out var el))
    {
        return null;
    }

    if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var number))
    {
        return (float)number;
    }

    if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out var parsed))
    {
        return (float)parsed;
    }

    return null;
}

static bool ReadJsonAsBool(JsonElement parent, string propertyName)
{
    if (!parent.TryGetProperty(propertyName, out var el))
    {
        return false;
    }

    return el.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => bool.TryParse(el.GetString(), out var parsed) && parsed,
        JsonValueKind.Number => el.TryGetInt32(out var number) && number != 0,
        _ => false
    };
}

static List<Cookie> FilterValidCookies(List<Cookie> cookies)
{
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var filtered = cookies
        .Where(c => !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Value))
        .Where(c => !c.Expires.HasValue || c.Expires.Value <= 0 || c.Expires.Value > now)
        .ToList();

    var dropped = cookies.Count - filtered.Count;
    if (dropped > 0)
    {
        Console.WriteLine($"[LOGIN] Dropped invalid/expired cookies: {dropped}");
    }

    return filtered;
}

static bool ValidateBootstrapCookies(List<Cookie> cookies)
{
    if (cookies.Count == 0)
    {
        return false;
    }

    var names = cookies
        .Where(c => !string.IsNullOrWhiteSpace(c.Name))
        .Select(c => c.Name.ToLowerInvariant())
        .ToHashSet();

    if (!names.Contains("reddit_session"))
    {
        Console.WriteLine("[LOGIN] Missing required Reddit cookie: reddit_session");
        return false;
    }

    return true;
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

static async Task SaveStorageStateAsync(IBrowserContext context, string path)
{
    var dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
    await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = path });
    Console.WriteLine($"[LOGIN] Storage state exported: {path}");
}

static async Task SaveActionScreenshotAsync(IPage page, RedditBotConfig config, string action, string? keyword = null)
{
    if (!config.Evidence.Enabled)
    {
        return;
    }

    var shouldCapture = action.StartsWith("LIKE", StringComparison.OrdinalIgnoreCase)
        ? config.Evidence.CaptureOnLike
        : action.StartsWith("COMMENT", StringComparison.OrdinalIgnoreCase) && config.Evidence.CaptureOnComment;

    if (!shouldCapture)
    {
        return;
    }

    try
    {
        Directory.CreateDirectory(config.Evidence.Directory);
        var safeKeyword = string.IsNullOrWhiteSpace(keyword) ? "none" : SanitizeFileName(keyword);
        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{action}_{safeKeyword}.png";
        var filePath = Path.Combine(config.Evidence.Directory, fileName);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filePath,
            FullPage = false
        });

        Console.WriteLine($"[EVIDENCE] screenshot saved: {filePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[EVIDENCE] screenshot failed: {ex.Message}");
    }
}

static string SanitizeFileName(string value)
{
    var invalidChars = Path.GetInvalidFileNameChars();
    return string.Concat(value.Select(c => invalidChars.Contains(c) ? '_' : c));
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
                await SaveActionScreenshotAsync(page, config, "COMMENT_SUBMIT", matchedRule?.Keyword);
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
    public List<string> FallbackBaseUrls { get; set; } =
    [
        "https://www.reddit.com/r/popular/",
        "https://www.reddit.com/"
    ];
    public int RunMinutes { get; set; } = 60;
    public bool Headless { get; set; } = false;
    public string ProfileDir { get; set; } = "./profiles/reddit";
    public double LikeProbability { get; set; } = 0.08;
    public double KeywordLikeProbability { get; set; } = 0.85;
    public double CommentProbability { get; set; } = 0.01;
    public double KeywordCommentProbability { get; set; } = 0.25;
    public double OpenRandomPostProbability { get; set; } = 0.55;
    public int MaxLikeAttempts { get; set; } = 3;
    public bool ReturnToFeedAfterAction { get; set; } = true;
    public int MinWaitMs { get; set; } = 4000;
    public int MaxWaitMs { get; set; } = 9000;
    public IsolationOptions Isolation { get; set; } = new();
    public LoginOptions Login { get; set; } = new();
    public ScrollOptions Scroll { get; set; } = new();
    public EvidenceOptions Evidence { get; set; } = new();
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
    public string Mode { get; set; } = "cookie_bootstrap";
    public string CookieFilePath { get; set; } = "./profiles/reddit/cookies.json";
    public string StorageStateFilePath { get; set; } = "./profiles/reddit/storageState.json";
    public bool WaitForManualConfirm { get; set; } = false;
    public bool ExportCookiesOnExit { get; set; } = true;
    public string ExportCookieFilePath { get; set; } = "./profiles/reddit/cookies.json";
    public string ExportStorageStateFilePath { get; set; } = "./profiles/reddit/storageState.json";
    public string ImportBrowserCdpEndpoint { get; set; } = "http://127.0.0.1:9222";
    public int ManualLoginDetectTimeoutMinutes { get; set; } = 5;
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

public class EvidenceOptions
{
    public bool Enabled { get; set; } = true;
    public bool CaptureOnLike { get; set; } = true;
    public bool CaptureOnComment { get; set; } = true;
    public string Directory { get; set; } = "./artifacts/reddit";
}

public class RedditSelectors
{
    public List<string> LikeButtons { get; set; } =
    [
        "button[aria-label*='upvote' i][aria-pressed='false']",
        "button[aria-label*='up vote' i][aria-pressed='false']",
        "button[id*='upvote-button'][aria-pressed='false']",
        "button[data-testid*='upvote' i]",
        "shreddit-post button[aria-label*='upvote' i]",
        "button[aria-label*='upvote' i]",
        "button[aria-label*='like' i]"
    ];

    public List<string> LikedStateIndicators { get; set; } =
    [
        "button[aria-label*='upvoted' i]",
        "button[aria-pressed='true'][aria-label*='upvote' i]",
        "button[id*='upvote-button'][aria-pressed='true']"
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
