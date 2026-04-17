# Instagram 自动浏览示例（先跑通流程）

## 文件说明
- `instagram_cookie_bootstrap_payload.json`：Instagram 登录态注入示例（基于 `sessionid`）。
- `instagram_auto_browse_payload.json`：短流程冒烟（打开 Explore -> 两次滚动 -> 完成）。
- `instagram_night_random_browse_1h_payload.json`：约 1 小时随机滚动浏览（固定 `https://www.instagram.com/explore/`，loop + random_wait + 低频随机点赞）。
- `../../sql/instagram_automation_seed.sql`：向现有库增量写入 Instagram 账号、模板和可运行任务。
- `../../sql/instagram_set_session.sql`：一键把 `sessionid` 写入 Instagram 模板与任务。
- `../../sql/reddit_ins_full_flow_seed.sql`：一条 SQL 同时重建 Reddit + Instagram 两平台的模板和任务。
- `../../sql/reddit_ins_like_only_reset_seed.sql`：一键重置为“仅 Reddit+Instagram 点赞联调”最小数据集（固定入口 + random_like）。
  - 已内置 queued `task_runs`，导入后应自动执行；若无反应，优先检查 agent 心跳与在线状态。
- `../../sql/reddit_ins_seed_validate.sql`：排查“启动后没反应”的就绪状态。
- `../../sql/reddit_ins_kickoff.sql`：强制把关键任务入队（bootstrap 优先）。

## 建议执行顺序
1. 先导入 SQL：
```bash
mysql -u<user> -p<password> <database_name> < sql/instagram_automation_seed.sql
```
2. 编辑 `sql/instagram_set_session.sql`，把 `@instagram_sessionid` 改成你账号的真实值后执行：
```bash
mysql -u<user> -p<password> <database_name> < sql/instagram_set_session.sql
```
3. 在任务列表先运行 `Instagram Cookie Bootstrap Task`，确认登录态可复用。
4. 再运行 `Instagram Auto Browse Task` 看链路是否通。
5. 最后运行 `Instagram Night Random Browse 1H Task` 做长时浏览验证。

## 说明
- 此示例默认是“浏览流程联调优先”，不包含登录动作。
- 如果你后续需要账号态，可以按 Reddit 的 cookie/bootstrap 方式扩展（把 `add_cookies` 放到首节点）。
- `sessionid` 获取建议：先在同一台机器手工登录 Instagram，然后从浏览器开发者工具 Cookies 里复制 `sessionid`。

## 启动后“没反应”快速处理
```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_ins_seed_validate.sql
mysql -u<user> -p<password> <database_name> < sql/reddit_ins_kickoff.sql
```
> 如果 `profiles_total = 0` 或 `agents_online = 0`，任务不会真正执行。
