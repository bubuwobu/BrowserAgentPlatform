namespace BrowserAgentPlatform.Api.Data.Entities;
public class WorkflowTask
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long BrowserProfileId { get; set; }
    public string SchedulingStrategy { get; set; } = "profile_owner"; // profile_owner preferred_agent least_loaded
    public long? PreferredAgentId { get; set; }
    public string Status { get; set; } = "queued"; // queued leased running completed failed cancelled
    public string PayloadJson { get; set; } = "{}";
    public string RetryPolicyJson { get; set; } = "{}";
    public int Priority { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 900;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
