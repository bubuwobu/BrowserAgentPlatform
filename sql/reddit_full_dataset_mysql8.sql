-- Reddit full dataset for MySQL8 schema-only imports.
-- This script assumes tables already exist (as in your exported schema) and inserts runnable seed data.
--
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_full_dataset_mysql8.sql

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE browser_artifacts;
TRUNCATE TABLE task_run_logs;
TRUNCATE TABLE run_isolation_reports;
TRUNCATE TABLE task_runs;
TRUNCATE TABLE browser_profile_locks;
TRUNCATE TABLE agent_commands;
TRUNCATE TABLE audit_events;
TRUNCATE TABLE tasks;
TRUNCATE TABLE task_templates;
TRUNCATE TABLE account_runtime_identities;
TRUNCATE TABLE device_profiles;
TRUNCATE TABLE proxy_bindings;
TRUNCATE TABLE launch_profiles;
TRUNCATE TABLE accounts;
TRUNCATE TABLE browser_profiles;
TRUNCATE TABLE agents;
TRUNCATE TABLE proxies;
TRUNCATE TABLE fingerprint_templates;

SET FOREIGN_KEY_CHECKS = 1;
START TRANSACTION;

-- Keep/create admin user
INSERT INTO users (`id`,`username`,`password_hash`,`display_name`,`role`,`created_at`)
VALUES (1,'admin','$2a$11$5L9x0zd4zRMvxBmreQ8wUeXPmikTVzZGZklvnUBPnDiYOBgdvI/16','Admin','admin',NOW())
ON DUPLICATE KEY UPDATE display_name=VALUES(display_name), role=VALUES(role);

-- Core config
INSERT INTO agents (`id`,`agent_key`,`name`,`machine_name`,`status`,`max_parallel_runs`,`current_runs`,`scheduler_tags`,`last_heartbeat_at`,`created_at`)
VALUES (1,'agent-local-001','Local Agent','LOCAL-MACHINE','online',2,0,'reddit,default',NOW(),NOW());

INSERT INTO fingerprint_templates (`id`,`name`,`config_json`,`created_at`)
VALUES (1,'Reddit Desktop Fingerprint','{"browser":"chrome","platform":"windows","locale":"en-US"}',NOW());

INSERT INTO browser_profiles (
  `id`,`name`,`owner_agent_id`,`proxy_id`,`fingerprint_template_id`,`status`,`isolation_level`,
  `local_profile_path`,`storage_root_path`,`download_root_path`,`startup_args_json`,`isolation_policy_json`,
  `runtime_meta_json`,`workspace_key`,`profile_root_path`,`artifact_root_path`,`temp_root_path`,
  `lifecycle_state`,`last_used_at`,`last_isolation_check_at`,`last_started_at`,`last_stopped_at`,`last_rebuild_at`,`created_at`
)
VALUES (
  1,'REDDIT PROFILE - MAIN',1,NULL,1,'idle','standard',
  '/tmp/bap/reddit/profile_main','/tmp/bap/reddit/storage_main','/tmp/bap/reddit/download_main',
  '["--window-size=1366,768","--disable-blink-features=AutomationControlled"]',
  '{"timezone":"America/Los_Angeles","locale":"en-US","webrtc":"disabled"}',
  '{"seed":"reddit_full_dataset_mysql8"}','reddit_profile_main','/tmp/bap/reddit/profile_main',
  'runtime/profiles/reddit_main/artifacts','runtime/profiles/reddit_main/temp',
  'ready',NOW(),NOW(),NULL,NULL,NULL,NOW()
);

INSERT INTO accounts (`id`,`name`,`platform`,`username`,`status`,`browser_profile_id`,`credential_json`,`metadata_json`,`created_at`)
VALUES (1,'Reddit Main Account','reddit','reddit_main','active',1,'{"mode":"cookie_bootstrap"}','{"site":"https://www.reddit.com","note":"replace <replace-reddit_session>"}',NOW());

INSERT INTO account_runtime_identities (`id`,`account_id`,`browser_profile_id`,`device_profile_id`,`proxy_binding_id`,`launch_profile_id`,`status`,`created_at`)
VALUES (1,1,1,NULL,NULL,NULL,'active',NOW());

