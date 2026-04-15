-- Instagram automation seed (browse-first)
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/instagram_automation_seed.sql

SET NAMES utf8mb4;
START TRANSACTION;

SET @profile_id := (SELECT id FROM browser_profiles ORDER BY id LIMIT 1);

INSERT INTO accounts (`name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`)
SELECT
  'Instagram Main Account',
  'instagram',
  'instagram_main',
  'active',
  @profile_id,
  '{"mode":"cookie_bootstrap"}',
  '{"site":"https://www.instagram.com/","note":"replace <replace-instagram_sessionid>"}',
  NOW()
WHERE @profile_id IS NOT NULL;

SET @account_id := LAST_INSERT_ID();

INSERT INTO task_templates (`name`, `definition_json`, `created_at`)
VALUES
(
  'Instagram Cookie Bootstrap Template',
  '{
  "steps": [
    {
      "id": "inject_cookies",
      "type": "add_cookies",
      "data": {
        "label": "注入 Instagram 会话 Cookie",
        "cookies": [
          {
            "name": "sessionid",
            "value": "<replace-instagram_sessionid>",
            "url": "https://www.instagram.com",
            "httpOnly": true,
            "secure": true,
            "sameSite": "Lax"
          }
        ]
      }
    },
    { "id": "open_home", "type": "open", "data": { "label": "打开 Instagram 首页", "url": "https://www.instagram.com/" } },
    { "id": "wait_home", "type": "wait_for_element", "data": { "label": "等待页面", "selector": "body", "timeout": 20000 } },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "inject_cookies", "target": "open_home" },
    { "source": "open_home", "target": "wait_home" },
    { "source": "wait_home", "target": "done" }
  ]
}',
  NOW()
),
(
  'Instagram Auto Browse Template',
  '{
  "steps": [
    { "id": "open_explore", "type": "open", "data": { "label": "打开 Instagram Explore", "url": "https://www.instagram.com/explore/" } },
    { "id": "wait_page", "type": "wait_for_element", "data": { "label": "等待页面加载", "selector": "body", "timeout": 20000 } },
    { "id": "scroll_1", "type": "scroll", "data": { "label": "首次滚动", "deltaY": 900 } },
    { "id": "wait_1", "type": "wait_for_timeout", "data": { "label": "停留", "timeout": 2500 } },
    { "id": "scroll_2", "type": "scroll", "data": { "label": "二次滚动", "deltaY": 1200 } },
    { "id": "wait_2", "type": "wait_for_timeout", "data": { "label": "停留", "timeout": 2800 } },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "open_explore", "target": "wait_page" },
    { "source": "wait_page", "target": "scroll_1" },
    { "source": "scroll_1", "target": "wait_1" },
    { "source": "wait_1", "target": "scroll_2" },
    { "source": "scroll_2", "target": "wait_2" },
    { "source": "wait_2", "target": "done" }
  ]
}',
  NOW()
),
(
  'Instagram Night Random Browse 1H Template',
  '{
  "steps": [
    { "id": "open_explore", "type": "open", "data": { "label": "打开 Instagram Explore", "url": "https://www.instagram.com/explore/" } },
    { "id": "wait_page", "type": "wait_for_element", "data": { "label": "等待页面加载", "selector": "body", "timeout": 20000 } },
    { "id": "start_wait", "type": "wait_for_timeout", "data": { "label": "首屏停留", "timeout": 5000 } },
    { "id": "cycle", "type": "loop", "data": { "label": "循环浏览约1小时", "minCount": 70, "maxCount": 90 } },
    { "id": "scroll_feed", "type": "scroll", "data": { "label": "模拟滚动浏览", "mode": "wheel", "times": 1, "minDeltaY": 420, "maxDeltaY": 1150, "minPauseMs": 120, "maxPauseMs": 420 } },
    { "id": "stay_random", "type": "random_wait", "data": { "label": "每次滚动间隔30-60秒", "minMs": 30000, "maxMs": 60000 } },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "open_explore", "target": "wait_page" },
    { "source": "wait_page", "target": "start_wait" },
    { "source": "start_wait", "target": "cycle" },
    { "source": "cycle", "sourceHandle": "loop", "target": "scroll_feed" },
    { "source": "cycle", "sourceHandle": "done", "target": "done" },
    { "source": "scroll_feed", "target": "stay_random" },
    { "source": "stay_random", "target": "cycle" }
  ]
}',
  NOW()
);

SET @tpl_auto := (SELECT id FROM task_templates WHERE name='Instagram Auto Browse Template' ORDER BY id DESC LIMIT 1);
SET @tpl_night := (SELECT id FROM task_templates WHERE name='Instagram Night Random Browse 1H Template' ORDER BY id DESC LIMIT 1);
SET @tpl_cookie := (SELECT id FROM task_templates WHERE name='Instagram Cookie Bootstrap Template' ORDER BY id DESC LIMIT 1);

INSERT INTO tasks (
  `name`, `browser_profile_id`, `scheduling_strategy`, `preferred_agent_id`, `status`,
  `payload_json`, `retry_policy_json`, `priority`, `timeout_seconds`, `created_at`,
  `account_id`, `is_enabled`, `schedule_type`, `schedule_config_json`, `next_run_at`, `last_run_at`
)
SELECT
  'Instagram Cookie Bootstrap Task',
  @profile_id,
  'profile_owner',
  NULL,
  'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_cookie),
  '{"maxRetries":1}',
  120,
  300,
  NOW(),
  NULLIF(@account_id, 0),
  1,
  'manual',
  '{}',
  NULL,
  NULL
WHERE @profile_id IS NOT NULL;

INSERT INTO tasks (
  `name`, `browser_profile_id`, `scheduling_strategy`, `preferred_agent_id`, `status`,
  `payload_json`, `retry_policy_json`, `priority`, `timeout_seconds`, `created_at`,
  `account_id`, `is_enabled`, `schedule_type`, `schedule_config_json`, `next_run_at`, `last_run_at`
)
SELECT
  'Instagram Auto Browse Task',
  @profile_id,
  'profile_owner',
  NULL,
  'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_auto),
  '{"maxRetries":1}',
  120,
  420,
  NOW(),
  NULLIF(@account_id, 0),
  1,
  'manual',
  '{}',
  NULL,
  NULL
WHERE @profile_id IS NOT NULL;

INSERT INTO tasks (
  `name`, `browser_profile_id`, `scheduling_strategy`, `preferred_agent_id`, `status`,
  `payload_json`, `retry_policy_json`, `priority`, `timeout_seconds`, `created_at`,
  `account_id`, `is_enabled`, `schedule_type`, `schedule_config_json`, `next_run_at`, `last_run_at`
)
SELECT
  'Instagram Night Random Browse 1H Task',
  @profile_id,
  'profile_owner',
  NULL,
  'queued',
  (SELECT definition_json FROM task_templates WHERE id=@tpl_night),
  '{"maxRetries":1}',
  90,
  5400,
  NOW(),
  NULLIF(@account_id, 0),
  1,
  'manual',
  '{}',
  NULL,
  NULL
WHERE @profile_id IS NOT NULL;

COMMIT;
