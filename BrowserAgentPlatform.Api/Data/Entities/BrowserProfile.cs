namespace BrowserAgentPlatform.Api.Data.Entities;
public class BrowserProfile
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long? OwnerAgentId { get; set; }
    public long? ProxyId { get; set; }
    public long? FingerprintTemplateId { get; set; }
    public string Status { get; set; } = "idle";
    public string LocalProfilePath { get; set; } = "";
    public string StartupArgsJson { get; set; } = "[]";
    public string RuntimeMetaJson { get; set; } = "{}";
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
