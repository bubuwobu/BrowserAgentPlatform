namespace BrowserAgentPlatform.Api.Data.Entities;
public class AgentNode
{
    public long Id { get; set; }
    public string AgentKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string Status { get; set; } = "offline";
    public int MaxParallelRuns { get; set; } = 1;
    public int CurrentRuns { get; set; } = 0;
    public string SchedulerTags { get; set; } = "";
    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
