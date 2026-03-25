namespace BrowserAgentPlatform.Api.Data.Entities;

public class RunIsolationReport
{
    public long Id { get; set; }
    public long TaskRunId { get; set; }
    public long BrowserProfileId { get; set; }
    public string ProxySnapshotJson { get; set; } = "{}";
    public string FingerprintSnapshotJson { get; set; } = "{}";
    public string StorageCheckJson { get; set; } = "{}";
    public string NetworkCheckJson { get; set; } = "{}";
    public string Result { get; set; } = "pass";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
