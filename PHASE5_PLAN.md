# Phase 5：执行中心 + Builder 易用性增强

## 本阶段目标
把系统从“能跑”推进到“好用、可调试、按钮有反馈”。

## 你当前最痛的点
1. Builder 难用
2. 很多按钮点击后没有明确反馈
3. 运行过程不够直观
4. 失败时定位困难
5. 模板/任务/节点之间复用效率低

## 第一批实施清单

### A. Builder 产品化 V1.5
- 左侧节点库
- 中间画布
- 右侧属性表单
- 底部节点列表
- 顶部工具栏：
  - 新建
  - 校验
  - 导入
  - 导出
  - 保存任务
  - 保存模板
- 节点支持：
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
- 每个按钮都有明确提示
- 新增：
  - 删除节点
  - 删除连线
  - 自动补全示例数据
  - 画布一键清空

### B. 运行中心可视化
- Live 页面增强：
  - 运行摘要卡片
  - 步骤日志时间线
  - 运行结果 JSON 展开
  - 产物区
  - 快速操作区：重跑 / 接管 / 测试打开
- 失败时更容易看出：
  - errorCode
  - errorMessage
  - 当前 step
  - 当前 URL

### C. 交互反馈补齐
- 前端统一消息提示
- 删除确认
- 空态提示
- 保存成功/失败提示
- 导入失败提示
- 校验失败提示

### D. Builder 校验
- 节点必须有 id
- 必填字段不能为空
- 连线起点/终点必须存在
- 至少有一个结束节点
- open 节点必须有 url
- click/type/wait_for_element/extract_text 必须有 selector

## 第一批交付物
- BrowserAgentPlatform.Web/src/views/WorkflowBuilderView.vue
- BrowserAgentPlatform.Web/src/views/LiveView.vue
- BrowserAgentPlatform.Web/src/components/ConfirmDialog.vue
- BrowserAgentPlatform.Web/src/components/FormField.vue
- BrowserAgentPlatform.Web/src/services/api.js
- README.txt

## 验收标准
1. Builder 不需要直接写 JSON 也能完成常见流程
2. Builder 所有核心按钮都有反应
3. Live 页面能更清楚看到执行过程
4. 删除操作都有确认
5. 导入导出和校验都有反馈
