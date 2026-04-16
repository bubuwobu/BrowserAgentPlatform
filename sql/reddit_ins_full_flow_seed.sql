-- Combined full-flow seed for Reddit + Instagram (existing DB)
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_ins_full_flow_seed.sql
-- Then inject real session values:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_set_session.sql
--   mysql -u<user> -p<password> <database_name> < sql/instagram_set_session.sql

SET NAMES utf8mb4;
START TRANSACTION;

SET @reddit_profile_id := (SELECT id FROM browser_profiles ORDER BY id LIMIT 1);
SET @ins_profile_id := (SELECT id FROM browser_profiles WHERE id <> @reddit_profile_id ORDER BY id LIMIT 1);

-- If there is no second profile, clone one from Reddit profile for Instagram
INSERT INTO browser_profiles (
  `name`,`owner_agent_id`,`proxy_id`,`fingerprint_template_id`,`status`,`isolation_level`,
  `local_profile_path`,`storage_root_path`,`download_root_path`,`startup_args_json`,`isolation_policy_json`,`runtime_meta_json`,
  `workspace_key`,`profile_root_path`,`artifact_root_path`,`temp_root_path`,`lifecycle_state`,`last_used_at`,`last_isolation_check_at`,`last_started_at`,`last_stopped_at`,`last_rebuild_at`,`created_at`
)
SELECT
  CONCAT(bp.name, ' - INS'), bp.owner_agent_id, bp.proxy_id, bp.fingerprint_template_id, 'idle', bp.isolation_level,
  CONCAT(bp.local_profile_path, '_ins'), CONCAT(bp.storage_root_path, '_ins'), CONCAT(bp.download_root_path, '_ins'),
  bp.startup_args_json, bp.isolation_policy_json, bp.runtime_meta_json,
  CONCAT(bp.workspace_key, '_ins_', UNIX_TIMESTAMP()), CONCAT(bp.profile_root_path, '_ins'), CONCAT(bp.artifact_root_path, '_ins'), CONCAT(bp.temp_root_path, '_ins'),
  'created', NULL, NULL, NULL, NULL, NULL, NOW()
FROM browser_profiles bp
WHERE bp.id = @reddit_profile_id
  AND @ins_profile_id IS NULL;

SET @ins_profile_id := COALESCE(@ins_profile_id, NULLIF(LAST_INSERT_ID(), 0));
SET @ins_profile_id := COALESCE(@ins_profile_id, @reddit_profile_id);

-- Accounts
INSERT INTO accounts (`name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`)
SELECT 'Reddit Main Account','reddit','reddit_main','active',@reddit_profile_id,'{"mode":"cookie_bootstrap"}','{"site":"https://www.reddit.com","note":"replace <replace-reddit_session>"}',NOW()
WHERE @reddit_profile_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM accounts WHERE platform='reddit' AND username='reddit_main');

INSERT INTO accounts (`name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`)
SELECT 'Instagram Main Account','instagram','instagram_main','active',@ins_profile_id,'{"mode":"cookie_bootstrap"}','{"site":"https://www.instagram.com/","note":"replace <replace-instagram_sessionid>"}',NOW()
WHERE @ins_profile_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM accounts WHERE platform='instagram' AND username='instagram_main');

SET @reddit_account_id := (SELECT id FROM accounts WHERE platform='reddit' AND username='reddit_main' ORDER BY id DESC LIMIT 1);
SET @ins_account_id := (SELECT id FROM accounts WHERE platform='instagram' AND username='instagram_main' ORDER BY id DESC LIMIT 1);

-- Rebuild Reddit templates/tasks (by stable names)
DELETE FROM tasks WHERE name IN (
  'Reddit Cookie Bootstrap Task',
  'Reddit Auto Browse Task',
  'Reddit Night Random Browse 1H Task'
);
DELETE FROM task_templates WHERE name IN (
  'Reddit Cookie Bootstrap Template',
  'Reddit Auto Browse Template',
  'Reddit Night Random Browse 1H Template'
);

INSERT INTO task_templates (`name`,`definition_json`,`created_at`) VALUES
(
  'Reddit Cookie Bootstrap Template',
  '{
  "steps": [
    {
      "id": "inject_cookies",
      "type": "add_cookies",
      "data": {
        "label": "注入 Reddit Cookie",
        "cookies": [
          {
            "name": "reddit_session",
            "value": "<replace-reddit_session>",
            "url": "https://www.reddit.com",
            "httpOnly": true,
            "secure": true,
            "sameSite": "Lax"
          }
        ]
      }
    },
    { "id": "open_home", "type": "open", "data": { "label": "打开 Reddit 首页", "url": "https://www.reddit.com/r/popular/" } },
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
  'Reddit Auto Browse Template',
  '{
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
}',
  NOW()
),
(
  'Reddit Night Random Browse 1H Template',
  '{
  "steps": [
    { "id": "open_home", "type": "open", "data": { "label": "打开 Reddit Popular", "url": "https://www.reddit.com/r/popular/" } },
    { "id": "wait_home", "type": "wait_for_element", "data": { "label": "等待 Popular 页面", "selector": "body", "timeout": 20000 } },
    { "id": "start_wait", "type": "wait_for_timeout", "data": { "label": "首屏停留", "timeout": 5000 } },
    { "id": "cycle", "type": "loop", "data": { "label": "循环浏览约1小时", "minCount": 70, "maxCount": 90 } },
    { "id": "scroll_feed", "type": "scroll", "data": { "label": "模拟滚动浏览", "mode": "wheel", "times": 1, "minDeltaY": 450, "maxDeltaY": 1200, "minPauseMs": 120, "maxPauseMs": 400 } },
    { "id": "random_like", "type": "random_like", "data": { "label": "随机点赞（低频）", "chance": 0.2, "selectors": ["button[aria-label*=upvote i]", "button[aria-label*=like i]"] } },
    { "id": "stay_random", "type": "random_wait", "data": { "label": "每次滚动间隔30-60秒", "minMs": 30000, "maxMs": 60000 } },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "open_home", "target": "wait_home" },
    { "source": "wait_home", "target": "start_wait" },
    { "source": "start_wait", "target": "cycle" },
    { "source": "cycle", "sourceHandle": "loop", "target": "scroll_feed" },
    { "source": "cycle", "sourceHandle": "done", "target": "done" },
    { "source": "scroll_feed", "target": "random_like" },
    { "source": "random_like", "target": "stay_random" },
    { "source": "stay_random", "target": "cycle" }
  ]
}',
  NOW()
);

