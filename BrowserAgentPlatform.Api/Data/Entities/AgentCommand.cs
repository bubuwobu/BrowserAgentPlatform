namespace BrowserAgentPlatform.Api.Data.Entities;
public class AgentCommand
{
    public long Id { get; set; }
    public long AgentId { get; set; }
    public long? ProfileId { get; set; }
    public string CommandType { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
