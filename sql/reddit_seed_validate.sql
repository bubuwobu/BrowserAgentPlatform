-- Validate Reddit seed readiness
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_seed_validate.sql

SELECT 'agents' AS check_name, COUNT(*) AS cnt FROM agents;
SELECT 'browser_profiles' AS check_name, COUNT(*) AS cnt FROM browser_profiles;
SELECT 'reddit_accounts' AS check_name, COUNT(*) AS cnt FROM accounts WHERE platform='reddit';
SELECT 'reddit_templates' AS check_name, COUNT(*) AS cnt FROM task_templates WHERE name LIKE 'Reddit%';
SELECT 'reddit_tasks' AS check_name, COUNT(*) AS cnt FROM tasks WHERE name LIKE 'Reddit%';

-- Ensure cookie placeholder exists (before running reddit_set_session.sql)
SELECT 'placeholder_in_template' AS check_name, COUNT(*) AS cnt
FROM task_templates
WHERE definition_json LIKE '%<replace-reddit_session>%';

-- Ensure cookie value already injected (after running reddit_set_session.sql)
SELECT 'session_injected_template' AS check_name, COUNT(*) AS cnt
FROM task_templates
WHERE definition_json LIKE '%reddit_session%'
  AND definition_json NOT LIKE '%<replace-reddit_session>%';

-- Queued tasks that can be picked by agent
SELECT id, name, status, browser_profile_id, is_enabled, schedule_type
FROM tasks
WHERE name LIKE 'Reddit%'
ORDER BY id;

SELECT
  'queued_night_random_task' AS check_name,
  COUNT(*) AS cnt
FROM tasks
WHERE name = 'Reddit Night Random Browse 1H Task'
  AND status = 'queued'
  AND is_enabled = 1;

SELECT
  'unexpected_queued_short_browse' AS check_name,
  COUNT(*) AS cnt
FROM tasks
WHERE name = 'Reddit Auto Browse Task'
  AND status = 'queued';

-- Recent runs/logs
SELECT id, task_id, status, current_step_id, created_at, finished_at
FROM task_runs
ORDER BY id DESC
LIMIT 10;

SELECT id, task_run_id, level, step_id, message, created_at
FROM task_run_logs
ORDER BY id DESC
LIMIT 20;
