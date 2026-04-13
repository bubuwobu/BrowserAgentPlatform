# Reddit 自动化示例数据（登录受限时可用）

## 背景
Reddit 登录页可能触发风控（地区/IP/指纹/自动化特征检测），导致自动化无法稳定进入登录流程。

## 文件说明
- `reddit_automation_payload.json`：最小可运行工作流（打开 Reddit -> 等待页面加载 -> 结束）。
- `reddit_public_api_payload.json`：推荐替代方案（直接访问 Reddit 公共 JSON 接口，不走登录页）。
- `reddit_cookie_bootstrap_payload.json`：Cookie 注入示例（配合 `add_cookies` 步骤实现半自动会话复用）。
- `reddit_login_strategy_analysis.md`：登录受限场景的方案分析（含你提到的“先登录再保存网页”的可行性结论）。
- `reddit_scheme_b_profile_checklist.md`：方案 B（人工首登 + 持久化 Profile）的字段级配置清单与防失效实践。
- `reddit_cookie_runbook.md`：从获取 Cookie 到节点配置、任务执行与排障的全流程手册。
- `../../sql/reddit_automation_seed.sql`：增量 SQL（会插入两套模板与任务：浏览版 + 公共接口版）。
- `../../sql/reddit_only_reset_seed.sql`：清空现有业务数据并只重建 Reddit 测试所需最小数据（推荐你当前场景）。
- `../../sql/reddit_only_full_flow_seed.sql`：清空数据并重建 Reddit 的完整测试数据（配置+模板+任务+运行记录+日志+产物记录）。
- `../../sql/reddit_set_session.sql`：一键把 `reddit_session` 写入模板和任务 JSON（无需进页面手改）。
- `../../sql/reddit_seed_validate.sql`：校验种子是否完整、任务是否可跑（定位“刷完 SQL 仍无法执行”）。
- `../../sql/browser_agent_platform_full_with_reddit.sql`：完整数据库 SQL（包含原有全部表+数据，并在末尾追加 Reddit 种子）。

## 运行方式（建议）
### A. 全量初始化（推荐新库）
```bash
mysql -u<user> -p<password> <database_name> < sql/browser_agent_platform_full_with_reddit.sql
```

### B. 增量补丁（已有库）
```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_automation_seed.sql
```

> 脚本会自动取 `browser_profiles` 第一条作为执行 Profile；如果没有 Profile，插入任务会自动跳过。


### C. 重建 Reddit 完整测试数据（推荐）
```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_only_full_flow_seed.sql
```

> 这会清空现有业务数据，并重建一整套 Reddit 数据：配置、模板、任务、任务运行记录、运行日志、产物记录。

### C2. 仅保留 Reddit 最小测试数据（会清空业务数据）
```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_only_reset_seed.sql
```

> 执行后数据库里会只保留 Reddit 相关的最小可运行数据。你只需要把任务里的 `reddit_session` 替换成真实值即可测试。


### D. 一键写入 reddit_session（无需页面手改）
1. 打开 `sql/reddit_set_session.sql`，把 `@reddit_session` 改成你的真实值。
2. 执行：
```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_set_session.sql
```


### E. 校验种子是否可跑
```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_seed_validate.sql
```

## 建议优先级
1. **Reddit 公共接口任务（推荐）**：稳定、无登录依赖、适合联调验收。
2. Reddit 浏览任务：仅验证可打开页面，不验证账号态。
3. 账号登录自动化：仅在你有稳定住宅代理+指纹策略+cookie 预热时再做。
