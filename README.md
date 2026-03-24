# BrowserAgentPlatform V4

这一版把核心功能收拢成一条真正能闭环的主线：

1. Web 登录
2. 配置代理 / 指纹模板 / BrowserProfile
3. 在可视化编排器里搭建任务
4. 保存为任务模板或直接创建任务
5. API 基于 MySQL 持久化队列并按多 Agent 调度策略分发
6. Agent 以独立 `user-data-dir` 启动 Profile 隔离浏览器执行
7. 通过 SignalR 推送运行日志、步骤状态、最新截图
8. Live 调试页查看实时进度和预览
9. 支持 Profile 测试打开 / 接管命令

## 这版做了什么

- MySQL 真接入
- JWT 登录鉴权
- 可视化分支连线编排
- 任务模板中心
- Profile 锁机制
- 多 Agent 调度策略
- 指纹模板中心
- Live 调试页 SignalR 实时推送
- 实时画面预览（连续截图推送，不是视频流）
- Profile 测试打开 / 接管命令

## 关于“完整跑完流程”

这版代码把主干闭环完整写出来了，但我无法在当前容器里实际编译验证，因为这里没有 `dotnet` 运行时。前端依赖可以安装，后端/Agent 代码是按 .NET 8 + EF Core + Playwright 组织的工程级代码结构。

## 目录

- `BrowserAgentPlatform.Api`
- `BrowserAgentPlatform.Agent`
- `BrowserAgentPlatform.Web`
- `sql/mysql_schema.sql`
- `docs/运行说明.md`
- `docs/旧项目可复用清单.md`
- `docs/前端页面接口对齐表.md`

## 默认账号

- 用户名：`admin`
- 密码：`Admin@123456`

> 首次启动 API 时会自动 seed 默认管理员。

