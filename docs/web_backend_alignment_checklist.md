# BrowserAgentPlatform.Web 与后端能力对齐清单（闭环验证优先）

> 目标：先把“自动化 + 隔离”验证闭环跑通，再逐步补全可观测与风控界面。

## 1. 当前 Web 已覆盖的后端能力

1. 登录、Dashboard summary、Agents 列表已打通。
2. Profiles 基础增删（创建 + 列表）与 test-open / takeover / unlock 已打通。
3. Tasks 创建、runs 列表、run 详情、Live 调试已打通。
4. Config（proxies/fingerprints）与 templates 基本可用。

## 2. 与后端新增能力不一致（必须优先对齐）

## 2.1 API SDK (`src/services/api.js`) 未暴露的新接口

需要新增以下方法：

- `profileIsolationCheck(id)` => `POST /api/profiles/{id}/isolation-check`
- `runIsolationReport(runId)` => `GET /api/tasks/runs/{runId}/isolation-report`
- `observabilityOverview()` => `GET /api/observability/overview`
- `auditEvents(take)` => `GET /api/observability/audit-events?take=...`
- `closedLoopStart(body)` => `POST /api/validation/closed-loop/start`
- `closedLoopExecute(body)` => `POST /api/validation/closed-loop/execute`

## 2.2 Profiles 页面字段未覆盖后端新增字段

后端 profile 入参已有：
- `storageRootPath`
- `downloadRootPath`
- `isolationPolicyJson`
- `isolationLevel`

Web 目前创建表单只提交：
- `name`
- `ownerAgentId`
- `proxyId`
- `fingerprintTemplateId`
- `localProfilePath`
- `startupArgsJson`

=> 需要补 UI 输入项并传给 `createProfile`。

## 2.3 Tasks 创建表单未覆盖调度增强字段

后端 task 入参已有：
- `timeoutSeconds`
- `retryPolicyJson`

Web 目前创建任务未提交上述字段。

=> 需要新增输入项并在 `createTask` body 中提交。

## 2.4 闭环验证功能尚无 UI

后端已提供闭环验证接口（start/execute），但 Web 尚无页面入口。

=> 建议新增 `ValidationView.vue`，支持：
1. 选择 profile + agentKey，点击“创建闭环验证任务”
2. 显示 runId/taskId + isolation 检查结果
3. 点击“执行闭环”
4. 实时展示 run status、logs、isolation report

## 2.5 可观测与审计无可视化

后端已有 observability & audit API，Web 仍使用 `/api/live/summary` 主视图。

=> Dashboard 增加“系统健康卡片”：
- queued / leased / running
- successRate24h / avgDurationSeconds24h
- onlineAgents

=> 新增 Audit 视图：最近 200 条审计事件过滤展示。

## 3. 闭环验证最小可用方案（建议先做）

按优先级：

1. `api.js` 增加 6 个新接口（见 2.1）
2. `ProfilesView.vue` 增加隔离字段 + “执行隔离检查”按钮
3. `TasksView.vue` 增加 timeout/retry 字段
4. 新增 `ValidationView.vue`：打通 closed-loop start/execute
5. `LiveView.vue` 增加 isolation report 区块

只完成以上 5 项，就可以在前端完整验证：

- 创建带隔离策略的 Profile
- 做隔离检查
- 创建闭环任务
- 执行闭环
- 查看 run 完成与 isolation report

## 4. 建议路由与导航变更

- `main.js` 新增 `/validation` 路由
- `App.vue` 侧边栏新增“闭环验证”入口
- （可选）新增 `/audit` 路由

## 5. 建议验收标准（前端视角）

1. 用户在 UI 上 3 分钟内完成一次闭环验证。
2. 验证后可在 UI 看到：
   - run 状态 completed/failed
   - 至少 2 条 TaskRunLog
   - isolation report 有数据
3. 若 profile 隔离配置无效，UI 显示明确错误并阻止执行。

