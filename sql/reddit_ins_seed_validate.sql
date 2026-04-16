-- Validate Reddit + Instagram seed readiness
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_ins_seed_validate.sql

SELECT 'profiles_total' AS check_name, COUNT(*) AS cnt FROM browser_profiles;
SELECT 'agents_online' AS check_name, COUNT(*) AS cnt FROM agents WHERE status='online';

SELECT 'reddit_tasks' AS check_name, COUNT(*) AS cnt
FROM tasks
WHERE name IN ('Reddit Cookie Bootstrap Task','Reddit Auto Browse Task','Reddit Night Random Browse 1H Task');

SELECT 'instagram_tasks' AS check_name, COUNT(*) AS cnt
FROM tasks
WHERE name IN ('Instagram Cookie Bootstrap Task','Instagram Auto Browse Task','Instagram Night Random Browse 1H Task');

SELECT 'queued_runs' AS check_name, COUNT(*) AS cnt
FROM task_runs WHERE status='queued';

SELECT id, name, status, is_enabled, schedule_type, browser_profile_id, timeout_seconds, created_at, last_run_at
FROM tasks
WHERE name LIKE 'Reddit %' OR name LIKE 'Instagram %'
ORDER BY id DESC;

-- Check whether Reddit and Instagram are sharing the same profile
SELECT platform, browser_profile_id, COUNT(*) AS account_cnt
FROM accounts
WHERE platform IN ('reddit','instagram')
GROUP BY platform, browser_profile_id
ORDER BY browser_profile_id, platform;

SELECT id, task_id, status, assigned_agent_id, current_step_id, heartbeat_at, created_at
FROM task_runs
ORDER BY id DESC
LIMIT 20;

SELECT id, agent_key, status, current_runs, max_parallel_runs, last_heartbeat_at
FROM agents
ORDER BY id;
