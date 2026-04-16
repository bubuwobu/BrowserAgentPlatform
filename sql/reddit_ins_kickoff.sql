-- Force kickoff for Reddit + Instagram bootstrap/browse tasks
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_ins_kickoff.sql

SET NAMES utf8mb4;
START TRANSACTION;

-- 1) Put key tasks into queued+enabled so queue scanner can pick them
UPDATE tasks
SET status='queued', is_enabled=1, schedule_type='manual', schedule_config_json='{}'
WHERE name IN (
  'Reddit Cookie Bootstrap Task',
  'Reddit Night Random Browse 1H Task',
  'Instagram Cookie Bootstrap Task',
  'Instagram Auto Browse Task',
  'Instagram Night Random Browse 1H Task'
);

-- 2) If no active run exists for a task, enqueue one run directly
INSERT INTO task_runs (`task_id`,`browser_profile_id`,`status`,`retry_count`,`max_retries`,`created_at`,`current_step_id`,`current_step_label`,`current_url`,`result_json`,`error_code`,`error_message`,`last_preview_path`,`lease_token`)
SELECT t.id, t.browser_profile_id, 'queued', 0, 1, NOW(), '', '', '', '{}', '', '', '', ''
FROM tasks t
WHERE t.name IN (
  'Reddit Cookie Bootstrap Task',
  'Instagram Cookie Bootstrap Task'
)
AND NOT EXISTS (
  SELECT 1 FROM task_runs r
  WHERE r.task_id = t.id AND r.status IN ('queued','leased','running')
);

COMMIT;
