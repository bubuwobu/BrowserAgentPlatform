# BrowserAgentPlatform 后端 Patch 清单（v1）

> 目标：基于当前 C# API 项目，最小改动实现“租约可靠 + 调度可控 + 隔离可验证”。
> 
> 范围：仅列出实体、DTO、Controller、Service、DbContext 的具体改动点与建议代码片段。

---

## 0. 变更总览（文件级）

### 需要改动的现有文件
1. `BrowserAgentPlatform.Api/Data/Entities/TaskRun.cs`
2. `BrowserAgentPlatform.Api/Data/Entities/BrowserProfile.cs`
3. `BrowserAgentPlatform.Api/Models/Dtos.cs`
4. `BrowserAgentPlatform.Api/Services/SchedulerService.cs`
5. `BrowserAgentPlatform.Api/Controllers/AgentsController.cs`
6. `BrowserAgentPlatform.Api/Services/QueueScanBackgroundService.cs`
7. `BrowserAgentPlatform.Api/Data/AppDbContext.cs`

### 建议新增文件
1. `BrowserAgentPlatform.Api/Data/Entities/RunIsolationReport.cs`
2. `BrowserAgentPlatform.Api/Services/RunWatchdogBackgroundService.cs`

---

## 1. Entity 层 Patch 清单

## 1.1 `TaskRun.cs`

### 新增字段
- `LeaseToken` (`string`, default `""`)
- `RetryCount` (`int`, default `0`)
- `MaxRetries` (`int`, default `0`)
- `ErrorCode` (`string`, default `""`)
- `ErrorMessage` (`string`, default `""`)
- `HeartbeatAt` (`DateTime?`)

### 建议状态集合
- `queued`
- `leased`
- `running`
- `completed`
- `failed`
- `timeout`
- `dead`
- `cancelled`

### 说明
- `LeaseToken` 必须与 lock 表中的 token 对齐，progress/complete 时都要校验。
- `HeartbeatAt` 用于 watchdog 识别卡死 run。

---

## 1.2 `BrowserProfile.cs`

### 新增字段
- `IsolationLevel` (`string`, default `"strict"`)
- `StorageRootPath` (`string`, default `""`)
- `DownloadRootPath` (`string`, default `""`)
- `IsolationPolicyJson` (`string`, default `"{}"`)
- `LastIsolationCheckAt` (`DateTime?`)

### 说明
- `LocalProfilePath` 保留；`StorageRootPath`/`DownloadRootPath` 用于强制目录隔离。
- `IsolationPolicyJson` 存放网络/DNS/语言/时区/权限策略。

---

## 1.3 新增 `RunIsolationReport.cs`

