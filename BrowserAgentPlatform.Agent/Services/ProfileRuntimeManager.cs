using System.Text.Json;
using BrowserAgentPlatform.Agent.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace BrowserAgentPlatform.Agent.Services;

public class ProfileRuntimeManager
{
    private readonly AgentOptions _options;
    private IPlaywright? _playwright;
    private readonly Dictionary<long, IBrowserContext> _contexts = new();

    public ProfileRuntimeManager(IOptions<AgentOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(_options.ProfilesRoot);
    }

    private async Task EnsurePlaywrightAsync()
    {
        _playwright ??= await Playwright.CreateAsync();
    }

    public async Task<IBrowserContext> GetOrLaunchAsync(long profileId, string? startupArgsJson, string? fingerprintJson, string? proxyJson, bool headed = false)
    {
        if (_contexts.TryGetValue(profileId, out var existing)) return existing;
        await EnsurePlaywrightAsync();

        var userDataDir = Path.Combine(_options.ProfilesRoot, $"profile_{profileId}");
        Directory.CreateDirectory(userDataDir);

        var launch = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = !headed,
            Args = new List<string>()
        };

        if (!string.IsNullOrWhiteSpace(startupArgsJson))
        {
            try
            {
                var args = JsonSerializer.Deserialize<List<string>>(startupArgsJson);
                if (args is not null) launch.Args = args;
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(proxyJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(proxyJson);
                launch.Proxy = new Proxy
                {
                    Server = $"{doc.RootElement.GetProperty("protocol").GetString()}://{doc.RootElement.GetProperty("host").GetString()}:{doc.RootElement.GetProperty("port").GetInt32()}",
                    Username = doc.RootElement.TryGetProperty("username", out var un) ? un.GetString() : null,
                    Password = doc.RootElement.TryGetProperty("password", out var pw) ? pw.GetString() : null
                };
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(fingerprintJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(fingerprintJson);
                if (doc.RootElement.TryGetProperty("viewport", out var viewport))
                {
                    launch.ViewportSize = new ViewportSize
                    {
                        Width = viewport.GetProperty("width").GetInt32(),
                        Height = viewport.GetProperty("height").GetInt32()
                    };
                }
                if (doc.RootElement.TryGetProperty("userAgent", out var ua)) launch.UserAgent = ua.GetString();
                if (doc.RootElement.TryGetProperty("locale", out var locale)) launch.Locale = locale.GetString();
                if (doc.RootElement.TryGetProperty("timezoneId", out var tz)) launch.TimezoneId = tz.GetString();
            }
            catch { }
        }

        var context = await _playwright!.Chromium.LaunchPersistentContextAsync(userDataDir, launch);
        _contexts[profileId] = context;
        return context;
    }

    public async Task CloseAsync(long profileId)
    {
        if (_contexts.TryGetValue(profileId, out var context))
        {
            await context.CloseAsync();
            _contexts.Remove(profileId);
        }
    }
}
