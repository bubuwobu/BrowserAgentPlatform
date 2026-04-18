-- Non-destructive repair SQL for Reddit+Instagram random-like tasks.
-- Use when self-check shows no target tasks / no task_runs.
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_ins_like_quick_repair.sql

SET NAMES utf8mb4;
START TRANSACTION;

SET @reddit_profile_id := (SELECT id FROM browser_profiles ORDER BY id LIMIT 1);
SET @instagram_profile_id := (SELECT id FROM browser_profiles WHERE id <> @reddit_profile_id ORDER BY id LIMIT 1);
SET @instagram_profile_id := COALESCE(@instagram_profile_id, @reddit_profile_id);

-- Fail-fast hints (non-blocking):
SELECT 'profile_check' AS check_item, @reddit_profile_id AS reddit_profile_id, @instagram_profile_id AS instagram_profile_id;

-- Accounts (upsert-like)
INSERT INTO accounts (`name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`)
SELECT 'Reddit Main Account','reddit','reddit_main','active',@reddit_profile_id,'{"mode":"cookie_bootstrap"}','{"site":"https://www.reddit.com","note":"replace <replace-reddit_session>"}',NOW()
WHERE @reddit_profile_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM accounts WHERE platform='reddit' AND username='reddit_main');

INSERT INTO accounts (`name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`)
SELECT 'Instagram Main Account','instagram','instagram_main','active',@instagram_profile_id,'{"mode":"cookie_bootstrap"}','{"site":"https://www.instagram.com/","note":"replace <replace-instagram_sessionid>"}',NOW()
WHERE @instagram_profile_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM accounts WHERE platform='instagram' AND username='instagram_main');

SET @reddit_account_id := (SELECT id FROM accounts WHERE platform='reddit' AND username='reddit_main' ORDER BY id DESC LIMIT 1);
SET @instagram_account_id := (SELECT id FROM accounts WHERE platform='instagram' AND username='instagram_main' ORDER BY id DESC LIMIT 1);

-- Rebuild only target templates to avoid drift
DELETE FROM task_templates WHERE name IN (
  'Reddit Cookie Bootstrap Template',
  'Instagram Cookie Bootstrap Template',
  'Reddit Night Random Like 1H Template',
  'Instagram Night Random Like 1H Template'
);

