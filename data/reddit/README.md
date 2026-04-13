# Reddit 自动化示例数据（登录受限时可用）

## 背景
Reddit 登录页可能触发风控（地区/IP/指纹/自动化特征检测），导致自动化无法稳定进入登录流程。

## 文件说明
- `reddit_automation_payload.json`：最小可运行工作流（打开 Reddit -> 等待页面加载 -> 结束）。
- `reddit_public_api_payload.json`：推荐替代方案（直接访问 Reddit 公共 JSON 接口，不走登录页）。
- `reddit_login_strategy_analysis.md`：登录受限场景的方案分析（含你提到的“先登录再保存网页”的可行性结论）。
- `reddit_scheme_b_profile_checklist.md`：方案 B（人工首登 + 持久化 Profile）的字段级配置清单与防失效实践。
- `../../sql/reddit_automation_seed.sql`：增量 SQL（会插入两套模板与任务：浏览版 + 公共接口版）。
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

## 建议优先级
1. **Reddit 公共接口任务（推荐）**：稳定、无登录依赖、适合联调验收。
2. Reddit 浏览任务：仅验证可打开页面，不验证账号态。
3. 账号登录自动化：仅在你有稳定住宅代理+指纹策略+cookie 预热时再做。