SET @rtpl_cookie := (SELECT id FROM task_templates WHERE name='Reddit Cookie Bootstrap Template' ORDER BY id DESC LIMIT 1);
SET @rtpl_auto := (SELECT id FROM task_templates WHERE name='Reddit Auto Browse Template' ORDER BY id DESC LIMIT 1);
SET @rtpl_night := (SELECT id FROM task_templates WHERE name='Reddit Night Random Browse 1H Template' ORDER BY id DESC LIMIT 1);

INSERT INTO tasks (`name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`)
SELECT 'Reddit Cookie Bootstrap Task',@reddit_profile_id,'profile_owner',NULL,'queued',(SELECT definition_json FROM task_templates WHERE id=@rtpl_cookie),'{"maxRetries":1}',120,300,NOW(),@reddit_account_id,1,'manual','{}',NULL,NULL
WHERE @reddit_profile_id IS NOT NULL;
INSERT INTO tasks (`name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`)
SELECT 'Reddit Auto Browse Task',@reddit_profile_id,'profile_owner',NULL,'completed',(SELECT definition_json FROM task_templates WHERE id=@rtpl_auto),'{"maxRetries":1}',120,420,NOW(),@reddit_account_id,0,'manual','{}',NULL,NULL
WHERE @reddit_profile_id IS NOT NULL;
INSERT INTO tasks (`name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`)
SELECT 'Reddit Night Random Browse 1H Task',@reddit_profile_id,'profile_owner',NULL,'queued',(SELECT definition_json FROM task_templates WHERE id=@rtpl_night),'{"maxRetries":1}',90,5400,NOW(),@reddit_account_id,1,'manual','{}',NULL,NULL
WHERE @reddit_profile_id IS NOT NULL;

-- Rebuild Instagram templates/tasks (by stable names)
DELETE FROM tasks WHERE name IN (
  'Instagram Cookie Bootstrap Task',
  'Instagram Auto Browse Task',
  'Instagram Night Random Browse 1H Task'
);
DELETE FROM task_templates WHERE name IN (
  'Instagram Cookie Bootstrap Template',
  'Instagram Auto Browse Template',
  'Instagram Night Random Browse 1H Template'
);

INSERT INTO task_templates (`name`,`definition_json`,`created_at`) VALUES
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
    { "id": "random_like", "type": "random_like", "data": { "label": "随机点赞（低频）", "chance": 0.22, "selectors": ["article button:has(svg[aria-label=Like])", "button:has(svg[aria-label=Like])"] } },
    { "id": "stay_random", "type": "random_wait", "data": { "label": "每次滚动间隔30-60秒", "minMs": 30000, "maxMs": 60000 } },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "open_explore", "target": "wait_page" },
    { "source": "wait_page", "target": "start_wait" },
    { "source": "start_wait", "target": "cycle" },
    { "source": "cycle", "sourceHandle": "loop", "target": "scroll_feed" },
    { "source": "cycle", "sourceHandle": "done", "target": "done" },
    { "source": "scroll_feed", "target": "random_like" },
    { "source": "random_like", "target": "stay_random" },
    { "source": "stay_random", "target": "cycle" }
  ]
}',
  NOW()
);

SET @itpl_cookie := (SELECT id FROM task_templates WHERE name='Instagram Cookie Bootstrap Template' ORDER BY id DESC LIMIT 1);
SET @itpl_auto := (SELECT id FROM task_templates WHERE name='Instagram Auto Browse Template' ORDER BY id DESC LIMIT 1);
SET @itpl_night := (SELECT id FROM task_templates WHERE name='Instagram Night Random Browse 1H Template' ORDER BY id DESC LIMIT 1);

INSERT INTO tasks (`name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`)
SELECT 'Instagram Cookie Bootstrap Task',@ins_profile_id,'profile_owner',NULL,'queued',(SELECT definition_json FROM task_templates WHERE id=@itpl_cookie),'{"maxRetries":1}',120,300,NOW(),@ins_account_id,1,'manual','{}',NULL,NULL
WHERE @ins_profile_id IS NOT NULL;
INSERT INTO tasks (`name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`)
SELECT 'Instagram Auto Browse Task',@ins_profile_id,'profile_owner',NULL,'queued',(SELECT definition_json FROM task_templates WHERE id=@itpl_auto),'{"maxRetries":1}',120,420,NOW(),@ins_account_id,1,'manual','{}',NULL,NULL
WHERE @ins_profile_id IS NOT NULL;
INSERT INTO tasks (`name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`)
SELECT 'Instagram Night Random Browse 1H Task',@ins_profile_id,'profile_owner',NULL,'queued',(SELECT definition_json FROM task_templates WHERE id=@itpl_night),'{"maxRetries":1}',90,5400,NOW(),@ins_account_id,1,'manual','{}',NULL,NULL
WHERE @ins_profile_id IS NOT NULL;

COMMIT;
