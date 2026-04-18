namespace BrowserAgentPlatform.Api.Data.Entities;
public class TaskRun
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public long BrowserProfileId { get; set; }
    public long? AssignedAgentId { get; set; }
    public string? LeaseToken { get; set; } = "";
    public string Status { get; set; } = "queued";
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 0;
    public string CurrentStepId { get; set; } = "";
    public string CurrentStepLabel { get; set; } = "";
    public string CurrentUrl { get; set; } = "";
    public string ResultJson { get; set; } = "{}";
    public string? ErrorCode { get; set; } = "";
    public string? ErrorMessage { get; set; } = "";
    public string LastPreviewPath { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? HeartbeatAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
