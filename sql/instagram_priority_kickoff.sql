-- Force Instagram-first execution when Reddit and Instagram share one profile
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/instagram_priority_kickoff.sql

SET NAMES utf8mb4;
START TRANSACTION;

-- 1) Pause Reddit tasks so they won't keep generating queued runs
UPDATE tasks
SET is_enabled = 0,
    status = 'completed'
WHERE name IN (
  'Reddit Cookie Bootstrap Task',
  'Reddit Auto Browse Task',
  'Reddit Night Random Browse 1H Task'
);

-- 2) Mark active/queued Reddit runs as failed to unblock profile lock
UPDATE task_runs r
JOIN tasks t ON t.id = r.task_id
SET r.status = 'failed',
    r.error_code = 'manual_pause_reddit',
    r.error_message = 'Paused Reddit runs to prioritize Instagram startup',
    r.finished_at = NOW()
WHERE t.name LIKE 'Reddit %'
  AND r.status IN ('queued','leased','running');

-- 3) Release related profile locks
UPDATE browser_profile_locks l
JOIN task_runs r ON r.id = l.task_run_id
JOIN tasks t ON t.id = r.task_id
SET l.status = 'released'
WHERE t.name LIKE 'Reddit %'
  AND l.status IN ('reserved','leased');

-- 4) Enable and queue Instagram tasks
UPDATE tasks
SET is_enabled = 1,
    status = 'queued',
    schedule_type = 'manual',
    schedule_config_json = '{}'
WHERE name IN (
  'Instagram Cookie Bootstrap Task',
  'Instagram Auto Browse Task',
  'Instagram Night Random Browse 1H Task'
);

-- 5) Ensure there is at least one queued bootstrap run for Instagram
INSERT INTO task_runs (`task_id`,`browser_profile_id`,`status`,`retry_count`,`max_retries`,`created_at`,`current_step_id`,`current_step_label`,`current_url`,`result_json`,`error_code`,`error_message`,`last_preview_path`,`lease_token`)
SELECT t.id, t.browser_profile_id, 'queued', 0, 1, NOW(), '', '', '', '{}', '', '', '', ''
FROM tasks t
WHERE t.name = 'Instagram Cookie Bootstrap Task'
AND NOT EXISTS (
  SELECT 1 FROM task_runs r
  WHERE r.task_id = t.id AND r.status IN ('queued','leased','running')
);

COMMIT;
