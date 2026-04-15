# Instagram 自动浏览示例（先跑通流程）

## 文件说明
- `instagram_auto_browse_payload.json`：短流程冒烟（打开 Explore -> 两次滚动 -> 完成）。
- `instagram_night_random_browse_1h_payload.json`：约 1 小时随机滚动浏览（loop + random_wait）。
- `../../sql/instagram_automation_seed.sql`：向现有库增量写入 Instagram 账号、模板和可运行任务。

## 建议执行顺序
1. 先导入 SQL：
```bash
mysql -u<user> -p<password> <database_name> < sql/instagram_automation_seed.sql
```
2. 在任务列表先运行 `Instagram Auto Browse Task` 看链路是否通。
3. 再运行 `Instagram Night Random Browse 1H Task` 做长时浏览验证。

## 说明
- 此示例默认是“浏览流程联调优先”，不包含登录动作。
- 如果你后续需要账号态，可以按 Reddit 的 cookie/bootstrap 方式扩展（把 `add_cookies` 放到首节点）。
