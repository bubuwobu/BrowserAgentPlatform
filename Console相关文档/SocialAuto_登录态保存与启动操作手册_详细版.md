# SocialAuto 登录态保存与启动操作手册（详细版）

## 1. 目标
这套流程的核心是：

1. 先在正常浏览器中手工登录 Reddit / Instagram  
2. 将当前浏览器登录态导出到项目目录  
3. 之后正式启动项目时，优先读取 `storageState.json`，`cookies.json` 作为兜底  
4. 只有登录失效时才重新导入

---

## 2. 推荐目录

### Reddit
- `SocialAuto.Reddit.Console\profiles\reddit\storageState.json`
- `SocialAuto.Reddit.Console\profiles\reddit\cookies.json`

### Instagram
- `SocialAuto.Instagram.Console\profiles\instagram\storageState.json`
- `SocialAuto.Instagram.Console\profiles\instagram\cookies.json`

---

## 3. 第一次完整操作顺序

### Reddit
```bat
dotnet build SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj
dotnet run --project SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj -- --import-browser
dotnet run --project SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj
```

### Instagram
```bat
dotnet build SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj
dotnet run --project SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj -- --import-browser
dotnet run --project SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj
```

---

## 4. 如何手工保存登录态

### 4.1 启动调试浏览器
```bat
"C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir="D:\ChromeDebugProfile"
```

备用：
```bat
"%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir="D:\ChromeDebugProfile"
```

### 4.2 验证调试端口
打开：
```text
http://127.0.0.1:9222/json/version
```

### 4.3 手工登录
在这个调试浏览器中打开 Reddit 或 Instagram 并手工登录。

### 4.4 导入登录态
```bat
dotnet run --project SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj -- --import-browser
dotnet run --project SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj -- --import-browser
```

---

## 5. 后续日常启动

### Reddit
```bat
dotnet run --project SocialAuto.Reddit.Console\SocialAuto.Reddit.Console.csproj
```

### Instagram
```bat
dotnet run --project SocialAuto.Instagram.Console\SocialAuto.Instagram.Console.csproj
```

只有在以下情况才需要重新导入：
- 登录态失效
- 切换账号
- profiles 目录被删除或覆盖
- 换电脑 / 换目录

---

## 6. appsettings.json login 节点建议

### Reddit
```json
"login": {
  "mode": "cookie_bootstrap",
  "cookieFilePath": "./profiles/reddit/cookies.json",
  "storageStateFilePath": "./profiles/reddit/storageState.json",
  "waitForManualConfirm": false,
  "exportCookiesOnExit": true,
  "exportCookieFilePath": "./profiles/reddit/cookies.json",
  "exportStorageStateFilePath": "./profiles/reddit/storageState.json",
  "importBrowserCdpEndpoint": "http://127.0.0.1:9222",
  "manualLoginDetectTimeoutMinutes": 5
}
```

### Instagram
```json
"login": {
  "mode": "cookie_bootstrap",
  "cookieFilePath": "./profiles/instagram/cookies.json",
  "storageStateFilePath": "./profiles/instagram/storageState.json",
  "waitForManualConfirm": false,
  "exportCookiesOnExit": true,
  "exportCookieFilePath": "./profiles/instagram/cookies.json",
  "exportStorageStateFilePath": "./profiles/instagram/storageState.json",
  "importBrowserCdpEndpoint": "http://127.0.0.1:9222",
  "manualLoginDetectTimeoutMinutes": 5
}
```

---

## 7. 常见问题
- 导入成功但正常启动没登录：先检查正式启动日志是否明确使用了 `storageState.json`
- bat 双击闪退：优先用 `cmd` 手动执行
- 调试浏览器没启动：检查 `http://127.0.0.1:9222/json/version`

