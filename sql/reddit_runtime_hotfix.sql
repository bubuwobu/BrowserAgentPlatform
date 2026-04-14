-- Runtime hotfix for existing Reddit tasks/templates
-- 1) Move entry page to /r/popular
-- 2) Normalize cookie bootstrap to use url-scoped cookie injection
-- Usage:
--   mysql -u<user> -p<password> <database_name> < sql/reddit_runtime_hotfix.sql

SET NAMES utf8mb4;
START TRANSACTION;

UPDATE task_templates
SET definition_json = REPLACE(definition_json, 'https://old.reddit.com/', 'https://www.reddit.com/r/popular/')
WHERE name LIKE 'Reddit%'
   OR definition_json LIKE '%old.reddit.com%';

UPDATE tasks
SET payload_json = REPLACE(payload_json, 'https://old.reddit.com/', 'https://www.reddit.com/r/popular/')
WHERE name LIKE 'Reddit%'
   OR payload_json LIKE '%old.reddit.com%';

UPDATE task_templates
SET definition_json = REPLACE(definition_json, '"url": "https://www.reddit.com/"', '"url": "https://www.reddit.com/r/popular/"')
WHERE name LIKE 'Reddit%'
   OR definition_json LIKE '%"id": "open_home"%';

UPDATE tasks
SET payload_json = REPLACE(payload_json, '"url": "https://www.reddit.com/"', '"url": "https://www.reddit.com/r/popular/"')
WHERE name LIKE 'Reddit%'
   OR payload_json LIKE '%"id": "open_home"%';

UPDATE task_templates
SET definition_json = REPLACE(
    REPLACE(definition_json, '"domain": ".reddit.com",\n            "path": "/",', '"url": "https://www.reddit.com",'),
    '"domain": ".reddit.com",\n      "path": "/",',
    '"url": "https://www.reddit.com",'
)
WHERE name IN ('Reddit Cookie Bootstrap Template')
   OR definition_json LIKE '%"name": "reddit_session"%';

UPDATE tasks
SET payload_json = REPLACE(
    REPLACE(payload_json, '"domain": ".reddit.com",\n            "path": "/",', '"url": "https://www.reddit.com",'),
    '"domain": ".reddit.com",\n      "path": "/",',
    '"url": "https://www.reddit.com",'
)
WHERE name LIKE 'Reddit%'
  AND payload_json LIKE '%"name": "reddit_session"%';

COMMIT;