INSERT INTO task_templates (`name`,`definition_json`,`created_at`) VALUES
(
  'Reddit Cookie Bootstrap Template',
  '{\n  "steps": [\n    {\n      "id": "inject_cookies",\n      "type": "add_cookies",\n      "data": {\n        "label": "注入 Reddit Cookie",\n        "cookies": [\n          {\n            "name": "reddit_session",\n            "value": "<replace-reddit_session>",\n            "url": "https://www.reddit.com",\n            "httpOnly": true,\n            "secure": true,\n            "sameSite": "Lax"\n          }\n        ]\n      }\n    },\n    { "id": "open_home", "type": "open", "data": { "label": "打开 Reddit Popular", "url": "https://www.reddit.com/r/popular/" } },\n    { "id": "wait_home", "type": "wait_for_element", "data": { "label": "等待页面", "selector": "body", "timeout": 20000 } },\n    { "id": "done", "type": "end_success", "data": { "label": "完成" } }\n  ],\n  "edges": [\n    { "source": "inject_cookies", "target": "open_home" },\n    { "source": "open_home", "target": "wait_home" },\n    { "source": "wait_home", "target": "done" }\n  ]\n}',
  NOW()
),
(
  'Instagram Cookie Bootstrap Template',
  '{\n  "steps": [\n    {\n      "id": "inject_cookies",\n      "type": "add_cookies",\n      "data": {\n        "label": "注入 Instagram 会话 Cookie",\n        "cookies": [\n          {\n            "name": "sessionid",\n            "value": "<replace-instagram_sessionid>",\n            "url": "https://www.instagram.com",\n            "httpOnly": true,\n            "secure": true,\n            "sameSite": "Lax"\n          }\n        ]\n      }\n    },\n    { "id": "open_explore", "type": "open", "data": { "label": "打开 Instagram Explore", "url": "https://www.instagram.com/explore/" } },\n    { "id": "wait_page", "type": "wait_for_element", "data": { "label": "等待页面", "selector": "body", "timeout": 20000 } },\n    { "id": "done", "type": "end_success", "data": { "label": "完成" } }\n  ],\n  "edges": [\n    { "source": "inject_cookies", "target": "open_explore" },\n    { "source": "open_explore", "target": "wait_page" },\n    { "source": "wait_page", "target": "done" }\n  ]\n}',
  NOW()
),
(
  'Reddit Night Random Like 1H Template',
  '{\n  "steps": [\n    { "id": "open_home", "type": "open", "data": { "label": "打开 Reddit Popular", "url": "https://www.reddit.com/r/popular/" } },\n    { "id": "wait_home", "type": "wait_for_element", "data": { "label": "等待 Popular 页面", "selector": "body", "timeout": 20000 } },\n    { "id": "start_wait", "type": "wait_for_timeout", "data": { "label": "首屏停留", "timeout": 5000 } },\n    { "id": "cycle", "type": "loop", "data": { "label": "循环浏览约1小时", "minCount": 70, "maxCount": 90 } },\n    { "id": "scroll_feed", "type": "scroll", "data": { "label": "模拟滚动浏览", "mode": "wheel", "times": 1, "minDeltaY": 450, "maxDeltaY": 1200, "minPauseMs": 120, "maxPauseMs": 400 } },\n    { "id": "random_like", "type": "random_like", "data": { "label": "随机点赞（低频）", "chance": 0.2, "selectors": ["button[aria-label*=upvote i]", "button[aria-label*=like i]"] } },\n    { "id": "stay_random", "type": "random_wait", "data": { "label": "每次滚动间隔30-60秒", "minMs": 30000, "maxMs": 60000 } },\n    { "id": "done", "type": "end_success", "data": { "label": "完成" } }\n  ],\n  "edges": [\n    { "source": "open_home", "target": "wait_home" },\n    { "source": "wait_home", "target": "start_wait" },\n    { "source": "start_wait", "target": "cycle" },\n    { "source": "cycle", "sourceHandle": "loop", "target": "scroll_feed" },\n    { "source": "cycle", "sourceHandle": "done", "target": "done" },\n    { "source": "scroll_feed", "target": "random_like" },\n    { "source": "random_like", "target": "stay_random" },\n    { "source": "stay_random", "target": "cycle" }\n  ]\n}',
  NOW()
),
(
  'Instagram Night Random Like 1H Template',
  '{\n  "steps": [\n    { "id": "open_explore", "type": "open", "data": { "label": "打开 Instagram Explore", "url": "https://www.instagram.com/explore/" } },\n    { "id": "wait_page", "type": "wait_for_element", "data": { "label": "等待页面加载", "selector": "body", "timeout": 20000 } },\n    { "id": "start_wait", "type": "wait_for_timeout", "data": { "label": "首屏停留", "timeout": 5000 } },\n    { "id": "cycle", "type": "loop", "data": { "label": "循环浏览约1小时", "minCount": 70, "maxCount": 90 } },\n    { "id": "scroll_feed", "type": "scroll", "data": { "label": "模拟滚动浏览", "mode": "wheel", "times": 1, "minDeltaY": 420, "maxDeltaY": 1150, "minPauseMs": 120, "maxPauseMs": 420 } },\n    { "id": "random_like", "type": "random_like", "data": { "label": "随机点赞（低频）", "chance": 0.22, "selectors": ["article button:has(svg[aria-label=Like])", "button:has(svg[aria-label=Like])"] } },\n    { "id": "stay_random", "type": "random_wait", "data": { "label": "每次滚动间隔30-60秒", "minMs": 30000, "maxMs": 60000 } },\n    { "id": "done", "type": "end_success", "data": { "label": "完成" } }\n  ],\n  "edges": [\n    { "source": "open_explore", "target": "wait_page" },\n    { "source": "wait_page", "target": "start_wait" },\n    { "source": "start_wait", "target": "cycle" },\n    { "source": "cycle", "sourceHandle": "loop", "target": "scroll_feed" },\n    { "source": "cycle", "sourceHandle": "done", "target": "done" },\n    { "source": "scroll_feed", "target": "random_like" },\n    { "source": "random_like", "target": "stay_random" },\n    { "source": "stay_random", "target": "cycle" }\n  ]\n}',
  NOW()
);

