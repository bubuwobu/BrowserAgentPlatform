namespace BrowserAgentPlatform.Api.Models;

public class RuntimeIdentityDescriptor
{
    public long RuntimeIdentityId { get; set; }
    public int IdentityVersion { get; set; } = 1;

    public long? AccountId { get; set; }
    public long? ProfileId { get; set; }
    public string? IdentityKey { get; set; }

    public long? BrowserProfileId
    {
        get => ProfileId;
        set => ProfileId = value;
    }

    public WorkspaceDescriptor? Workspace { get; set; } = new();
    public DeviceProfileDescriptor? Device { get; set; } = new();
    public ProxyDescriptor? Proxy { get; set; } = new();
    public LaunchProfileDescriptor? Launch { get; set; } = new();

    public string? LifecycleState { get; set; } = "ready";
    public string? LifecyclePolicyJson { get; set; } = "{}";

    public RuntimeIdentityDescriptor() { }

    public RuntimeIdentityDescriptor(
        long? accountId,
        long? profileId,
        string? identityKey,
        WorkspaceDescriptor? workspace,
        DeviceProfileDescriptor? device,
        ProxyDescriptor? proxy,
        LaunchProfileDescriptor? launch,
        string? lifecycleState,
        string? lifecyclePolicyJson)
    {
        AccountId = accountId;
        ProfileId = profileId;
        IdentityKey = identityKey;
        Workspace = workspace ?? new WorkspaceDescriptor();
        Device = device ?? new DeviceProfileDescriptor();
        Proxy = proxy ?? new ProxyDescriptor();
        Launch = launch ?? new LaunchProfileDescriptor();
        LifecycleState = string.IsNullOrWhiteSpace(lifecycleState) ? "ready" : lifecycleState;
        LifecyclePolicyJson = string.IsNullOrWhiteSpace(lifecyclePolicyJson) ? "{}" : lifecyclePolicyJson;
    }
}

public class WorkspaceDescriptor
{
    public string WorkspaceKey { get; set; } = "";
    public string WorkspaceRootPath { get; set; } = "";
    public string ProfileRootPath { get; set; } = "";
    public string StorageRootPath { get; set; } = "";
    public string DownloadRootPath { get; set; } = "";
    public string ArtifactRootPath { get; set; } = "";
    public string LogRootPath { get; set; } = "";
    public string TempRootPath { get; set; } = "";
    public string StateFilePath { get; set; } = "";

    public string WorkspaceRoot
    {
        get => WorkspaceRootPath;
        set => WorkspaceRootPath = value ?? "";
    }

    public WorkspaceDescriptor() { }

    public WorkspaceDescriptor(
        string workspaceKey,
        string workspaceRootPath,
        string profileRootPath,
        string storageRootPath,
        string downloadRootPath,
        string artifactRootPath,
        string logRootPath,
        string tempRootPath,
        string stateFilePath)
    {
        WorkspaceKey = workspaceKey ?? "";
        WorkspaceRootPath = workspaceRootPath ?? "";
        ProfileRootPath = profileRootPath ?? "";
        StorageRootPath = storageRootPath ?? "";
        DownloadRootPath = downloadRootPath ?? "";
        ArtifactRootPath = artifactRootPath ?? "";
        LogRootPath = logRootPath ?? "";
        TempRootPath = tempRootPath ?? "";
        StateFilePath = stateFilePath ?? "";
    }
}

public class DeviceProfileDescriptor
{
    public string? UserAgent { get; set; }
    public int? ViewportWidth { get; set; }
    public int? ViewportHeight { get; set; }
    public string? Locale { get; set; }
    public string? TimezoneId { get; set; }
    public string? RawFingerprintJson { get; set; }

    public string? FingerprintJson
    {
        get => RawFingerprintJson;
        set => RawFingerprintJson = value;
    }

    public DeviceProfileDescriptor() { }

    public DeviceProfileDescriptor(
        string? userAgent,
        int? viewportWidth,
        int? viewportHeight,
        string? locale,
        string? timezoneId,
        string? rawFingerprintJson)
    {
        UserAgent = userAgent;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Locale = locale;
        TimezoneId = timezoneId;
        RawFingerprintJson = rawFingerprintJson;
    }
}

public class ProxyDescriptor
{
    public long? ProxyId { get; set; }
    public string? Protocol { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? RawProxyJson { get; set; }
    public string? Region { get; set; }
    public string? TimezoneId { get; set; }
    public string? Locale { get; set; }

    public ProxyDescriptor() { }

    public ProxyDescriptor(
        string? protocol,
        string? host,
        int? port,
        string? username,
        string? password,
        string? rawProxyJson)
    {
        Protocol = protocol;
        Host = host;
        Port = port;
        Username = username;
        Password = password;
        RawProxyJson = rawProxyJson;
    }
}

public class ProxyBindingDescriptor : ProxyDescriptor
{
    public ProxyBindingDescriptor() { }

    public ProxyBindingDescriptor(
        string? protocol,
        string? host,
        int? port,
        string? username,
        string? password,
        string? rawProxyJson)
        : base(protocol, host, port, username, password, rawProxyJson)
    {
    }
}

public class LaunchProfileDescriptor
{
    public bool? Headless { get; set; }
    public List<string>? Args { get; set; }
    public string? RawStartupArgsJson { get; set; }

    public string Channel { get; set; } = "chromium";
    public bool AcceptDownloads { get; set; } = true;
    public bool JavaScriptEnabled { get; set; } = true;
    public bool IgnoreHttpsErrors { get; set; }
    public string? PermissionsJson { get; set; }
    public string? ExtraArgsJson
    {
        get => RawStartupArgsJson;
        set => RawStartupArgsJson = value;
    }
    public string? ContextOptionsJson { get; set; }

    public LaunchProfileDescriptor() { }

    public LaunchProfileDescriptor(bool? headless, List<string>? args, string? rawStartupArgsJson)
    {
        Headless = headless;
        Args = args;
        RawStartupArgsJson = rawStartupArgsJson;
    }

    public LaunchProfileDescriptor(string channel, bool headless, string? extraArgsJson = null)
    {
        Channel = string.IsNullOrWhiteSpace(channel) ? "chromium" : channel;
        Headless = headless;
        RawStartupArgsJson = extraArgsJson;
    }
}
