# 方案 B：人工首登 + 持久化 Profile（可直接照填）

> 目标：只做一次人工登录，后续任务复用同一 Profile 的会话状态，尽量降低 Reddit 风控触发概率。

## 1) 平台字段配置清单（Profile）

以下字段建议在 `Profiles` 页面创建/编辑时按此填写：

- **Name**：`Reddit Profile 01`
- **Owner Agent**：固定一个（例如 `agent-local-001`）
- **Proxy**：固定一个长期可用代理（优先住宅代理，地区与账号常用地区一致）
- **Fingerprint Template**：固定模板，不要频繁切换
- **Lifecycle State**：`ready`
- **Isolation Level**：`standard` 或 `strict`（建议 `standard` 起步）

路径类字段（关键：必须长期稳定，不要改）：
- **Profile Root Path**：`/tmp/bap/reddit/profile_01`
- **Storage Root Path**：`/tmp/bap/reddit/storage_01`
- **Download Root Path**：`/tmp/bap/reddit/download_01`
- **Artifact Root Path**：`/tmp/bap/reddit/artifacts_01`
- **Temp Root Path**：`/tmp/bap/reddit/temp_01`

启动参数（Startup Args JSON）：
```json
[
  "--window-size=1366,768",
  "--disable-blink-features=AutomationControlled"
]
```

隔离策略（Isolation Policy JSON）：
```json
{
  "timezone": "America/Los_Angeles",
  "locale": "en-US",
  "webrtc": "disabled"
}
```

---

## 2) 平台字段配置清单（Account）

在 `账号中心` 新增 Reddit 账号：

- **Name**：`Reddit Main Account`
- **Platform**：`reddit`
- **Username**：你的 Reddit 用户名
- **Status**：`active`
- **BrowserProfileId**：绑定上面创建的 `Reddit Profile 01`

`credential_json`（建议）：
```json
{
  "mode": "manual_first_login",
  "note": "password stored outside platform secret manager"
}
```

`metadata_json`（建议）：
```json
{
  "region": "US",
  "network": "residential",
  "purpose": "reddit_session_reuse"
}
```

---

## 3) 人工首登标准流程（一次）

1. 确保 Agent 在 **headed** 模式，使用上面的 Profile 启动浏览器。
2. 手工打开 Reddit 登录页并完成登录（含 2FA/邮箱验证）。
3. 登录成功后，不要立即关闭：
   - 浏览首页 2-5 分钟；
   - 轻量滚动、点击 1-2 个帖子；
   - 避免高频操作。
4. 正常关闭浏览器，让会话状态写回 Profile 目录。

---

## 4) 后续自动化任务配置（只读优先）

任务建议先做低风险动作：
- open 首页/子版块
- wait_for_element
- extract_text
- end_success

避免一上来就做：
- 批量点赞/评论
- 高频翻页
- 短时间重复登录

---

## 5) 如何避免 Cookie 快速失效（关键实践）

1. **固定三元组**：`IP + 指纹模板 + Profile 路径` 必须稳定。
2. **固定执行节点**：尽量同一 Agent 执行，不跨机器。
3. **控制频率**：
   - 每次任务间隔 >= 15 分钟（起步）；
   - 每日运行次数先小后大。
4. **行为拟人化**：输入节奏、停留时间、滚动节奏不要机械。
5. **失败回退**：若出现登录页/403/挑战页，自动停止并转人工补登。

---

## 6) 推荐监控指标

在 `task_runs` / 日志里重点看：
- 当前 URL 是否跳回登录页；
- 403 / challenge / captcha 关键字出现频次；
- 同 Profile 的连续失败次数；
- 会话可用时长（从人工首登到首次失效）。

---

## 7) 最小可执行任务样例（复用已登录态）

```json
{
  "steps": [
    { "id": "open_home", "type": "open", "data": { "label": "打开 Reddit 首页", "url": "https://www.reddit.com/r/popular/" } },
    { "id": "wait_feed", "type": "wait_for_element", "data": { "label": "等待内容区域", "selector": "body", "timeout": 20000 } },
    { "id": "extract_title", "type": "extract_text", "data": { "label": "提取页面文本", "selector": "body" } },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "open_home", "target": "wait_feed" },
    { "source": "wait_feed", "target": "extract_title" },
    { "source": "extract_title", "target": "done" }
  ]
}
```

---

## 8) 常见故障与处理

- **现象**：突然跳登录页  
  **处理**：暂停任务，人工补登；检查是否换了 IP / Agent / 指纹模板。

- **现象**：页面可开但操作失败  
  **处理**：降低动作频率，先只读采集，确认会话稳定后再逐步加动作。

- **现象**：连续出现 challenge/captcha  
  **处理**：停止自动化 24h，恢复人工浏览一段时间，再小流量重启。


---

## 9) 已实现的自动化能力（本次代码变更）

Agent 执行器新增两种步骤类型：
- `add_cookies`：在任务执行时向浏览器上下文注入 cookies
- `clear_cookies`：清理当前浏览器上下文 cookies

`add_cookies` 的 `data` 示例：
```json
{
  "label": "注入 Cookie",
  "cookies": [
    {
      "name": "reddit_session",
      "value": "<cookie-value>",
      "domain": ".reddit.com",
      "path": "/",
      "httpOnly": true,
      "secure": true,
      "sameSite": "Lax"
    }
  ]
}
```

可直接参考：`data/reddit/reddit_cookie_bootstrap_payload.json`。
