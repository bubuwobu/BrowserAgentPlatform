UPDATE tasks SET name = '' WHERE name IS NULL;
UPDATE tasks SET scheduling_strategy = 'least_loaded' WHERE scheduling_strategy IS NULL;
UPDATE tasks SET status = 'queued' WHERE status IS NULL;
UPDATE tasks SET payload_json = '{}' WHERE payload_json IS NULL;
UPDATE tasks SET retry_policy_json = '{"maxRetries":1}' WHERE retry_policy_json IS NULL;

UPDATE task_runs SET lease_token = '' WHERE lease_token IS NULL;
UPDATE task_runs SET status = 'queued' WHERE status IS NULL;
UPDATE task_runs SET current_step_id = '' WHERE current_step_id IS NULL;
UPDATE task_runs SET current_step_label = '' WHERE current_step_label IS NULL;
UPDATE task_runs SET current_url = '' WHERE current_url IS NULL;
UPDATE task_runs SET result_json = '{}' WHERE result_json IS NULL;
UPDATE task_runs SET error_code = '' WHERE error_code IS NULL;
UPDATE task_runs SET error_message = '' WHERE error_message IS NULL;
UPDATE task_runs SET last_preview_path = '' WHERE last_preview_path IS NULL;

UPDATE browser_profile_locks SET lease_token = '' WHERE lease_token IS NULL;
UPDATE browser_profile_locks SET status = 'reserved' WHERE status IS NULL;

UPDATE browser_profiles SET name = '' WHERE name IS NULL;
UPDATE browser_profiles SET status = 'idle' WHERE status IS NULL;
UPDATE browser_profiles SET isolation_level = 'strict' WHERE isolation_level IS NULL;
UPDATE browser_profiles SET local_profile_path = '' WHERE local_profile_path IS NULL;
UPDATE browser_profiles SET storage_root_path = '' WHERE storage_root_path IS NULL;
UPDATE browser_profiles SET download_root_path = '' WHERE download_root_path IS NULL;
UPDATE browser_profiles SET startup_args_json = '[]' WHERE startup_args_json IS NULL;
UPDATE browser_profiles SET isolation_policy_json = '{}' WHERE isolation_policy_json IS NULL;
UPDATE browser_profiles SET runtime_meta_json = '{}' WHERE runtime_meta_json IS NULL;

UPDATE agents SET agent_key = '' WHERE agent_key IS NULL;
UPDATE agents SET name = '' WHERE name IS NULL;
UPDATE agents SET machine_name = '' WHERE machine_name IS NULL;
UPDATE agents SET status = 'offline' WHERE status IS NULL;
UPDATE agents SET scheduler_tags = '' WHERE scheduler_tags IS NULL;
