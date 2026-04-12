# Reddit 自动化示例数据（仅浏览）

## 文件说明
- `reddit_automation_payload.json`：最小可运行工作流（打开 Reddit -> 等待页面加载 -> 结束）。
- `../../sql/reddit_automation_seed.sql`：一键插入 Account / Template / Task 的 SQL（仅浏览版本）。

## 运行方式（建议）
1. 确保 `browser_profiles` 表里至少有 1 条数据（脚本会自动取第一条 Profile）。
2. 导入 SQL：
   ```bash
   mysql -u<user> -p<password> <database_name> < sql/reddit_automation_seed.sql
   ```
3. 在任务列表里找到 `Reddit 浏览任务 #...`，启动执行。

## 设计说明
- 当前按你的要求，第一步只做“能够浏览”：仅打开 `https://old.reddit.com/` 并等待页面加载成功。
- 后续如果你要加“搜索 / 提取 / 点赞评论”等动作，可以在这个最小流程基础上继续扩展。
