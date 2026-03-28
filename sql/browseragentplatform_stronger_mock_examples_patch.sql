/*
  BrowserAgentPlatform stronger test-data patch
  Goal:
  - clear old business data
  - seed richer, immediately testable data for:
      1) TikTok mock site   -> http://localhost:3001
      2) Facebook-like site -> http://localhost:3000
  - keep only useful demo data
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================
-- 1) clear business data
-- ============================
DELETE FROM `browser_artifacts`;
DELETE FROM `task_run_logs`;
DELETE FROM `run_isolation_reports`;
DELETE FROM `browser_profile_locks`;
DELETE FROM `agent_commands`;
DELETE FROM `task_runs`;
DELETE FROM `tasks`;
DELETE FROM `task_templates`;
DELETE FROM `accounts`;
DELETE FROM `audit_events`;
DELETE FROM `browser_profiles`;
DELETE FROM `proxies`;
DELETE FROM `fingerprint_templates`;
DELETE FROM `agents`;

-- keep only one admin
DELETE FROM `users`;
INSERT INTO `users`
(`id`, `username`, `password_hash`, `display_name`, `role`, `created_at`)
VALUES
(1, 'admin', '$2a$11$5L9x0zd4zRMvxBmreQ8wUeXPmikTVzZGZklvnUBPnDiYOBgdvI/16', 'Admin', 'admin', NOW());

-- ============================
-- 2) base infrastructure
-- ============================
INSERT INTO `agents`
(`id`, `agent_key`, `name`, `machine_name`, `status`, `max_parallel_runs`, `current_runs`, `scheduler_tags`, `last_heartbeat_at`, `created_at`)
VALUES
(1, 'agent-local-001', 'Local Agent', 'DEV-PC', 'online', 3, 0, 'default,demo,local', NOW(), NOW());

INSERT INTO `fingerprint_templates`
(`id`, `name`, `config_json`, `created_at`)
VALUES
(1, 'Default Chrome', '{"browser":"chrome","platform":"windows"}', NOW()),
(2, 'Mobile Chrome', '{"browser":"chrome","platform":"android","device":"pixel"}', NOW());

INSERT INTO `proxies`
(`id`, `name`, `protocol`, `host`, `port`, `username`, `password`, `notes`, `created_at`)
VALUES
(1, 'No Proxy', 'http', '127.0.0.1', 0, '', '', 'local no-proxy placeholder', NOW());

-- ============================
-- 3) profiles
-- ============================
INSERT INTO `browser_profiles`
(`id`, `name`, `owner_agent_id`, `proxy_id`, `fingerprint_template_id`, `status`, `isolation_level`, `local_profile_path`, `storage_root_path`, `download_root_path`, `startup_args_json`, `isolation_policy_json`, `runtime_meta_json`, `last_used_at`, `last_isolation_check_at`, `created_at`)
VALUES
(1, 'TIKTOK MOCK PROFILE - ALICE', 1, NULL, 1, 'idle', 'standard', '/tmp/bap/tiktok/alice/profile', '/tmp/bap/tiktok/alice/storage', '/tmp/bap/tiktok/alice/download', '[]', '{"timezone":"Asia/Shanghai","locale":"zh-CN"}', '{"seed":"tiktok-alice"}', NULL, NOW(), NOW()),
(2, 'TIKTOK MOCK PROFILE - BOB',   1, NULL, 1, 'idle', 'standard', '/tmp/bap/tiktok/bob/profile',   '/tmp/bap/tiktok/bob/storage',   '/tmp/bap/tiktok/bob/download',   '[]', '{"timezone":"Asia/Shanghai","locale":"zh-CN"}', '{"seed":"tiktok-bob"}',   NULL, NOW(), NOW()),
(3, 'TIKTOK MOCK PROFILE - CINDY', 1, NULL, 2, 'idle', 'standard', '/tmp/bap/tiktok/cindy/profile', '/tmp/bap/tiktok/cindy/storage', '/tmp/bap/tiktok/cindy/download', '[]', '{"timezone":"Asia/Shanghai","locale":"zh-CN"}', '{"seed":"tiktok-cindy"}', NULL, NOW(), NOW()),
(4, 'FACEBOOK MOCK PROFILE - ALICE', 1, NULL, 1, 'idle', 'standard', '/tmp/bap/facebook/alice/profile', '/tmp/bap/facebook/alice/storage', '/tmp/bap/facebook/alice/download', '[]', '{"timezone":"Asia/Shanghai","locale":"zh-CN"}', '{"seed":"facebook-alice"}', NULL, NOW(), NOW()),
(5, 'FACEBOOK MOCK PROFILE - BOB',   1, NULL, 1, 'idle', 'standard', '/tmp/bap/facebook/bob/profile',   '/tmp/bap/facebook/bob/storage',   '/tmp/bap/facebook/bob/download',   '[]', '{"timezone":"Asia/Shanghai","locale":"zh-CN"}', '{"seed":"facebook-bob"}',   NULL, NOW(), NOW()),
(6, 'FACEBOOK MOCK PROFILE - CINDY', 1, NULL, 2, 'idle', 'standard', '/tmp/bap/facebook/cindy/profile', '/tmp/bap/facebook/cindy/storage', '/tmp/bap/facebook/cindy/download', '[]', '{"timezone":"Asia/Shanghai","locale":"zh-CN"}', '{"seed":"facebook-cindy"}', NULL, NOW(), NOW());

-- ============================
-- 4) accounts
-- ============================
INSERT INTO `accounts`
(`id`, `name`, `platform`, `username`, `status`, `browser_profile_id`, `credential_json`, `metadata_json`, `created_at`)
VALUES
(1, 'TikTok Alice',  'tiktok_mock',        'alice', 'active', 1, '{"username":"alice","password":"123456"}', '{"site":"http://localhost:3001","role":"seed"}', NOW()),
(2, 'TikTok Bob',    'tiktok_mock',        'bob',   'active', 2, '{"username":"bob","password":"123456"}',   '{"site":"http://localhost:3001","role":"seed"}', NOW()),
(3, 'TikTok Cindy',  'tiktok_mock',        'cindy', 'active', 3, '{"username":"cindy","password":"123456"}', '{"site":"http://localhost:3001","role":"seed"}', NOW()),
(4, 'Facebook Alice','facebook_like_mock', 'alice', 'active', 4, '{"username":"alice","password":"123456"}', '{"site":"http://localhost:3000","role":"seed"}', NOW()),
(5, 'Facebook Bob',  'facebook_like_mock', 'bob',   'active', 5, '{"username":"bob","password":"123456"}',   '{"site":"http://localhost:3000","role":"seed"}', NOW()),
(6, 'Facebook Cindy','facebook_like_mock', 'cindy', 'active', 6, '{"username":"cindy","password":"123456"}', '{"site":"http://localhost:3000","role":"seed"}', NOW());

-- ============================
-- 5) task templates
-- ============================
INSERT INTO `task_templates`
(`id`, `name`, `definition_json`, `created_at`)
VALUES
(1, 'TikTok Mock Session Template',
'{
  "steps":[
    {
      "id":"tiktok_session",
      "type":"tiktok_mock_session",
      "data":{
        "label":"执行 TikTok Mock 自动化会话",
        "baseUrl":"http://localhost:3001",
        "username":"alice",
        "password":"123456",
        "minVideos":2,
        "maxVideos":4,
        "minWatchMs":2500,
        "maxWatchMs":7000,
        "minLikes":1,
        "maxLikes":2,
        "minComments":1,
        "maxComments":2,
        "behaviorProfile":"balanced",
        "commentProvider":"deepseek"
      }
    },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[{"source":"tiktok_session","target":"done"}]
}', NOW()),
(2, 'Facebook Login + Like + Comment Template',
'{
  "steps":[
    { "id":"open_login", "type":"open", "data":{"label":"打开登录页","url":"http://localhost:3000/login"} },
    { "id":"wait_login_form", "type":"wait_for_element", "data":{"label":"等待登录表单","selector":"form[action=''/login'']","timeout":10000} },
    { "id":"type_username", "type":"type", "data":{"label":"输入用户名","selector":"input[name=''username'']","value":"alice"} },
    { "id":"type_password", "type":"type", "data":{"label":"输入密码","selector":"input[name=''password'']","value":"123456"} },
    { "id":"click_login", "type":"click", "data":{"label":"点击登录","selector":"button[type=''submit'']"} },
    { "id":"wait_feed", "type":"wait_for_element", "data":{"label":"等待 feed","selector":"[data-testid=''feed-root'']","timeout":10000} },
    { "id":"click_like", "type":"click", "data":{"label":"点赞第一条帖子","selector":"[data-testid=''post-like-button'']"} },
    { "id":"click_comment_toggle", "type":"click", "data":{"label":"展开评论区","selector":"[data-testid=''post-comment-toggle'']"} },
    { "id":"type_comment", "type":"type", "data":{"label":"输入评论","selector":"[data-testid=''post-comment-input'']","value":"这是一条 Facebook mock 自动化评论。"} },
    { "id":"submit_comment", "type":"click", "data":{"label":"提交评论","selector":"[data-testid=''post-comment-submit'']"} },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[
    {"source":"open_login","target":"wait_login_form"},
    {"source":"wait_login_form","target":"type_username"},
    {"source":"type_username","target":"type_password"},
    {"source":"type_password","target":"click_login"},
    {"source":"click_login","target":"wait_feed"},
    {"source":"wait_feed","target":"click_like"},
    {"source":"click_like","target":"click_comment_toggle"},
    {"source":"click_comment_toggle","target":"type_comment"},
    {"source":"type_comment","target":"submit_comment"},
    {"source":"submit_comment","target":"done"}
  ]
}', NOW()),
(3, 'Facebook Login Only Smoke Template',
'{
  "steps":[
    { "id":"open_login", "type":"open", "data":{"label":"打开登录页","url":"http://localhost:3000/login"} },
    { "id":"wait_login_form", "type":"wait_for_element", "data":{"label":"等待登录表单","selector":"form[action=''/login'']","timeout":10000} },
    { "id":"type_username", "type":"type", "data":{"label":"输入用户名","selector":"input[name=''username'']","value":"bob"} },
    { "id":"type_password", "type":"type", "data":{"label":"输入密码","selector":"input[name=''password'']","value":"123456"} },
    { "id":"click_login", "type":"click", "data":{"label":"点击登录","selector":"button[type=''submit'']"} },
    { "id":"wait_feed", "type":"wait_for_element", "data":{"label":"等待 feed","selector":"[data-testid=''feed-root'']","timeout":10000} },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[
    {"source":"open_login","target":"wait_login_form"},
    {"source":"wait_login_form","target":"type_username"},
    {"source":"type_username","target":"type_password"},
    {"source":"type_password","target":"click_login"},
    {"source":"click_login","target":"wait_feed"},
    {"source":"wait_feed","target":"done"}
  ]
}', NOW()),
(4, 'TikTok Manual Browse Template',
'{
  "steps":[
    { "id":"open_login", "type":"open", "data":{"label":"打开登录页","url":"http://localhost:3001/login"} },
    { "id":"wait_login_form", "type":"wait_for_element", "data":{"label":"等待登录表单","selector":"[data-testid=''login-form'']","timeout":10000} },
    { "id":"type_username", "type":"type", "data":{"label":"输入用户名","selector":"[data-testid=''username-input'']","value":"bob"} },
    { "id":"type_password", "type":"type", "data":{"label":"输入密码","selector":"[data-testid=''password-input'']","value":"123456"} },
    { "id":"click_login", "type":"click", "data":{"label":"点击登录","selector":"[data-testid=''login-submit'']"} },
    { "id":"wait_stack", "type":"wait_for_element", "data":{"label":"等待视频列表","selector":"[data-testid=''tt-video-stack'']","timeout":10000} },
    { "id":"click_comment", "type":"click", "data":{"label":"打开评论区","selector":"[data-testid=''tt-comment-toggle'']"} },
    { "id":"type_comment", "type":"type", "data":{"label":"输入评论","selector":"[data-testid=''tt-comment-input'']","value":"这是一条 TikTok mock 自动化评论。"} },
    { "id":"submit_comment", "type":"click", "data":{"label":"提交评论","selector":"[data-testid=''tt-comment-submit'']"} },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[
    {"source":"open_login","target":"wait_login_form"},
    {"source":"wait_login_form","target":"type_username"},
    {"source":"type_username","target":"type_password"},
    {"source":"type_password","target":"click_login"},
    {"source":"click_login","target":"wait_stack"},
    {"source":"wait_stack","target":"click_comment"},
    {"source":"click_comment","target":"type_comment"},
    {"source":"type_comment","target":"submit_comment"},
    {"source":"submit_comment","target":"done"}
  ]
}', NOW());

-- ============================
-- 6) tasks
-- ============================
INSERT INTO `tasks`
(`id`, `name`, `browser_profile_id`, `scheduling_strategy`, `preferred_agent_id`, `status`, `payload_json`, `retry_policy_json`, `priority`, `timeout_seconds`, `created_at`, `account_id`, `is_enabled`, `schedule_type`, `schedule_config_json`, `next_run_at`, `last_run_at`)
VALUES
(1, 'TikTok Alice 闭环测试任务', 1, 'profile_owner', NULL, 'queued',
'{
  "steps":[
    {
      "id":"tiktok_session",
      "type":"tiktok_mock_session",
      "data":{
        "label":"执行 TikTok Mock 自动化会话",
        "baseUrl":"http://localhost:3001",
        "username":"alice",
        "password":"123456",
        "minVideos":2,
        "maxVideos":4,
        "minWatchMs":2500,
        "maxWatchMs":7000,
        "minLikes":1,
        "maxLikes":2,
        "minComments":1,
        "maxComments":2,
        "behaviorProfile":"balanced",
        "commentProvider":"deepseek"
      }
    },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[{"source":"tiktok_session","target":"done"}]
}',
'{"maxRetries":1}', 120, 300, NOW(), 1, 1, 'manual', '{}', NULL, NULL),

(2, 'TikTok Bob 手动浏览评论任务', 2, 'profile_owner', NULL, 'queued',
'{
  "steps":[
    { "id":"open_login", "type":"open", "data":{"label":"打开登录页","url":"http://localhost:3001/login"} },
    { "id":"wait_login_form", "type":"wait_for_element", "data":{"label":"等待登录表单","selector":"[data-testid=''login-form'']","timeout":10000} },
    { "id":"type_username", "type":"type", "data":{"label":"输入用户名","selector":"[data-testid=''username-input'']","value":"bob"} },
    { "id":"type_password", "type":"type", "data":{"label":"输入密码","selector":"[data-testid=''password-input'']","value":"123456"} },
    { "id":"click_login", "type":"click", "data":{"label":"点击登录","selector":"[data-testid=''login-submit'']"} },
    { "id":"wait_stack", "type":"wait_for_element", "data":{"label":"等待视频列表","selector":"[data-testid=''tt-video-stack'']","timeout":10000} },
    { "id":"click_comment", "type":"click", "data":{"label":"打开评论区","selector":"[data-testid=''tt-comment-toggle'']"} },
    { "id":"type_comment", "type":"type", "data":{"label":"输入评论","selector":"[data-testid=''tt-comment-input'']","value":"这是一条 TikTok mock 自动化评论。"} },
    { "id":"submit_comment", "type":"click", "data":{"label":"提交评论","selector":"[data-testid=''tt-comment-submit'']"} },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[
    {"source":"open_login","target":"wait_login_form"},
    {"source":"wait_login_form","target":"type_username"},
    {"source":"type_username","target":"type_password"},
    {"source":"type_password","target":"click_login"},
    {"source":"click_login","target":"wait_stack"},
    {"source":"wait_stack","target":"click_comment"},
    {"source":"click_comment","target":"type_comment"},
    {"source":"type_comment","target":"submit_comment"},
    {"source":"submit_comment","target":"done"}
  ]
}',
'{"maxRetries":1}', 105, 300, NOW(), 2, 1, 'manual', '{}', NULL, NULL),

(3, 'TikTok Cindy 定时随机浏览任务', 3, 'profile_owner', NULL, 'queued',
'{
  "steps":[
    {
      "id":"tiktok_session",
      "type":"tiktok_mock_session",
      "data":{
        "label":"执行 TikTok Mock 自动化会话",
        "baseUrl":"http://localhost:3001",
        "username":"cindy",
        "password":"123456",
        "minVideos":3,
        "maxVideos":5,
        "minWatchMs":3000,
        "maxWatchMs":9000,
        "minLikes":1,
        "maxLikes":3,
        "minComments":0,
        "maxComments":1,
        "behaviorProfile":"balanced",
        "commentProvider":"deepseek"
      }
    },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[{"source":"tiktok_session","target":"done"}]
}',
'{"maxRetries":1}', 100, 300, NOW(), 3, 1, 'daily_window_random',
'{"timezone":"UTC","windowStart":"09:00","windowEnd":"23:00","maxRunsPerDay":2,"randomMinuteStep":15}',
DATE_ADD(NOW(), INTERVAL 10 MINUTE), NULL),

(4, 'Facebook Alice 点赞评论任务', 4, 'profile_owner', NULL, 'queued',
'{
  "steps":[
    { "id":"open_login", "type":"open", "data":{"label":"打开登录页","url":"http://localhost:3000/login"} },
    { "id":"wait_login_form", "type":"wait_for_element", "data":{"label":"等待登录表单","selector":"form[action=''/login'']","timeout":10000} },
    { "id":"type_username", "type":"type", "data":{"label":"输入用户名","selector":"input[name=''username'']","value":"alice"} },
    { "id":"type_password", "type":"type", "data":{"label":"输入密码","selector":"input[name=''password'']","value":"123456"} },
    { "id":"click_login", "type":"click", "data":{"label":"点击登录","selector":"button[type=''submit'']"} },
    { "id":"wait_feed", "type":"wait_for_element", "data":{"label":"等待 feed","selector":"[data-testid=''feed-root'']","timeout":10000} },
    { "id":"click_like", "type":"click", "data":{"label":"点赞第一条帖子","selector":"[data-testid=''post-like-button'']"} },
    { "id":"click_comment_toggle", "type":"click", "data":{"label":"展开评论区","selector":"[data-testid=''post-comment-toggle'']"} },
    { "id":"type_comment", "type":"type", "data":{"label":"输入评论","selector":"[data-testid=''post-comment-input'']","value":"这是一条 Facebook mock 自动化评论。"} },
    { "id":"submit_comment", "type":"click", "data":{"label":"提交评论","selector":"[data-testid=''post-comment-submit'']"} },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[
    {"source":"open_login","target":"wait_login_form"},
    {"source":"wait_login_form","target":"type_username"},
    {"source":"type_username","target":"type_password"},
    {"source":"type_password","target":"click_login"},
    {"source":"click_login","target":"wait_feed"},
    {"source":"wait_feed","target":"click_like"},
    {"source":"click_like","target":"click_comment_toggle"},
    {"source":"click_comment_toggle","target":"type_comment"},
    {"source":"type_comment","target":"submit_comment"},
    {"source":"submit_comment","target":"done"}
  ]
}',
'{"maxRetries":1}', 130, 300, NOW(), 4, 1, 'manual', '{}', NULL, NULL),

(5, 'Facebook Bob 登录冒烟任务', 5, 'profile_owner', NULL, 'queued',
'{
  "steps":[
    { "id":"open_login", "type":"open", "data":{"label":"打开登录页","url":"http://localhost:3000/login"} },
    { "id":"wait_login_form", "type":"wait_for_element", "data":{"label":"等待登录表单","selector":"form[action=''/login'']","timeout":10000} },
    { "id":"type_username", "type":"type", "data":{"label":"输入用户名","selector":"input[name=''username'']","value":"bob"} },
    { "id":"type_password", "type":"type", "data":{"label":"输入密码","selector":"input[name=''password'']","value":"123456"} },
    { "id":"click_login", "type":"click", "data":{"label":"点击登录","selector":"button[type=''submit'']"} },
    { "id":"wait_feed", "type":"wait_for_element", "data":{"label":"等待 feed","selector":"[data-testid=''feed-root'']","timeout":10000} },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[
    {"source":"open_login","target":"wait_login_form"},
    {"source":"wait_login_form","target":"type_username"},
    {"source":"type_username","target":"type_password"},
    {"source":"type_password","target":"click_login"},
    {"source":"click_login","target":"wait_feed"},
    {"source":"wait_feed","target":"done"}
  ]
}',
'{"maxRetries":1}', 95, 300, NOW(), 5, 1, 'manual', '{}', NULL, NULL),

(6, 'Facebook Cindy 定时互动任务', 6, 'profile_owner', NULL, 'queued',
'{
  "steps":[
    { "id":"open_login", "type":"open", "data":{"label":"打开登录页","url":"http://localhost:3000/login"} },
    { "id":"wait_login_form", "type":"wait_for_element", "data":{"label":"等待登录表单","selector":"form[action=''/login'']","timeout":10000} },
    { "id":"type_username", "type":"type", "data":{"label":"输入用户名","selector":"input[name=''username'']","value":"cindy"} },
    { "id":"type_password", "type":"type", "data":{"label":"输入密码","selector":"input[name=''password'']","value":"123456"} },
    { "id":"click_login", "type":"click", "data":{"label":"点击登录","selector":"button[type=''submit'']"} },
    { "id":"wait_feed", "type":"wait_for_element", "data":{"label":"等待 feed","selector":"[data-testid=''feed-root'']","timeout":10000} },
    { "id":"click_comment_toggle", "type":"click", "data":{"label":"展开评论区","selector":"[data-testid=''post-comment-toggle'']"} },
    { "id":"type_comment", "type":"type", "data":{"label":"输入评论","selector":"[data-testid=''post-comment-input'']","value":"Cindy 自动化评论测试。"} },
    { "id":"submit_comment", "type":"click", "data":{"label":"提交评论","selector":"[data-testid=''post-comment-submit'']"} },
    { "id":"done", "type":"end_success", "data":{"label":"完成"} }
  ],
  "edges":[
    {"source":"open_login","target":"wait_login_form"},
    {"source":"wait_login_form","target":"type_username"},
    {"source":"type_username","target":"type_password"},
    {"source":"type_password","target":"click_login"},
    {"source":"click_login","target":"wait_feed"},
    {"source":"wait_feed","target":"click_comment_toggle"},
    {"source":"click_comment_toggle","target":"type_comment"},
    {"source":"type_comment","target":"submit_comment"},
    {"source":"submit_comment","target":"done"}
  ]
}',
'{"maxRetries":1}', 100, 300, NOW(), 6, 1, 'daily_window_random',
'{"timezone":"UTC","windowStart":"10:00","windowEnd":"22:00","maxRunsPerDay":2,"randomMinuteStep":10}',
DATE_ADD(NOW(), INTERVAL 15 MINUTE), NULL);

-- ============================
-- 7) run history
-- ============================
INSERT INTO `task_runs`
(`id`, `task_id`, `browser_profile_id`, `assigned_agent_id`, `lease_token`, `status`, `retry_count`, `max_retries`, `current_step_id`, `current_step_label`, `current_url`, `result_json`, `error_code`, `error_message`, `last_preview_path`, `created_at`, `started_at`, `heartbeat_at`, `finished_at`)
VALUES
(1, 1, 1, 1, '', 'completed', 0, 1, 'done', '完成', 'http://localhost:3001/feed?videoId=103', '{"tiktok_session":{"mode":"tiktok_mock_session","baseUrl":"http://localhost:3001","watchedVideos":3,"likedVideos":2,"commentedVideos":1}}', '', '', '', NOW(), NOW(), NOW(), NOW()),
(2, 2, 2, 1, '', 'completed', 0, 1, 'done', '完成', 'http://localhost:3001/feed?videoId=105', '{"manual_tiktok":{"baseUrl":"http://localhost:3001","commentedVideos":1}}', '', '', '', NOW(), NOW(), NOW(), NOW()),
(3, 4, 4, 1, '', 'completed', 0, 1, 'done', '完成', 'http://localhost:3000/feed', '{"facebook_flow":{"baseUrl":"http://localhost:3000","likedPosts":1,"commentedPosts":1}}', '', '', '', NOW(), NOW(), NOW(), NOW()),
(4, 5, 5, 1, '', 'failed', 1, 1, 'wait_feed', '等待 feed', 'http://localhost:3000/login', '{"ok":false}', 'timeout', 'wait feed timeout', '', NOW(), NOW(), NOW(), NOW()),
(5, 3, 3, NULL, '', 'queued', 0, 1, '', '', '', '{}', '', '', '', NOW(), NULL, NULL, NULL),
(6, 6, 6, NULL, '', 'queued', 0, 1, '', '', '', '{}', '', '', '', NOW(), NULL, NULL, NULL),
(7, 1, 1, 1, '', 'running', 0, 1, 'tiktok_session', '执行 TikTok Mock 自动化会话', 'http://localhost:3001/feed?videoId=104', '{}', '', '', '', NOW(), NOW(), NOW(), NULL);

INSERT INTO `task_run_logs`
(`id`, `task_run_id`, `level`, `step_id`, `message`, `created_at`)
VALUES
(1, 1, 'info', 'tiktok_session', 'Executing tiktok_mock_session', NOW()),
(2, 1, 'info', 'done', 'Run finished with status: completed', NOW()),
(3, 2, 'info', 'open_login', 'Opened TikTok mock login page', NOW()),
(4, 2, 'info', 'done', 'Run finished with status: completed', NOW()),
(5, 3, 'info', 'open_login', 'Opened Facebook-like mock login page', NOW()),
(6, 3, 'info', 'done', 'Run finished with status: completed', NOW()),
(7, 4, 'error', 'wait_feed', 'Wait feed timeout', NOW()),
(8, 7, 'info', 'tiktok_session', 'Run is still executing', NOW());

INSERT INTO `run_isolation_reports`
(`id`, `task_run_id`, `browser_profile_id`, `proxy_snapshot_json`, `fingerprint_snapshot_json`, `storage_check_json`, `network_check_json`, `result`, `created_at`)
VALUES
(1, 1, 1, '{}', '{"name":"Default Chrome"}', '{"ok":true}', '{"ok":true}', 'pass', NOW()),
(2, 3, 4, '{}', '{"name":"Default Chrome"}', '{"ok":true}', '{"ok":true}', 'pass', NOW()),
(3, 4, 5, '{}', '{"name":"Default Chrome"}', '{"ok":false}', '{"ok":true}', 'pass', NOW());

INSERT INTO `browser_artifacts`
(`id`, `task_run_id`, `artifact_type`, `file_path`, `file_name`, `created_at`)
VALUES
(1, 1, 'screenshot', 'data/artifacts/1/final_tiktok.png', 'final_tiktok.png', NOW()),
(2, 3, 'screenshot', 'data/artifacts/3/final_facebook.png', 'final_facebook.png', NOW()),
(3, 7, 'screenshot', 'data/artifacts/7/preview_running.png', 'preview_running.png', NOW());

INSERT INTO `browser_profile_locks`
(`id`, `profile_id`, `task_id`, `task_run_id`, `agent_id`, `lease_token`, `status`, `expires_at`, `released_at`, `release_reason`, `created_at`)
VALUES
(1, 1, 1, 7, 1, 'lease-running-0001', 'leased', DATE_ADD(NOW(), INTERVAL 20 MINUTE), NULL, NULL, NOW());

INSERT INTO `audit_events`
(`id`, `event_type`, `actor_type`, `actor_id`, `target_type`, `target_id`, `details_json`, `created_at`)
VALUES
(1, 'seed_reset', 'system', 'sql_patch', 'task', '1', '{"example":"tiktok-alice"}', NOW()),
(2, 'seed_reset', 'system', 'sql_patch', 'task', '4', '{"example":"facebook-alice"}', NOW()),
(3, 'demo_seed', 'system', 'sql_patch', 'task_run', '1', '{"note":"completed tiktok run"}', NOW()),
(4, 'demo_seed', 'system', 'sql_patch', 'task_run', '3', '{"note":"completed facebook run"}', NOW());

-- ============================
-- 8) auto increment reset
-- ============================
ALTER TABLE `agents` AUTO_INCREMENT = 2;
ALTER TABLE `fingerprint_templates` AUTO_INCREMENT = 3;
ALTER TABLE `proxies` AUTO_INCREMENT = 2;
ALTER TABLE `browser_profiles` AUTO_INCREMENT = 7;
ALTER TABLE `accounts` AUTO_INCREMENT = 7;
ALTER TABLE `task_templates` AUTO_INCREMENT = 5;
ALTER TABLE `tasks` AUTO_INCREMENT = 7;
ALTER TABLE `task_runs` AUTO_INCREMENT = 8;
ALTER TABLE `task_run_logs` AUTO_INCREMENT = 9;
ALTER TABLE `run_isolation_reports` AUTO_INCREMENT = 4;
ALTER TABLE `browser_artifacts` AUTO_INCREMENT = 4;
ALTER TABLE `browser_profile_locks` AUTO_INCREMENT = 2;
ALTER TABLE `audit_events` AUTO_INCREMENT = 5;

SET FOREIGN_KEY_CHECKS = 1;
