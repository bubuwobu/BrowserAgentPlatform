本补丁用于修复 Agent 点击“测试打开”后无反应的问题。

本次修复内容：
1. 修复 Agent 心跳 commands 解析后 JsonDocument 被释放导致的：
   System.ObjectDisposedException: Cannot access a disposed object. Object name: 'JsonDocument'
2. 在 PlatformApiClient 中将 commands 改为 Clone 后返回，避免把失效的 JsonElement 传到 AgentWorker。
3. 在 AgentWorker 中增加命令处理日志，方便继续排查：
   - 收到什么命令
   - 对应 profileId
   - 启动/关闭 profile 时的日志
   - 异常堆栈输出

使用方式：
- 解压后直接覆盖到仓库根目录
- 重新编译 Agent：
  dotnet build
- 重新运行 Agent：
  dotnet run

覆盖文件：
- BrowserAgentPlatform.Agent/Services/PlatformApiClient.cs
- BrowserAgentPlatform.Agent/Services/AgentWorker.cs
