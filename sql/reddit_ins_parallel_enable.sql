-- Enable parallel Reddit + Instagram execution
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_ins_parallel_enable.sql

SET NAMES utf8mb4;
START TRANSACTION;

-- Pick the profile currently used by Reddit first (task -> account -> fallback first profile)
SET @reddit_profile_id := (
  SELECT t.browser_profile_id
  FROM tasks t
  WHERE t.name LIKE 'Reddit %' AND t.browser_profile_id IS NOT NULL
  ORDER BY t.id DESC
  LIMIT 1
);
SET @reddit_profile_id := COALESCE(
  @reddit_profile_id,
  (
    SELECT a.browser_profile_id
    FROM accounts a
    WHERE a.platform = 'reddit' AND a.browser_profile_id IS NOT NULL
    ORDER BY a.id DESC
    LIMIT 1
  ),
  (SELECT id FROM browser_profiles ORDER BY id LIMIT 1)
);

-- Prefer an existing Instagram profile if available; otherwise pick a different one
SET @ins_profile_id := (
  SELECT t.browser_profile_id
  FROM tasks t
  WHERE t.name LIKE 'Instagram %' AND t.browser_profile_id IS NOT NULL AND t.browser_profile_id <> @reddit_profile_id
  ORDER BY t.id DESC
  LIMIT 1
);
SET @ins_profile_id := COALESCE(
  @ins_profile_id,
  (
    SELECT a.browser_profile_id
    FROM accounts a
    WHERE a.platform = 'instagram' AND a.browser_profile_id IS NOT NULL AND a.browser_profile_id <> @reddit_profile_id
    ORDER BY a.id DESC
    LIMIT 1
  ),
  (SELECT id FROM browser_profiles WHERE id <> @reddit_profile_id ORDER BY id LIMIT 1)
);

-- If there is no second profile, clone one from Reddit profile for Instagram
INSERT INTO browser_profiles (
  `name`,`owner_agent_id`,`proxy_id`,`fingerprint_template_id`,`status`,`isolation_level`,
  `local_profile_path`,`storage_root_path`,`download_root_path`,`startup_args_json`,`isolation_policy_json`,`runtime_meta_json`,
  `workspace_key`,`profile_root_path`,`artifact_root_path`,`temp_root_path`,`lifecycle_state`,`last_used_at`,`last_isolation_check_at`,`last_started_at`,`last_stopped_at`,`last_rebuild_at`,`created_at`
)
SELECT
  CONCAT(bp.name, ' - INS'), bp.owner_agent_id, bp.proxy_id, bp.fingerprint_template_id, 'idle', bp.isolation_level,
  CONCAT(bp.local_profile_path, '_ins'), CONCAT(bp.storage_root_path, '_ins'), CONCAT(bp.download_root_path, '_ins'),
  bp.startup_args_json, bp.isolation_policy_json, bp.runtime_meta_json,
  CONCAT(bp.workspace_key, '_ins_', UNIX_TIMESTAMP()), CONCAT(bp.profile_root_path, '_ins'), CONCAT(bp.artifact_root_path, '_ins'), CONCAT(bp.temp_root_path, '_ins'),
  'created', NULL, NULL, NULL, NULL, NULL, NOW()
FROM browser_profiles bp
WHERE bp.id = @reddit_profile_id
  AND @ins_profile_id IS NULL;

SET @ins_profile_id := COALESCE(@ins_profile_id, NULLIF(LAST_INSERT_ID(), 0));
SET @ins_profile_id := COALESCE(@ins_profile_id, @reddit_profile_id);

-- Bind platform accounts and tasks to dedicated profiles
UPDATE accounts SET browser_profile_id = @reddit_profile_id WHERE platform='reddit';
UPDATE accounts SET browser_profile_id = @ins_profile_id WHERE platform='instagram';

UPDATE tasks SET browser_profile_id = @reddit_profile_id WHERE name LIKE 'Reddit %';
UPDATE tasks SET browser_profile_id = @ins_profile_id WHERE name LIKE 'Instagram %';

-- Ensure both platforms are runnable
UPDATE tasks
SET status='queued', is_enabled=1, schedule_type='manual', schedule_config_json='{}'
WHERE name IN (
  'Reddit Cookie Bootstrap Task',
  'Reddit Night Random Browse 1H Task',
  'Instagram Cookie Bootstrap Task',
  'Instagram Auto Browse Task',
  'Instagram Night Random Browse 1H Task'
);

-- Enqueue bootstrap runs for both platforms if missing
INSERT INTO task_runs (`task_id`,`browser_profile_id`,`status`,`retry_count`,`max_retries`,`created_at`,`current_step_id`,`current_step_label`,`current_url`,`result_json`,`error_code`,`error_message`,`last_preview_path`,`lease_token`)
SELECT t.id, t.browser_profile_id, 'queued', 0, 1, NOW(), '', '', '', '{}', '', '', '', ''
FROM tasks t
WHERE t.name IN ('Reddit Cookie Bootstrap Task','Instagram Cookie Bootstrap Task')
AND NOT EXISTS (
  SELECT 1 FROM task_runs r
  WHERE r.task_id = t.id AND r.status IN ('queued','leased','running')
);

COMMIT;
