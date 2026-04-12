# Reddit 自动化示例数据（仅浏览）

## 文件说明
- `reddit_automation_payload.json`：最小可运行工作流（打开 Reddit -> 等待页面加载 -> 结束）。
- `../../sql/reddit_automation_seed.sql`：增量 SQL（仅插入 Reddit 的 Account / Template / Task）。
- `../../sql/browser_agent_platform_full_with_reddit.sql`：完整数据库 SQL（包含原有全部表+数据，并在末尾追加 Reddit 浏览任务种子）。

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

## 设计说明
- 当前按你的要求，第一步只做“能够浏览”：仅打开 `https://old.reddit.com/` 并等待页面加载成功。
- 后续如果你要加“搜索 / 提取 / 点赞评论”等动作，可以在这个最小流程基础上继续扩展。