```csharp
namespace BrowserAgentPlatform.Api.Data.Entities;

public class RunIsolationReport
{
    public long Id { get; set; }
    public long TaskRunId { get; set; }
    public long BrowserProfileId { get; set; }
    public string ProxySnapshotJson { get; set; } = "{}";
    public string FingerprintSnapshotJson { get; set; } = "{}";
    public string StorageCheckJson { get; set; } = "{}";
    public string NetworkCheckJson { get; set; } = "{}";
    public string Result { get; set; } = "pass";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

## 2. DTO Patch 清单（`Models/Dtos.cs`）

## 2.1 变更 `AgentPullResponse`

### 现状
```csharp
public record AgentPullResponse(long? TaskRunId, long? TaskId, long? ProfileId, string? LeaseToken, string? PayloadJson);
```

### 建议
```csharp
public record AgentPullResponse(
    long? TaskRunId,
    long? TaskId,
    long? ProfileId,
    string? LeaseToken,
    string? PayloadJson,
    int? TimeoutSeconds,
    string? RetryPolicyJson,
    string? IsolationPolicyJson
);
```

## 2.2 变更 `AgentProgressRequest`

### 建议新增字段
- `string LeaseToken`
- `DateTime? HeartbeatAt`
- `string? MetricsJson`

```csharp
public record AgentProgressRequest(
    long TaskRunId,
    string Status,
    string CurrentStepId,
    string CurrentStepLabel,
    string CurrentUrl,
    string Message,
    string? PreviewBase64,
    string LeaseToken,
    DateTime? HeartbeatAt,
    string? MetricsJson
);
```

## 2.3 变更 `AgentCompleteRequest`

### 建议新增字段
- `string LeaseToken`
- `string? ErrorCode`
- `string? ErrorMessage`
- `string? IsolationReportJson`

```csharp
public record AgentCompleteRequest(
    long TaskRunId,
    string Status,
    string ResultJson,
    string? FinalPreviewBase64,
    string LeaseToken,
    string? ErrorCode,
    string? ErrorMessage,
    string? IsolationReportJson
);
```

## 2.4 变更 `WorkflowTaskRequest`

### 建议新增字段
- `int TimeoutSeconds`
- `string RetryPolicyJson`

```csharp
public record WorkflowTaskRequest(
    string Name,
    long BrowserProfileId,
    string SchedulingStrategy,
    long? PreferredAgentId,
    string PayloadJson,
    int Priority,
    int TimeoutSeconds,
    string RetryPolicyJson
);
```

---

## 3. Service 层 Patch 清单

## 3.1 `SchedulerService.cs`

### 必做改动 A：租约写入 `TaskRun.LeaseToken`
在 `LeaseNextRunForAgentAsync` 中生成 `leaseToken` 后，除写入 `BrowserProfileLock.LeaseToken` 外，
同时写入 `run.LeaseToken = leaseToken`。

### 必做改动 B：`RefreshLeaseAsync` 支持 token 校验
当前逻辑是对的，但调用方要传真实 token。
建议增加返回值：`Task<bool>`，便于 controller 判断续约是否成功。

### 必做改动 C：引入事务/并发保护
为“选 queued run + 检查 lock + 设置 leased”包裹事务。
避免多 agent 抢占同一 run。

### 必做改动 D：优先级排序
当前按 run id 排序，建议 join tasks 后按
1) `task.Priority desc`
2) `run.Id asc`

### 必做改动 E：释放状态一致性
`ReleaseRunAsync` 里根据 run 最终状态设置 profile 状态：
- completed/failed/timeout/dead/cancelled => `idle`
- running/leased => `busy`

---

## 3.2 新增 `RunWatchdogBackgroundService.cs`

### 职责
- 定时扫描 `leased/running` 的 run：
  - lease 过期
  - heartbeat 超时
  - 超过 timeoutSeconds
- 将 run 标为 `timeout`，写日志，调用 `ReleaseRunAsync`
- 若可重试：重置为 `queued` 并 `RetryCount + 1`
- 否则标记 `dead`

### 建议扫描频率
- 每 10 秒

---

## 4. Controller 层 Patch 清单

## 4.1 `AgentsController.cs`

### `Pull`
- 返回 `TimeoutSeconds`、`RetryPolicyJson`、`IsolationPolicyJson`。
- 空结果时返回新结构空值。

### `ReportProgress`
- 校验 `LeaseToken` 非空。
- 校验 `request.LeaseToken == run.LeaseToken`，不一致返回 `409`。
- `run.HeartbeatAt = request.HeartbeatAt ?? DateTime.UtcNow`。
- 调用 `_scheduler.RefreshLeaseAsync(run.Id, request.LeaseToken)`。
- 若续租失败，返回 `409 lease expired`。

### `ReportComplete`
- 同样校验 lease token。
- 写入 `ErrorCode/ErrorMessage`。
- 若 `IsolationReportJson` 不为空，入库 `RunIsolationReports`。
- 最后调用 `_scheduler.ReleaseRunAsync(run.Id)`。

### 新增接口建议
- `POST /api/runs/{runId}/cancel`
- `GET /api/runs/{runId}/isolation-report`

---

## 4.2 （可选）`TasksController.cs`

### `Create`
- 支持 `TimeoutSeconds`、`RetryPolicyJson`。
- 入参校验：`Priority`、`TimeoutSeconds` 合法范围。

---

## 5. Background Service Patch 清单

## 5.1 `QueueScanBackgroundService.cs`

### 改动点
当前从 `tasks` 扫描并创建 `task_runs` 时仅写 `Status=queued`。
建议同步写入：
- `MaxRetries`（从任务策略计算）
- `RetryCount=0`

### 说明
这样 watchdog 可以无状态地处理重试。

---

## 6. DbContext Patch 清单（`AppDbContext.cs`）

## 6.1 新增 DbSet
```csharp
public DbSet<RunIsolationReport> RunIsolationReports => Set<RunIsolationReport>();
```

## 6.2 TaskRun 映射新增列
在 `modelBuilder.Entity<TaskRun>` 增加：
- `lease_token`
- `retry_count`
- `max_retries`
- `error_code`
- `error_message`
- `heartbeat_at`

并增加索引：
- `(status, assigned_agent_id)` 保留
- `(status, created_at)`
- `(lease_token)`

## 6.3 BrowserProfile 映射新增列
- `isolation_level`
- `storage_root_path`
- `download_root_path`
- `isolation_policy_json`
- `last_isolation_check_at`

## 6.4 新增 RunIsolationReport 映射
表名：`run_isolation_reports`
列见实体定义，索引：`task_run_id`。

---

## 7. 落地顺序（建议）

1. 先改 `TaskRun + Dtos + AgentsController(progress/complete)` 修复 lease token。
2. 再改 `SchedulerService`（事务 + priority + release 一致性）。
3. 再改 `BrowserProfile` 隔离字段与 DbContext。
4. 新增 `RunIsolationReport` 与写入逻辑。
5. 最后新增 watchdog 背景服务。

---

## 8. 联调验收清单

1. 一个 run 被下发后，`run.lease_token` 与 lock token 一致。
2. progress 使用错误 token 时返回 409。
3. 心跳断开后 run 会被 watchdog 标记 timeout 并释放 profile。
4. 重试次数超过上限后 run 变 dead，不再重新入队。
5. 每次 complete 都能看到 isolation report（如果 agent 提交了）。

---

## 9. 风险提醒

1. `EnsureCreated` 在生产不利于可控迁移，后续建议转 migration 流程。
2. 匿名 agent 接口建议至少增加签名或 mTLS。
3. `PayloadJson/PolicyJson` 需要加 schema 校验，避免脏配置。

