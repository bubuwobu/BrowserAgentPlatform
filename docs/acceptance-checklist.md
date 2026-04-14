# BrowserAgentPlatform 验收打勾清单（TikTok Mock 示例）

> 目标：从**基础配置**到**流程执行**，一步一步完成完整验收。  
> 适用范围：BrowserAgentPlatform（Web + API + Agent）+ tiktok_mock_site。  
> 建议执行人：产品、测试、实施工程师。

---

## 0. 环境准备

### 0.1 TikTok Mock Site

- [ ] 启动 `tiktok_mock_site`
  - 命令：
    - `npm install`
    - `npm start`
- [ ] 可访问：
  - `http://localhost:3001/login`
  - `http://localhost:3001/feed`
- [ ] 测试账号可用：
  - `alice / 123456`
  - `bob / 123456`
  - `cindy / 123456`

---

### 0.2 API / Web / Agent

- [ ] API 启动成功（默认前端请求基址：`http://localhost:12126`）
- [ ] Web 启动成功并可登录
- [ ] Agent 启动成功（建议 `AgentKey=agent-local-001`）

---

### 0.3 登录验证

- [ ] 登录页输入：
  - 用户名：`admin`
  - 密码：`Admin@123456`
- [ ] 登录后可进入首页（Token 生效，后续 API 请求有 Authorization Header）

---

## 1. 基础配置（按顺序执行）

---

## 1.1 指纹模板（Fingerprint）

### 操作路径
`指纹模板` -> `新增指纹模板`

### 建议填写（示例）
- 模板名称：`TikTok Desktop CN`
- 指纹配置 JSON：
```json
{
  "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123 Safari/537.36",
  "viewport": { "width": 1366, "height": 768 },
  "locale": "zh-CN",
  "timezoneId": "America/Los_Angeles"
}
```

### 字段作用
- `userAgent`：模拟浏览器身份
- `viewport`：模拟设备分辨率
- `locale`：页面语言/地区偏好
- `timezoneId`：时区行为一致性（风控相关）

### 验收勾选
- [ ] 模板创建成功
- [ ] 列表可见
- [ ] JSON 保存后可再次编辑加载

---

## 1.2 代理（Proxy）

> 当前系统可通过 API 管理代理，Profile 页面可直接选择代理绑定。

### 创建方式（API）
`POST /api/config/proxies`

### 建议请求体（示例）
```json
{
  "name": "TikTok Proxy SG",
  "protocol": "http",
  "host": "127.0.0.1",
  "port": 8080,
  "username": "",
  "password": "",
  "notes": "for tiktok mock"
}
```

### 字段作用
- `protocol`：代理协议（http/socks5）
- `host` / `port`：代理地址
- `username` / `password`：鉴权代理凭证
- `notes`：运维备注

### 验收勾选
- [ ] 代理创建成功
- [ ] 在 Profile 的“代理”下拉框中可选到该项

---

## 1.3 Profile（浏览器身份核心）

### 操作路径
`Profiles` -> `新增 Profile`

### 建议填写（示例）
- 名称：`TikTok Profile 01`
- Owner Agent：`agent-local-001`（必须建议绑定）
- 代理：选择 `TikTok Proxy SG`（或无代理）
- 指纹模板：`TikTok Desktop CN`
- 本地路径：`D:\bap\profiles\tiktok_01`（Linux 可 `/tmp/bap/profiles/tiktok_01`）
- 隔离级别：`strict`
- Workspace Key：`tiktok_ws_01`
- Lifecycle State：`ready`
- 存储根路径：`D:\bap\storage\tiktok_01`
- 下载根路径：`D:\bap\downloads\tiktok_01`
- Profile Root Path：`D:\bap\profiles\tiktok_01`
- Artifact Root Path：`D:\bap\artifacts\tiktok_01`
- Temp Root Path：`D:\bap\tmp\tiktok_01`
- 启动参数 JSON：`["--start-maximized"]`
- 隔离策略 JSON：
```json
{
  "timezone": "Asia/Shanghai",
  "locale": "zh-CN",
  "webrtc": "disabled"
}
```

### 字段作用
- `Owner Agent`：决定 test-open / takeover 命令能否下发（未绑定会失败）
- `ProxyId`：网络出口一致性
- `FingerprintTemplateId`：设备/浏览器画像一致性
- 各类路径：数据隔离，防止账号/任务污染
- `IsolationPolicyJson`：执行前策略校验依据
- `LifecycleState`：运行阶段（created/ready/leased/running 等）

### 验收勾选
- [ ] Profile 创建成功并显示在列表
- [ ] “隔离检查”可执行并返回结果
- [ ] （可选）“测试打开”成功（要求已绑定 Owner Agent）
- [ ] （可选）“开始接管/结束接管”命令可下发

---

## 1.4 账号（Account）

### 操作路径
`账号中心` -> `新增账号`

### 建议填写（示例）
- 账号名称：`TikTok Alice`
- 平台：`tiktok`
- 用户名：`alice`
- 状态：`active`
- 绑定 BrowserProfile：`TikTok Profile 01`
- 凭证 JSON：
```json
{
  "password": "123456",
  "note": "mock account"
}
```
- 附加信息 JSON：
```json
{
  "region": "CN",
  "purpose": "validation"
}
```

