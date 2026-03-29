namespace BrowserAgentPlatform.Api.Data.Entities;
public class BrowserProfileLock
{
    public long Id { get; set; }
    public long ProfileId { get; set; }
    public long? TaskId { get; set; }
    public long? TaskRunId { get; set; }
    public long? AgentId { get; set; }
    public string LeaseToken { get; set; } = "";
    public string Status { get; set; } = "reserved"; // reserved leased released
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
