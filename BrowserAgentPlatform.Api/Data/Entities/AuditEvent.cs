namespace BrowserAgentPlatform.Api.Data.Entities;

public class AuditEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = "";
    public string ActorType { get; set; } = "";
    public string ActorId { get; set; } = "";
    public string TargetType { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string DetailsJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
