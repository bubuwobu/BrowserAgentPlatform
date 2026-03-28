ALTER TABLE tasks
  ADD COLUMN account_id BIGINT NULL AFTER browser_profile_id,
  ADD COLUMN is_enabled TINYINT(1) NOT NULL DEFAULT 1 AFTER status,
  ADD COLUMN schedule_type VARCHAR(64) NOT NULL DEFAULT 'manual' AFTER is_enabled,
  ADD COLUMN schedule_config_json LONGTEXT NULL AFTER schedule_type,
  ADD COLUMN next_run_at DATETIME NULL AFTER schedule_config_json,
  ADD COLUMN last_run_at DATETIME NULL AFTER next_run_at;

CREATE TABLE IF NOT EXISTS accounts (
  id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  platform VARCHAR(64) NOT NULL DEFAULT 'generic',
  username VARCHAR(255) NOT NULL DEFAULT '',
  status VARCHAR(64) NOT NULL DEFAULT 'active',
  browser_profile_id BIGINT NULL,
  credential_json LONGTEXT NULL,
  metadata_json LONGTEXT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX ix_accounts_browser_profile_id ON accounts(browser_profile_id);
CREATE INDEX ix_tasks_next_run_at ON tasks(is_enabled, schedule_type, next_run_at);
