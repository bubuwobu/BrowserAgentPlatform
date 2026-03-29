namespace BrowserAgentPlatform.Api.Data.Entities;

public class WorkflowTask
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long BrowserProfileId { get; set; }
    public long? AccountId { get; set; }
    public string SchedulingStrategy { get; set; } = "profile_owner";
    public long? PreferredAgentId { get; set; }
    public string Status { get; set; } = "queued";
    public bool IsEnabled { get; set; } = true;
    public string ScheduleType { get; set; } = "manual";
    public string ScheduleConfigJson { get; set; } = "{}";
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string RetryPolicyJson { get; set; } = "{}";
    public int Priority { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 900;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
