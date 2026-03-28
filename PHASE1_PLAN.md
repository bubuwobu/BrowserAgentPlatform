# BrowserAgentPlatform Phase 1 计划

## 总目标
第一阶段先把 5 个核心痛点从“不能用”拉到“能用”：

1. 编辑器从 JSON 壳升级成字段表单可配置版
2. 任务支持账号绑定
3. 任务支持每日时间窗随机调度
4. 前端主要表单全部补 label / help
5. 前后端接口补齐，按钮点击有真实效果

## 本阶段范围

### 后端
- 新增 `Account` 实体与 `AccountsController`
- 任务模型增加：
  - `AccountId`
  - `IsEnabled`
  - `ScheduleType`
  - `ScheduleConfigJson`
  - `NextRunAt`
  - `LastRunAt`
- 新增任务 CRUD / run-now / delete
- 新增模板 CRUD
- 新增指纹模板 update/delete
- 新增 profile delete
- 新增 `TaskScheduleBackgroundService`
- 提供 SQL 升级脚本

### 前端
- `api.js` 补齐 CRUD 接口
- 新增 `AccountsView.vue`
- `TasksView.vue` 增加：
  - 账号绑定
  - 调度设置
  - label / help
  - 编辑与删除
- `WorkflowBuilderView.vue` 改为表单型节点编辑器 V1
- `TemplatesView.vue` / `FingerprintsView.vue` / `ProfilesView.vue` 接上真实编辑删除
- 导航增加“账号管理”

## 第一阶段验收
- 可创建账号
- 可创建任务并绑定账号
- 可设置 `daily_window_random`
- 可在任务列表里看到账号/调度信息
- 常见节点不需要直接写 JSON
- 编辑按钮都能保存
