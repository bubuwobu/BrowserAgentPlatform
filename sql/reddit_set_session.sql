-- One-shot update script: inject reddit_session into seeded Reddit template/task JSON.
-- Usage:
--   1) Edit @reddit_session value below.
--   2) Run: mysql -u<user> -p<password> <database_name> < sql/reddit_set_session.sql

SET NAMES utf8mb4;

-- TODO: replace this value before running.
SET @reddit_session := 'REPLACE_ME_WITH_REAL_REDDIT_SESSION';

START TRANSACTION;

-- Update template payload placeholders.
UPDATE task_templates
SET template_json = REPLACE(
    REPLACE(template_json, '<replace-reddit_session>', @reddit_session),
    '<replace-with-your-cookie-value>',
    @reddit_session
)
WHERE name IN (
  'Reddit Cookie Bootstrap Template',
  'Reddit Browse Only Template',
  'Reddit Public JSON API Template'
)
OR template_json LIKE '%<replace-reddit_session>%'
OR template_json LIKE '%<replace-with-your-cookie-value>%';

-- Update runnable task payload placeholders.
UPDATE tasks
SET payload_json = REPLACE(
    REPLACE(payload_json, '<replace-reddit_session>', @reddit_session),
    '<replace-with-your-cookie-value>',
    @reddit_session
)
WHERE name LIKE 'Reddit%'
  OR payload_json LIKE '%<replace-reddit_session>%'
  OR payload_json LIKE '%<replace-with-your-cookie-value>%';

COMMIT;

-- Verify
-- SELECT id, name FROM task_templates WHERE template_json LIKE CONCAT('%', @reddit_session, '%');
-- SELECT id, name, status FROM tasks WHERE payload_json LIKE CONCAT('%', @reddit_session, '%');
