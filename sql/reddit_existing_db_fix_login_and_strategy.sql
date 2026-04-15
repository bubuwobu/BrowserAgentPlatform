-- Hotfix for existing Reddit DB
-- Goal:
-- 1) Keep browsing strategy on /r/popular only (no subreddit switching)
-- 2) Re-queue cookie bootstrap so login/session can be refreshed first
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_existing_db_fix_login_and_strategy.sql

SET NAMES utf8mb4;
START TRANSACTION;

SET @auto_browse_payload := '{
  "steps": [
    { "id": "open_home", "type": "open", "data": { "label": "打开 Reddit Popular", "url": "https://www.reddit.com/r/popular/" } },
    { "id": "wait_home", "type": "wait_for_element", "data": { "label": "等待 Popular 页面", "selector": "body", "timeout": 20000 } },
    { "id": "scroll_1", "type": "scroll", "data": { "label": "首次滚动", "deltaY": 900 } },
    { "id": "wait_1", "type": "wait_for_timeout", "data": { "label": "停留", "timeout": 2200 } },
    { "id": "scroll_2", "type": "scroll", "data": { "label": "二次滚动", "deltaY": 1100 } },
    { "id": "wait_2", "type": "wait_for_timeout", "data": { "label": "停留", "timeout": 2600 } },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "open_home", "target": "wait_home" },
    { "source": "wait_home", "target": "scroll_1" },
    { "source": "scroll_1", "target": "wait_1" },
    { "source": "wait_1", "target": "scroll_2" },
    { "source": "scroll_2", "target": "wait_2" },
    { "source": "wait_2", "target": "done" }
  ]
}';

SET @night_browse_payload := '{
  "steps": [
    { "id": "open_home", "type": "open", "data": { "label": "打开 Reddit Popular", "url": "https://www.reddit.com/r/popular/" } },
    { "id": "wait_home", "type": "wait_for_element", "data": { "label": "等待 Popular 页面", "selector": "body", "timeout": 20000 } },
    { "id": "start_wait", "type": "wait_for_timeout", "data": { "label": "首屏停留", "timeout": 5000 } },
    { "id": "cycle", "type": "loop", "data": { "label": "循环浏览约1小时", "minCount": 70, "maxCount": 90 } },
    { "id": "scroll_feed", "type": "scroll", "data": { "label": "模拟滚动浏览", "mode": "wheel", "times": 1, "minDeltaY": 450, "maxDeltaY": 1200, "minPauseMs": 120, "maxPauseMs": 400 } },
    { "id": "stay_random", "type": "random_wait", "data": { "label": "每次滚动间隔30-60秒", "minMs": 30000, "maxMs": 60000 } },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "open_home", "target": "wait_home" },
    { "source": "wait_home", "target": "start_wait" },
    { "source": "start_wait", "target": "cycle" },
    { "source": "cycle", "sourceHandle": "loop", "target": "scroll_feed" },
    { "source": "cycle", "sourceHandle": "done", "target": "done" },
    { "source": "scroll_feed", "target": "stay_random" },
    { "source": "stay_random", "target": "cycle" }
  ]
}';

UPDATE task_templates SET definition_json = @auto_browse_payload
WHERE name = 'Reddit Auto Browse Template';

UPDATE task_templates SET definition_json = @night_browse_payload
WHERE name = 'Reddit Night Random Browse 1H Template';

UPDATE tasks SET payload_json = @auto_browse_payload
WHERE name = 'Reddit Auto Browse Task';

UPDATE tasks SET payload_json = @night_browse_payload
WHERE name = 'Reddit Night Random Browse 1H Task';

-- ensure cookie bootstrap can run manually
UPDATE tasks
SET status='queued', is_enabled=1, schedule_type='manual', schedule_config_json='{}', timeout_seconds=300
WHERE name='Reddit Cookie Bootstrap Task';

-- ensure night task can run manually after cookie bootstrap
UPDATE tasks
SET status='queued', is_enabled=1, schedule_type='manual', schedule_config_json='{}', timeout_seconds=5400
WHERE name='Reddit Night Random Browse 1H Task';

-- disable short auto-browse task to avoid accidental runs that look like "only browsed a few times"
UPDATE tasks
SET is_enabled=0, status='completed', timeout_seconds=420
WHERE name='Reddit Auto Browse Task';

COMMIT;
