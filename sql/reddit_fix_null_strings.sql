-- Hotfix for existing seeded data that may contain NULL in string-mapped columns.
-- Prevents EF/MySql InvalidCastException (DBNull -> string) during scheduler leasing.
--
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_fix_null_strings.sql

SET NAMES utf8mb4;

UPDATE task_runs
SET
  lease_token = COALESCE(lease_token, ''),
  status = COALESCE(status, 'queued'),
  current_step_id = COALESCE(current_step_id, ''),
  current_step_label = COALESCE(current_step_label, ''),
  current_url = COALESCE(current_url, ''),
  result_json = COALESCE(result_json, '{}'),
  error_code = COALESCE(error_code, ''),
  error_message = COALESCE(error_message, ''),
  last_preview_path = COALESCE(last_preview_path, '');

-- Optional inspect
-- SELECT id, lease_token, status, current_step_id, error_code, error_message
-- FROM task_runs
-- WHERE lease_token IS NULL OR status IS NULL OR current_step_id IS NULL
--    OR current_step_label IS NULL OR current_url IS NULL OR result_json IS NULL
--    OR error_code IS NULL OR error_message IS NULL OR last_preview_path IS NULL;