-- Templates
INSERT INTO task_templates (`id`,`name`,`definition_json`,`created_at`) VALUES
(1,'Reddit Cookie Bootstrap Template','{\n  "steps": [\n    {\n      "id": "inject_cookies",\n      "type": "add_cookies",\n      "data": {\n        "label": "注入 Reddit Cookie",\n        "cookies": [\n          {\n            "name": "reddit_session",\n            "value": "<replace-reddit_session>",\n            "domain": ".reddit.com",\n            "path": "/",\n            "httpOnly": true,\n            "secure": true,\n            "sameSite": "Lax"\n          }\n        ]\n      }\n    },\n    { "id": "open_home", "type": "open", "data": { "label": "打开 Reddit 首页", "url": "https://www.reddit.com/" } },\n    { "id": "wait_home", "type": "wait_for_element", "data": { "label": "等待页面", "selector": "body", "timeout": 20000 } },\n    { "id": "extract_home", "type": "extract_text", "data": { "label": "提取页面文本", "selector": "body" } },\n    { "id": "done", "type": "end_success", "data": { "label": "完成" } }\n  ],\n  "edges": [\n    { "source": "inject_cookies", "target": "open_home" },\n    { "source": "open_home", "target": "wait_home" },\n    { "source": "wait_home", "target": "extract_home" },\n    { "source": "extract_home", "target": "done" }\n  ]\n}',NOW()),
(2,'Reddit Public JSON API Template','{\n  "steps": [\n    { "id": "open_public_json", "type": "open", "data": { "label": "打开 Reddit 公共 JSON 接口", "url": "https://www.reddit.com/r/technology/hot.json?limit=10" } },\n    { "id": "wait_body", "type": "wait_for_element", "data": { "label": "等待 JSON 页面加载", "selector": "body", "timeout": 20000 } },\n    { "id": "extract_raw", "type": "extract_text", "data": { "label": "提取 JSON 原文", "selector": "body" } },\n    { "id": "done", "type": "end_success", "data": { "label": "完成" } }\n  ],\n  "edges": [\n    { "source": "open_public_json", "target": "wait_body" },\n    { "source": "wait_body", "target": "extract_raw" },\n    { "source": "extract_raw", "target": "done" }\n  ],\n  "assertions": [\n    { "type": "text_contains", "label": "返回包含 data 字段", "sourceStepId": "extract_raw", "expected": "\\"data\\"" },\n    { "type": "text_contains", "label": "返回包含 children 字段", "sourceStepId": "extract_raw", "expected": "\\"children\\"" }\n  ]\n}',NOW()),
(3,'Reddit Auto Browse Template','{\n  "steps": [\n    { \"id\": \"open_home\", \"type\": \"open\", \"data\": { \"label\": \"打开 Reddit 首页\", \"url\": \"https://www.reddit.com/\" } },\n    { \"id\": \"wait_home\", \"type\": \"wait_for_element\", \"data\": { \"label\": \"等待首页\", \"selector\": \"body\", \"timeout\": 20000 } },\n    { \"id\": \"scroll_1\", \"type\": \"scroll\", \"data\": { \"label\": \"首次滚动\", \"deltaY\": 900 } },\n    { \"id\": \"wait_1\", \"type\": \"wait_for_timeout\", \"data\": { \"label\": \"停留\", \"timeout\": 2200 } },\n    { \"id\": \"open_sub_tech\", \"type\": \"open\", \"data\": { \"label\": \"打开 technology 板块\", \"url\": \"https://www.reddit.com/r/technology/\" } },\n    { \"id\": \"wait_sub_tech\", \"type\": \"wait_for_element\", \"data\": { \"label\": \"等待 technology\", \"selector\": \"body\", \"timeout\": 20000 } },\n    { \"id\": \"scroll_2\", \"type\": \"scroll\", \"data\": { \"label\": \"二次滚动\", \"deltaY\": 1100 } },\n    { \"id\": \"wait_2\", \"type\": \"wait_for_timeout\", \"data\": { \"label\": \"停留\", \"timeout\": 2600 } },\n    { \"id\": \"open_sub_news\", \"type\": \"open\", \"data\": { \"label\": \"打开 worldnews 板块\", \"url\": \"https://www.reddit.com/r/worldnews/\" } },\n    { \"id\": \"wait_sub_news\", \"type\": \"wait_for_element\", \"data\": { \"label\": \"等待 worldnews\", \"selector\": \"body\", \"timeout\": 20000 } },\n    { \"id\": \"done\", \"type\": \"end_success\", \"data\": { \"label\": \"完成\" } }\n  ],\n  \"edges\": [\n    { \"source\": \"open_home\", \"target\": \"wait_home\" },\n    { \"source\": \"wait_home\", \"target\": \"scroll_1\" },\n    { \"source\": \"scroll_1\", \"target\": \"wait_1\" },\n    { \"source\": \"wait_1\", \"target\": \"open_sub_tech\" },\n    { \"source\": \"open_sub_tech\", \"target\": \"wait_sub_tech\" },\n    { \"source\": \"wait_sub_tech\", \"target\": \"scroll_2\" },\n    { \"source\": \"scroll_2\", \"target\": \"wait_2\" },\n    { \"source\": \"wait_2\", \"target\": \"open_sub_news\" },\n    { \"source\": \"open_sub_news\", \"target\": \"wait_sub_news\" },\n    { \"source\": \"wait_sub_news\", \"target\": \"done\" }\n  ]\n}',NOW()),
(4,'Reddit Night Random Browse 1H Template','{\n  \"steps\": [\n    { \"id\": \"open_home\", \"type\": \"open\", \"data\": { \"label\": \"打开 Reddit 首页\", \"url\": \"https://www.reddit.com/\" } },\n    { \"id\": \"wait_home\", \"type\": \"wait_for_element\", \"data\": { \"label\": \"等待首页\", \"selector\": \"body\", \"timeout\": 20000 } },\n    { \"id\": \"start_wait\", \"type\": \"wait_for_timeout\", \"data\": { \"label\": \"打开后停留约5秒\", \"timeout\": 5000 } },\n    { \"id\": \"cycle\", \"type\": \"loop\", \"data\": { \"label\": \"循环浏览约1小时\", \"minCount\": 70, \"maxCount\": 90 } },\n    { \"id\": \"pick_sub\", \"type\": \"branch\", \"data\": { \"label\": \"随机选择板块\", \"mode\": \"random\" } },\n    { \"id\": \"open_tech\", \"type\": \"open\", \"data\": { \"label\": \"打开 r/technology\", \"url\": \"https://www.reddit.com/r/technology/\" } },\n    { \"id\": \"open_world\", \"type\": \"open\", \"data\": { \"label\": \"打开 r/worldnews\", \"url\": \"https://www.reddit.com/r/worldnews/\" } },\n    { \"id\": \"open_science\", \"type\": \"open\", \"data\": { \"label\": \"打开 r/science\", \"url\": \"https://www.reddit.com/r/science/\" } },\n    { \"id\": \"wait_sub\", \"type\": \"wait_for_element\", \"data\": { \"label\": \"等待板块加载\", \"selector\": \"body\", \"timeout\": 20000 } },\n    { \"id\": \"scroll_feed\", \"type\": \"scroll\", \"data\": { \"label\": \"模拟滚动浏览\", \"mode\": \"wheel\", \"times\": 1, \"minDeltaY\": 450, \"maxDeltaY\": 1200, \"minPauseMs\": 120, \"maxPauseMs\": 400 } },\n    { \"id\": \"stay_random\", \"type\": \"random_wait\", \"data\": { \"label\": \"每次滚动间隔30-60秒\", \"minMs\": 30000, \"maxMs\": 60000 } },\n    { \"id\": \"done\", \"type\": \"end_success\", \"data\": { \"label\": \"完成\" } }\n  ],\n  \"edges\": [\n    { \"source\": \"open_home\", \"target\": \"wait_home\" },\n    { \"source\": \"wait_home\", \"target\": \"start_wait\" },\n    { \"source\": \"start_wait\", \"target\": \"cycle\" },\n    { \"source\": \"cycle\", \"sourceHandle\": \"loop\", \"target\": \"pick_sub\" },\n    { \"source\": \"cycle\", \"sourceHandle\": \"done\", \"target\": \"done\" },\n    { \"source\": \"pick_sub\", \"target\": \"open_tech\" },\n    { \"source\": \"pick_sub\", \"target\": \"open_world\" },\n    { \"source\": \"pick_sub\", \"target\": \"open_science\" },\n    { \"source\": \"open_tech\", \"target\": \"wait_sub\" },\n    { \"source\": \"open_world\", \"target\": \"wait_sub\" },\n    { \"source\": \"open_science\", \"target\": \"wait_sub\" },\n    { \"source\": \"wait_sub\", \"target\": \"scroll_feed\" },\n    { \"source\": \"scroll_feed\", \"target\": \"stay_random\" },\n    { \"source\": \"stay_random\", \"target\": \"cycle\" }\n  ]\n}',NOW());

