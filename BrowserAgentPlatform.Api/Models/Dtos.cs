namespace BrowserAgentPlatform.Api.Models;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string DisplayName, string Role);

public record AgentRegisterRequest(string AgentKey, string Name, string MachineName, int MaxParallelRuns, string SchedulerTags);
public record AgentHeartbeatRequest(string AgentKey, int CurrentRuns);
public record AgentPullResponse(long? TaskRunId, long? TaskId, long? ProfileId, string? LeaseToken, string? PayloadJson);
public record AgentProgressRequest(long TaskRunId, string Status, string CurrentStepId, string CurrentStepLabel, string CurrentUrl, string Message, string? PreviewBase64);
public record AgentCompleteRequest(long TaskRunId, string Status, string ResultJson, string? FinalPreviewBase64);

public record ProxyUpsertRequest(string Name, string Protocol, string Host, int Port, string Username, string Password, string Notes);
public record FingerprintTemplateRequest(string Name, string ConfigJson);
public record BrowserProfileRequest(string Name, long? OwnerAgentId, long? ProxyId, long? FingerprintTemplateId, string LocalProfilePath, string StartupArgsJson);
public record TaskTemplateRequest(string Name, string DefinitionJson);
public record WorkflowTaskRequest(string Name, long BrowserProfileId, string SchedulingStrategy, long? PreferredAgentId, string PayloadJson, int Priority);

public record TestOpenProfileRequest(long ProfileId);
public record TakeoverRequest(long ProfileId, bool Headed);
