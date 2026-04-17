# Social Auto Console Bots (Reddit / Instagram)

两个独立控制台程序：

- `SocialAuto.Reddit.Console`
- `SocialAuto.Instagram.Console`

核心能力（默认一小时）：
1. 固定入口自动浏览。
2. 低频随机点赞。
3. 低频随机评论。
4. 预留隔离参数（指纹/行为/时区/UA/视口）配置位。

## 运行前准备

1. 安装 .NET 8 SDK
2. 在项目目录执行 Playwright 浏览器安装（每个项目都可执行一次）：

```bash
dotnet build SocialAuto.Reddit.Console/SocialAuto.Reddit.Console.csproj
dotnet tool install --global Microsoft.Playwright.CLI || true
playwright install chromium
```

## 运行

```bash
dotnet run --project SocialAuto.Reddit.Console/SocialAuto.Reddit.Console.csproj
# 或指定配置文件
dotnet run --project SocialAuto.Reddit.Console/SocialAuto.Reddit.Console.csproj -- ./SocialAuto.Reddit.Console/appsettings.json


dotnet run --project SocialAuto.Instagram.Console/SocialAuto.Instagram.Console.csproj
# 或指定配置文件
dotnet run --project SocialAuto.Instagram.Console/SocialAuto.Instagram.Console.csproj -- ./SocialAuto.Instagram.Console/appsettings.json
```

程序会自动按以下顺序查找配置：  
1) 命令行参数指定路径；  
2) 输出目录 `appsettings.json`；  
3) 当前目录 `appsettings.json`；  
4) `SocialAuto.Reddit.Console/appsettings.json` 或 `SocialAuto.Instagram.Console/appsettings.json`。

## 说明

- 目前按你的需求：单账号、简单可跑优先。
- `profileDir` 为持久化浏览器目录：首次可人工登录，后续复用登录态。
- 评论/点赞选择器依赖平台页面结构，若页面变动需要在配置或代码里微调。
