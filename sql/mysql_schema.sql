CREATE TABLE IF NOT EXISTS users (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  username VARCHAR(64) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  display_name VARCHAR(128) NOT NULL,
  role VARCHAR(32) NOT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS agents (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  agent_key VARCHAR(128) NOT NULL UNIQUE,
  name VARCHAR(128) NOT NULL,
  machine_name VARCHAR(128) NOT NULL,
  status VARCHAR(32) NOT NULL,
  max_parallel_runs INT NOT NULL,
  current_runs INT NOT NULL,
  scheduler_tags VARCHAR(255) NOT NULL,
  last_heartbeat_at DATETIME NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS proxy_configs (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(128) NOT NULL,
  protocol VARCHAR(16) NOT NULL,
  host VARCHAR(255) NOT NULL,
  port INT NOT NULL,
  username VARCHAR(128) NOT NULL,
  password VARCHAR(128) NOT NULL,
  notes TEXT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS fingerprint_templates (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(128) NOT NULL,
  config_json LONGTEXT NOT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS browser_profiles (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(128) NOT NULL,
  owner_agent_id BIGINT NULL,
  proxy_id BIGINT NULL,
  fingerprint_template_id BIGINT NULL,
  status VARCHAR(32) NOT NULL,
  local_profile_path VARCHAR(255) NOT NULL,
  startup_args_json LONGTEXT NOT NULL,
  runtime_meta_json LONGTEXT NOT NULL,
  last_used_at DATETIME NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS browser_profile_locks (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  profile_id BIGINT NOT NULL,
  task_id BIGINT NULL,
  task_run_id BIGINT NULL,
  agent_id BIGINT NULL,
  lease_token VARCHAR(64) NOT NULL,
  status VARCHAR(32) NOT NULL,
  expires_at DATETIME NOT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS task_templates (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(128) NOT NULL,
  definition_json LONGTEXT NOT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS tasks (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(128) NOT NULL,
  browser_profile_id BIGINT NOT NULL,
  scheduling_strategy VARCHAR(32) NOT NULL,
  preferred_agent_id BIGINT NULL,
  status VARCHAR(32) NOT NULL,
  payload_json LONGTEXT NOT NULL,
  priority INT NOT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS task_runs (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  task_id BIGINT NOT NULL,
  browser_profile_id BIGINT NOT NULL,
  assigned_agent_id BIGINT NULL,
  status VARCHAR(32) NOT NULL,
  current_step_id VARCHAR(64) NOT NULL,
  current_step_label VARCHAR(255) NOT NULL,
  current_url VARCHAR(1024) NOT NULL,
  result_json LONGTEXT NOT NULL,
  last_preview_path VARCHAR(255) NOT NULL,
  created_at DATETIME NOT NULL,
  started_at DATETIME NULL,
  finished_at DATETIME NULL
);

CREATE TABLE IF NOT EXISTS task_run_logs (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  task_run_id BIGINT NOT NULL,
  level VARCHAR(16) NOT NULL,
  step_id VARCHAR(64) NOT NULL,
  message TEXT NOT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS browser_artifacts (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  task_run_id BIGINT NOT NULL,
  artifact_type VARCHAR(32) NOT NULL,
  file_path VARCHAR(255) NOT NULL,
  file_name VARCHAR(255) NOT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS agent_commands (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  agent_id BIGINT NOT NULL,
  profile_id BIGINT NULL,
  command_type VARCHAR(64) NOT NULL,
  payload_json LONGTEXT NOT NULL,
  status VARCHAR(32) NOT NULL,
  created_at DATETIME NOT NULL
);
