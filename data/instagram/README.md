# Instagram 自动浏览示例（先跑通流程）

## 文件说明
- `instagram_cookie_bootstrap_payload.json`：Instagram 登录态注入示例（基于 `sessionid`）。
- `instagram_auto_browse_payload.json`：短流程冒烟（打开 Explore -> 两次滚动 -> 完成）。
- `instagram_night_random_browse_1h_payload.json`：约 1 小时随机滚动浏览（loop + random_wait）。
- `../../sql/instagram_automation_seed.sql`：向现有库增量写入 Instagram 账号、模板和可运行任务。
- `../../sql/instagram_set_session.sql`：一键把 `sessionid` 写入 Instagram 模板与任务。
- `../../sql/reddit_ins_full_flow_seed.sql`：一条 SQL 同时重建 Reddit + Instagram 两平台的模板和任务。
- `../../sql/reddit_ins_seed_validate.sql`：排查“启动后没反应”的就绪状态。
- `../../sql/reddit_ins_kickoff.sql`：强制把关键任务入队（bootstrap 优先）。
- `../../sql/instagram_priority_kickoff.sql`：当 Reddit 与 Instagram 共用一个 Profile 时，强制 Instagram 先跑。
- `../../sql/reddit_ins_parallel_enable.sql`：给 Reddit 和 Instagram 绑定不同 Profile，实现两平台并行运行。

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

## Reddit 在跑、Instagram 不跑（常见原因）
同一个 `browser_profile_id` 不能并发执行两个任务（会有 profile lock）。  
如果 Reddit 长任务先占住了 profile，Instagram 会一直排队。

可执行：
```bash
mysql -u<user> -p<password> <database_name> < sql/instagram_priority_kickoff.sql
```
这会临时暂停 Reddit、释放锁并优先拉起 Instagram bootstrap。

## 让 Reddit 和 Instagram 一起跑（并行）
```bash
mysql -u<user> -p<password> <database_name> < sql/reddit_ins_parallel_enable.sql
```
该脚本会尽量为 Instagram 绑定第二个 `browser_profile_id`；如果不存在，会自动克隆一个 Profile。