SET @tpl_reddit_cookie := (SELECT id FROM task_templates WHERE name='Reddit Cookie Bootstrap Template' ORDER BY id DESC LIMIT 1);
SET @tpl_instagram_cookie := (SELECT id FROM task_templates WHERE name='Instagram Cookie Bootstrap Template' ORDER BY id DESC LIMIT 1);
SET @tpl_reddit_night := (SELECT id FROM task_templates WHERE name='Reddit Night Random Like 1H Template' ORDER BY id DESC LIMIT 1);
SET @tpl_instagram_night := (SELECT id FROM task_templates WHERE name='Instagram Night Random Like 1H Template' ORDER BY id DESC LIMIT 1);

-- Rebuild target tasks only
DELETE FROM tasks WHERE name IN (
  'Reddit Cookie Bootstrap Task',
  'Instagram Cookie Bootstrap Task',
  'Reddit Night Random Like 1H Task',
  'Instagram Night Random Like 1H Task'
);

INSERT INTO tasks (
  `name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`
) VALUES
(
  'Reddit Cookie Bootstrap Task', @reddit_profile_id, 'profile_owner', NULL, 'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_reddit_cookie), '{"maxRetries":1}', 120, 300, NOW(), @reddit_account_id, 1, 'manual', '{}', NULL, NULL
),
(
  'Instagram Cookie Bootstrap Task', @instagram_profile_id, 'profile_owner', NULL, 'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_instagram_cookie), '{"maxRetries":1}', 120, 300, NOW(), @instagram_account_id, 1, 'manual', '{}', NULL, NULL
),
(
  'Reddit Night Random Like 1H Task', @reddit_profile_id, 'profile_owner', NULL, 'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_reddit_night), '{"maxRetries":1}', 90, 5400, NOW(), @reddit_account_id, 1, 'manual', '{}', NULL, NULL
),
(
  'Instagram Night Random Like 1H Task', @instagram_profile_id, 'profile_owner', NULL, 'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_instagram_night), '{"maxRetries":1}', 90, 5400, NOW(), @instagram_account_id, 1, 'manual', '{}', NULL, NULL
);

-- Ensure queued runs exist immediately
INSERT INTO task_runs (`task_id`,`browser_profile_id`,`status`,`retry_count`,`max_retries`,`created_at`)
SELECT t.id, t.browser_profile_id, 'queued', 0,
       COALESCE(CAST(JSON_UNQUOTE(JSON_EXTRACT(t.retry_policy_json, '$.maxRetries')) AS UNSIGNED), 0),
       NOW()
FROM tasks t
WHERE t.name IN (
  'Reddit Cookie Bootstrap Task',
  'Instagram Cookie Bootstrap Task',
  'Reddit Night Random Like 1H Task',
  'Instagram Night Random Like 1H Task'
);

COMMIT;

-- Verify
SELECT id, name, status, is_enabled FROM tasks WHERE name LIKE '%Random Like%' OR name LIKE '%Cookie Bootstrap%' ORDER BY id;
SELECT id, task_id, status, created_at FROM task_runs ORDER BY id DESC LIMIT 20;
