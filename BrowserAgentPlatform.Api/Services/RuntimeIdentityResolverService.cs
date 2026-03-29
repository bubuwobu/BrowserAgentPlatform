using System.Text.Json;
using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Services;

public class RuntimeIdentityResolverService
{
    private readonly AppDbContext _db;
    private readonly WorkspacePathBuilder _workspacePathBuilder;

    public RuntimeIdentityResolverService(AppDbContext db, WorkspacePathBuilder workspacePathBuilder)
    {
        _db = db;
        _workspacePathBuilder = workspacePathBuilder;
    }

    public async Task<RuntimeIdentityDescriptor?> ResolveForTaskAsync(long taskId, long profileId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task is null) return null;
        var profile = await _db.BrowserProfiles.FindAsync(profileId);
        if (profile is null) return null;
        var account = task.AccountId.HasValue ? await _db.Accounts.FindAsync(task.AccountId.Value) : null;
        var proxy = profile.ProxyId.HasValue ? await _db.Proxies.FindAsync(profile.ProxyId.Value) : null;
        var fingerprint = profile.FingerprintTemplateId.HasValue ? await _db.FingerprintTemplates.FindAsync(profile.FingerprintTemplateId.Value) : null;

        var workspace = _workspacePathBuilder.Build(account, profile);
        var device = BuildDeviceDescriptor(fingerprint?.ConfigJson);
        var launch = BuildLaunchDescriptor(profile.StartupArgsJson);
        var proxyDescriptor = proxy is null ? null : new ProxyDescriptor(
            proxy.Protocol,
            proxy.Host,
            proxy.Port,
            proxy.Username,
            proxy.Password,
            JsonSerializer.Serialize(new
            {
                protocol = proxy.Protocol,
                host = proxy.Host,
                port = proxy.Port,
                username = proxy.Username,
                password = proxy.Password
            }));

        var lifecyclePolicyJson = string.IsNullOrWhiteSpace(profile.IsolationPolicyJson) ? "{}" : profile.IsolationPolicyJson;

        return new RuntimeIdentityDescriptor(
            task.AccountId,
            profile.Id,
            $"task:{task.Id}:profile:{profile.Id}:account:{task.AccountId?.ToString() ?? "none"}",
            workspace,
            device,
            proxyDescriptor,
            launch,
            ReadLifecycleState(profile.RuntimeMetaJson),
            lifecyclePolicyJson
        );
    }

    private static DeviceProfileDescriptor BuildDeviceDescriptor(string? fingerprintJson)
    {
        if (string.IsNullOrWhiteSpace(fingerprintJson))
            return new DeviceProfileDescriptor(null, null, null, null, null, null);

        try
        {
            using var doc = JsonDocument.Parse(fingerprintJson);
            var root = doc.RootElement;
            int? width = null; int? height = null;
            if (root.TryGetProperty("viewport", out var viewport) && viewport.ValueKind == JsonValueKind.Object)
            {
                if (viewport.TryGetProperty("width", out var vw) && vw.ValueKind == JsonValueKind.Number) width = vw.GetInt32();
                if (viewport.TryGetProperty("height", out var vh) && vh.ValueKind == JsonValueKind.Number) height = vh.GetInt32();
            }

            return new DeviceProfileDescriptor(
                root.TryGetProperty("userAgent", out var ua) ? ua.GetString() : null,
                width,
                height,
                root.TryGetProperty("locale", out var locale) ? locale.GetString() : null,
                root.TryGetProperty("timezoneId", out var tz) ? tz.GetString() : null,
                fingerprintJson
            );
        }
        catch
        {
            return new DeviceProfileDescriptor(null, null, null, null, null, fingerprintJson);
        }
    }

    private static LaunchProfileDescriptor BuildLaunchDescriptor(string? startupArgsJson)
    {
        List<string>? args = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(startupArgsJson))
            {
                args = JsonSerializer.Deserialize<List<string>>(startupArgsJson);
            }
        }
        catch { }

        return new LaunchProfileDescriptor(null, args, startupArgsJson);
    }

    private static string ReadLifecycleState(string? runtimeMetaJson)
    {
        if (string.IsNullOrWhiteSpace(runtimeMetaJson)) return "ready";
        try
        {
            using var doc = JsonDocument.Parse(runtimeMetaJson);
            if (doc.RootElement.TryGetProperty("lifecycleState", out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "ready";
            }
        }
        catch { }
        return "ready";
    }
}
