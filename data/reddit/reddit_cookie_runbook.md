# Reddit Cookie 会话复用实操手册（按步骤执行）

> 适用版本：已支持 `add_cookies` / `clear_cookies` 节点类型的 Agent。  
> 目标：不走自动登录，使用“人工首登 + Cookie 注入”让任务稳定跑起来。

---

## 快速开始（重置为纯 Reddit 数据）

如果你希望“一键清空历史数据，只留并重建 Reddit 完整测试数据”，先执行：

```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_only_full_flow_seed.sql
```

执行后会自动生成：
- 1 个 Agent
- 1 个 Reddit Profile
- 1 个 Reddit 账号
- 1 个 Cookie Bootstrap 模板
- 1 个可运行任务（queued）

你只需要把模板/任务中的 `reddit_session` 替换为真实值即可。

然后把 `reddit_session` 一键写入模板/任务（不用进页面改）：

```bash
# 先编辑 sql/reddit_set_session.sql 里的 @reddit_session
mysql -u<user> -p<password> <database_name> < sql/reddit_set_session.sql
```


最后做一次校验（确认任务可跑）：

```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_seed_validate.sql
```

---

## 0. 先决条件

1. API / Agent / Web 能正常启动。
2. 你已有一个可用的 Browser Profile（建议固定一个 Profile，不要频繁切换）。
3. 你的网络出口尽量稳定（IP/地区不要频繁变化）。

---

## 1) 获取 Cookie（推荐方式：浏览器开发者工具）

### 方法 A：Chrome DevTools 手工复制（推荐）
1. 用你要复用的账号在浏览器中手工登录 Reddit（`https://www.reddit.com/`）。
2. 打开开发者工具：`F12`。
3. 进入 `Application`（应用） -> `Storage` -> `Cookies` -> 选择 `https://www.reddit.com`。
4. 找到关键 cookie（例如 `reddit_session`，以及你业务需要的其他 cookie）。
5. 记录字段：
   - `Name`
   - `Value`
   - `Domain`（通常是 `.reddit.com`）
   - `Path`（通常是 `/`）
   - `Expires/Max-Age`（如果有）
   - `HttpOnly`
   - `Secure`
   - `SameSite`

### 方法 B：导出插件
也可以使用 cookie 导出插件导出 JSON，但请确认字段名能对应到平台 `add_cookies` 的格式。

---

## 2) 组织成平台可用 JSON（`add_cookies`）

`TaskExecutor` 的 `add_cookies` 接收 `data.cookies` 数组。每个元素至少需要：
- `name`
- `value`
- `url` 或 `domain`（二选一至少一个）

推荐模板：
```json
{
  "label": "注入 Reddit Cookie",
  "cookies": [
    {
      "name": "reddit_session",
      "value": "<你的真实值>",
      "domain": ".reddit.com",
      "path": "/",
      "httpOnly": true,
      "secure": true,
      "sameSite": "Lax"
    }
  ]
}
```

> 如果你有 `expires`，传 Unix 时间戳（秒）即可。

---

## 3) 节点怎么配置（工作流）

建议最小流程（先验证会话可用）：
1. `inject_cookies`（type=`add_cookies`）
2. `open_home`（type=`open`，url=`https://www.reddit.com/`）
3. `wait_home`（type=`wait_for_element`，selector=`body`）
4. `done`（type=`end_success`）

你可以直接使用：
- `data/reddit/reddit_cookie_bootstrap_payload.json`

该文件就是上述流程，可直接导入后把 cookie 值替换掉。

---

## 4) 在平台中创建任务（详细）

1. 打开 `任务` / `Workflow Builder`。
2. 新建任务，绑定你的 `BrowserProfile`（必须是你准备用于会话复用的那个）。
3. 将 payload 设置为 `reddit_cookie_bootstrap_payload.json` 内容。
4. 把 `inject_cookies.data.cookies[0].value` 替换成真实 cookie。
5. 保存任务，状态设为可执行（`queued` / `manual`）。
6. 点击运行。

---

## 5) 如何判断成功

运行成功通常会看到：
- 任务状态 `completed`；
- `inject_cookies` 步骤结果里 `added > 0`；
- `open_home` 后没有立刻跳转到登录挑战页。

若你需要更严格校验，可追加：
- `extract_text` 提取页面内容；
- `assertions.text_contains` 检查是否包含某些已登录特征文本。

---

## 6) 常见问题与处理

### Q1: `added=0`
- 原因：cookie 数组为空，或 cookie 缺少 `url/domain`。
- 处理：检查 JSON 字段拼写和必填项。

### Q2: 注入后仍跳登录
- 原因：cookie 已过期、IP/指纹漂移、缺少关键 cookie。
- 处理：
  1) 重新人工登录并重新导出；
  2) 固定代理和指纹模板；
  3) 避免跨机执行同一 Profile。

### Q3: 偶发 challenge/captcha
- 原因：行为频率过高或网络环境异常。
- 处理：降低频率，增加停留时间，必要时暂停 24h 后再恢复。

---

## 7) 进阶建议

1. 在任务开头可加 `clear_cookies`，避免脏会话干扰。  
2. 先跑“只读任务”，稳定后再逐步加互动动作。  
3. 给任务加失败回退：一旦检测到登录页/challenge，自动 `end_fail` 并转人工补登。


---

## 8) 为什么“刷完 SQL 还是跑不起来”（快速排查）

1. `reddit_set_session.sql` 没执行，模板里还是占位符。  
   - 现象：任务启动后登录态无效。  
   - 检查：`task_templates.definition_json` 是否还包含 `<replace-reddit_session>`。

2. Agent 未在线或未拉取任务。  
   - 现象：任务一直停留 `queued`。  
   - 检查：`agents.status` 是否 `online`，Agent 进程日志是否正常轮询。

3. Profile 不可用。  
   - 现象：任务创建后马上失败。  
   - 检查：`browser_profiles.lifecycle_state` 是否 `ready`，路径是否可写。

4. Cookie 本身失效。  
   - 现象：`add_cookies` 成功但页面仍跳登录/挑战。  
   - 处理：重新人工登录导出最新 cookie，保持 IP/指纹稳定。
