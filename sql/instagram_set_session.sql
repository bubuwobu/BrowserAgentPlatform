-- One-shot update script: inject instagram sessionid into templates/tasks
-- Usage:
--   1) Edit @instagram_sessionid below.
--   2) Run: mysql -u<user> -p<password> <database_name> < sql/instagram_set_session.sql

SET NAMES utf8mb4;
SET @instagram_sessionid := 'REPLACE_ME_WITH_REAL_INSTAGRAM_SESSIONID';

START TRANSACTION;

UPDATE task_templates
SET definition_json = REPLACE(
    REPLACE(
        REPLACE(definition_json, '<replace-instagram_sessionid>', @instagram_sessionid),
        '<replace-instagram-sessionid>',
        @instagram_sessionid
    ),
    '<replace-with-your-instagram-sessionid>',
    @instagram_sessionid
)
WHERE name LIKE 'Instagram%'
   OR definition_json LIKE '%<replace-instagram_sessionid>%'
   OR definition_json LIKE '%<replace-instagram-sessionid>%'
   OR definition_json LIKE '%<replace-with-your-instagram-sessionid>%';

UPDATE tasks
SET payload_json = REPLACE(
    REPLACE(
        REPLACE(payload_json, '<replace-instagram_sessionid>', @instagram_sessionid),
        '<replace-instagram-sessionid>',
        @instagram_sessionid
    ),
    '<replace-with-your-instagram-sessionid>',
    @instagram_sessionid
)
WHERE name LIKE 'Instagram%'
   OR payload_json LIKE '%<replace-instagram_sessionid>%'
   OR payload_json LIKE '%<replace-instagram-sessionid>%'
   OR payload_json LIKE '%<replace-with-your-instagram-sessionid>%';

COMMIT;
