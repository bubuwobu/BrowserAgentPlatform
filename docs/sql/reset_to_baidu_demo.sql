-- BrowserAgentPlatform - 一键清库并重建“百度搜索”最小可运行数据（MySQL）
-- 使用前请先确认连接的是测试库，不要在生产库直接执行。

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- 1) 清理运行相关数据（保留 users）
TRUNCATE TABLE run_isolation_reports;
TRUNCATE TABLE audit_events;
TRUNCATE TABLE browser_artifacts;
TRUNCATE TABLE task_run_logs;
TRUNCATE TABLE browser_profile_locks;
TRUNCATE TABLE task_runs;
TRUNCATE TABLE tasks;
TRUNCATE TABLE task_templates;
TRUNCATE TABLE agent_commands;
TRUNCATE TABLE browser_profiles;
TRUNCATE TABLE proxies;
TRUNCATE TABLE fingerprint_templates;

SET FOREIGN_KEY_CHECKS = 1;

-- 2) 保证至少有一个在线 agent
INSERT INTO agents
    (agent_key, name, machine_name, status, max_parallel_runs, current_runs, scheduler_tags, last_heartbeat_at, created_at)
SELECT
    'agent-local-001', 'Local Agent', 'local-machine', 'online', 2, 0, '["default"]', UTC_TIMESTAMP(), UTC_TIMESTAMP()
WHERE NOT EXISTS (SELECT 1 FROM agents WHERE agent_key = 'agent-local-001');

UPDATE agents
SET status = 'online',
    current_runs = 0,
    max_parallel_runs = CASE WHEN max_parallel_runs <= 0 THEN 1 ELSE max_parallel_runs END,
    last_heartbeat_at = UTC_TIMESTAMP()
WHERE agent_key = 'agent-local-001';

-- 3) 指纹模板 + 浏览器 profile（最小可运行）
INSERT INTO fingerprint_templates (name, config_json, created_at)
VALUES ('Default Chrome', '{"browser":"chrome","platform":"windows"}', UTC_TIMESTAMP());

SET @fp_id = LAST_INSERT_ID();

INSERT INTO browser_profiles
    (name, owner_agent_id, proxy_id, fingerprint_template_id, status, isolation_level, local_profile_path, storage_root_path, download_root_path, startup_args_json, isolation_policy_json, runtime_meta_json, last_used_at, last_isolation_check_at, created_at)
VALUES
    ('BAIDU DEMO PROFILE', NULL, NULL, @fp_id, 'idle', 'standard', '/tmp/bap/baidu/profile-1', '/tmp/bap/baidu/storage-1', '/tmp/bap/baidu/download-1', '[]', '{"timezone":"Asia/Shanghai","locale":"zh-CN"}', '{}', NULL, NULL, UTC_TIMESTAMP());

SET @profile_id = LAST_INSERT_ID();

-- 4) 百度搜索任务（队列任务，调度策略 least_loaded，避免 owner 绑定问题）
INSERT INTO tasks
    (name, browser_profile_id, scheduling_strategy, preferred_agent_id, status, payload_json, retry_policy_json, priority, timeout_seconds, created_at)
VALUES
(
    'BAIDU SEARCH DEMO',
    @profile_id,
    'least_loaded',
    NULL,
    'queued',
    JSON_OBJECT(
        'steps', JSON_ARRAY(
            JSON_OBJECT('id','step_open','type','open','data',JSON_OBJECT('label','打开百度首页','url','https://www.baidu.com')),
            JSON_OBJECT('id','step_wait_input','type','wait_for_element','data',JSON_OBJECT('label','等待搜索框','selector','#kw','timeout',15000)),
            JSON_OBJECT('id','step_type_keyword','type','type','data',JSON_OBJECT('label','输入关键词','selector','#kw','value','BrowserAgentPlatform 自动化测试')),
            JSON_OBJECT('id','step_click_search','type','click','data',JSON_OBJECT('label','点击搜索','selector','#su')),
            JSON_OBJECT('id','step_wait_result','type','wait_for_element','data',JSON_OBJECT('label','等待结果区域','selector','#content_left','timeout',15000)),
            JSON_OBJECT('id','step_extract_title','type','extract_text','data',JSON_OBJECT('label','提取首条标题','selector','#content_left h3')),
            JSON_OBJECT('id','step_done','type','end_success','data',JSON_OBJECT('label','完成'))
        ),
        'edges', JSON_ARRAY(
            JSON_OBJECT('source','step_open','target','step_wait_input'),
            JSON_OBJECT('source','step_wait_input','target','step_type_keyword'),
            JSON_OBJECT('source','step_type_keyword','target','step_click_search'),
            JSON_OBJECT('source','step_click_search','target','step_wait_result'),
            JSON_OBJECT('source','step_wait_result','target','step_extract_title'),
            JSON_OBJECT('source','step_extract_title','target','step_done')
        )
    ),
    '{"maxRetries":1}',
    220,
    300,
    UTC_TIMESTAMP()
);

-- 5) 可选：清理历史异常 NULL，防止旧数据触发 string cast 问题
UPDATE agents SET name = COALESCE(name, ''), machine_name = COALESCE(machine_name, ''), status = COALESCE(status, 'offline');
UPDATE browser_profiles
SET name = COALESCE(name, ''),
    status = COALESCE(status, 'idle'),
    isolation_level = COALESCE(isolation_level, 'standard'),
    local_profile_path = COALESCE(local_profile_path, ''),
    storage_root_path = COALESCE(storage_root_path, ''),
    download_root_path = COALESCE(download_root_path, ''),
    startup_args_json = COALESCE(startup_args_json, '[]'),
    isolation_policy_json = COALESCE(isolation_policy_json, '{}'),
    runtime_meta_json = COALESCE(runtime_meta_json, '{}');
UPDATE tasks
SET name = COALESCE(name, ''),
    scheduling_strategy = COALESCE(scheduling_strategy, 'least_loaded'),
    status = COALESCE(status, 'queued'),
    payload_json = COALESCE(payload_json, '{}'),
    retry_policy_json = COALESCE(retry_policy_json, '{"maxRetries":1}');
UPDATE task_runs
SET lease_token = COALESCE(lease_token, ''),
    status = COALESCE(status, 'queued'),
    current_step_id = COALESCE(current_step_id, ''),
    current_step_label = COALESCE(current_step_label, ''),
    current_url = COALESCE(current_url, ''),
    result_json = COALESCE(result_json, '{}'),
    error_code = COALESCE(error_code, ''),
    error_message = COALESCE(error_message, ''),
    last_preview_path = COALESCE(last_preview_path, '');

-- 6) 验证数据
SELECT id, name, status, browser_profile_id, scheduling_strategy, priority, created_at
FROM tasks
ORDER BY id DESC
LIMIT 10;
