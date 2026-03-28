# BrowserAgentPlatform 第二阶段计划

## 目标
围绕你提出的 5 个核心问题，先把“任务建模、账号绑定、调度基础、编辑器 V1、接口对齐、表单可读性”做成一套能继续迭代的基线。

## 范围

### 1. 编辑器可用化 V1
- 节点属性从直接编辑裸 JSON，改成表单驱动
- 支持常用节点：
  - open
  - click
  - type
  - wait_for_element
  - wait_for_timeout
  - scroll
  - extract_text
  - branch
  - loop
  - end_success
  - end_fail
- 保留高级 JSON 折叠区，给技术用户兜底

### 2. 任务调度基础
- 任务增加：
  - accountId
  - isEnabled
  - scheduleType
  - scheduleConfigJson
  - nextRunAt
  - lastRunAt
- 第一版支持：
  - manual
  - daily_window_random
- 新增 TaskScheduleBackgroundService，自动生成 run

### 3. 账号关联
- 新增 accounts 表和 Account 模型
- 一个账号绑定一个 BrowserProfile
- 任务可绑定 Account
- 前端新增账号管理页面

### 4. 表单可读性修复
- Tasks / Profiles / Fingerprints / Templates / Accounts / Builder 统一补 label
- 给关键字段增加帮助说明
- 减少 placeholder 代替 label 的情况

### 5. 前后端接口对齐
- Profiles：补 delete
- Fingerprints：补 update/delete
- Templates：补 update/delete
- Tasks：补 update/delete/run-now/toggle-enabled
- Accounts：完整 CRUD
- 前端 api.js 同步对齐

## 验收标准
1. 可以创建账号并绑定 BrowserProfile
2. 可以创建任务并绑定账号
3. 可以设置任务为 daily_window_random
4. 到时间能自动生成 task_runs
5. Tasks 页可以真正编辑任务
6. Builder 不需要直接写 JSON 才能做常见编排
7. 表单字段能看懂用途
