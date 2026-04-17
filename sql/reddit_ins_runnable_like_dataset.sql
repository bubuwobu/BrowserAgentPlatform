-- Runnable Reddit + Instagram browse-like dataset for existing schema.
-- This script is designed for an existing DB (tables already created).
-- It rebuilds only like-related data and queues runs immediately.
-- Strong single-agent binding: all seeded tasks are pinned to one agent key.
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_ins_runnable_like_dataset.sql

SET NAMES utf8mb4;
START TRANSACTION;

-- 0) Pick runtime anchors
SET @target_agent_key := 'agent-local-001';
SET @agent_id := (SELECT id FROM agents WHERE agent_key = @target_agent_key ORDER BY id LIMIT 1);
SET @reddit_profile_id := (SELECT id FROM browser_profiles ORDER BY id LIMIT 1);
SET @instagram_profile_id := (SELECT id FROM browser_profiles WHERE id <> @reddit_profile_id ORDER BY id LIMIT 1);
SET @instagram_profile_id := COALESCE(@instagram_profile_id, @reddit_profile_id);

-- 1) Ensure selected agent exists (for strict binding)
INSERT INTO agents (`agent_key`, `name`, `machine_name`, `status`, `max_parallel_runs`, `current_runs`, `scheduler_tags`, `last_heartbeat_at`, `created_at`)
SELECT @target_agent_key, 'Local Agent', 'LOCAL-MACHINE', 'online', 2, 0, 'default', NOW(), NOW()
WHERE @agent_id IS NULL;

SET @agent_id := COALESCE(@agent_id, NULLIF(LAST_INSERT_ID(), 0));

-- 2) Normalize selected agent (helps scheduler state)
UPDATE agents
SET status = 'online', current_runs = 0, last_heartbeat_at = NOW()
WHERE id = @agent_id;

-- 3) Remove old like-related runs/tasks/templates/accounts only
DELETE r FROM task_runs r
JOIN tasks t ON t.id = r.task_id
WHERE t.name IN (
  'Reddit Cookie Bootstrap Task',
  'Instagram Cookie Bootstrap Task',
  'Reddit Night Random Like 1H Task',
  'Instagram Night Random Like 1H Task'
);

DELETE FROM tasks WHERE name IN (
  'Reddit Cookie Bootstrap Task',
  'Instagram Cookie Bootstrap Task',
  'Reddit Night Random Like 1H Task',
  'Instagram Night Random Like 1H Task'
);

DELETE FROM task_templates WHERE name IN (
  'Reddit Cookie Bootstrap Template',
  'Instagram Cookie Bootstrap Template',
  'Reddit Night Random Like 1H Template',
  'Instagram Night Random Like 1H Template'
);

DELETE FROM accounts WHERE platform IN ('reddit', 'instagram') AND username IN ('reddit_main', 'instagram_main');

-- 4) Accounts
INSERT INTO accounts (`name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`) VALUES
('Reddit Main Account','reddit','reddit_main','active',@reddit_profile_id,'{"mode":"cookie_bootstrap"}','{"site":"https://www.reddit.com","note":"replace <replace-reddit_session>"}',NOW()),
('Instagram Main Account','instagram','instagram_main','active',@instagram_profile_id,'{"mode":"cookie_bootstrap"}','{"site":"https://www.instagram.com/","note":"replace <replace-instagram_sessionid>"}',NOW());

SET @reddit_account_id := (SELECT id FROM accounts WHERE platform='reddit' AND username='reddit_main' ORDER BY id DESC LIMIT 1);
SET @instagram_account_id := (SELECT id FROM accounts WHERE platform='instagram' AND username='instagram_main' ORDER BY id DESC LIMIT 1);

-- 5) Templates
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

-- 6) Tasks: strong bind to one agent key
INSERT INTO tasks (
  `name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`
) VALUES
(
  'Reddit Cookie Bootstrap Task', @reddit_profile_id, 'preferred_agent', @agent_id, 'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_reddit_cookie), '{"maxRetries":1}', 120, 300, NOW(), @reddit_account_id, 1, 'manual', '{}', NULL, NULL
),
(
  'Instagram Cookie Bootstrap Task', @instagram_profile_id, 'preferred_agent', @agent_id, 'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_instagram_cookie), '{"maxRetries":1}', 120, 300, NOW(), @instagram_account_id, 1, 'manual', '{}', NULL, NULL
),
(
  'Reddit Night Random Like 1H Task', @reddit_profile_id, 'preferred_agent', @agent_id, 'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_reddit_night), '{"maxRetries":1}', 90, 5400, NOW(), @reddit_account_id, 1, 'manual', '{}', NULL, NULL
),
(
  'Instagram Night Random Like 1H Task', @instagram_profile_id, 'preferred_agent', @agent_id, 'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_instagram_night), '{"maxRetries":1}', 90, 5400, NOW(), @instagram_account_id, 1, 'manual', '{}', NULL, NULL
);

-- 7) Queue runs now
INSERT INTO task_runs (`task_id`,`browser_profile_id`,`status`,`retry_count`,`max_retries`,`current_step_id`,`current_step_label`,`current_url`,`result_json`,`error_code`,`error_message`,`last_preview_path`,`created_at`,`started_at`,`heartbeat_at`,`finished_at`)
SELECT
  t.id,
  t.browser_profile_id,
  'queued',
  0,
  COALESCE(CAST(JSON_UNQUOTE(JSON_EXTRACT(t.retry_policy_json, '$.maxRetries')) AS UNSIGNED), 0),
  '',
  '',
  '',
  '{}',
  NULL,
  NULL,
  '',
  NOW(),
  NULL,
  NULL,
  NULL
FROM tasks t
WHERE t.name IN (
  'Reddit Cookie Bootstrap Task',
  'Instagram Cookie Bootstrap Task',
  'Reddit Night Random Like 1H Task',
  'Instagram Night Random Like 1H Task'
);

COMMIT;

-- 8) Verify
SELECT id, agent_key, status, last_heartbeat_at FROM agents WHERE id = @agent_id;
SELECT @target_agent_key AS target_agent_key, @agent_id AS target_agent_id;
SELECT id, name, scheduling_strategy, preferred_agent_id, status FROM tasks ORDER BY id DESC LIMIT 10;
SELECT id, task_id, status, created_at FROM task_runs ORDER BY id DESC LIMIT 20;
