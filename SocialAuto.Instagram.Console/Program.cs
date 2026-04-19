using System.Text.Json;
using Microsoft.Playwright;

var (config, configBaseDir) = await LoadConfigAsync(args);
NormalizeConfigPaths(config, configBaseDir);

var random = new Random();
var endAt = DateTime.UtcNow.AddMinutes(config.RunMinutes);

Console.WriteLine($"[BOOT] Instagram bot start. RunMinutes={config.RunMinutes}, BaseUrl={config.BaseUrl}");
Console.WriteLine($"[BOOT] mode: keyword-first; openPostProb={config.OpenRandomPostProbability}, randomLikeProb={config.LikeProbability}, keywordLikeProb={config.KeywordLikeProbability}");

using var playwright = await Playwright.CreateAsync();
if (args.Any(a => string.Equals(a, "--import-browser", StringComparison.OrdinalIgnoreCase)))
{
    await ImportFromRunningChromeAsync(config);
    Console.WriteLine("[BOOT] 已从运行中的 Chrome 导入登录态，程序结束。");
    return;
}

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
Console.WriteLine("[HOTKEY] 控制台命令：save=立即导出当前登录态，import-browser=从已登录 Chrome 导入，status=检查登录态，quit=退出程序");
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
        var openedPost = await TryRandomClickAsync(page, config.Selectors.PostEntryButtons, "OPEN_POST", random);
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

Console.WriteLine("[DONE] Instagram bot completed.");
if (config.Login.ExportCookiesOnExit)
{
    await PersistAuthStateForNextRunAsync(context, config);
}

commandCts.Cancel();
await StopCommandLoopAsync(commandLoopTask);

static async Task<(InstagramBotConfig Config, string ConfigBaseDir)> LoadConfigAsync(string[] args)
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
        FindProjectConfig(currentDir, "SocialAuto.Instagram.Console") ??
        FindProjectConfig(baseDir, "SocialAuto.Instagram.Console");

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
        return (new InstagramBotConfig(), currentDir);
    }

    Console.WriteLine($"[BOOT] Using config: {configPath}");
    var config = JsonSerializer.Deserialize<InstagramBotConfig>(
                     await File.ReadAllTextAsync(configPath),
                     new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                 ) ?? new InstagramBotConfig();

    return (config, Path.GetDirectoryName(configPath) ?? currentDir);
}

static void NormalizeConfigPaths(InstagramBotConfig config, string configBaseDir)
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
static BrowserNewContextOptions BuildBrowserContextOptions(InstagramBotConfig config)
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


