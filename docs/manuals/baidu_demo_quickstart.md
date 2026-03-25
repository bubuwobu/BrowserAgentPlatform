# 百度搜索示例：一次性跑通操作说明

> 目标：只保留一套最小数据，创建并执行一个“打开百度 -> 搜索 -> 提取标题”的任务。

## 1) 执行清库+造数 SQL

在 MySQL 客户端执行：

```sql
SOURCE docs/sql/reset_to_baidu_demo.sql;
```

如果你的客户端不支持 `SOURCE`，直接复制该文件内容执行即可。

## 2) 启动服务

1. 启动 API（`BrowserAgentPlatform.Api`）。
2. 启动 Agent（`BrowserAgentPlatform.Agent`），并确保它连接的是同一个 API 地址。
3. 启动 Web（`BrowserAgentPlatform.Web`）。

## 3) 检查是否具备执行条件

在 Web 看以下三点：

1. `Dashboard` 中至少一个 Agent 为 `online`。
2. `任务中心` 能看到任务 `BAIDU SEARCH DEMO`（状态 queued）。
3. 约 5~15 秒后，`最近运行` 出现新 run，状态从 `queued -> leased/running -> completed/failed`。

## 4) 如果 run 一直 queued，按顺序排查

1. Agent 是否在线、是否持续心跳。
2. Agent 与 API 是否同库（最常见问题：连错数据库实例）。
3. API 日志是否有 pull/progress/complete 请求。

建议直接查库：

```sql
-- 看是否有 queued run
SELECT id, task_id, browser_profile_id, status, assigned_agent_id, created_at
FROM task_runs
ORDER BY id DESC
LIMIT 20;

-- 看 agent 是否在线
SELECT id, agent_key, status, current_runs, max_parallel_runs, last_heartbeat_at
FROM agents
ORDER BY id DESC
LIMIT 20;
```

## 5) 预期结果

- 在 `Live 调试` 可看到当前步骤、URL、预览图（如果 agent 能截图）；
- `resultJson` 中可看到提取结果（首条标题文本）。
