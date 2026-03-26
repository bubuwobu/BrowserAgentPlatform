namespace BrowserAgentPlatform.Agent.Contracts;

public class AgentRegisterRequest
{
    public string AgentKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string MachineName { get; set; } = "";
    public int MaxParallelRuns { get; set; }
    public string SchedulerTags { get; set; } = "";
}
public class AgentHeartbeatRequest
{
    public string AgentKey { get; set; } = "";
    public int CurrentRuns { get; set; }
}
public class AgentPullResponse
{
    public long? TaskRunId { get; set; }
    public long? TaskId { get; set; }
    public long? ProfileId { get; set; }
    public string? LeaseToken { get; set; }
    public string? PayloadJson { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? RetryPolicyJson { get; set; }
    public string? IsolationPolicyJson { get; set; }
}
public class AgentProgressRequest
{
    public long TaskRunId { get; set; }
    public string Status { get; set; } = "";
    public string CurrentStepId { get; set; } = "";
    public string CurrentStepLabel { get; set; } = "";
    public string CurrentUrl { get; set; } = "";
    public string Message { get; set; } = "";
    public string? PreviewBase64 { get; set; }
    public string LeaseToken { get; set; } = "";
    public DateTime? HeartbeatAt { get; set; }
    public string? MetricsJson { get; set; }
}
public class AgentCompleteRequest
{
    public long TaskRunId { get; set; }
    public string Status { get; set; } = "";
    public string ResultJson { get; set; } = "{}";
    public string? FinalPreviewBase64 { get; set; }
    public string LeaseToken { get; set; } = "";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IsolationReportJson { get; set; }
}