-- Tasks
INSERT INTO tasks (`id`,`name`,`browser_profile_id`,`scheduling_strategy`,`preferred_agent_id`,`status`,`payload_json`,`retry_policy_json`,`priority`,`timeout_seconds`,`created_at`,`account_id`,`is_enabled`,`schedule_type`,`schedule_config_json`,`next_run_at`,`last_run_at`) VALUES
(1,'Reddit Cookie Bootstrap Task',1,'profile_owner',NULL,'completed',(SELECT definition_json FROM task_templates WHERE id=1),'{"maxRetries":1}',100,300,NOW(),1,0,'manual','{}',NULL,NOW()),
(2,'Reddit Public JSON Task',1,'profile_owner',NULL,'completed',(SELECT definition_json FROM task_templates WHERE id=2),'{"maxRetries":1}',110,300,NOW(),1,1,'manual','{}',NULL,NOW()),
(3,'Reddit Auto Browse Task',1,'profile_owner',NULL,'completed',(SELECT definition_json FROM task_templates WHERE id=3),'{"maxRetries":1}',120,420,NOW(),1,0,'manual','{}',NULL,NOW()),
(4,'Reddit Night Random Browse 1H Task',1,'profile_owner',NULL,'queued',(SELECT definition_json FROM task_templates WHERE id=4),'{"maxRetries":1}',90,5400,NOW(),1,1,'manual','{}',NULL,NULL);

