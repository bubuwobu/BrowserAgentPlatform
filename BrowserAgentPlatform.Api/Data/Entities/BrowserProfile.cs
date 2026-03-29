namespace BrowserAgentPlatform.Api.Data.Entities;
public class BrowserProfile
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long? OwnerAgentId { get; set; }
    public long? ProxyId { get; set; }
    public long? FingerprintTemplateId { get; set; }
    public string Status { get; set; } = "idle";
    public string IsolationLevel { get; set; } = "strict";
    public string LocalProfilePath { get; set; } = "";
    public string StorageRootPath { get; set; } = "";
    public string DownloadRootPath { get; set; } = "";
    public string StartupArgsJson { get; set; } = "[]";
    public string IsolationPolicyJson { get; set; } = "{}";
    public string RuntimeMetaJson { get; set; } = "{}";
    public string WorkspaceKey { get; set; } = "";
    public string ProfileRootPath { get; set; } = "";
    public string ArtifactRootPath { get; set; } = "";
    public string TempRootPath { get; set; } = "";
    public string LifecycleState { get; set; } = "created";
    public DateTime? LastUsedAt { get; set; }
    public DateTime? LastIsolationCheckAt { get; set; }
    public DateTime? LastStartedAt { get; set; }
    public DateTime? LastStoppedAt { get; set; }
    public DateTime? LastRebuildAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
