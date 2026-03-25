# BrowserAgentPlatform 界面验收操作手册（Web）

> 目标：让不熟悉系统的同学也能在 15~30 分钟内完成“自动化 + 隔离”闭环验收。

## 1. 准备阶段

## 1.1 启动后端 API
- 确保可访问 `http://localhost:12126/`。
- 首次启动会自动建库并写入默认管理员账号。

## 1.2 启动前端 Web
- 在 `BrowserAgentPlatform.Web` 执行：
  - `npm run dev`
- 默认打开地址（通常）：`http://localhost:5173`。

## 1.3 登录账号
- 默认账号：
  - 用户名：`admin`
  - 密码：`Admin@123456`

---

## 2. 验收路径总览（建议顺序）

1. 登录
2. 配置/创建 Profile（含隔离字段）
3. 执行 Profile 隔离检查
4. 用 Postman 触发闭环 start/execute
5. 在任务与 Live 页面查看结果
6. 看 Dashboard / Observability / Audit 是否有数据

---

## 3. 详细操作步骤

## Step A：登录
1. 打开 `/login` 页面。
2. 输入 `admin / Admin@123456`，点击“登录”。
3. 成功后进入 Dashboard。

**预期结果**
- 页面跳转到 `/`。
- 左侧能看到菜单：Dashboard / Profiles / 任务中心 / Live。

---

## Step B：创建带隔离配置的 Profile
1. 打开 **Profiles** 页面。
2. 在“新建 Profile”区域填写：
   - `name`: `acceptance-profile-ui`
   - `localProfilePath`: `/tmp/bap/profile-ui`
   - `storageRootPath`: `/tmp/bap/storage-ui`
   - `downloadRootPath`: `/tmp/bap/download-ui`
   - `isolationLevel`: `strict`
   - `startupArgsJson`: `[]`
   - `isolationPolicyJson`: `{"timezone":"Asia/Shanghai","locale":"zh-CN"}`
3. 点击“保存”。

**预期结果**
- 页面提示 Profile 创建成功。
- Profile 列表出现新记录，能看到 isolation/storage 字段。

---

## Step C：执行隔离检查
1. 在刚创建的 Profile 卡片点击“隔离检查”。

**预期结果**
- 页面提示“隔离检查通过”或“失败原因”。
- 通过时可看到 `lastIsolationCheckAt` 刷新。

> 如果失败，优先检查：
> - `isolationPolicyJson` 是否合法 JSON
> - `startupArgsJson` 是否合法 JSON 数组

---

## Step D：触发闭环验证（Postman）
> 当前 Web 还没有独立 Validation 页面，推荐用 Postman Collection 执行。

1. 导入：
   - Collection: `docs/postman/BrowserAgentPlatform_Acceptance.postman_collection.json`
   - Environment: `docs/postman/BrowserAgentPlatform_Acceptance.postman_environment.json`
2. 选择环境后，按顺序执行：
   - `1. Login`
   - `3. Create Profile (Isolation Fields)`（可选，如果你已在 UI 创建）
   - `4. Profile Isolation Check`
   - `5. Closed Loop Start`
   - `6. Closed Loop Execute`
   - `7. Run Detail`
   - `8. Run Isolation Report`
   - `10. Observability Overview`
   - `11. Audit Events`

**预期结果**
- `5` 返回 `taskId/runId`。
- `6` 返回 `ok: true`（或明确失败原因）。
- `7` 中 run 状态为 `completed` 或 `failed`（有错误信息）。
- `8` 中至少有一条 isolation report 数据。
- `11` 中能看到闭环相关 audit 事件。

---

## Step E：Web 页面核对结果

## 任务中心
1. 打开“任务中心”。
2. 在“最近运行”找到 runId。
3. 确认状态与 Postman 一致。

## Live 调试
1. 点击 run 的“查看 Live”。
2. 看运行状态、日志、预览图（如果有）。

## Dashboard
1. 返回 Dashboard。
2. 看 queued/running/completed 等统计是否符合本次执行。

---

## 4. 常见问题与处理

## Q1：登录失败
- 检查 API 是否启动。
- 检查数据库连通。
- 首次启动是否执行 seed。

## Q2：隔离检查失败
- `isolationPolicyJson` 不是合法 JSON。
- `startupArgsJson` 不是 JSON 数组。
- profile 引用了不存在的 proxy / fingerprint。

## Q3：闭环 execute 失败
- `runId` 不匹配当前可租赁任务。
- 先重新执行 `Closed Loop Start` 再执行 `Execute`。

## Q4：看不到审计数据
- 确认执行过 start/execute。
- 调整 `Audit Events` 的 `take` 参数（如 500）。

---

## 5. 验收记录模板（建议）

- 验收时间：
- 版本/分支：
- 用例 A（隔离检查通过）：通过/失败（原因）
- 用例 B（闭环 start/execute）：通过/失败（原因）
- 用例 C（Run Isolation Report 存在）：通过/失败
- 用例 D（Audit Events 有数据）：通过/失败
- 结论：可进入下一阶段 / 需修复项列表

