本补丁包用于直接覆盖当前 BrowserAgentPlatform 仓库中的对应文件。

本次补丁新增/增强：
1. 前端 API 增加 select 下拉所需的数据接口
2. Profile 创建表单改为下拉选择：
   - OwnerAgentId
   - ProxyId
   - FingerprintTemplateId
3. Tasks 页面增加：
   - 任务创建表单可视化
   - BrowserProfileId 下拉
   - PreferredAgentId 下拉
   - 调度策略下拉
   - 自动刷新
4. Profiles 页面增加：
   - 在线 Agent 优先显示
   - 自动刷新状态
   - 成功/失败消息
   - 强制解锁
5. Live 页面增强：
   - SignalR 地址使用环境变量
   - 操作按钮联动（测试打开 / 开始接管 / 结束接管）
   - 自动刷新预览
6. Dashboard 增强：
   - 最近运行
   - 最近 Agent
   - 最近 Profile
7. 后端 CORS 支持多个本地前端地址
8. Live summary 返回更多可视化字段

使用方式：
- 解压后直接覆盖到仓库根目录
- 重新启动 BrowserAgentPlatform.Api
- 重新启动 BrowserAgentPlatform.Web

建议测试顺序：
登录 -> Agent 在线 -> 创建指纹模板 -> 创建 Profile -> 测试打开 -> 创建任务 -> Live 查看运行过程
