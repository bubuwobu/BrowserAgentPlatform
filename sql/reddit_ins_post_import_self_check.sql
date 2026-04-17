-- Post-import self-check checklist for Reddit + Instagram random-like seed.
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_ins_post_import_self_check.sql

SET NAMES utf8mb4;

SELECT '01_agents_heartbeat' AS check_item;
SELECT
  a.id,
  a.agent_key,
  a.status,
  a.last_heartbeat_at,
  CASE
    WHEN a.last_heartbeat_at IS NULL THEN 'missing_heartbeat'
    WHEN TIMESTAMPDIFF(SECOND, a.last_heartbeat_at, NOW()) <= 120 THEN 'fresh_heartbeat'
    ELSE 'stale_heartbeat'
  END AS heartbeat_health
FROM agents a
ORDER BY a.id;

SELECT '02_profiles_for_reddit_instagram' AS check_item;
SELECT
  p.id,
  p.name,
  p.status,
  COUNT(CASE WHEN acc.platform='reddit' THEN 1 END) AS reddit_accounts,
  COUNT(CASE WHEN acc.platform='instagram' THEN 1 END) AS instagram_accounts
FROM browser_profiles p
LEFT JOIN accounts acc ON acc.browser_profile_id = p.id
GROUP BY p.id, p.name, p.status
ORDER BY p.id;

SELECT '03_target_tasks_status' AS check_item;
SELECT
  t.id,
  t.name,
  t.status,
  t.is_enabled,
  t.schedule_type,
  t.timeout_seconds,
  t.browser_profile_id,
  t.account_id
FROM tasks t
WHERE t.name IN (
  'Reddit Cookie Bootstrap Task',
  'Instagram Cookie Bootstrap Task',
  'Reddit Night Random Like 1H Task',
  'Instagram Night Random Like 1H Task'
)
ORDER BY t.id;

SELECT '04_task_runs_status_summary' AS check_item;
SELECT
  r.status,
  COUNT(*) AS cnt
FROM task_runs r
GROUP BY r.status
ORDER BY cnt DESC;

SELECT '05_latest_target_runs' AS check_item;
SELECT
  r.id,
  t.name AS task_name,
  r.status,
  r.current_step_id,
  r.current_step_label,
  r.current_url,
  r.started_at,
  r.finished_at,
  r.updated_at
FROM task_runs r
JOIN tasks t ON t.id = r.task_id
WHERE t.name IN (
  'Reddit Cookie Bootstrap Task',
  'Instagram Cookie Bootstrap Task',
  'Reddit Night Random Like 1H Task',
  'Instagram Night Random Like 1H Task'
)
ORDER BY r.id DESC
LIMIT 20;

SELECT '06_failed_runs_and_errors' AS check_item;
SELECT
  r.id,
  t.name AS task_name,
  r.status,
  r.error_code,
  r.error_message,
  r.updated_at
FROM task_runs r
JOIN tasks t ON t.id = r.task_id
WHERE r.status = 'failed'
ORDER BY r.id DESC
LIMIT 20;

SELECT '07_recent_run_logs' AS check_item;
SELECT
  l.id,
  l.task_run_id,
  l.level,
  l.message,
  l.created_at
FROM task_run_logs l
ORDER BY l.id DESC
LIMIT 50;

SELECT '08_quick_diagnosis' AS check_item;
SELECT
  CASE
    WHEN NOT EXISTS (
      SELECT 1 FROM agents a
      WHERE a.last_heartbeat_at IS NOT NULL
        AND TIMESTAMPDIFF(SECOND, a.last_heartbeat_at, NOW()) <= 120
    ) THEN 'NO_FRESH_AGENT_HEARTBEAT'
    WHEN NOT EXISTS (
      SELECT 1 FROM task_runs r WHERE r.status IN ('queued', 'leased', 'running', 'completed', 'failed')
    ) THEN 'NO_TASK_RUNS_CREATED'
    WHEN EXISTS (
      SELECT 1 FROM task_runs r WHERE r.status='failed'
    ) THEN 'RUNS_HAVE_FAILURES_CHECK_SECTION_06'
    ELSE 'PIPELINE_LOOKS_ALIVE'
  END AS diagnosis;
