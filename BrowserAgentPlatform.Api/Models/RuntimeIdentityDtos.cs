namespace BrowserAgentPlatform.Api.Models;

public class RuntimeIdentityDto
{
    public long RuntimeIdentityId { get; set; }
    public long AccountId { get; set; }
    public long BrowserProfileId { get; set; }
    public int IdentityVersion { get; set; } = 1;
    public RuntimeWorkspaceDto Workspace { get; set; } = new();
    public RuntimeDeviceProfileDto Device { get; set; } = new();
    public RuntimeProxyBindingDto Proxy { get; set; } = new();
    public RuntimeLaunchProfileDto Launch { get; set; } = new();
}

public class RuntimeWorkspaceDto
{
    public string WorkspaceKey { get; set; } = "";
    public string WorkspaceRoot { get; set; } = "";
    public string ProfileRootPath { get; set; } = "";
    public string StorageRootPath { get; set; } = "";
    public string DownloadRootPath { get; set; } = "";
    public string ArtifactRootPath { get; set; } = "";
    public string TempRootPath { get; set; } = "";
}

public class RuntimeDeviceProfileDto
{
    public string BrowserFamily { get; set; } = "chrome";
    public string Platform { get; set; } = "windows";
    public string UserAgent { get; set; } = "";
    public int ViewportWidth { get; set; } = 1366;
    public int ViewportHeight { get; set; } = 768;
    public string Locale { get; set; } = "en-US";
    public string TimezoneId { get; set; } = "UTC";
    public string ColorScheme { get; set; } = "light";
    public string PlatformName { get; set; } = "Win32";
    public string GeolocationJson { get; set; } = "{}";
    public string FingerprintSnapshotJson { get; set; } = "{}";
}

public class RuntimeProxyBindingDto
{
    public bool Enabled { get; set; }
    public long? ProxyId { get; set; }
    public string Name { get; set; } = "";
    public string Protocol { get; set; } = "http";
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Region { get; set; } = "";
    public string TimezoneId { get; set; } = "";
    public string Locale { get; set; } = "";
}

public class RuntimeLaunchProfileDto
{
    public string Channel { get; set; } = "chromium";
    public bool Headless { get; set; }
    public bool AcceptDownloads { get; set; } = true;
    public bool JavaScriptEnabled { get; set; } = true;
    public bool IgnoreHttpsErrors { get; set; }
    public string PermissionsJson { get; set; } = "[]";
    public string ExtraArgsJson { get; set; } = "[]";
    public string ContextOptionsJson { get; set; } = "{}";
}
