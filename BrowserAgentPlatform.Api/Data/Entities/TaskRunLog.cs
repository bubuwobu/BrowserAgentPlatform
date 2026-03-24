namespace BrowserAgentPlatform.Api.Data.Entities;
public class TaskRunLog
{
    public long Id { get; set; }
    public long TaskRunId { get; set; }
    public string Level { get; set; } = "info";
    public string StepId { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