### 字段作用
- `platform`：业务平台分类
- `username`：业务账号标识
- `browserProfileId`：账号与浏览器身份绑定，避免串号
- `credentialJson`：账号凭证/补充数据（自定义）
- `metadataJson`：扩展标签信息（自定义）

### 验收勾选
- [ ] 账号创建成功
- [ ] 账号列表显示 profile 绑定关系
- [ ] 编辑后数据可保留
- [ ] 删除时若被任务绑定会被后端拦截（符合预期）

---

## 2. 流程配置（TikTok 示例）

---

## 2.1 闭环工作台（推荐主流程）

### 页面路径
`闭环工作台`

---

### Step 1：选择 Profile 并执行隔离检查

#### 输入
- `ProfileId`：选择 `TikTok Profile 01`

#### 作用
- 校验 profile 当前配置是否满足执行条件

#### 验收勾选
- [ ] 点击“执行隔离检查”后结果为“通过”
- [ ] 若失败，错误信息明确可定位（路径/策略/绑定问题）

---

### Step 2：创建闭环任务

#### 输入
- `agentKey`：`agent-local-001`
- `taskName`：`tiktok-closedloop-001`
- Payload：使用页面默认 TikTok 样例（可直接用）

#### 默认样例字段说明
- `baseUrl`：`http://localhost:3001`
- `username/password`：`alice/123456`
- `minVideos/maxVideos`：刷视频数量范围
- `minWatchMs/maxWatchMs`：单视频观看时长
- `minLikes/maxLikes`：点赞次数范围
- `minComments/maxComments`：评论次数范围
- `behaviorProfile`：行为风格标签
- `commentProvider`：评论生成提供方

#### 验收勾选
- [ ] 点击“创建闭环 Run”成功
- [ ] 页面返回并显示 `runId`

---

### Step 3：执行闭环

#### 输入
- 无额外输入（使用上一步 `runId`）

#### 作用
- 后端 lease run 给指定 agent 并推进执行状态

#### 验收勾选
- [ ] 执行状态显示“执行成功”
- [ ] 可点击“查看 Live”

---

## 2.2 任务中心（通用能力补验）

### 操作路径
`任务中心` -> `新增任务`

### 建议字段填写（示例）
- 任务名称：`TikTok Daily Browse`
- 绑定账号：`TikTok Alice`
- BrowserProfile：`TikTok Profile 01`（应与账号一致）
- 调度策略：
  - `least_loaded`（默认）
  - `profile_owner`
  - `preferred_agent`（需额外选 agent）
- 是否启用：`true`
- 任务调度类型：`manual`
- 优先级：`100`
- 超时：`300`
- RetryPolicyJson：`{"maxRetries":1}`
- PayloadJson：可填 TikTok 流程 JSON 或从编排器导入

### 验收勾选
- [ ] 任务创建成功
- [ ] “立即执行”后产生新 run
- [ ] “启停”可切换 `isEnabled`
- [ ] “删除”生效并从列表消失
- [ ] 最近运行区能看到对应 run，且可跳转 Live

---

## 3. Live 页面结果验收

### 操作路径
在任务中心点击 run 的“查看 Live”

### 验收勾选（逐项）
- [ ] Run 状态变化正确（queued -> running -> completed/failed）
- [ ] 当前步骤、当前 URL 正常展示
- [ ] 时间线日志持续写入
- [ ] 结果 JSON 有内容（成功场景）
- [ ] 错误信息可见（失败场景）
- [ ] Isolation Report 可查看
- [ ] Artifact 列表可查看（有产物时）
- [ ] “重跑”可生成 replay run

---

## 4. 异常恢复与运维动作

- [ ] Profile `test-open` 可下发
- [ ] Profile `takeover_start` / `takeover_stop` 可下发
- [ ] Profile `unlock` 可释放锁（异常卡死时）
- [ ] Agent 在线状态与心跳更新正常
- [ ] Dashboard 汇总指标与最近运行一致

---

## 5. 最终总验收（One Page）

### 基础配置
- [ ] 指纹模板 OK
- [ ] 代理配置 OK
- [ ] Profile 配置 OK
- [ ] 账号配置 OK

### 主流程
- [ ] 隔离检查通过
- [ ] 创建闭环 run 成功
- [ ] 执行闭环成功
- [ ] Live 日志/结果/隔离报告齐全

### 回归动作
- [ ] 立即执行 OK
- [ ] 重跑 OK
- [ ] 接管命令 OK
- [ ] 解锁 OK

---

## 6. 失败定位速查（建议附在验收记录后）

- Step2 成功、Step3 失败（`no run leased for agent`）  
  -> 检查 Agent 是否启动、`agentKey` 是否一致、Agent 心跳是否正常。

- TikTok 页面失败  
  -> 检查 mock 站是否在 `http://localhost:3001`，账号是否 `alice/123456`。

- test-open/takeover 失败  
  -> 检查 Profile 是否绑定 `Owner Agent`。

- 账号与任务绑定报错  
  -> 检查任务 BrowserProfile 与账号绑定 BrowserProfile 是否一致。