-- Runs + logs + artifacts
INSERT INTO task_runs (`id`,`task_id`,`browser_profile_id`,`assigned_agent_id`,`lease_token`,`status`,`retry_count`,`max_retries`,`current_step_id`,`current_step_label`,`current_url`,`result_json`,`error_code`,`error_message`,`last_preview_path`,`created_at`,`started_at`,`heartbeat_at`,`finished_at`) VALUES
(1,2,1,1,'lease-reddit-demo-0001','completed',0,1,'done','完成','https://www.reddit.com/r/technology/hot.json?limit=10','{"extract_raw":"{\"kind\":\"Listing\",\"data\":{\"children\":[]}}","assertions":{"allPassed":true,"total":2,"passed":2,"failed":0}}','','','/data/artifacts/1/final_reddit_json.png',NOW(),NOW(),NOW(),NOW()),
(2,4,1,NULL,'','queued',0,1,'','','','{}','','','',NOW(),NULL,NULL,NULL);

INSERT INTO task_run_logs (`id`,`task_run_id`,`level`,`step_id`,`message`,`created_at`) VALUES
(1,1,'info','open_public_json','Executing open',NOW()),
(2,1,'info','wait_body','Executing wait_for_element',NOW()),
(3,1,'info','extract_raw','Executing extract_text',NOW()),
(4,1,'info','done','Executing end_success',NOW()),
(5,1,'info','','Run finished with status: completed',NOW()),
(6,2,'info','cycle','Night random browse queued and waiting for execution',NOW());

INSERT INTO browser_artifacts (`id`,`task_run_id`,`artifact_type`,`file_path`,`file_name`,`created_at`)
VALUES (1,1,'screenshot','data/artifacts/1/final_reddit_json.png','final_reddit_json.png',NOW());

INSERT INTO run_isolation_reports (`id`,`task_run_id`,`browser_profile_id`,`proxy_snapshot_json`,`fingerprint_snapshot_json`,`storage_check_json`,`network_check_json`,`result`,`created_at`)
VALUES (1,1,1,'{}','{}','{"ok":true}','{"ok":true}','pass',NOW());

INSERT INTO audit_events (`id`,`event_type`,`actor_type`,`actor_id`,`target_type`,`target_id`,`details_json`,`created_at`) VALUES
(1,'seed_reset','system','reddit_full_dataset_mysql8','task','1','{"note":"queued cookie bootstrap"}',NOW()),
(2,'seed_reset','system','reddit_full_dataset_mysql8','task','2','{"note":"completed public json run"}',NOW()),
(3,'demo_seed','system','reddit_full_dataset_mysql8','task_run','1','{"status":"completed"}',NOW());

COMMIT;

-- verify
-- SELECT COUNT(*) FROM tasks;
-- SELECT id,name,status FROM tasks;
-- SELECT id,task_id,status FROM task_runs;
