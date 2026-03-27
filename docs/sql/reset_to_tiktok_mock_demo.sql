SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;
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

INSERT INTO agents
    (agent_key, name, machine_name, status, max_parallel_runs, current_runs, scheduler_tags, last_heartbeat_at, created_at)
SELECT 'agent-local-001', 'Local Agent', 'local-machine', 'online', 2, 0, '["default"]', UTC_TIMESTAMP(), UTC_TIMESTAMP()
WHERE NOT EXISTS (SELECT 1 FROM agents WHERE agent_key = 'agent-local-001');

UPDATE agents
SET status='online', current_runs=0, max_parallel_runs=CASE WHEN max_parallel_runs<=0 THEN 1 ELSE max_parallel_runs END, last_heartbeat_at=UTC_TIMESTAMP()
WHERE agent_key='agent-local-001';

INSERT INTO fingerprint_templates (name, config_json, created_at)
VALUES ('Default Chrome', '{"browser":"chrome","platform":"windows"}', UTC_TIMESTAMP());
SET @fp_id = LAST_INSERT_ID();

INSERT INTO browser_profiles
    (name, owner_agent_id, proxy_id, fingerprint_template_id, status, isolation_level, local_profile_path, storage_root_path, download_root_path, startup_args_json, isolation_policy_json, runtime_meta_json, last_used_at, last_isolation_check_at, created_at)
VALUES
    ('TIKTOK MOCK DEMO PROFILE', NULL, NULL, @fp_id, 'idle', 'standard', '/tmp/bap/tiktok/profile-1', '/tmp/bap/tiktok/storage-1', '/tmp/bap/tiktok/download-1', '[]', '{"timezone":"Asia/Shanghai","locale":"zh-CN"}', '{}', NULL, NULL, UTC_TIMESTAMP());
SET @profile_id = LAST_INSERT_ID();

INSERT INTO tasks
    (name, browser_profile_id, scheduling_strategy, preferred_agent_id, status, payload_json, retry_policy_json, priority, timeout_seconds, created_at)
VALUES
(
    'TIKTOK MOCK RANDOM SESSION',
    @profile_id,
    'least_loaded',
    NULL,
    'queued',
    JSON_OBJECT(
        'steps', JSON_ARRAY(
            JSON_OBJECT('id','tiktok_session','type','tiktok_mock_session','data',JSON_OBJECT(
                'label','执行 TikTok Mock 自动化会话',
                'baseUrl','http://localhost:3001',
                'username','alice',
                'password','123456',
                'minVideos',3,
                'maxVideos',8,
                'minWatchMs',3000,
                'maxWatchMs',9000,
                'minLikes',1,
                'maxLikes',4,
                'minComments',1,
                'maxComments',3
            )),
            JSON_OBJECT('id','done','type','end_success','data',JSON_OBJECT('label','完成'))
        ),
        'edges', JSON_ARRAY(
            JSON_OBJECT('source','tiktok_session','target','done')
        )
    ),
    '{"maxRetries":1}',
    240,
    600,
    UTC_TIMESTAMP()
);
