namespace BrowserAgentPlatform.Api.Models;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string DisplayName, string Role);

public record AgentRegisterRequest(string AgentKey, string Name, string MachineName, int MaxParallelRuns, string SchedulerTags);
public record AgentHeartbeatRequest(string AgentKey, int CurrentRuns);
public record AgentPullResponse(long? TaskRunId, long? TaskId, long? ProfileId, string? LeaseToken, string? PayloadJson, int? TimeoutSeconds, string? RetryPolicyJson, string? IsolationPolicyJson);
public record AgentProgressRequest(long TaskRunId, string Status, string CurrentStepId, string CurrentStepLabel, string CurrentUrl, string Message, string? PreviewBase64, string LeaseToken, DateTime? HeartbeatAt, string? MetricsJson);
public record AgentCompleteRequest(long TaskRunId, string Status, string ResultJson, string? FinalPreviewBase64, string LeaseToken, string? ErrorCode, string? ErrorMessage, string? IsolationReportJson);

public record ProxyUpsertRequest(string Name, string Protocol, string Host, int Port, string Username, string Password, string Notes);
public record FingerprintTemplateRequest(string Name, string ConfigJson);
public record BrowserProfileRequest(string Name, long? OwnerAgentId, long? ProxyId, long? FingerprintTemplateId, string LocalProfilePath, string StorageRootPath, string DownloadRootPath, string StartupArgsJson, string IsolationPolicyJson, string IsolationLevel);
public record TaskTemplateRequest(string Name, string DefinitionJson);

public record WorkflowTaskRequest(
    string? Name,
    long BrowserProfileId,
    long? AccountId,
    string? SchedulingStrategy,
    long? PreferredAgentId,
    bool IsEnabled,
    string? ScheduleType,
    string? ScheduleConfigJson,
    string? PayloadJson,
    int Priority,
    int? TimeoutSeconds,
    string? RetryPolicyJson);

public record AccountRequest(
    string Name,
    string Platform,
    string Username,
    string Status,
    long? BrowserProfileId,
    string? CredentialJson,
    string? MetadataJson);

public record ClosedLoopStartRequest(long ProfileId, string AgentKey, string? TaskName, string? PayloadJson);
public record ClosedLoopExecuteRequest(long RunId, string AgentKey);

public record TestOpenProfileRequest(long ProfileId);
public record TakeoverRequest(long ProfileId, bool Headed);
