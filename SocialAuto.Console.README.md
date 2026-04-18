# Social Auto Console Bots (Reddit / Instagram)

两个独立控制台程序：

- `SocialAuto.Reddit.Console`
- `SocialAuto.Instagram.Console`

核心能力（默认一小时）：
1. 固定入口自动浏览。
2. 低频随机点赞。
3. 低频随机评论。
4. 预留隔离参数（指纹/行为/时区/UA/视口）配置位。


## 最小可跑（中国瓷器关键词）

下面这套已经写进两个项目的 `appsettings.json`，更新后可直接跑：

- 关键词：`中国瓷器`、`青花瓷`、`景德镇`、`汝窑`、`官窑`
- 互动策略：命中关键词高概率点赞/评论，未命中低概率随机点赞/评论
- 等待时间：`4~9` 秒（更快观察效果）
- 运行时长：`30` 分钟
- 证据截图：点赞和评论提交后自动截图，方便排查
- 默认入口：
  - Reddit：`https://www.reddit.com/search/?q=中国瓷器`
  - Instagram：`https://www.instagram.com/explore/tags/chineseporcelain/`

如需更激进，可把：
- `keywordLikeProbability` 调到 `0.9`
- `keywordCommentProbability` 调到 `0.3`
- `openRandomPostProbability` 调到 `0.65`

截图目录默认值：
- Reddit: `./artifacts/reddit`
- Instagram: `./artifacts/instagram`

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

## 默认登录（推荐流程）

### 方案 A：manual_once（首次推荐）
1. `login.mode = manual_once`
2. 启动程序后，在弹出的浏览器里手工完成登录
3. 回到控制台按回车继续跑任务
4. 程序结束时自动导出 cookies 到 `login.exportCookieFilePath`

### 方案 B：cookie_bootstrap（后续稳定复用）
1. 把 `login.mode` 改为 `cookie_bootstrap`
2. 确保 `login.cookieFilePath` 指向上一步导出的 cookies 文件
3. 启动时程序会自动注入 cookie，再打开目标页面

> 隔离建议（单账号阶段也建议保留）：每个平台用独立 `profileDir`，并保持固定 `isolation` 参数（timezone/locale/UA/viewport）。
