-- Reddit automation seed (browse-only flow, no login required)
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_automation_seed.sql

SET NAMES utf8mb4;
START TRANSACTION;

-- Reuse the first existing profile so the task can run immediately.
SET @profile_id := (
  SELECT id FROM browser_profiles ORDER BY id LIMIT 1
);

-- 1) Optional account seed (for bookkeeping)
INSERT INTO accounts (`name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`)
SELECT
  'Reddit Visitor Demo',
  'reddit',
  'reddit_guest',
  'active',
  @profile_id,
  '{"mode":"guest"}',
  '{"site":"https://old.reddit.com","notes":"browse-only automation seed"}',
  NOW()
WHERE @profile_id IS NOT NULL;

SET @account_id := LAST_INSERT_ID();

-- 2) Task template seed (only open + wait + done)
INSERT INTO task_templates (`name`, `template_json`, `created_at`)
VALUES (
  'Reddit Browse Only Template',
  '{\n  "steps": [\n    { "id": "open_reddit_home", "type": "open", "data": { "label": "打开 Reddit 首页", "url": "https://old.reddit.com/" } },\n    { "id": "wait_home_ready", "type": "wait_for_element", "data": { "label": "等待首页内容加载", "selector": "body", "timeout": 20000 } },\n    { "id": "done", "type": "end_success", "data": { "label": "完成" } }\n  ],\n  "edges": [\n    { "source": "open_reddit_home", "target": "wait_home_ready" },\n    { "source": "wait_home_ready", "target": "done" }\n  ]\n}',
  NOW()
);

SET @template_id := LAST_INSERT_ID();

-- 3) Runnable task seed (manual)
INSERT INTO tasks (
  `name`, `browser_profile_id`, `scheduling_strategy`, `preferred_agent_id`, `status`,
  `payload_json`, `retry_policy_json`, `priority`, `timeout_seconds`, `created_at`,
  `account_id`, `is_enabled`, `schedule_type`, `schedule_config_json`, `next_run_at`, `last_run_at`
)
SELECT
  CONCAT('Reddit 浏览任务 #', DATE_FORMAT(NOW(), '%Y%m%d%H%i%S')),
  @profile_id,
  'profile_owner',
  NULL,
  'queued',
  (SELECT template_json FROM task_templates WHERE id = @template_id),
  '{"maxRetries":1}',
  110,
  300,
  NOW(),
  NULLIF(@account_id, 0),
  1,
  'manual',
  '{}',
  NULL,
  NULL
WHERE @profile_id IS NOT NULL;

COMMIT;

-- Validation query:
-- SELECT id, name, status, created_at FROM tasks WHERE name LIKE 'Reddit 浏览任务 %' ORDER BY id DESC LIMIT 5;