static Task StartConsoleCommandLoopAsync(IBrowserContext context, IPage page, InstagramBotConfig config, CancellationToken cancellationToken)
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
                    await PersistAuthStateForNextRunAsync(context, config);
                    Console.WriteLine("[HOTKEY] 当前登录态已导出到 profiles 目录（cookies + storageState）。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HOTKEY] cookie 导出失败: {ex.Message}");
                }
            }
            else if (normalized is "import-browser" or "attach" or "import")
            {
                try
                {
                    await ImportFromRunningChromeAsync(config);
                    Console.WriteLine("[HOTKEY] 已从运行中的 Chrome 导入登录态到 profiles 目录。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HOTKEY] 导入运行中 Chrome 失败: {ex.Message}");
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

static async Task<bool> TryLikeAsync(IPage page, InstagramBotConfig config, Random random, string label)
{
    for (var attempt = 1; attempt <= config.MaxLikeAttempts; attempt++)
    {
        await EnsurePostOpenedForLikeAsync(page, config, random);

        foreach (var selector in config.Selectors.LikeButtons)
        {
            try
            {
                var locator = page.Locator(selector);
                var count = await locator.CountAsync();
                if (count == 0) continue;

                for (var i = 0; i < count; i++)
                {
                    var target = locator.Nth(i);
                    if (!await target.IsVisibleAsync()) continue;
                    if (await IsAlreadyLikedAsync(target, config.Selectors.LikeStateInButtonSelectors))
                    {
                        continue;
                    }

                    try
                    {
                        await target.ScrollIntoViewIfNeededAsync();
                        await page.WaitForTimeoutAsync(random.Next(100, 250));
                        await target.ClickAsync(new LocatorClickOptions { Delay = random.Next(40, 120), Timeout = 5000 });
                        var confirmed = await ConfirmLikeStateAsync(page, target, config.Selectors);
                        if (confirmed)
                        {
                            Console.WriteLine($"[{label}] clicked selector={selector}, index={i}, confirmed=true");
                            return true;
                        }
                    }
                    catch
                    {
                        await target.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 5000 });
                        var confirmed = await ConfirmLikeStateAsync(page, target, config.Selectors);
                        if (confirmed)
                        {
                            Console.WriteLine($"[{label}] force-clicked selector={selector}, index={i}, confirmed=true");
                            return true;
                        }
                    }

                    Console.WriteLine($"[{label}] clicked selector={selector}, index={i}, but not confirmed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{label}] selector failed: {selector}, err={ex.Message}");
            }
        }

        if (attempt < config.MaxLikeAttempts)
        {
            Console.WriteLine($"[{label}] retry like attempt {attempt}/{config.MaxLikeAttempts}");
            await page.WaitForTimeoutAsync(500 * attempt);
        }
    }

    Console.WriteLine($"[{label}] no like target clicked.");
    return false;
}

static async Task<bool> ConfirmLikeStateAsync(IPage page, ILocator clickedButton, InstagramSelectors selectors)
{
    await page.WaitForTimeoutAsync(450);

    if (await IsAlreadyLikedAsync(clickedButton, selectors.LikeStateInButtonSelectors))
    {
        return true;
    }

    foreach (var selector in selectors.LikedStateIndicators)
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

static async Task<bool> IsAlreadyLikedAsync(ILocator button, List<string> likeStateInButtonSelectors)
{
    foreach (var stateSelector in likeStateInButtonSelectors)
    {
        try
        {
            var state = button.Locator(stateSelector);
            if (await state.CountAsync() > 0 && await state.First.IsVisibleAsync())
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

static async Task EnsurePostOpenedForLikeAsync(IPage page, InstagramBotConfig config, Random random)
{
    if (page.Url.Contains("/p/", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var opened = await TryRandomClickAsync(page, config.Selectors.PostEntryButtons, "OPEN_POST_FOR_LIKE", random);
    if (opened)
    {
        await page.WaitForTimeoutAsync(random.Next(900, 1800));
    }
}

static async Task TryReturnToFeedAsync(IPage page, InstagramBotConfig config)
{
    if (!config.ReturnToFeedAfterAction)
    {
        return;
    }

    if (!page.Url.Contains("/p/", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    try
    {
        var closeButton = page.Locator("div[role='dialog'] button[aria-label='Close'], div[role='dialog'] svg[aria-label='Close']").First;
        if (await closeButton.CountAsync() > 0 && await closeButton.IsVisibleAsync())
        {
            await closeButton.ClickAsync(new LocatorClickOptions { Timeout = 4000 });
            Console.WriteLine("[NAV] closed post dialog and returned to feed.");
            return;
        }

        await page.Keyboard.PressAsync("Escape");
        await page.WaitForTimeoutAsync(300);
        if (!page.Url.Contains("/p/", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[NAV] returned to feed by Escape.");
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

// Backward-compat shim for older branches/call sites that still reference EnsureFeedAsync.
static Task EnsureFeedAsync(IPage page, InstagramBotConfig config)
{
    return TryReturnToFeedAsync(page, config);
}

static async Task<bool> BootstrapLoginAsync(IBrowserContext context, IPage page, InstagramBotConfig config)
{
    var bootstrapPath = ResolveAuthStatePathWithFallback(config);
    var bootstrapCookies = new List<Cookie>();

    if (string.Equals(config.Login.Mode, "cookie_bootstrap", StringComparison.OrdinalIgnoreCase) && File.Exists(bootstrapPath))
    {
        bootstrapCookies = await LoadCookiesAsync(bootstrapPath);
        bootstrapCookies = FilterValidCookies(bootstrapCookies);
        if (ValidateBootstrapCookies(bootstrapCookies))
        {
            await context.ClearCookiesAsync();
            await context.AddCookiesAsync(bootstrapCookies);
            Console.WriteLine($"[LOGIN] Loaded auth cookies: {bootstrapCookies.Count} from {bootstrapPath}");
        }
        else
        {
            Console.WriteLine("[LOGIN] Bootstrap auth file exists but required cookies are missing/invalid.");
        }
    }
    else if (string.Equals(config.Login.Mode, "cookie_bootstrap", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"[LOGIN] Auth state file not found: {bootstrapPath}, fallback to manual login.");
    }

    // First try the normal target page once. If auth is valid, we should land in an already logged-in view.
    await GoWithRetryAsync(page, config.BaseUrl);
    var loginOk = await StabilizeLoginAsync(context, page, config, bootstrapCookies);
    if (loginOk)
    {
        await PersistAuthStateForNextRunAsync(context, config);
        return true;
    }

    // Important: when login is not ready, stop all business navigation and stay on the login page.
    Console.WriteLine("[LOGIN] Manual login mode: automation is paused and the page will not be refreshed.");
    await NavigateToInstagramLoginAsync(page);
    loginOk = await WaitForManualLoginAndAutoPersistAsync(context, page, config);

    if (loginOk)
    {
        await PersistAuthStateForNextRunAsync(context, config);
    }

    return loginOk;
}

static string ResolveAuthStatePathWithFallback(InstagramBotConfig config)
{
    var fallbackCandidates = new[]
    {
        config.Login.StorageStateFilePath,
        config.Login.ExportStorageStateFilePath,
        config.Login.CookieFilePath,
        config.Login.ExportCookieFilePath,
        Path.Combine(Directory.GetCurrentDirectory(), "SocialAuto.Instagram.Console", "default.cookies.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "SocialAuto.Instagram.Console", "profiles", "instagram", "cookies.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "default.cookies.json")
    };

    foreach (var candidate in fallbackCandidates)
    {
        if (File.Exists(candidate))
        {
            Console.WriteLine($"[LOGIN] Cookie path fallback: {candidate}");
            return candidate;
        }
    }

    return config.Login.CookieFilePath;
}

static async Task PersistAuthStateForNextRunAsync(IBrowserContext context, InstagramBotConfig config)
{
    await SaveStorageStateAsync(context, config.Login.ExportStorageStateFilePath);
    await SaveCookiesAsync(context, config.Login.ExportCookieFilePath);

    if (!string.Equals(config.Login.ExportStorageStateFilePath, config.Login.StorageStateFilePath, StringComparison.OrdinalIgnoreCase))
    {
        await SaveStorageStateAsync(context, config.Login.StorageStateFilePath);
    }

    if (!string.Equals(config.Login.ExportCookieFilePath, config.Login.CookieFilePath, StringComparison.OrdinalIgnoreCase))
    {
        await SaveCookiesAsync(context, config.Login.CookieFilePath);
    }
}

static async Task<bool> StabilizeLoginAsync(
    IBrowserContext context,
    IPage page,
    InstagramBotConfig config,
    List<Cookie> bootstrapCookies)
{
    const int maxAttempts = 2;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        await WaitForSafeDomAsync(page);
        var loggedIn = await ReportLoginStateAsync(page);
        if (loggedIn)
        {
            return true;
        }

        if (bootstrapCookies.Count > 0 && attempt < maxAttempts)
        {
            await context.ClearCookiesAsync();
            await context.AddCookiesAsync(bootstrapCookies);
            Console.WriteLine($"[LOGIN] retry login bootstrap {attempt}/{maxAttempts}");
            await page.WaitForTimeoutAsync(1000 * attempt);
            await GoWithRetryAsync(page, config.BaseUrl, 1);
        }
    }

    return false;
}

static async Task NavigateToInstagramLoginAsync(IPage page)
{
    if (page.Url.Contains("/accounts/login", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    try
    {
        await page.GotoAsync("https://www.instagram.com/accounts/login/", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60000
        });
    }
    catch (PlaywrightException ex)
    {
        Console.WriteLine($"[LOGIN] navigate to login page failed: {ex.Message}");
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

static async Task<bool> ReportLoginStateAsync(IPage page, bool verbose = true)
{
    var url = page.Url ?? string.Empty;
    var loginIndicators = new[]
    {
        "input[name='username']",
        "input[name='password']",
        "button[type='submit']"
    };

    var loggedInIndicators = new[]
    {
        "a[href='/direct/inbox/']",
        "a[href='/explore/']",
        "a[href='/reels/']",
        "svg[aria-label='Home']",
        "nav"
    };

    var hasLoginUi = false;
    foreach (var selector in loginIndicators)
    {
        if (await IsSelectorVisibleSafeAsync(page, selector))
        {
            hasLoginUi = true;
            break;
        }
    }

    var hasLoggedInUi = false;
    foreach (var selector in loggedInIndicators)
    {
        if (await IsSelectorVisibleSafeAsync(page, selector))
        {
            hasLoggedInUi = true;
            break;
        }
    }

    if (url.Contains("/accounts/login", StringComparison.OrdinalIgnoreCase) || hasLoginUi)
    {
        if (hasLoggedInUi)
        {
            if (verbose)
            {
                Console.WriteLine($"[LOGIN] logged-in UI detected while URL still looks transitional. url={url}");
            }
            return true;
        }

        if (verbose)
        {
            Console.WriteLine($"[LOGIN] 未检测到登录态，当前页面可能仍需登录。url={url}");
            Console.WriteLine("[LOGIN] 请检查 cookie 是否过期，或是否缺少关键 cookie（例如 sessionid、csrftoken）。");
        }
        return false;
    }

    if (hasLoggedInUi)
    {
        if (verbose)
        {
            Console.WriteLine($"[LOGIN] 登录态已就绪。url={url}");
        }
        return true;
    }

    // Fallback: check key cookies as an additional hint, without forcing navigation.
    try
    {
        var cookies = await page.Context.CookiesAsync(new[] { "https://www.instagram.com/" });
        var hasSession = cookies.Any(c => string.Equals(c.Name, "sessionid", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(c.Value));
        var hasCsrf = cookies.Any(c => string.Equals(c.Name, "csrftoken", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(c.Value));
        if (hasSession && hasCsrf)
        {
            if (verbose)
            {
                Console.WriteLine($"[LOGIN] login cookies detected. url={url}");
            }
            return true;
        }
    }
    catch
    {
    }

    if (verbose)
    {
        Console.WriteLine($"[LOGIN] 登录态无法确认，按未登录处理。url={url}");
    }
    return false;
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

static async Task<bool> IsSelectorVisibleSafeAsync(IPage page, string selector)
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
            cookie.Url = "https://www.instagram.com";
        }

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

    var required = new[] { "sessionid", "csrftoken" };
    var missing = required.Where(r => !names.Contains(r)).ToList();
    if (missing.Count > 0)
    {
        Console.WriteLine($"[LOGIN] Missing required IG cookies: {string.Join(", ", missing)}");
        return false;
    }

    return true;
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

static async Task<bool> WaitForManualLoginAndAutoPersistAsync(IBrowserContext context, IPage page, InstagramBotConfig config)
{
    await NavigateToInstagramLoginAsync(page);
    Console.WriteLine("[LOGIN] Please complete Instagram login manually in the opened browser window.");
    Console.WriteLine("[LOGIN] Automation is paused. This page will not be refreshed during manual login.");

    var deadline = DateTime.UtcNow.AddMinutes(Math.Max(1, config.Login.ManualLoginDetectTimeoutMinutes));
    while (DateTime.UtcNow < deadline)
    {
        await page.WaitForTimeoutAsync(2000);
        var loggedIn = await ReportLoginStateAsync(page, verbose: false);
        if (loggedIn)
        {
            Console.WriteLine("[LOGIN] 已检测到你手工登录成功，正在自动保存登录态...");
            await PersistAuthStateForNextRunAsync(context, config);
            return true;
        }
    }

    Console.WriteLine("[LOGIN] 在等待窗口内未检测到手工登录成功。");
    return false;
}

static async Task ImportFromRunningChromeAsync(InstagramBotConfig config)
{
    using var playwright = await Playwright.CreateAsync();
    var browser = await playwright.Chromium.ConnectOverCDPAsync(config.Login.ImportBrowserCdpEndpoint);
    var context = browser.Contexts.FirstOrDefault();
    if (context is null)
    {
        throw new InvalidOperationException("未找到可用的浏览器上下文。请先用带远程调试端口的 Chrome 打开并登录站点。");
    }

    var page = context.Pages.FirstOrDefault();
    if (page is not null && !string.IsNullOrWhiteSpace(config.BaseUrl))
    {
        await GoWithRetryAsync(page, config.BaseUrl);
    }

    await PersistAuthStateForNextRunAsync(context, config);
}
/*
static async Task ImportFromRunningChromeAsync(InstagramBotConfig config)
{
    using var playwright = await Playwright.CreateAsync();
    var browser = await playwright.Chromium.ConnectOverCDPAsync(config.Login.ImportBrowserCdpEndpoint);
    try
    {
        var context = browser.Contexts.FirstOrDefault();
        if (context is null)
        {
            throw new InvalidOperationException("未找到可用的浏览器上下文。请先用带远程调试端口的 Chrome 打开并登录站点。");
        }

        var page = context.Pages.FirstOrDefault();
        if (page is not null && !string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            await GoWithRetryAsync(page, config.BaseUrl);
        }

        await PersistAuthStateForNextRunAsync(context, config);
    }
    catch (Exception ex) { }
}*/


static async Task SaveStorageStateAsync(IBrowserContext context, string path)
{
    var dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
    await context.StorageStateAsync(new BrowserContextStorageStateOptions
    {
        Path = path
    });
    Console.WriteLine($"[LOGIN] Storage state exported: {path}");
}

static async Task SaveCookiesAsync(IBrowserContext context, string path)
{
    var cookies = await context.CookiesAsync();
    var dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { cookies }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"[LOGIN] Cookies exported: {path}");
}

static async Task SaveActionScreenshotAsync(IPage page, InstagramBotConfig config, string action, string? keyword = null)
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

static async Task TryCommentAsync(IPage page, InstagramBotConfig config, Random random, KeywordRule? matchedRule, string postText)
{
    try
    {
        if (!page.Url.Contains("/p/", StringComparison.OrdinalIgnoreCase))
        {
            await TryRandomClickAsync(page, config.Selectors.PostEntryButtons, "OPEN_POST", random);
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

static string GenerateAiStyleComment(InstagramBotConfig config, KeywordRule? matchedRule, Random random, string postText)
{
    var starters = new[] { "Love this", "Great vibe", "Nice one", "Awesome share" };
    var closers = new[] { "keep posting!", "looks super clean.", "this is inspiring.", "really enjoyed this." };

    if (matchedRule is not null && matchedRule.CommentTemplates.Count > 0)
    {
        return matchedRule.CommentTemplates[random.Next(matchedRule.CommentTemplates.Count)];
    }

    if (config.CommentTemplates.Count > 0)
    {
        var baseComment = config.CommentTemplates[random.Next(config.CommentTemplates.Count)];
        if (!string.IsNullOrWhiteSpace(postText))
        {
            var trimmed = postText.Length > 24 ? postText[..24] + "..." : postText;
            return $"{baseComment} [{trimmed}]";
        }

        return baseComment;
    }

    return $"{starters[random.Next(starters.Length)]}, {closers[random.Next(closers.Length)]}";
}

public class InstagramBotConfig
{
    public string BaseUrl { get; set; } = "https://www.instagram.com/explore/tags/chineseporcelain/";
    public int RunMinutes { get; set; } = 60;
    public bool Headless { get; set; } = false;
    public string ProfileDir { get; set; } = "./profiles/instagram";
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
    public InstagramSelectors Selectors { get; set; } = new();
    public List<KeywordRule> KeywordRules { get; set; } =
    [
        new() { Keyword = "中国瓷器", CommentTemplates = ["中国瓷器的线条和釉感都很高级。", "这件中国瓷器拍得很有氛围。"] },
        new() { Keyword = "青花瓷", CommentTemplates = ["青花瓷元素太好看了，蓝白配色很绝。", "这组青花瓷内容很有东方美感。"] },
        new() { Keyword = "景德镇", CommentTemplates = ["景德镇工艺细节很打动人。", "景德镇作品的质感一眼就能看出来。"] },
        new() { Keyword = "汝窑", CommentTemplates = ["汝窑釉色太温润了，太治愈了。", "这个汝窑风格质感真的很高级。"] },
        new() { Keyword = "官窑", CommentTemplates = ["官窑风格很经典，审美在线。", "这件官窑器型和比例都很舒服。"] }
    ];

    public List<string> CommentTemplates { get; set; } =
    [
        "Love this 🙌",
        "Great shot 🔥",
        "Really nice content, thanks for posting!"
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
    public string CookieFilePath { get; set; } = "./profiles/instagram/cookies.json";
    public bool WaitForManualConfirm { get; set; } = false;
    public int ManualLoginDetectTimeoutMinutes { get; set; } = 5;
    public bool ExportCookiesOnExit { get; set; } = true;
    public string StorageStateFilePath { get; set; } = "./profiles/instagram/storageState.json";
    public string ExportStorageStateFilePath { get; set; } = "./profiles/instagram/storageState.json";
    public string ExportCookieFilePath { get; set; } = "./profiles/instagram/cookies.json";
    public string ImportBrowserCdpEndpoint { get; set; } = "http://127.0.0.1:9222";
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

public class EvidenceOptions
{
    public bool Enabled { get; set; } = true;
    public bool CaptureOnLike { get; set; } = true;
    public bool CaptureOnComment { get; set; } = true;
    public string Directory { get; set; } = "./artifacts/instagram";
}

public class InstagramSelectors
{
    public List<string> LikeButtons { get; set; } =
    [
        "div[role='dialog'] article button:has(svg[aria-label='Like'])",
        "div[role='dialog'] section button:has(svg[aria-label='Like'])",
        "article button:has(svg[aria-label='Like'])",
        "article button:has(svg[aria-label='Like' i])",
        "section button:has(svg[aria-label='Like'])",
        "button:has(svg[aria-label='Like' i])",
        "button:has(svg[aria-label='赞'])"
    ];

    public List<string> LikedStateIndicators { get; set; } =
    [
        "div[role='dialog'] button:has(svg[aria-label='Unlike'])",
        "button:has(svg[aria-label='Unlike' i])",
        "button:has(svg[aria-label='已赞'])"
    ];

    public List<string> LikeStateInButtonSelectors { get; set; } =
    [
        "svg[aria-label='Unlike' i]",
        "svg[aria-label='已赞']"
    ];

    public List<string> PostEntryButtons { get; set; } =
    [
        "article a[href*='/p/']",
        "a[href*='/p/']"
    ];

    public List<string> PostTextSelectors { get; set; } =
    [
        "article h1",
        "article span",
        "meta[property='og:title']"
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
