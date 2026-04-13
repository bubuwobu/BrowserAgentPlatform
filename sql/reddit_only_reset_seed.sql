-- Reset current database data and seed ONLY Reddit-related records.
-- WARNING: this script deletes existing runtime/business data in the listed tables.
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_only_reset_seed.sql

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- 1) Clear runtime/history first
TRUNCATE TABLE browser_artifacts;
TRUNCATE TABLE task_run_logs;
TRUNCATE TABLE run_isolation_reports;
TRUNCATE TABLE task_runs;
TRUNCATE TABLE browser_profile_locks;
TRUNCATE TABLE agent_commands;
TRUNCATE TABLE audit_events;

-- 2) Clear scheduling/business data
TRUNCATE TABLE tasks;
TRUNCATE TABLE task_templates;
TRUNCATE TABLE accounts;
TRUNCATE TABLE account_runtime_identities;
TRUNCATE TABLE device_profiles;
TRUNCATE TABLE proxy_bindings;
TRUNCATE TABLE launch_profiles;

-- 3) Keep admin user, but reset infra config to minimal
TRUNCATE TABLE browser_profiles;
TRUNCATE TABLE agents;
TRUNCATE TABLE proxies;
TRUNCATE TABLE fingerprint_templates;

SET FOREIGN_KEY_CHECKS = 1;

START TRANSACTION;

-- 4) Minimal infra seed
INSERT INTO agents (`agent_key`, `name`, `machine_name`, `status`, `max_parallel_runs`, `current_runs`, `scheduler_tags`, `last_heartbeat_at`, `created_at`)
VALUES ('agent-local-001', 'Local Agent', 'LOCAL-MACHINE', 'online', 2, 0, 'default', NOW(), NOW());

SET @agent_id := LAST_INSERT_ID();

INSERT INTO fingerprint_templates (`name`, `fingerprint_json`, `created_at`)
VALUES ('Reddit Desktop Fingerprint', '{"browser":"chrome","platform":"windows","locale":"en-US"}', NOW());

SET @fp_id := LAST_INSERT_ID();

INSERT INTO browser_profiles (
  `name`, `owner_agent_id`, `proxy_id`, `fingerprint_template_id`, `status`, `isolation_level`,
  `local_profile_path`, `storage_root_path`, `download_root_path`, `startup_args_json`,
  `isolation_policy_json`, `runtime_meta_json`, `workspace_key`, `profile_root_path`,
  `artifact_root_path`, `temp_root_path`, `lifecycle_state`, `last_used_at`,
  `last_isolation_check_at`, `last_started_at`, `last_stopped_at`, `last_rebuild_at`, `created_at`
) VALUES (
  'REDDIT PROFILE - MAIN', @agent_id, NULL, @fp_id, 'idle', 'standard',
  '/tmp/bap/reddit/profile_main', '/tmp/bap/reddit/storage_main', '/tmp/bap/reddit/download_main',
  '["--window-size=1366,768","--disable-blink-features=AutomationControlled"]',
  '{"timezone":"America/Los_Angeles","locale":"en-US","webrtc":"disabled"}',
  '{}', 'reddit_profile_main', '/tmp/bap/reddit/profile_main',
  'runtime/profiles/reddit_main/artifacts', 'runtime/profiles/reddit_main/temp',
  'ready', NULL, NOW(), NULL, NULL, NULL, NOW()
);

SET @profile_id := LAST_INSERT_ID();

-- 5) Reddit account seed
INSERT INTO accounts (`name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`)
VALUES (
  'Reddit Main Account',
  'reddit',
  'reddit_main',
  'active',
  @profile_id,
  '{"mode":"manual_first_login_or_cookie_bootstrap"}',
  '{"site":"https://www.reddit.com","note":"replace reddit_session cookie before running"}',
  NOW()
);

SET @account_id := LAST_INSERT_ID();

-- 6) Cookie bootstrap template (replace reddit_session value before run)
INSERT INTO task_templates (`name`, `template_json`, `created_at`)
VALUES (
  'Reddit Cookie Bootstrap Template',
  '{\n  "steps": [\n    {\n      "id": "inject_cookies",\n      "type": "add_cookies",\n      "data": {\n        "label": "注入 Reddit Cookie",\n        "cookies": [\n          {\n            "name": "reddit_session",\n            "value": "<replace-reddit_session>",\n            "domain": ".reddit.com",\n            "path": "/",\n            "httpOnly": true,\n            "secure": true,\n            "sameSite": "Lax"\n          }\n        ]\n      }\n    },\n    {\n      "id": "open_home",\n      "type": "open",\n      "data": {\n        "label": "打开 Reddit 首页",\n        "url": "https://www.reddit.com/"\n      }\n    },\n    {\n      "id": "wait_home",\n      "type": "wait_for_element",\n      "data": {\n        "label": "等待页面",\n        "selector": "body",\n        "timeout": 20000\n      }\n    },\n    {\n      "id": "done",\n      "type": "end_success",\n      "data": {\n        "label": "完成"\n      }\n    }\n  ],\n  "edges": [\n    { "source": "inject_cookies", "target": "open_home" },\n    { "source": "open_home", "target": "wait_home" },\n    { "source": "wait_home", "target": "done" }\n  ]\n}',
  NOW()
);

SET @template_id := LAST_INSERT_ID();

-- 7) Runnable task
INSERT INTO tasks (
  `name`, `browser_profile_id`, `scheduling_strategy`, `preferred_agent_id`, `status`,
  `payload_json`, `retry_policy_json`, `priority`, `timeout_seconds`, `created_at`,
  `account_id`, `is_enabled`, `schedule_type`, `schedule_config_json`, `next_run_at`, `last_run_at`
) VALUES (
  'Reddit Cookie Bootstrap Task',
  @profile_id,
  'profile_owner',
  NULL,
  'queued',
  (SELECT template_json FROM task_templates WHERE id = @template_id),
  '{"maxRetries":1}',
  100,
  300,
  NOW(),
  @account_id,
  1,
  'manual',
  '{}',
  NULL,
  NULL
);

COMMIT;

-- 8) Verify
-- SELECT id, name, platform, username, status FROM accounts;
-- SELECT id, name FROM browser_profiles;
-- SELECT id, name, status FROM tasks;
