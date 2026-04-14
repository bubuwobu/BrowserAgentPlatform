/*
 Navicat Premium Data Transfer

 Source Server         : 本地电脑
 Source Server Type    : MySQL
 Source Server Version : 80043
 Source Host           : localhost:3306
 Source Schema         : browser_agent_platform

 Target Server Type    : MySQL
 Target Server Version : 80043
 File Encoding         : 65001

 Date: 13/04/2026 09:51:35
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for account_runtime_identities
-- ----------------------------
DROP TABLE IF EXISTS `account_runtime_identities`;
CREATE TABLE `account_runtime_identities`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT,
  `account_id` bigint(0) NOT NULL,
  `browser_profile_id` bigint(0) NULL DEFAULT NULL,
  `device_profile_id` bigint(0) NULL DEFAULT NULL,
  `proxy_binding_id` bigint(0) NULL DEFAULT NULL,
  `launch_profile_id` bigint(0) NULL DEFAULT NULL,
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT 'active',
  `created_at` datetime(0) NULL DEFAULT CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of account_runtime_identities
-- ----------------------------
INSERT INTO `account_runtime_identities` VALUES (1, 1, 1, 1, 1, 1, 'active', '2026-03-29 04:33:32');

-- ----------------------------
-- Table structure for accounts
-- ----------------------------
DROP TABLE IF EXISTS `accounts`;
CREATE TABLE `accounts`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `platform` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `username` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `status` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'active',
  `browser_profile_id` bigint(0) NULL DEFAULT NULL,
  `credential_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `metadata_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `created_at` datetime(0) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of accounts
-- ----------------------------
INSERT INTO `accounts` VALUES (1, 'TikTok Alice', 'tiktok_mock', 'alice', 'active', 5, '{\"username\":\"alice\",\"password\":\"123456\"}', '{\"site\":\"http://localhost:3001\",\"role\":\"seed\"}', '2026-03-29 13:44:21');
INSERT INTO `accounts` VALUES (2, 'Facebook Alice', 'facebook_like_mock', 'alice', 'active', 2, '{\"username\":\"alice\",\"password\":\"123456\"}', '{\"site\":\"http://localhost:3000\",\"role\":\"seed\"}', '2026-03-29 13:44:21');
INSERT INTO `accounts` VALUES (3, 'Reddit Visitor Demo', 'reddit', 'reddit_guest', 'active', 5, '{\"mode\":\"guest\"}', '{\"site\":\"https://old.reddit.com\",\"notes\":\"browse-only automation seed\"}', '2026-04-12 17:24:51');
INSERT INTO `accounts` VALUES (5, 'Reddit Visitor Demo', 'reddit', 'reddit_guest', 'active', 5, '{\"mode\":\"guest\"}', '{\"site\":\"https://old.reddit.com\",\"notes\":\"browse-only automation seed\"}', '2026-04-12 17:33:48');

-- ----------------------------
-- Table structure for agent_commands
-- ----------------------------
DROP TABLE IF EXISTS `agent_commands`;
CREATE TABLE `agent_commands`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT 'Agent命令主键ID',
  `agent_id` bigint(0) NOT NULL COMMENT '目标Agent ID',
  `profile_id` bigint(0) NULL DEFAULT NULL COMMENT '关联Profile ID，可为空',
  `command_type` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '命令类型，如 test_open_profile / takeover_start / takeover_stop',
  `payload_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '命令参数JSON',
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pending' COMMENT '命令状态：pending / processing / completed / failed',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_agent_commands_agent_status_created`(`agent_id`, `status`, `created_at`) USING BTREE,
  INDEX `idx_agent_commands_profile_id`(`profile_id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Agent命令队列表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of agent_commands
-- ----------------------------
INSERT INTO `agent_commands` VALUES (1, 1, 5, 'test_open_profile', '{\"profileId\":5}', 'sent', '2026-04-05 09:25:02');

-- ----------------------------
-- Table structure for agents
-- ----------------------------
DROP TABLE IF EXISTS `agents`;
CREATE TABLE `agents`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT 'Agent节点主键ID',
  `agent_key` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Agent唯一标识',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT 'Agent名称',
  `machine_name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '机器名称',
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'offline' COMMENT '状态：offline / online / busy',
  `max_parallel_runs` int(0) NOT NULL DEFAULT 1 COMMENT '最大并发执行数',
  `current_runs` int(0) NOT NULL DEFAULT 0 COMMENT '当前执行中的任务数',
  `scheduler_tags` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '调度标签，逗号分隔或自定义文本',
  `last_heartbeat_at` datetime(0) NULL DEFAULT NULL COMMENT '最后心跳时间',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uk_agents_agent_key`(`agent_key`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Agent节点表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of agents
-- ----------------------------
INSERT INTO `agents` VALUES (1, 'agent-local-001', 'Local Agent', 'DEV-PC', 'online', 2, 0, 'default', '2026-04-13 01:21:16', '2026-03-29 13:44:21');
INSERT INTO `agents` VALUES (2, 'demo-agent-1', 'Demo Agent 1', 'demo-machine-1', 'online', 2, 0, 'demo,default', '2026-03-29 13:50:02', '2026-03-29 13:50:12');
INSERT INTO `agents` VALUES (3, 'demo-agent-2', 'Demo Agent 2', 'demo-machine-2', 'offline', 1, 0, 'demo,backup', '2026-03-29 13:20:12', '2026-03-29 13:50:12');

-- ----------------------------
-- Table structure for audit_events
-- ----------------------------
DROP TABLE IF EXISTS `audit_events`;
CREATE TABLE `audit_events`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT,
  `event_type` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `actor_type` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `actor_id` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `target_type` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `target_id` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `details_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 100 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of audit_events
-- ----------------------------
INSERT INTO `audit_events` VALUES (1, 'demo_seed', 'system', 'db_seeder', 'task_run', '2', '{\"note\":\"seed completed run\"}', '2026-03-29 11:54:13.268632');
INSERT INTO `audit_events` VALUES (2, 'demo_seed', 'system', 'db_seeder', 'task_run', '3', '{\"note\":\"seed failed run\"}', '2026-03-29 12:53:13.268632');
INSERT INTO `audit_events` VALUES (3, 'agent_register', 'agent', 'agent-local-001', 'agent', '1', '{\"machine\":\"DEV-PC\"}', '2026-04-05 07:40:50.718889');
INSERT INTO `audit_events` VALUES (4, 'agent_register', 'agent', 'agent-local-001', 'agent', '1', '{\"machine\":\"DEV-PC\"}', '2026-04-05 08:58:29.588476');
INSERT INTO `audit_events` VALUES (5, 'closed_loop_start', 'user', 'manual', 'task_run', '4', '{\"profileId\":5,\"Ok\":true,\"Errors\":[],\"Warnings\":[]}', '2026-04-05 09:17:51.388161');
INSERT INTO `audit_events` VALUES (6, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '4', '{}', '2026-04-05 09:17:52.222493');
INSERT INTO `audit_events` VALUES (7, 'run_progress', 'agent', 'agent-local-001', 'task_run', '4', '{\"status\":\"running\",\"step\":\"tiktok_session\"}', '2026-04-05 09:17:53.549180');
INSERT INTO `audit_events` VALUES (8, 'run_progress', 'agent', 'agent-local-001', 'task_run', '4', '{\"status\":\"running\",\"step\":\"done\"}', '2026-04-05 09:18:48.824226');
INSERT INTO `audit_events` VALUES (9, 'run_complete', 'agent', 'agent-local-001', 'task_run', '4', '{\"status\":\"completed\"}', '2026-04-05 09:18:49.110382');
INSERT INTO `audit_events` VALUES (10, 'agent_register', 'agent', 'agent-local-001', 'agent', '1', '{\"machine\":\"DEV-PC\"}', '2026-04-05 09:23:53.979524');
INSERT INTO `audit_events` VALUES (11, 'profile_test_open', 'user', 'Admin', 'profile', '5', '{}', '2026-04-05 09:25:02.480085');
INSERT INTO `audit_events` VALUES (12, 'agent_register', 'agent', 'agent-local-001', 'agent', '1', '{\"machine\":\"DEV-PC\"}', '2026-04-05 09:41:47.365527');
INSERT INTO `audit_events` VALUES (13, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '5', '{}', '2026-04-05 09:46:43.460285');
INSERT INTO `audit_events` VALUES (14, 'run_complete', 'agent', 'agent-local-001', 'task_run', '5', '{\"status\":\"failed\"}', '2026-04-05 09:46:43.591427');
INSERT INTO `audit_events` VALUES (15, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '9', '{}', '2026-04-05 10:01:50.255194');
INSERT INTO `audit_events` VALUES (16, 'run_complete', 'agent', 'agent-local-001', 'task_run', '9', '{\"status\":\"failed\"}', '2026-04-05 10:01:50.323323');
INSERT INTO `audit_events` VALUES (17, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '10', '{}', '2026-04-05 10:01:53.381428');
INSERT INTO `audit_events` VALUES (18, 'run_complete', 'agent', 'agent-local-001', 'task_run', '10', '{\"status\":\"failed\"}', '2026-04-05 10:01:53.444374');
INSERT INTO `audit_events` VALUES (19, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '11', '{}', '2026-04-05 15:50:53.157488');
INSERT INTO `audit_events` VALUES (20, 'run_complete', 'agent', 'agent-local-001', 'task_run', '11', '{\"status\":\"failed\"}', '2026-04-05 15:50:53.219838');
INSERT INTO `audit_events` VALUES (21, 'demo_seed', 'system', 'db_seeder', 'task_run', '13', '{\"note\":\"seed completed run\"}', '2026-04-06 01:32:14.558013');
INSERT INTO `audit_events` VALUES (22, 'demo_seed', 'system', 'db_seeder', 'task_run', '14', '{\"note\":\"seed failed run\"}', '2026-04-06 02:31:14.558013');
INSERT INTO `audit_events` VALUES (23, 'agent_register', 'agent', 'agent-local-001', 'agent', '1', '{\"machine\":\"DEV-PC\"}', '2026-04-06 03:30:20.248716');
INSERT INTO `audit_events` VALUES (24, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '15', '{}', '2026-04-06 03:30:20.533514');
INSERT INTO `audit_events` VALUES (25, 'run_progress', 'agent', 'agent-local-001', 'task_run', '15', '{\"status\":\"running\",\"step\":\"step_open\"}', '2026-04-06 03:30:21.430137');
INSERT INTO `audit_events` VALUES (26, 'run_progress', 'agent', 'agent-local-001', 'task_run', '15', '{\"status\":\"running\",\"step\":\"step_wait_input\"}', '2026-04-06 03:30:32.734026');
INSERT INTO `audit_events` VALUES (27, 'run_complete', 'agent', 'agent-local-001', 'task_run', '15', '{\"status\":\"failed\"}', '2026-04-06 03:30:40.758924');
INSERT INTO `audit_events` VALUES (28, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '16', '{}', '2026-04-06 03:31:29.066130');
INSERT INTO `audit_events` VALUES (29, 'run_complete', 'agent', 'agent-local-001', 'task_run', '16', '{\"status\":\"failed\"}', '2026-04-06 03:31:29.135965');
INSERT INTO `audit_events` VALUES (30, 'demo_seed', 'system', 'db_seeder', 'task_run', '18', '{\"note\":\"seed completed run\"}', '2026-04-06 01:57:29.466686');
INSERT INTO `audit_events` VALUES (31, 'demo_seed', 'system', 'db_seeder', 'task_run', '19', '{\"note\":\"seed failed run\"}', '2026-04-06 02:56:29.466686');
INSERT INTO `audit_events` VALUES (32, 'agent_register', 'agent', 'agent-local-001', 'agent', '1', '{\"machine\":\"DEV-PC\"}', '2026-04-06 03:53:39.049546');
INSERT INTO `audit_events` VALUES (33, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '20', '{}', '2026-04-06 03:53:39.300067');
INSERT INTO `audit_events` VALUES (34, 'run_progress', 'agent', 'agent-local-001', 'task_run', '20', '{\"status\":\"running\",\"step\":\"step_open\"}', '2026-04-06 03:53:40.150382');
INSERT INTO `audit_events` VALUES (35, 'run_complete', 'agent', 'agent-local-001', 'task_run', '20', '{\"status\":\"failed\"}', '2026-04-06 03:53:40.971798');
INSERT INTO `audit_events` VALUES (36, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '21', '{}', '2026-04-06 03:55:21.826975');
INSERT INTO `audit_events` VALUES (37, 'run_progress', 'agent', 'agent-local-001', 'task_run', '21', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-06 03:55:22.245997');
INSERT INTO `audit_events` VALUES (38, 'run_complete', 'agent', 'agent-local-001', 'task_run', '21', '{\"status\":\"failed\"}', '2026-04-06 03:55:23.028255');
INSERT INTO `audit_events` VALUES (39, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '22', '{}', '2026-04-06 03:55:24.969187');
INSERT INTO `audit_events` VALUES (40, 'run_progress', 'agent', 'agent-local-001', 'task_run', '22', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-06 03:55:25.112190');
INSERT INTO `audit_events` VALUES (41, 'run_complete', 'agent', 'agent-local-001', 'task_run', '22', '{\"status\":\"failed\"}', '2026-04-06 03:55:25.869588');
INSERT INTO `audit_events` VALUES (42, 'agent_register', 'agent', 'agent-local-001', 'agent', '1', '{\"machine\":\"DEV-PC\"}', '2026-04-06 05:04:24.385689');
INSERT INTO `audit_events` VALUES (43, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '23', '{}', '2026-04-06 05:05:29.875129');
INSERT INTO `audit_events` VALUES (44, 'run_progress', 'agent', 'agent-local-001', 'task_run', '23', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-06 05:05:30.966985');
INSERT INTO `audit_events` VALUES (45, 'run_progress', 'agent', 'agent-local-001', 'task_run', '23', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-06 05:05:35.101524');
INSERT INTO `audit_events` VALUES (46, 'run_progress', 'agent', 'agent-local-001', 'task_run', '23', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-06 05:05:35.508689');
INSERT INTO `audit_events` VALUES (47, 'run_progress', 'agent', 'agent-local-001', 'task_run', '23', '{\"status\":\"running\",\"step\":\"scroll_feed\"}', '2026-04-06 05:05:39.853409');
INSERT INTO `audit_events` VALUES (48, 'run_complete', 'agent', 'agent-local-001', 'task_run', '23', '{\"status\":\"failed\"}', '2026-04-06 05:05:39.957936');
INSERT INTO `audit_events` VALUES (49, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '24', '{}', '2026-04-06 15:29:52.532264');
INSERT INTO `audit_events` VALUES (50, 'run_progress', 'agent', 'agent-local-001', 'task_run', '24', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-06 15:29:52.767649');
INSERT INTO `audit_events` VALUES (51, 'run_progress', 'agent', 'agent-local-001', 'task_run', '24', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-06 15:29:53.845195');
INSERT INTO `audit_events` VALUES (52, 'run_progress', 'agent', 'agent-local-001', 'task_run', '24', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-06 15:29:54.014014');
INSERT INTO `audit_events` VALUES (53, 'run_progress', 'agent', 'agent-local-001', 'task_run', '24', '{\"status\":\"running\",\"step\":\"scroll_feed\"}', '2026-04-06 15:29:58.188566');
INSERT INTO `audit_events` VALUES (54, 'run_complete', 'agent', 'agent-local-001', 'task_run', '24', '{\"status\":\"failed\"}', '2026-04-06 15:29:58.271838');
INSERT INTO `audit_events` VALUES (55, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '25', '{}', '2026-04-06 15:30:20.820222');
INSERT INTO `audit_events` VALUES (56, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '25', '{}', '2026-04-06 15:30:23.980137');
INSERT INTO `audit_events` VALUES (57, 'run_progress', 'agent', 'agent-local-001', 'task_run', '25', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-06 15:30:50.904462');
INSERT INTO `audit_events` VALUES (58, 'run_progress', 'agent', 'agent-local-001', 'task_run', '25', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-06 15:30:54.100918');
INSERT INTO `audit_events` VALUES (59, 'run_progress', 'agent', 'agent-local-001', 'task_run', '25', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-06 15:30:55.300776');
INSERT INTO `audit_events` VALUES (60, 'run_progress', 'agent', 'agent-local-001', 'task_run', '25', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-06 15:30:56.854375');
INSERT INTO `audit_events` VALUES (61, 'run_progress', 'agent', 'agent-local-001', 'task_run', '25', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-06 15:30:58.787493');
INSERT INTO `audit_events` VALUES (62, 'run_progress', 'agent', 'agent-local-001', 'task_run', '25', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-06 15:31:01.978890');
INSERT INTO `audit_events` VALUES (63, 'run_progress', 'agent', 'agent-local-001', 'task_run', '25', '{\"status\":\"running\",\"step\":\"scroll_feed\"}', '2026-04-06 15:31:09.118985');
INSERT INTO `audit_events` VALUES (64, 'run_complete', 'agent', 'agent-local-001', 'task_run', '25', '{\"status\":\"failed\"}', '2026-04-06 15:31:09.210060');
INSERT INTO `audit_events` VALUES (65, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '26', '{}', '2026-04-07 09:00:02.699610');
INSERT INTO `audit_events` VALUES (66, 'run_complete', 'agent', 'agent-local-001', 'task_run', '26', '{\"status\":\"failed\"}', '2026-04-07 09:00:02.772146');
INSERT INTO `audit_events` VALUES (67, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '27', '{}', '2026-04-07 09:00:05.870357');
INSERT INTO `audit_events` VALUES (68, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '27', '{}', '2026-04-07 09:00:08.975995');
INSERT INTO `audit_events` VALUES (69, 'run_progress', 'agent', 'agent-local-001', 'task_run', '27', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-07 09:00:35.990598');
INSERT INTO `audit_events` VALUES (70, 'run_progress', 'agent', 'agent-local-001', 'task_run', '27', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-07 09:00:39.047129');
INSERT INTO `audit_events` VALUES (71, 'run_progress', 'agent', 'agent-local-001', 'task_run', '27', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-07 09:00:41.776174');
INSERT INTO `audit_events` VALUES (72, 'run_progress', 'agent', 'agent-local-001', 'task_run', '27', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-07 09:00:43.721635');
INSERT INTO `audit_events` VALUES (73, 'run_progress', 'agent', 'agent-local-001', 'task_run', '27', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-07 09:00:46.782039');
INSERT INTO `audit_events` VALUES (74, 'run_progress', 'agent', 'agent-local-001', 'task_run', '27', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-07 09:01:13.899177');
INSERT INTO `audit_events` VALUES (75, 'run_progress', 'agent', 'agent-local-001', 'task_run', '27', '{\"status\":\"running\",\"step\":\"scroll_feed\"}', '2026-04-07 09:01:20.874924');
INSERT INTO `audit_events` VALUES (76, 'run_complete', 'agent', 'agent-local-001', 'task_run', '27', '{\"status\":\"failed\"}', '2026-04-07 09:01:20.936339');
INSERT INTO `audit_events` VALUES (77, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '28', '{}', '2026-04-08 09:00:40.047663');
INSERT INTO `audit_events` VALUES (78, 'run_complete', 'agent', 'agent-local-001', 'task_run', '28', '{\"status\":\"failed\"}', '2026-04-08 09:00:40.111650');
INSERT INTO `audit_events` VALUES (79, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '29', '{}', '2026-04-08 09:00:43.217082');
INSERT INTO `audit_events` VALUES (80, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '29', '{}', '2026-04-08 09:00:46.343249');
INSERT INTO `audit_events` VALUES (81, 'run_progress', 'agent', 'agent-local-001', 'task_run', '29', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-08 09:01:13.313251');
INSERT INTO `audit_events` VALUES (82, 'run_progress', 'agent', 'agent-local-001', 'task_run', '29', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-08 09:01:16.456796');
INSERT INTO `audit_events` VALUES (83, 'run_progress', 'agent', 'agent-local-001', 'task_run', '29', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-08 09:01:18.181139');
INSERT INTO `audit_events` VALUES (84, 'run_progress', 'agent', 'agent-local-001', 'task_run', '29', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-08 09:01:19.877505');
INSERT INTO `audit_events` VALUES (85, 'run_progress', 'agent', 'agent-local-001', 'task_run', '29', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-08 09:01:22.972047');
INSERT INTO `audit_events` VALUES (86, 'run_progress', 'agent', 'agent-local-001', 'task_run', '29', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-08 09:01:25.048176');
INSERT INTO `audit_events` VALUES (87, 'run_progress', 'agent', 'agent-local-001', 'task_run', '29', '{\"status\":\"running\",\"step\":\"scroll_feed\"}', '2026-04-08 09:01:57.118959');
INSERT INTO `audit_events` VALUES (88, 'run_complete', 'agent', 'agent-local-001', 'task_run', '29', '{\"status\":\"failed\"}', '2026-04-08 09:01:57.195778');
INSERT INTO `audit_events` VALUES (89, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '30', '{}', '2026-04-09 09:00:15.919357');
INSERT INTO `audit_events` VALUES (90, 'run_complete', 'agent', 'agent-local-001', 'task_run', '30', '{\"status\":\"failed\"}', '2026-04-09 09:00:15.982990');
INSERT INTO `audit_events` VALUES (91, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '31', '{}', '2026-04-09 09:00:19.090878');
INSERT INTO `audit_events` VALUES (92, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '31', '{}', '2026-04-09 09:00:22.213110');
INSERT INTO `audit_events` VALUES (93, 'run_progress', 'agent', 'agent-local-001', 'task_run', '31', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-09 09:00:49.209335');
INSERT INTO `audit_events` VALUES (94, 'run_complete', 'agent', 'agent-local-001', 'task_run', '31', '{\"status\":\"failed\"}', '2026-04-09 09:00:49.989718');
INSERT INTO `audit_events` VALUES (95, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '32', '{}', '2026-04-10 09:00:43.761587');
INSERT INTO `audit_events` VALUES (96, 'run_complete', 'agent', 'agent-local-001', 'task_run', '32', '{\"status\":\"failed\"}', '2026-04-10 09:00:43.835641');
INSERT INTO `audit_events` VALUES (97, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '33', '{}', '2026-04-10 09:00:46.952381');
INSERT INTO `audit_events` VALUES (98, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '33', '{}', '2026-04-10 09:00:50.050286');
INSERT INTO `audit_events` VALUES (99, 'run_progress', 'agent', 'agent-local-001', 'task_run', '33', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-10 09:01:17.038657');
INSERT INTO `audit_events` VALUES (100, 'run_complete', 'agent', 'agent-local-001', 'task_run', '33', '{\"status\":\"failed\"}', '2026-04-10 09:01:17.688321');
INSERT INTO `audit_events` VALUES (101, 'agent_register', 'agent', 'agent-local-001', 'agent', '1', '{\"machine\":\"DEV-PC\"}', '2026-04-12 09:34:16.507751');
INSERT INTO `audit_events` VALUES (102, 'closed_loop_start', 'user', 'manual', 'task_run', '39', '{\"profileId\":9,\"Ok\":true,\"Errors\":[],\"Warnings\":[]}', '2026-04-12 09:38:15.585216');
INSERT INTO `audit_events` VALUES (103, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '34', '{}', '2026-04-12 17:33:51.205441');
INSERT INTO `audit_events` VALUES (104, 'run_complete', 'agent', 'agent-local-001', 'task_run', '34', '{\"status\":\"failed\"}', '2026-04-12 17:33:51.372817');
INSERT INTO `audit_events` VALUES (105, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '35', '{}', '2026-04-12 17:33:54.416338');
INSERT INTO `audit_events` VALUES (106, 'run_progress', 'agent', 'agent-local-001', 'task_run', '35', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-12 17:33:55.824978');
INSERT INTO `audit_events` VALUES (107, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '39', '{}', '2026-04-12 17:33:57.821008');
INSERT INTO `audit_events` VALUES (108, 'run_progress', 'agent', 'agent-local-001', 'task_run', '39', '{\"status\":\"running\",\"step\":\"tiktok_session\"}', '2026-04-12 17:33:58.589585');
INSERT INTO `audit_events` VALUES (109, 'run_complete', 'agent', 'agent-local-001', 'task_run', '39', '{\"status\":\"failed\"}', '2026-04-12 17:34:01.083686');
INSERT INTO `audit_events` VALUES (110, 'run_progress', 'agent', 'agent-local-001', 'task_run', '35', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-12 17:34:09.152668');
INSERT INTO `audit_events` VALUES (111, 'run_progress', 'agent', 'agent-local-001', 'task_run', '35', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-12 17:34:09.330438');
INSERT INTO `audit_events` VALUES (112, 'run_progress', 'agent', 'agent-local-001', 'task_run', '35', '{\"status\":\"running\",\"step\":\"scroll_feed\"}', '2026-04-12 17:34:13.537468');
INSERT INTO `audit_events` VALUES (113, 'run_complete', 'agent', 'agent-local-001', 'task_run', '35', '{\"status\":\"failed\"}', '2026-04-12 17:34:13.643810');
INSERT INTO `audit_events` VALUES (114, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '36', '{}', '2026-04-12 17:34:16.664162');
INSERT INTO `audit_events` VALUES (115, 'run_progress', 'agent', 'agent-local-001', 'task_run', '36', '{\"status\":\"running\",\"step\":\"open_home\"}', '2026-04-12 17:34:16.841727');
INSERT INTO `audit_events` VALUES (116, 'run_progress', 'agent', 'agent-local-001', 'task_run', '36', '{\"status\":\"running\",\"step\":\"wait_page\"}', '2026-04-12 17:34:18.510075');
INSERT INTO `audit_events` VALUES (117, 'run_progress', 'agent', 'agent-local-001', 'task_run', '36', '{\"status\":\"running\",\"step\":\"warmup_wait\"}', '2026-04-12 17:34:18.809862');
INSERT INTO `audit_events` VALUES (118, 'run_progress', 'agent', 'agent-local-001', 'task_run', '36', '{\"status\":\"running\",\"step\":\"scroll_feed\"}', '2026-04-12 17:34:23.043383');
INSERT INTO `audit_events` VALUES (119, 'run_complete', 'agent', 'agent-local-001', 'task_run', '36', '{\"status\":\"failed\"}', '2026-04-12 17:34:23.122946');
INSERT INTO `audit_events` VALUES (120, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '37', '{}', '2026-04-12 17:34:26.164158');
INSERT INTO `audit_events` VALUES (121, 'run_progress', 'agent', 'agent-local-001', 'task_run', '37', '{\"status\":\"running\",\"step\":\"open_reddit_home\"}', '2026-04-12 17:34:26.344292');
INSERT INTO `audit_events` VALUES (122, 'run_progress', 'agent', 'agent-local-001', 'task_run', '37', '{\"status\":\"running\",\"step\":\"wait_home_ready\"}', '2026-04-12 17:34:27.668719');
INSERT INTO `audit_events` VALUES (123, 'run_progress', 'agent', 'agent-local-001', 'task_run', '37', '{\"status\":\"running\",\"step\":\"done\"}', '2026-04-12 17:34:27.828478');
INSERT INTO `audit_events` VALUES (124, 'run_complete', 'agent', 'agent-local-001', 'task_run', '37', '{\"status\":\"completed\"}', '2026-04-12 17:34:27.986703');
INSERT INTO `audit_events` VALUES (125, 'run_pulled', 'agent', 'agent-local-001', 'task_run', '38', '{}', '2026-04-12 17:34:29.333749');
INSERT INTO `audit_events` VALUES (126, 'run_progress', 'agent', 'agent-local-001', 'task_run', '38', '{\"status\":\"running\",\"step\":\"open_reddit_home\"}', '2026-04-12 17:34:29.485774');
INSERT INTO `audit_events` VALUES (127, 'run_progress', 'agent', 'agent-local-001', 'task_run', '38', '{\"status\":\"running\",\"step\":\"wait_home_ready\"}', '2026-04-12 17:34:29.842378');
INSERT INTO `audit_events` VALUES (128, 'run_progress', 'agent', 'agent-local-001', 'task_run', '38', '{\"status\":\"running\",\"step\":\"done\"}', '2026-04-12 17:34:30.018674');
INSERT INTO `audit_events` VALUES (129, 'run_complete', 'agent', 'agent-local-001', 'task_run', '38', '{\"status\":\"completed\"}', '2026-04-12 17:34:30.165121');

-- ----------------------------
-- Table structure for backup_accounts_20260405
-- ----------------------------
DROP TABLE IF EXISTS `backup_accounts_20260405`;
CREATE TABLE `backup_accounts_20260405`  (
  `id` bigint(0) NOT NULL DEFAULT 0,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `platform` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `username` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `status` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'active',
  `browser_profile_id` bigint(0) NULL DEFAULT NULL,
  `credential_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `metadata_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `created_at` datetime(0) NOT NULL
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for backup_task_runs_20260405
-- ----------------------------
DROP TABLE IF EXISTS `backup_task_runs_20260405`;
CREATE TABLE `backup_task_runs_20260405`  (
  `id` bigint(0) NOT NULL DEFAULT 0 COMMENT '任务运行主键ID',
  `task_id` bigint(0) NOT NULL COMMENT '所属任务ID',
  `browser_profile_id` bigint(0) NOT NULL COMMENT '执行时使用的Profile ID',
  `assigned_agent_id` bigint(0) NULL DEFAULT NULL COMMENT '实际分配的Agent ID',
  `lease_token` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'queued' COMMENT '运行状态：queued / leased / running / completed / failed / cancelled',
  `retry_count` int(0) NOT NULL DEFAULT 0,
  `max_retries` int(0) NOT NULL DEFAULT 0,
  `current_step_id` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '当前执行步骤ID',
  `current_step_label` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '当前执行步骤名称',
  `current_url` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '当前页面URL',
  `result_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '执行结果JSON',
  `error_code` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `error_message` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `last_preview_path` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '最后一次预览截图路径',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  `started_at` datetime(0) NULL DEFAULT NULL COMMENT '开始执行时间',
  `heartbeat_at` datetime(0) NULL DEFAULT NULL,
  `finished_at` datetime(0) NULL DEFAULT NULL COMMENT '结束时间'
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for backup_tasks_20260405
-- ----------------------------
DROP TABLE IF EXISTS `backup_tasks_20260405`;
CREATE TABLE `backup_tasks_20260405`  (
  `id` bigint(0) NOT NULL DEFAULT 0 COMMENT '任务主键ID',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '任务名称',
  `browser_profile_id` bigint(0) NOT NULL COMMENT '绑定执行的Profile ID',
  `scheduling_strategy` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'profile_owner' COMMENT '调度策略：profile_owner / preferred_agent / least_loaded',
  `preferred_agent_id` bigint(0) NULL DEFAULT NULL COMMENT '优先Agent ID',
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'queued' COMMENT '状态：queued / leased / running / completed / failed / cancelled',
  `payload_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '任务编排与参数JSON',
  `retry_policy_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `priority` int(0) NOT NULL DEFAULT 100 COMMENT '任务优先级，数值越小可表示越高优先级',
  `timeout_seconds` int(0) NOT NULL DEFAULT 900,
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  `account_id` bigint(0) NULL DEFAULT NULL,
  `is_enabled` tinyint(1) NOT NULL DEFAULT 1,
  `schedule_type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'manual',
  `schedule_config_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `next_run_at` datetime(0) NULL DEFAULT NULL,
  `last_run_at` datetime(0) NULL DEFAULT NULL
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of backup_tasks_20260405
-- ----------------------------
INSERT INTO `backup_tasks_20260405` VALUES (1, 'TikTok Alice 闭环测试任务', 1, 'profile_owner', NULL, 'queued', '{\r\n    \"steps\":[\r\n      {\r\n        \"id\":\"tiktok_session\",\r\n        \"type\":\"tiktok_mock_session\",\r\n        \"data\":{\r\n          \"label\":\"执行 TikTok Mock 自动化会话\",\r\n          \"baseUrl\":\"http://localhost:3001\",\r\n          \"username\":\"alice\",\r\n          \"password\":\"123456\",\r\n          \"minVideos\":2,\r\n          \"maxVideos\":4,\r\n          \"minWatchMs\":2500,\r\n          \"maxWatchMs\":7000,\r\n          \"minLikes\":1,\r\n          \"maxLikes\":2,\r\n          \"minComments\":1,\r\n          \"maxComments\":2,\r\n          \"behaviorProfile\":\"balanced\",\r\n          \"commentProvider\":\"deepseek\"\r\n        }\r\n      },\r\n      { \"id\":\"done\", \"type\":\"end_success\", \"data\":{\"label\":\"完成\"} }\r\n    ],\r\n    \"edges\":[\r\n      {\"source\":\"tiktok_session\",\"target\":\"done\"}\r\n    ],\r\n    \"startupArgsJson\":\"[]\"\r\n  }', '{\"maxRetries\":1}', 100, 900, '2026-03-29 13:44:21', 1, 1, 'manual', NULL, NULL, NULL);
INSERT INTO `backup_tasks_20260405` VALUES (2, 'Facebook Alice 登录评论任务', 2, 'profile_owner', NULL, 'queued', '{\r\n    \"steps\":[\r\n      { \"id\":\"open_login\", \"type\":\"open\", \"data\":{\"label\":\"打开登录页\",\"url\":\"http://localhost:3000/login\"} },\r\n      { \"id\":\"wait_login_form\", \"type\":\"wait_for_element\", \"data\":{\"label\":\"等待登录表单\",\"selector\":\"form[action=\'/login\']\",\"timeout\":10000} },\r\n      { \"id\":\"type_username\", \"type\":\"type\", \"data\":{\"label\":\"输入用户名\",\"selector\":\"input[name=\'username\']\",\"text\":\"alice\"} },\r\n      { \"id\":\"type_password\", \"type\":\"type\", \"data\":{\"label\":\"输入密码\",\"selector\":\"input[name=\'password\']\",\"text\":\"123456\"} },\r\n      { \"id\":\"click_login\", \"type\":\"click\", \"data\":{\"label\":\"点击登录\",\"selector\":\"button[type=\'submit\']\"} },\r\n      { \"id\":\"wait_feed\", \"type\":\"wait_for_element\", \"data\":{\"label\":\"等待Feed\",\"selector\":\"[data-testid=\'fb-feed\'], .feed, main\",\"timeout\":10000} },\r\n      { \"id\":\"click_comment_toggle\", \"type\":\"click\", \"data\":{\"label\":\"展开评论\",\"selector\":\"[data-testid=\'comment-toggle\'], .comment-btn\"} },\r\n      { \"id\":\"type_comment\", \"type\":\"type\", \"data\":{\"label\":\"输入评论\",\"selector\":\"[data-testid=\'comment-input\'], .comment-input\",\"text\":\"hello\"} },\r\n      { \"id\":\"submit_comment\", \"type\":\"click\", \"data\":{\"label\":\"提交评论\",\"selector\":\"[data-testid=\'comment-submit\'], .submit-comment, .submit\"} },\r\n      { \"id\":\"done\", \"type\":\"end_success\", \"data\":{\"label\":\"完成\"} }\r\n    ],\r\n    \"edges\":[\r\n      {\"source\":\"open_login\",\"target\":\"wait_login_form\"},\r\n      {\"source\":\"wait_login_form\",\"target\":\"type_username\"},\r\n      {\"source\":\"type_username\",\"target\":\"type_password\"},\r\n      {\"source\":\"type_password\",\"target\":\"click_login\"},\r\n      {\"source\":\"click_login\",\"target\":\"wait_feed\"},\r\n      {\"source\":\"wait_feed\",\"target\":\"click_comment_toggle\"},\r\n      {\"source\":\"click_comment_toggle\",\"target\":\"type_comment\"},\r\n      {\"source\":\"type_comment\",\"target\":\"submit_comment\"},\r\n      {\"source\":\"submit_comment\",\"target\":\"done\"}\r\n    ],\r\n    \"startupArgsJson\":\"[]\"\r\n  }', '{\"maxRetries\":1}', 100, 900, '2026-03-29 13:44:21', 2, 1, 'manual', NULL, NULL, NULL);
INSERT INTO `backup_tasks_20260405` VALUES (8, 'TK Daily Browse', 5, 'preferred_agent', 1, 'failed', '{\n  \"name\": \"TK Daily Browse\",\n  \"browserProfileId\": 1,\n  \"accountId\": 1,\n  \"schedulingStrategy\": \"least_loaded\",\n  \"preferredAgentId\": null,\n  \"isEnabled\": true,\n  \"scheduleType\": \"daily_window_random\",\n  \"scheduleConfigJson\": \"{\\\"timezone\\\":\\\"UTC\\\",\\\"windowStart\\\":\\\"01:00\\\",\\\"windowEnd\\\":\\\"03:00\\\",\\\"maxRunsPerDay\\\":1,\\\"randomMinuteStep\\\":5}\",\n  \"payloadJson\": \"{\\\"steps\\\":[{\\\"id\\\":\\\"tiktok_session\\\",\\\"type\\\":\\\"tiktok_mock_session\\\",\\\"data\\\":{\\\"label\\\":\\\"daily session\\\",\\\"baseUrl\\\":\\\"http://localhost:3001\\\",\\\"username\\\":\\\"alice\\\",\\\"password\\\":\\\"123456\\\",\\\"minVideos\\\":2,\\\"maxVideos\\\":4,\\\"minWatchMs\\\":2500,\\\"maxWatchMs\\\":7000,\\\"minLikes\\\":1,\\\"maxLikes\\\":2,\\\"minComments\\\":1,\\\"maxComments\\\":2,\\\"behaviorProfile\\\":\\\"balanced\\\",\\\"commentProvider\\\":\\\"deepseek\\\"}},{\\\"id\\\":\\\"done\\\",\\\"type\\\":\\\"end_success\\\",\\\"data\\\":{\\\"label\\\":\\\"完成\\\"}}],\\\"edges\\\":[{\\\"source\\\":\\\"tiktok_session\\\",\\\"target\\\":\\\"done\\\"}]}\",\n  \"priority\": 100,\n  \"timeoutSeconds\": 300,\n  \"retryPolicyJson\": \"{\\\"maxRetries\\\":1}\"\n}', '{\"maxRetries\":1}', 100, 300, '2026-04-05 09:46:16', 1, 1, 'daily_window_random', '{\"timezone\":\"UTC\",\"windowStart\":\"09:00\",\"windowEnd\":\"18:00\",\"maxRunsPerDay\":1,\"randomMinuteStep\":5}', '2026-04-05 11:10:00', '2026-04-05 09:46:43');

-- ----------------------------
-- Table structure for browser_artifacts
-- ----------------------------
DROP TABLE IF EXISTS `browser_artifacts`;
CREATE TABLE `browser_artifacts`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '浏览器产物主键ID',
  `task_run_id` bigint(0) NOT NULL COMMENT '所属任务运行ID',
  `artifact_type` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'screenshot' COMMENT '产物类型：screenshot / video / html / trace',
  `file_path` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '文件路径',
  `file_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '文件名',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_browser_artifacts_task_run_id`(`task_run_id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 28 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '浏览器运行产物表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of browser_artifacts
-- ----------------------------
INSERT INTO `browser_artifacts` VALUES (1, 4, 'screenshot', 'data/artifacts/4/preview_20260405091753471.png', 'preview_20260405091753471.png', '2026-04-05 09:17:53');
INSERT INTO `browser_artifacts` VALUES (2, 4, 'screenshot', 'data/artifacts/4/preview_20260405091848775.png', 'preview_20260405091848775.png', '2026-04-05 09:18:49');
INSERT INTO `browser_artifacts` VALUES (3, 4, 'screenshot', 'data/artifacts/4/final_20260405091849051.png', 'final_20260405091849051.png', '2026-04-05 09:18:49');
INSERT INTO `browser_artifacts` VALUES (6, 20, 'screenshot', 'data/artifacts/20/preview_20260406035340089.png', 'preview_20260406035340089.png', '2026-04-06 03:53:40');
INSERT INTO `browser_artifacts` VALUES (7, 21, 'screenshot', 'data/artifacts/21/preview_20260406035522211.png', 'preview_20260406035522211.png', '2026-04-06 03:55:22');
INSERT INTO `browser_artifacts` VALUES (8, 22, 'screenshot', 'data/artifacts/22/preview_20260406035525077.png', 'preview_20260406035525077.png', '2026-04-06 03:55:25');
INSERT INTO `browser_artifacts` VALUES (9, 23, 'screenshot', 'data/artifacts/23/preview_20260406050530889.png', 'preview_20260406050530889.png', '2026-04-06 05:05:31');
INSERT INTO `browser_artifacts` VALUES (10, 23, 'screenshot', 'data/artifacts/23/preview_20260406050535065.png', 'preview_20260406050535065.png', '2026-04-06 05:05:35');
INSERT INTO `browser_artifacts` VALUES (11, 23, 'screenshot', 'data/artifacts/23/preview_20260406050535468.png', 'preview_20260406050535468.png', '2026-04-06 05:05:35');
INSERT INTO `browser_artifacts` VALUES (12, 23, 'screenshot', 'data/artifacts/23/preview_20260406050539811.png', 'preview_20260406050539811.png', '2026-04-06 05:05:40');
INSERT INTO `browser_artifacts` VALUES (13, 24, 'screenshot', 'data/artifacts/24/preview_20260406152952728.png', 'preview_20260406152952728.png', '2026-04-06 15:29:53');
INSERT INTO `browser_artifacts` VALUES (14, 24, 'screenshot', 'data/artifacts/24/preview_20260406152953804.png', 'preview_20260406152953804.png', '2026-04-06 15:29:54');
INSERT INTO `browser_artifacts` VALUES (15, 24, 'screenshot', 'data/artifacts/24/preview_20260406152953973.png', 'preview_20260406152953973.png', '2026-04-06 15:29:54');
INSERT INTO `browser_artifacts` VALUES (16, 24, 'screenshot', 'data/artifacts/24/preview_20260406152958149.png', 'preview_20260406152958149.png', '2026-04-06 15:29:58');
INSERT INTO `browser_artifacts` VALUES (17, 25, 'screenshot', 'data/artifacts/25/preview_20260406153055240.png', 'preview_20260406153055240.png', '2026-04-06 15:30:55');
INSERT INTO `browser_artifacts` VALUES (18, 25, 'screenshot', 'data/artifacts/25/preview_20260406153056799.png', 'preview_20260406153056799.png', '2026-04-06 15:30:57');
INSERT INTO `browser_artifacts` VALUES (19, 25, 'screenshot', 'data/artifacts/25/preview_20260406153058737.png', 'preview_20260406153058737.png', '2026-04-06 15:30:59');
INSERT INTO `browser_artifacts` VALUES (20, 25, 'screenshot', 'data/artifacts/25/preview_20260406153101938.png', 'preview_20260406153101938.png', '2026-04-06 15:31:02');
INSERT INTO `browser_artifacts` VALUES (21, 25, 'screenshot', 'data/artifacts/25/preview_20260406153109067.png', 'preview_20260406153109067.png', '2026-04-06 15:31:09');
INSERT INTO `browser_artifacts` VALUES (22, 27, 'screenshot', 'data/artifacts/27/preview_20260407090041730.png', 'preview_20260407090041730.png', '2026-04-07 09:00:42');
INSERT INTO `browser_artifacts` VALUES (23, 27, 'screenshot', 'data/artifacts/27/preview_20260407090043661.png', 'preview_20260407090043661.png', '2026-04-07 09:00:44');
INSERT INTO `browser_artifacts` VALUES (24, 27, 'screenshot', 'data/artifacts/27/preview_20260407090046747.png', 'preview_20260407090046747.png', '2026-04-07 09:00:47');
INSERT INTO `browser_artifacts` VALUES (25, 29, 'screenshot', 'data/artifacts/29/preview_20260408090118141.png', 'preview_20260408090118141.png', '2026-04-08 09:01:18');
INSERT INTO `browser_artifacts` VALUES (26, 29, 'screenshot', 'data/artifacts/29/preview_20260408090119838.png', 'preview_20260408090119838.png', '2026-04-08 09:01:20');
INSERT INTO `browser_artifacts` VALUES (27, 29, 'screenshot', 'data/artifacts/29/preview_20260408090122925.png', 'preview_20260408090122925.png', '2026-04-08 09:01:23');
INSERT INTO `browser_artifacts` VALUES (28, 29, 'screenshot', 'data/artifacts/29/preview_20260408090125003.png', 'preview_20260408090125003.png', '2026-04-08 09:01:25');
INSERT INTO `browser_artifacts` VALUES (29, 35, 'screenshot', 'data/artifacts/35/preview_20260412173355765.png', 'preview_20260412173355765.png', '2026-04-12 17:33:56');
INSERT INTO `browser_artifacts` VALUES (30, 39, 'screenshot', 'data/artifacts/39/preview_20260412173358307.png', 'preview_20260412173358307.png', '2026-04-12 17:33:58');
INSERT INTO `browser_artifacts` VALUES (31, 35, 'screenshot', 'data/artifacts/35/preview_20260412173409113.png', 'preview_20260412173409113.png', '2026-04-12 17:34:09');
INSERT INTO `browser_artifacts` VALUES (32, 35, 'screenshot', 'data/artifacts/35/preview_20260412173409300.png', 'preview_20260412173409300.png', '2026-04-12 17:34:09');
INSERT INTO `browser_artifacts` VALUES (33, 35, 'screenshot', 'data/artifacts/35/preview_20260412173413495.png', 'preview_20260412173413495.png', '2026-04-12 17:34:13');
INSERT INTO `browser_artifacts` VALUES (34, 36, 'screenshot', 'data/artifacts/36/preview_20260412173416804.png', 'preview_20260412173416804.png', '2026-04-12 17:34:17');
INSERT INTO `browser_artifacts` VALUES (35, 36, 'screenshot', 'data/artifacts/36/preview_20260412173418468.png', 'preview_20260412173418468.png', '2026-04-12 17:34:18');
INSERT INTO `browser_artifacts` VALUES (36, 36, 'screenshot', 'data/artifacts/36/preview_20260412173418765.png', 'preview_20260412173418765.png', '2026-04-12 17:34:19');
INSERT INTO `browser_artifacts` VALUES (37, 36, 'screenshot', 'data/artifacts/36/preview_20260412173423010.png', 'preview_20260412173423010.png', '2026-04-12 17:34:23');
INSERT INTO `browser_artifacts` VALUES (38, 37, 'screenshot', 'data/artifacts/37/preview_20260412173426305.png', 'preview_20260412173426305.png', '2026-04-12 17:34:26');
INSERT INTO `browser_artifacts` VALUES (39, 37, 'screenshot', 'data/artifacts/37/preview_20260412173427631.png', 'preview_20260412173427631.png', '2026-04-12 17:34:28');
INSERT INTO `browser_artifacts` VALUES (40, 37, 'screenshot', 'data/artifacts/37/preview_20260412173427794.png', 'preview_20260412173427794.png', '2026-04-12 17:34:28');
INSERT INTO `browser_artifacts` VALUES (41, 37, 'screenshot', 'data/artifacts/37/final_20260412173427941.png', 'final_20260412173427941.png', '2026-04-12 17:34:28');
INSERT INTO `browser_artifacts` VALUES (42, 38, 'screenshot', 'data/artifacts/38/preview_20260412173429451.png', 'preview_20260412173429451.png', '2026-04-12 17:34:29');
INSERT INTO `browser_artifacts` VALUES (43, 38, 'screenshot', 'data/artifacts/38/preview_20260412173429804.png', 'preview_20260412173429804.png', '2026-04-12 17:34:30');
INSERT INTO `browser_artifacts` VALUES (44, 38, 'screenshot', 'data/artifacts/38/preview_20260412173429985.png', 'preview_20260412173429985.png', '2026-04-12 17:34:30');
INSERT INTO `browser_artifacts` VALUES (45, 38, 'screenshot', 'data/artifacts/38/final_20260412173430117.png', 'final_20260412173430117.png', '2026-04-12 17:34:30');

-- ----------------------------
-- Table structure for browser_profile_locks
-- ----------------------------
DROP TABLE IF EXISTS `browser_profile_locks`;
CREATE TABLE `browser_profile_locks`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT 'Profile锁主键ID',
  `profile_id` bigint(0) NOT NULL COMMENT '被锁定的Profile ID',
  `task_id` bigint(0) NULL DEFAULT NULL COMMENT '关联任务ID',
  `task_run_id` bigint(0) NULL DEFAULT NULL COMMENT '关联任务运行ID',
  `agent_id` bigint(0) NULL DEFAULT NULL COMMENT '持有锁的Agent ID',
  `lease_token` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '租约令牌，用于续租和释放',
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'reserved' COMMENT '锁状态：reserved / leased / released',
  `expires_at` datetime(0) NOT NULL COMMENT '锁过期时间',
  `released_at` datetime(0) NULL DEFAULT NULL,
  `release_reason` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_browser_profile_locks_profile_status`(`profile_id`, `status`) USING BTREE,
  INDEX `idx_browser_profile_locks_task_run_id`(`task_run_id`) USING BTREE,
  INDEX `idx_profile_lock_active`(`profile_id`, `status`, `expires_at`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 21 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '浏览器Profile锁表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of browser_profile_locks
-- ----------------------------
INSERT INTO `browser_profile_locks` VALUES (1, 5, 7, 4, 1, '13e2996526224a8e8fa1e7a403569688', 'released', '2026-04-05 09:38:49', NULL, NULL, '2026-04-05 09:17:52');
INSERT INTO `browser_profile_locks` VALUES (2, 5, 8, 5, 1, '0441beee775c480f9df28273c137de29', 'released', '2026-04-05 10:06:43', NULL, NULL, '2026-04-05 09:46:43');
INSERT INTO `browser_profile_locks` VALUES (5, 5, 9, 11, 1, '0a1a58a23fc24085bb61e1f4e616cec6', 'released', '2026-04-05 16:10:53', NULL, NULL, '2026-04-05 15:50:53');
INSERT INTO `browser_profile_locks` VALUES (7, 5, 9, 16, 1, '839d4887c1c14f8ababa5a4a0f086e6b', 'released', '2026-04-06 03:51:29', NULL, NULL, '2026-04-06 03:31:29');
INSERT INTO `browser_profile_locks` VALUES (8, 9, 17, 20, 1, '76a9251c5c1347439af1d803ad2df55e', 'released', '2026-04-06 04:13:40', NULL, NULL, '2026-04-06 03:53:39');
INSERT INTO `browser_profile_locks` VALUES (9, 5, 18, 21, 1, '6e1cba94b7274ee891afd2c00d5d82c8', 'released', '2026-04-06 04:15:22', NULL, NULL, '2026-04-06 03:55:22');
INSERT INTO `browser_profile_locks` VALUES (10, 5, 18, 22, 1, '47d8cc6823ca49cd95513957f366f53f', 'released', '2026-04-06 04:15:25', NULL, NULL, '2026-04-06 03:55:25');
INSERT INTO `browser_profile_locks` VALUES (11, 5, 18, 23, 1, 'a65e529b0f8a4d93870f15f14256e3c4', 'released', '2026-04-06 05:25:40', NULL, NULL, '2026-04-06 05:05:30');
INSERT INTO `browser_profile_locks` VALUES (12, 5, 18, 24, 1, 'a49345cdd6e3473c8afaca1fce4d1af6', 'released', '2026-04-06 15:49:58', NULL, NULL, '2026-04-06 15:29:52');
INSERT INTO `browser_profile_locks` VALUES (13, 5, 18, 25, 1, '7c39976d9a91425d94e506ac23b8ea83', 'released', '2026-04-06 15:51:09', NULL, NULL, '2026-04-06 15:30:21');
INSERT INTO `browser_profile_locks` VALUES (14, 5, 9, 26, 1, '4e32330381dd476d94917db762251b8e', 'released', '2026-04-07 09:20:03', NULL, NULL, '2026-04-07 09:00:03');
INSERT INTO `browser_profile_locks` VALUES (15, 5, 18, 27, 1, '85334435026344ccbde1c6d5fb211d60', 'released', '2026-04-07 09:21:21', NULL, NULL, '2026-04-07 09:00:06');
INSERT INTO `browser_profile_locks` VALUES (16, 5, 9, 28, 1, '188a9836d40d4da4befe76b5bc338283', 'released', '2026-04-08 09:20:40', NULL, NULL, '2026-04-08 09:00:40');
INSERT INTO `browser_profile_locks` VALUES (17, 5, 18, 29, 1, '7b49eaa9ede1405596c926098fe766f3', 'released', '2026-04-08 09:21:57', NULL, NULL, '2026-04-08 09:00:43');
INSERT INTO `browser_profile_locks` VALUES (18, 5, 9, 30, 1, 'ed9962b6950645169ecc1c1c1525906a', 'released', '2026-04-09 09:20:16', NULL, NULL, '2026-04-09 09:00:16');
INSERT INTO `browser_profile_locks` VALUES (19, 5, 18, 31, 1, 'd00fdd8b756b4b2a8157eedd5f473977', 'released', '2026-04-09 09:20:49', NULL, NULL, '2026-04-09 09:00:19');
INSERT INTO `browser_profile_locks` VALUES (20, 5, 9, 32, 1, 'e38612921a3a44d3b60e6b2ec236d200', 'released', '2026-04-10 09:20:44', NULL, NULL, '2026-04-10 09:00:44');
INSERT INTO `browser_profile_locks` VALUES (21, 5, 18, 33, 1, '0d4d9b67b427425584de3b0f84f29d66', 'released', '2026-04-10 09:21:17', NULL, NULL, '2026-04-10 09:00:47');
INSERT INTO `browser_profile_locks` VALUES (886, 5, 9, 34, 1, '11c50167723d4fd1a2f762c6eef01b26', 'released', '2026-04-12 17:53:26', NULL, NULL, '2026-04-12 17:33:26');
INSERT INTO `browser_profile_locks` VALUES (887, 5, 18, 35, 1, '70b474aa9a4c4461bfc0b47a8910dc83', 'released', '2026-04-12 17:54:14', NULL, NULL, '2026-04-12 17:33:54');
INSERT INTO `browser_profile_locks` VALUES (888, 9, 20, 39, 1, 'e3d08a9d48eb4afcb841617fcb23903c', 'released', '2026-04-12 17:53:59', NULL, NULL, '2026-04-12 17:33:58');
INSERT INTO `browser_profile_locks` VALUES (889, 5, 18, 36, 1, '0a6303799ab1494ba7ce89765e8c321a', 'released', '2026-04-12 17:54:23', NULL, NULL, '2026-04-12 17:34:17');
INSERT INTO `browser_profile_locks` VALUES (890, 5, 19, 37, 1, 'ec774c4e4f8e4595967bacbfa923f391', 'released', '2026-04-12 17:54:28', NULL, NULL, '2026-04-12 17:34:26');
INSERT INTO `browser_profile_locks` VALUES (891, 5, 19, 38, 1, 'fc8fdef4f643493ebd762fda6a7413ba', 'released', '2026-04-12 17:54:30', NULL, NULL, '2026-04-12 17:34:29');

-- ----------------------------
-- Table structure for browser_profiles
-- ----------------------------
DROP TABLE IF EXISTS `browser_profiles`;
CREATE TABLE `browser_profiles`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '浏览器Profile主键ID',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT 'Profile名称',
  `owner_agent_id` bigint(0) NULL DEFAULT NULL COMMENT '归属Agent ID，通常用于profile_owner调度',
  `proxy_id` bigint(0) NULL DEFAULT NULL COMMENT '绑定代理ID',
  `fingerprint_template_id` bigint(0) NULL DEFAULT NULL COMMENT '绑定指纹模板ID',
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'idle' COMMENT '状态：idle / locked / running',
  `isolation_level` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'strict',
  `local_profile_path` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '客户端本地Profile目录路径',
  `storage_root_path` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `download_root_path` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `startup_args_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '浏览器启动参数JSON',
  `isolation_policy_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `runtime_meta_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '运行时元数据JSON',
  `workspace_key` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `profile_root_path` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `artifact_root_path` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `temp_root_path` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `lifecycle_state` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'created',
  `last_used_at` datetime(0) NULL DEFAULT NULL COMMENT '最后使用时间',
  `last_isolation_check_at` datetime(0) NULL DEFAULT NULL,
  `last_started_at` datetime(0) NULL DEFAULT NULL,
  `last_stopped_at` datetime(0) NULL DEFAULT NULL,
  `last_rebuild_at` datetime(0) NULL DEFAULT NULL,
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_browser_profiles_name`(`name`) USING BTREE,
  INDEX `idx_browser_profiles_owner_agent_id`(`owner_agent_id`) USING BTREE,
  INDEX `idx_browser_profiles_proxy_id`(`proxy_id`) USING BTREE,
  INDEX `idx_browser_profiles_fingerprint_template_id`(`fingerprint_template_id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 9 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '浏览器隔离Profile表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of browser_profiles
-- ----------------------------
INSERT INTO `browser_profiles` VALUES (5, 'TikTok Profile 01', 1, 1, 3, 'idle', 'strict', 'D:\\bap\\profiles\\tiktok_01', 'D:\\bap\\storage\\tiktok_01', 'D:\\bap\\downloads\\tiktok_01', '[\"--start-maximized\"]', '{\n  \"timezone\": \"Asia/Shanghai\",\n  \"locale\": \"zh-CN\",\n  \"webrtc\": \"disabled\"\n}', '{\"level\":\"strict\",\"localProfilePath\":\"D:\\\\bap\\\\profiles\\\\tiktok_01\",\"storageRootPath\":\"D:\\\\bap\\\\storage\\\\tiktok_01\",\"downloadRootPath\":\"D:\\\\bap\\\\downloads\\\\tiktok_01\",\"proxy\":{\"Protocol\":\"http\",\"Host\":\"127.0.0.1\",\"Port\":0,\"hasAuth\":false},\"fingerprint\":{\"Id\":3,\"Name\":\"TikTok Desktop CN\"},\"rawPolicy\":{\"timezone\":\"Asia/Shanghai\",\"locale\":\"zh-CN\",\"webrtc\":\"disabled\"},\"lifecycle\":{\"state\":\"ready\",\"updatedAt\":\"2026-04-12T17:34:30.1440676Z\",\"finalState\":\"completed\"},\"workspace\":{\"workspaceKey\":\"tiktok_ws_01\",\"profileRootPath\":\"D:\\\\bap\\\\profiles\\\\tiktok_01\",\"localProfilePath\":\"D:\\\\bap\\\\profiles\\\\tiktok_01\",\"storageRootPath\":\"D:\\\\bap\\\\storage\\\\tiktok_01\",\"downloadRootPath\":\"D:\\\\bap\\\\downloads\\\\tiktok_01\",\"artifactRootPath\":\"D:\\\\bap\\\\artifacts\\\\tiktok_01\",\"tempRootPath\":\"D:\\\\bap\\\\tmp\\\\tiktok_01\"}}', 'tiktok_ws_01', 'D:\\bap\\profiles\\tiktok_01', 'D:\\bap\\artifacts\\tiktok_01', 'D:\\bap\\tmp\\tiktok_01', 'ready', '2026-04-12 17:34:30', '2026-04-12 17:34:29', '2026-04-05 09:17:52', '2026-04-12 17:34:30', NULL, '2026-04-05 09:15:20');
INSERT INTO `browser_profiles` VALUES (8, 'DEMO Profile Isolated', 1, 1, 3, 'idle', 'strict', '/tmp/bap/demo/profile-isolated', '/tmp/bap/demo/storage-isolated', '/tmp/bap/demo/download-isolated', '[\"--start-maximized\"]', '{\"timezone\":\"Asia/Shanghai\",\"locale\":\"zh-CN\",\"webrtc\":\"disabled\"}', '{}', '', '', '', '', 'created', '2026-04-05 06:41:42', '2026-04-05 09:26:42', NULL, NULL, NULL, '2026-04-05 09:41:42');
INSERT INTO `browser_profiles` VALUES (9, 'DEMO Profile Standard', NULL, NULL, 3, 'idle', 'standard', '/tmp/bap/demo/profile-standard', '/tmp/bap/demo/storage-standard', '/tmp/bap/demo/download-standard', '[]', '{\"timezone\":\"UTC\",\"locale\":\"en-US\"}', '{\"level\":\"standard\",\"localProfilePath\":\"/tmp/bap/demo/profile-standard\",\"storageRootPath\":\"/tmp/bap/demo/storage-standard\",\"downloadRootPath\":\"/tmp/bap/demo/download-standard\",\"proxy\":null,\"fingerprint\":{\"Id\":3,\"Name\":\"TikTok Desktop CN\"},\"rawPolicy\":{\"timezone\":\"UTC\",\"locale\":\"en-US\"},\"lifecycle\":{\"state\":\"broken\",\"updatedAt\":\"2026-04-12T17:34:01.0637406Z\",\"finalState\":\"failed\"},\"workspace\":{\"workspaceKey\":\"\",\"profileRootPath\":\"\",\"localProfilePath\":\"/tmp/bap/demo/profile-standard\",\"storageRootPath\":\"/tmp/bap/demo/storage-standard\",\"downloadRootPath\":\"/tmp/bap/demo/download-standard\",\"artifactRootPath\":\"\",\"tempRootPath\":\"\"}}', '', '', '', '', 'broken', '2026-04-12 17:34:01', '2026-04-12 17:33:58', '2026-04-06 03:30:21', '2026-04-12 17:34:01', NULL, '2026-04-05 09:41:42');

-- ----------------------------
-- Table structure for device_profiles
-- ----------------------------
DROP TABLE IF EXISTS `device_profiles`;
CREATE TABLE `device_profiles`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT,
  `account_id` bigint(0) NOT NULL,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT '',
  `user_agent` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `viewport_width` int(0) NULL DEFAULT 1440,
  `viewport_height` int(0) NULL DEFAULT 900,
  `locale` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT 'en-US',
  `timezone_id` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT 'America/Los_Angeles',
  `created_at` datetime(0) NULL DEFAULT CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of device_profiles
-- ----------------------------
INSERT INTO `device_profiles` VALUES (1, 1, 'acc1_device', NULL, 1440, 900, 'en-US', 'America/Los_Angeles', '2026-03-29 04:33:32');

-- ----------------------------
-- Table structure for fingerprint_templates
-- ----------------------------
DROP TABLE IF EXISTS `fingerprint_templates`;
CREATE TABLE `fingerprint_templates`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '指纹模板主键ID',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '指纹模板名称',
  `config_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '指纹模板配置JSON',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '浏览器指纹模板表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of fingerprint_templates
-- ----------------------------
INSERT INTO `fingerprint_templates` VALUES (3, 'TikTok Desktop CN', '{\n  \"userAgent\": \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123 Safari/537.36\",\n  \"viewport\": { \"width\": 1366, \"height\": 768 },\n  \"locale\": \"zh-CN\",\n  \"timezoneId\": \"America/Los_Angeles\"\n}', '2026-04-05 08:47:25');

-- ----------------------------
-- Table structure for launch_profiles
-- ----------------------------
DROP TABLE IF EXISTS `launch_profiles`;
CREATE TABLE `launch_profiles`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `headless` tinyint(0) NULL DEFAULT 0,
  `created_at` datetime(0) NULL DEFAULT CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of launch_profiles
-- ----------------------------
INSERT INTO `launch_profiles` VALUES (1, 'default', 0, '2026-03-29 04:33:32');

-- ----------------------------
-- Table structure for proxies
-- ----------------------------
DROP TABLE IF EXISTS `proxies`;
CREATE TABLE `proxies`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '代理配置主键ID',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '代理名称',
  `protocol` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'http' COMMENT '代理协议：http / https / socks5',
  `host` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '代理主机地址',
  `port` int(0) NOT NULL DEFAULT 0 COMMENT '代理端口',
  `username` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '代理用户名',
  `password` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '代理密码',
  `notes` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '代理备注',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '代理配置表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of proxies
-- ----------------------------
INSERT INTO `proxies` VALUES (1, 'No Proxy', 'http', '127.0.0.1', 0, '', '', 'local no-proxy placeholder', '2026-03-29 13:44:20');
INSERT INTO `proxies` VALUES (2, 'Demo NoAuth Proxy', 'http', '127.0.0.1', 8080, '', '', 'Local demo proxy for UI testing', '2026-03-29 13:50:12');
INSERT INTO `proxies` VALUES (3, 'Demo Auth Proxy', 'socks5', '127.0.0.1', 1080, 'demo_user', 'demo_pass', 'Authenticated demo proxy', '2026-03-29 13:50:12');

-- ----------------------------
-- Table structure for proxy_bindings
-- ----------------------------
DROP TABLE IF EXISTS `proxy_bindings`;
CREATE TABLE `proxy_bindings`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT,
  `account_id` bigint(0) NOT NULL,
  `proxy_id` bigint(0) NULL DEFAULT NULL,
  `bind_mode` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT 'fixed',
  `created_at` datetime(0) NULL DEFAULT CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of proxy_bindings
-- ----------------------------
INSERT INTO `proxy_bindings` VALUES (1, 1, 1, 'fixed', '2026-03-29 04:33:32');

-- ----------------------------
-- Table structure for run_isolation_reports
-- ----------------------------
DROP TABLE IF EXISTS `run_isolation_reports`;
CREATE TABLE `run_isolation_reports`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT,
  `task_run_id` bigint(0) NOT NULL,
  `browser_profile_id` bigint(0) NOT NULL,
  `proxy_snapshot_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `fingerprint_snapshot_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `storage_check_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `network_check_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `result` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pass',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_run_isolation_reports_runid`(`task_run_id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of run_isolation_reports
-- ----------------------------
INSERT INTO `run_isolation_reports` VALUES (1, 2, 4, '{\"host\":\"127.0.0.1\",\"port\":8080}', '{\"name\":\"Default Desktop Chrome\"}', '{\"ok\":true}', '{\"ok\":true}', 'pass', '2026-03-29 11:54:13');
INSERT INTO `run_isolation_reports` VALUES (3, 18, 9, '{\"host\":\"127.0.0.1\",\"port\":8080}', '{\"name\":\"Default Desktop Chrome\"}', '{\"ok\":true}', '{\"ok\":true}', 'pass', '2026-04-06 01:57:29');

-- ----------------------------
-- Table structure for task_run_logs
-- ----------------------------
DROP TABLE IF EXISTS `task_run_logs`;
CREATE TABLE `task_run_logs`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '任务运行日志主键ID',
  `task_run_id` bigint(0) NOT NULL COMMENT '所属任务运行ID',
  `level` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'info' COMMENT '日志级别：info / warn / error / debug',
  `step_id` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '关联步骤ID',
  `message` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '日志内容',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_task_run_logs_task_run_id`(`task_run_id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 68 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '任务运行日志表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of task_run_logs
-- ----------------------------
INSERT INTO `task_run_logs` VALUES (10, 11, 'error', '', 'Run finished with status: failed', '2026-04-05 15:50:53');
INSERT INTO `task_run_logs` VALUES (17, 16, 'error', '', 'Run finished with status: failed', '2026-04-06 03:31:29');
INSERT INTO `task_run_logs` VALUES (18, 18, 'info', 'open', 'Opened target URL successfully', '2026-04-06 01:55:29');
INSERT INTO `task_run_logs` VALUES (19, 18, 'info', 'done', 'Run completed', '2026-04-06 01:57:29');
INSERT INTO `task_run_logs` VALUES (20, 19, 'error', 'open', 'DNS resolve failed', '2026-04-06 02:55:29');
INSERT INTO `task_run_logs` VALUES (21, 20, 'info', 'step_open', 'Executing open', '2026-04-06 03:53:40');
INSERT INTO `task_run_logs` VALUES (22, 20, 'error', '', 'Run finished with status: failed', '2026-04-06 03:53:41');
INSERT INTO `task_run_logs` VALUES (23, 21, 'info', 'open_home', 'Executing open', '2026-04-06 03:55:22');
INSERT INTO `task_run_logs` VALUES (24, 21, 'error', '', 'Run finished with status: failed', '2026-04-06 03:55:23');
INSERT INTO `task_run_logs` VALUES (25, 22, 'info', 'open_home', 'Executing open', '2026-04-06 03:55:25');
INSERT INTO `task_run_logs` VALUES (26, 22, 'error', '', 'Run finished with status: failed', '2026-04-06 03:55:26');
INSERT INTO `task_run_logs` VALUES (27, 23, 'info', 'open_home', 'Executing open', '2026-04-06 05:05:31');
INSERT INTO `task_run_logs` VALUES (28, 23, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-06 05:05:35');
INSERT INTO `task_run_logs` VALUES (29, 23, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-06 05:05:35');
INSERT INTO `task_run_logs` VALUES (30, 23, 'info', 'scroll_feed', 'Executing scroll', '2026-04-06 05:05:40');
INSERT INTO `task_run_logs` VALUES (31, 23, 'error', '', 'Run finished with status: failed', '2026-04-06 05:05:40');
INSERT INTO `task_run_logs` VALUES (32, 24, 'info', 'open_home', 'Executing open', '2026-04-06 15:29:53');
INSERT INTO `task_run_logs` VALUES (33, 24, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-06 15:29:54');
INSERT INTO `task_run_logs` VALUES (34, 24, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-06 15:29:54');
INSERT INTO `task_run_logs` VALUES (35, 24, 'info', 'scroll_feed', 'Executing scroll', '2026-04-06 15:29:58');
INSERT INTO `task_run_logs` VALUES (36, 24, 'error', '', 'Run finished with status: failed', '2026-04-06 15:29:58');
INSERT INTO `task_run_logs` VALUES (37, 25, 'info', 'open_home', 'Executing open', '2026-04-06 15:30:51');
INSERT INTO `task_run_logs` VALUES (38, 25, 'info', 'open_home', 'Executing open', '2026-04-06 15:30:54');
INSERT INTO `task_run_logs` VALUES (39, 25, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-06 15:30:55');
INSERT INTO `task_run_logs` VALUES (40, 25, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-06 15:30:57');
INSERT INTO `task_run_logs` VALUES (41, 25, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-06 15:30:59');
INSERT INTO `task_run_logs` VALUES (42, 25, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-06 15:31:02');
INSERT INTO `task_run_logs` VALUES (43, 25, 'info', 'scroll_feed', 'Executing scroll', '2026-04-06 15:31:09');
INSERT INTO `task_run_logs` VALUES (44, 25, 'error', '', 'Run finished with status: failed', '2026-04-06 15:31:09');
INSERT INTO `task_run_logs` VALUES (45, 26, 'error', '', 'Run finished with status: failed', '2026-04-07 09:00:03');
INSERT INTO `task_run_logs` VALUES (46, 27, 'info', 'open_home', 'Executing open', '2026-04-07 09:00:36');
INSERT INTO `task_run_logs` VALUES (47, 27, 'info', 'open_home', 'Executing open', '2026-04-07 09:00:39');
INSERT INTO `task_run_logs` VALUES (48, 27, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-07 09:00:42');
INSERT INTO `task_run_logs` VALUES (49, 27, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-07 09:00:44');
INSERT INTO `task_run_logs` VALUES (50, 27, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-07 09:00:47');
INSERT INTO `task_run_logs` VALUES (51, 27, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-07 09:01:14');
INSERT INTO `task_run_logs` VALUES (52, 27, 'info', 'scroll_feed', 'Executing scroll', '2026-04-07 09:01:21');
INSERT INTO `task_run_logs` VALUES (53, 27, 'error', '', 'Run finished with status: failed', '2026-04-07 09:01:21');
INSERT INTO `task_run_logs` VALUES (54, 28, 'error', '', 'Run finished with status: failed', '2026-04-08 09:00:40');
INSERT INTO `task_run_logs` VALUES (55, 29, 'info', 'open_home', 'Executing open', '2026-04-08 09:01:13');
INSERT INTO `task_run_logs` VALUES (56, 29, 'info', 'open_home', 'Executing open', '2026-04-08 09:01:16');
INSERT INTO `task_run_logs` VALUES (57, 29, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-08 09:01:18');
INSERT INTO `task_run_logs` VALUES (58, 29, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-08 09:01:20');
INSERT INTO `task_run_logs` VALUES (59, 29, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-08 09:01:23');
INSERT INTO `task_run_logs` VALUES (60, 29, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-08 09:01:25');
INSERT INTO `task_run_logs` VALUES (61, 29, 'info', 'scroll_feed', 'Executing scroll', '2026-04-08 09:01:57');
INSERT INTO `task_run_logs` VALUES (62, 29, 'error', '', 'Run finished with status: failed', '2026-04-08 09:01:57');
INSERT INTO `task_run_logs` VALUES (63, 30, 'error', '', 'Run finished with status: failed', '2026-04-09 09:00:16');
INSERT INTO `task_run_logs` VALUES (64, 31, 'info', 'open_home', 'Executing open', '2026-04-09 09:00:49');
INSERT INTO `task_run_logs` VALUES (65, 31, 'error', '', 'Run finished with status: failed', '2026-04-09 09:00:50');
INSERT INTO `task_run_logs` VALUES (66, 32, 'error', '', 'Run finished with status: failed', '2026-04-10 09:00:44');
INSERT INTO `task_run_logs` VALUES (67, 33, 'info', 'open_home', 'Executing open', '2026-04-10 09:01:17');
INSERT INTO `task_run_logs` VALUES (68, 33, 'error', '', 'Run finished with status: failed', '2026-04-10 09:01:18');
INSERT INTO `task_run_logs` VALUES (69, 34, 'error', '', 'Run finished with status: failed', '2026-04-12 17:33:51');
INSERT INTO `task_run_logs` VALUES (70, 35, 'info', 'open_home', 'Executing open', '2026-04-12 17:33:56');
INSERT INTO `task_run_logs` VALUES (71, 39, 'info', 'tiktok_session', 'Executing tiktok_mock_session', '2026-04-12 17:33:59');
INSERT INTO `task_run_logs` VALUES (72, 39, 'error', '', 'Run finished with status: failed', '2026-04-12 17:34:01');
INSERT INTO `task_run_logs` VALUES (73, 35, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-12 17:34:09');
INSERT INTO `task_run_logs` VALUES (74, 35, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-12 17:34:09');
INSERT INTO `task_run_logs` VALUES (75, 35, 'info', 'scroll_feed', 'Executing scroll', '2026-04-12 17:34:14');
INSERT INTO `task_run_logs` VALUES (76, 35, 'error', '', 'Run finished with status: failed', '2026-04-12 17:34:14');
INSERT INTO `task_run_logs` VALUES (77, 36, 'info', 'open_home', 'Executing open', '2026-04-12 17:34:17');
INSERT INTO `task_run_logs` VALUES (78, 36, 'info', 'wait_page', 'Executing wait_for_element', '2026-04-12 17:34:18');
INSERT INTO `task_run_logs` VALUES (79, 36, 'info', 'warmup_wait', 'Executing wait_for_timeout', '2026-04-12 17:34:19');
INSERT INTO `task_run_logs` VALUES (80, 36, 'info', 'scroll_feed', 'Executing scroll', '2026-04-12 17:34:23');
INSERT INTO `task_run_logs` VALUES (81, 36, 'error', '', 'Run finished with status: failed', '2026-04-12 17:34:23');
INSERT INTO `task_run_logs` VALUES (82, 37, 'info', 'open_reddit_home', 'Executing open', '2026-04-12 17:34:26');
INSERT INTO `task_run_logs` VALUES (83, 37, 'info', 'wait_home_ready', 'Executing wait_for_element', '2026-04-12 17:34:28');
INSERT INTO `task_run_logs` VALUES (84, 37, 'info', 'done', 'Executing end_success', '2026-04-12 17:34:28');
INSERT INTO `task_run_logs` VALUES (85, 37, 'info', '', 'Run finished with status: completed', '2026-04-12 17:34:28');
INSERT INTO `task_run_logs` VALUES (86, 38, 'info', 'open_reddit_home', 'Executing open', '2026-04-12 17:34:29');
INSERT INTO `task_run_logs` VALUES (87, 38, 'info', 'wait_home_ready', 'Executing wait_for_element', '2026-04-12 17:34:30');
INSERT INTO `task_run_logs` VALUES (88, 38, 'info', 'done', 'Executing end_success', '2026-04-12 17:34:30');
INSERT INTO `task_run_logs` VALUES (89, 38, 'info', '', 'Run finished with status: completed', '2026-04-12 17:34:30');

-- ----------------------------
-- Table structure for task_runs
-- ----------------------------
DROP TABLE IF EXISTS `task_runs`;
CREATE TABLE `task_runs`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '任务运行主键ID',
  `task_id` bigint(0) NOT NULL COMMENT '所属任务ID',
  `browser_profile_id` bigint(0) NOT NULL COMMENT '执行时使用的Profile ID',
  `assigned_agent_id` bigint(0) NULL DEFAULT NULL COMMENT '实际分配的Agent ID',
  `lease_token` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'queued' COMMENT '运行状态：queued / leased / running / completed / failed / cancelled',
  `retry_count` int(0) NOT NULL DEFAULT 0,
  `max_retries` int(0) NOT NULL DEFAULT 0,
  `current_step_id` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '当前执行步骤ID',
  `current_step_label` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '当前执行步骤名称',
  `current_url` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '当前页面URL',
  `result_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '执行结果JSON',
  `error_code` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `error_message` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `last_preview_path` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '最后一次预览截图路径',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  `started_at` datetime(0) NULL DEFAULT NULL COMMENT '开始执行时间',
  `heartbeat_at` datetime(0) NULL DEFAULT NULL,
  `finished_at` datetime(0) NULL DEFAULT NULL COMMENT '结束时间',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_task_runs_status_agent`(`status`, `assigned_agent_id`) USING BTREE,
  INDEX `idx_task_runs_task_id`(`task_id`) USING BTREE,
  INDEX `idx_task_runs_browser_profile_id`(`browser_profile_id`) USING BTREE,
  INDEX `idx_task_runs_status_priority_created`(`status`, `created_at`) USING BTREE,
  INDEX `idx_task_runs_lease_token`(`lease_token`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 33 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '任务运行实例表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of task_runs
-- ----------------------------
INSERT INTO `task_runs` VALUES (1, 3, 3, NULL, '', 'queued', 0, 1, '', '', '', '{}', '', '', '', '2026-03-29 13:45:13', NULL, NULL, NULL);
INSERT INTO `task_runs` VALUES (2, 4, 4, 2, '', 'completed', 0, 0, 'done', '完成', 'https://httpbin.org/get', '{\"ok\":true}', '', '', '', '2026-03-29 11:50:13', '2026-03-29 11:51:13', '2026-03-29 11:53:13', '2026-03-29 11:54:13');
INSERT INTO `task_runs` VALUES (3, 5, 3, 2, '', 'failed', 1, 2, 'open', '打开页面', 'https://invalid.domain.example', '{\"ok\":false}', 'dns_error', 'Domain not found', '', '2026-03-29 12:50:13', '2026-03-29 12:51:13', '2026-03-29 12:52:13', '2026-03-29 12:53:13');
INSERT INTO `task_runs` VALUES (4, 7, 5, 1, '', 'completed', 0, 0, 'done', '完成', 'http://localhost:3001/feed?videoId=104', '{\"tiktok_session\":{\"mode\":\"tiktok_mock_session\",\"baseUrl\":\"http://localhost:3001\",\"watchedVideos\":3,\"watchedTarget\":3,\"likedVideos\":2,\"likeTarget\":2,\"commentedVideos\":2,\"commentTarget\":2,\"behaviorMetrics\":{\"avgWatchMs\":5354,\"watchPattern\":\"engaged\",\"behaviorProfile\":\"balanced\",\"commentProvider\":\"deepseek\",\"avgTypingDelayMs\":156.34074074074073,\"typingChars\":135,\"commentDuplicateRate\":0,\"anomalyRate\":0}}}', '', '', '/data/artifacts/4/final_20260405091849051.png', '2026-04-05 09:17:51', NULL, '2026-04-05 09:18:49', '2026-04-05 09:18:49');
INSERT INTO `task_runs` VALUES (5, 8, 5, 1, '', 'failed', 0, 1, '', '', '', '{\"error\":\"The given key was not present in the dictionary.\"}', 'executor_error', 'The given key was not present in the dictionary.', '', '2026-04-05 09:46:43', NULL, '2026-04-05 09:46:43', '2026-04-05 09:46:44');
INSERT INTO `task_runs` VALUES (6, 1, 1, NULL, '', 'queued', 0, 1, '', '', '', '{}', '', '', '', '2026-04-05 10:00:48', NULL, NULL, NULL);
INSERT INTO `task_runs` VALUES (7, 2, 2, NULL, '', 'queued', 0, 1, '', '', '', '{}', '', '', '', '2026-04-05 10:00:48', NULL, NULL, NULL);
INSERT INTO `task_runs` VALUES (8, 6, 4, NULL, '', 'queued', 0, 1, '', '', '', '{}', '', '', '', '2026-04-05 10:00:48', NULL, NULL, NULL);
INSERT INTO `task_runs` VALUES (11, 9, 5, 1, '', 'failed', 0, 1, '', '', '', '{\"error\":\"The given key was not present in the dictionary.\"}', 'executor_error', 'The given key was not present in the dictionary.', '', '2026-04-05 15:50:53', NULL, '2026-04-05 15:50:53', '2026-04-05 15:50:53');
INSERT INTO `task_runs` VALUES (16, 9, 5, 1, '', 'failed', 0, 1, '', '', '', '{\"error\":\"The given key was not present in the dictionary.\"}', 'executor_error', 'The given key was not present in the dictionary.', '', '2026-04-06 03:31:27', NULL, '2026-04-06 03:31:29', '2026-04-06 03:31:29');
INSERT INTO `task_runs` VALUES (17, 14, 8, NULL, '', 'queued', 0, 1, '', '', '', '{}', '', '', '', '2026-04-06 03:48:29', NULL, NULL, NULL);
INSERT INTO `task_runs` VALUES (18, 15, 9, 2, '', 'completed', 0, 0, 'done', '完成', 'https://httpbin.org/get', '{\"ok\":true}', '', '', '', '2026-04-06 01:53:29', '2026-04-06 01:54:29', '2026-04-06 01:56:29', '2026-04-06 01:57:29');
INSERT INTO `task_runs` VALUES (19, 16, 8, 2, '', 'failed', 1, 2, 'open', '打开页面', 'https://invalid.domain.example', '{\"ok\":false}', 'dns_error', 'Domain not found', '', '2026-04-06 02:53:29', '2026-04-06 02:54:29', '2026-04-06 02:55:29', '2026-04-06 02:56:29');
INSERT INTO `task_runs` VALUES (20, 17, 9, 1, '', 'failed', 0, 1, 'step_open', '打开百度首页', 'about:blank', '{\"error\":\"net::ERR_EMPTY_RESPONSE at https://www.baidu.com/\\nCall log:\\n  - navigating to \\u0022https://www.baidu.com/\\u0022, waiting until \\u0022load\\u0022\"}', 'executor_error', 'net::ERR_EMPTY_RESPONSE at https://www.baidu.com/\nCall log:\n  - navigating to \"https://www.baidu.com/\", waiting until \"load\"', '/data/artifacts/20/preview_20260406035340089.png', '2026-04-06 03:53:30', NULL, '2026-04-06 03:53:40', '2026-04-06 03:53:41');
INSERT INTO `task_runs` VALUES (21, 18, 5, 1, '', 'failed', 0, 1, 'open_home', '打开 TikTok 首页', 'about:blank', '{\"error\":\"net::ERR_EMPTY_RESPONSE at https://www.tiktok.com/\\nCall log:\\n  - navigating to \\u0022https://www.tiktok.com/\\u0022, waiting until \\u0022load\\u0022\"}', 'executor_error', 'net::ERR_EMPTY_RESPONSE at https://www.tiktok.com/\nCall log:\n  - navigating to \"https://www.tiktok.com/\", waiting until \"load\"', '/data/artifacts/21/preview_20260406035522211.png', '2026-04-06 03:55:20', NULL, '2026-04-06 03:55:22', '2026-04-06 03:55:23');
INSERT INTO `task_runs` VALUES (22, 18, 5, 1, '', 'failed', 0, 1, 'open_home', '打开 TikTok 首页', 'chrome-error://chromewebdata/', '{\"error\":\"net::ERR_EMPTY_RESPONSE at https://www.tiktok.com/\\nCall log:\\n  - navigating to \\u0022https://www.tiktok.com/\\u0022, waiting until \\u0022load\\u0022\"}', 'executor_error', 'net::ERR_EMPTY_RESPONSE at https://www.tiktok.com/\nCall log:\n  - navigating to \"https://www.tiktok.com/\", waiting until \"load\"', '/data/artifacts/22/preview_20260406035525077.png', '2026-04-06 03:55:23', NULL, '2026-04-06 03:55:25', '2026-04-06 03:55:26');
INSERT INTO `task_runs` VALUES (23, 18, 5, 1, '', 'failed', 0, 1, 'scroll_feed', '下滑一屏', 'https://www.tiktok.com/explore', '{\"error\":\"ReferenceError: arguments is not defined\\n    at eval (eval at evaluate (:234:30), \\u003Canonymous\\u003E:1:20)\\n    at eval (\\u003Canonymous\\u003E)\\n    at UtilityScript.evaluate (\\u003Canonymous\\u003E:234:30)\\n    at UtilityScript.\\u003Canonymous\\u003E (\\u003Canonymous\\u003E:1:44)\"}', 'executor_error', 'ReferenceError: arguments is not defined\n    at eval (eval at evaluate (:234:30), <anonymous>:1:20)\n    at eval (<anonymous>)\n    at UtilityScript.evaluate (<anonymous>:234:30)\n    at UtilityScript.<anonymous> (<anonymous>:1:44)', '/data/artifacts/23/preview_20260406050539811.png', '2026-04-06 05:05:28', NULL, '2026-04-06 05:05:40', '2026-04-06 05:05:40');
INSERT INTO `task_runs` VALUES (24, 18, 5, 1, '', 'failed', 0, 1, 'scroll_feed', '下滑一屏', 'https://www.tiktok.com/', '{\"error\":\"ReferenceError: arguments is not defined\\n    at eval (eval at evaluate (:234:30), \\u003Canonymous\\u003E:1:20)\\n    at eval (\\u003Canonymous\\u003E)\\n    at UtilityScript.evaluate (\\u003Canonymous\\u003E:234:30)\\n    at UtilityScript.\\u003Canonymous\\u003E (\\u003Canonymous\\u003E:1:44)\"}', 'executor_error', 'ReferenceError: arguments is not defined\n    at eval (eval at evaluate (:234:30), <anonymous>:1:20)\n    at eval (<anonymous>)\n    at UtilityScript.evaluate (<anonymous>:234:30)\n    at UtilityScript.<anonymous> (<anonymous>:1:44)', '/data/artifacts/24/preview_20260406152958149.png', '2026-04-06 15:29:51', NULL, '2026-04-06 15:29:58', '2026-04-06 15:29:58');
INSERT INTO `task_runs` VALUES (25, 18, 5, 1, '', 'failed', 0, 1, 'scroll_feed', '下滑一屏', 'https://www.tiktok.com/', '{\"error\":\"ReferenceError: arguments is not defined\\n    at eval (eval at evaluate (:234:30), \\u003Canonymous\\u003E:1:20)\\n    at eval (\\u003Canonymous\\u003E)\\n    at UtilityScript.evaluate (\\u003Canonymous\\u003E:234:30)\\n    at UtilityScript.\\u003Canonymous\\u003E (\\u003Canonymous\\u003E:1:44)\"}', 'executor_error', 'ReferenceError: arguments is not defined\n    at eval (eval at evaluate (:234:30), <anonymous>:1:20)\n    at eval (<anonymous>)\n    at UtilityScript.evaluate (<anonymous>:234:30)\n    at UtilityScript.<anonymous> (<anonymous>:1:44)', '/data/artifacts/25/preview_20260406153109067.png', '2026-04-06 15:30:20', NULL, '2026-04-06 15:31:09', '2026-04-06 15:31:09');
INSERT INTO `task_runs` VALUES (26, 9, 5, 1, '', 'failed', 0, 1, '', '', '', '{\"error\":\"The given key was not present in the dictionary.\"}', 'executor_error', 'The given key was not present in the dictionary.', '', '2026-04-07 09:00:02', NULL, '2026-04-07 09:00:03', '2026-04-07 09:00:03');
INSERT INTO `task_runs` VALUES (27, 18, 5, 1, '', 'failed', 0, 1, 'scroll_feed', '下滑一屏', 'https://www.tiktok.com/', '{\"error\":\"ReferenceError: arguments is not defined\\n    at eval (eval at evaluate (:234:30), \\u003Canonymous\\u003E:1:20)\\n    at eval (\\u003Canonymous\\u003E)\\n    at UtilityScript.evaluate (\\u003Canonymous\\u003E:234:30)\\n    at UtilityScript.\\u003Canonymous\\u003E (\\u003Canonymous\\u003E:1:44)\"}', 'executor_error', 'ReferenceError: arguments is not defined\n    at eval (eval at evaluate (:234:30), <anonymous>:1:20)\n    at eval (<anonymous>)\n    at UtilityScript.evaluate (<anonymous>:234:30)\n    at UtilityScript.<anonymous> (<anonymous>:1:44)', '/data/artifacts/27/preview_20260407090046747.png', '2026-04-07 09:00:02', NULL, '2026-04-07 09:01:21', '2026-04-07 09:01:21');
INSERT INTO `task_runs` VALUES (28, 9, 5, 1, '', 'failed', 0, 1, '', '', '', '{\"error\":\"The given key was not present in the dictionary.\"}', 'executor_error', 'The given key was not present in the dictionary.', '', '2026-04-08 09:00:38', NULL, '2026-04-08 09:00:40', '2026-04-08 09:00:40');
INSERT INTO `task_runs` VALUES (29, 18, 5, 1, '', 'failed', 0, 1, 'scroll_feed', '下滑一屏', 'https://www.tiktok.com/', '{\"error\":\"ReferenceError: arguments is not defined\\n    at eval (eval at evaluate (:234:30), \\u003Canonymous\\u003E:1:20)\\n    at eval (\\u003Canonymous\\u003E)\\n    at UtilityScript.evaluate (\\u003Canonymous\\u003E:234:30)\\n    at UtilityScript.\\u003Canonymous\\u003E (\\u003Canonymous\\u003E:1:44)\"}', 'executor_error', 'ReferenceError: arguments is not defined\n    at eval (eval at evaluate (:234:30), <anonymous>:1:20)\n    at eval (<anonymous>)\n    at UtilityScript.evaluate (<anonymous>:234:30)\n    at UtilityScript.<anonymous> (<anonymous>:1:44)', '/data/artifacts/29/preview_20260408090125003.png', '2026-04-08 09:00:38', NULL, '2026-04-08 09:01:57', '2026-04-08 09:01:57');
INSERT INTO `task_runs` VALUES (30, 9, 5, 1, '', 'failed', 0, 1, '', '', '', '{\"error\":\"The given key was not present in the dictionary.\"}', 'executor_error', 'The given key was not present in the dictionary.', '', '2026-04-09 09:00:13', NULL, '2026-04-09 09:00:16', '2026-04-09 09:00:16');
INSERT INTO `task_runs` VALUES (31, 18, 5, 1, '', 'failed', 0, 1, 'open_home', '打开 TikTok 首页', 'https://www.tiktok.com/', '{\"error\":\"net::ERR_CONNECTION_CLOSED at https://www.tiktok.com/\\nCall log:\\n  - navigating to \\u0022https://www.tiktok.com/\\u0022, waiting until \\u0022load\\u0022\"}', 'executor_error', 'net::ERR_CONNECTION_CLOSED at https://www.tiktok.com/\nCall log:\n  - navigating to \"https://www.tiktok.com/\", waiting until \"load\"', '', '2026-04-09 09:00:13', NULL, '2026-04-09 09:00:49', '2026-04-09 09:00:50');
INSERT INTO `task_runs` VALUES (32, 9, 5, 1, '', 'failed', 0, 1, '', '', '', '{\"error\":\"The given key was not present in the dictionary.\"}', 'executor_error', 'The given key was not present in the dictionary.', '', '2026-04-10 09:00:43', NULL, '2026-04-10 09:00:44', '2026-04-10 09:00:44');
INSERT INTO `task_runs` VALUES (33, 18, 5, 1, '', 'failed', 0, 1, 'open_home', '打开 TikTok 首页', 'chrome-error://chromewebdata/', '{\"error\":\"net::ERR_CONNECTION_CLOSED at https://www.tiktok.com/\\nCall log:\\n  - navigating to \\u0022https://www.tiktok.com/\\u0022, waiting until \\u0022load\\u0022\"}', 'executor_error', 'net::ERR_CONNECTION_CLOSED at https://www.tiktok.com/\nCall log:\n  - navigating to \"https://www.tiktok.com/\", waiting until \"load\"', '', '2026-04-10 09:00:43', NULL, '2026-04-10 09:01:17', '2026-04-10 09:01:18');
INSERT INTO `task_runs` VALUES (34, 9, 5, 1, '', 'failed', 0, 1, '', '', '', '{\"error\":\"The given key was not present in the dictionary.\"}', 'executor_error', 'The given key was not present in the dictionary.', '', '2026-04-12 09:33:01', NULL, '2026-04-12 17:33:26', '2026-04-12 17:33:51');
INSERT INTO `task_runs` VALUES (35, 18, 5, 1, '', 'failed', 0, 1, 'scroll_feed', '下滑一屏', 'https://www.tiktok.com/', '{\"error\":\"ReferenceError: arguments is not defined\\n    at eval (eval at evaluate (:234:30), \\u003Canonymous\\u003E:1:20)\\n    at eval (\\u003Canonymous\\u003E)\\n    at UtilityScript.evaluate (\\u003Canonymous\\u003E:234:30)\\n    at UtilityScript.\\u003Canonymous\\u003E (\\u003Canonymous\\u003E:1:44)\"}', 'executor_error', 'ReferenceError: arguments is not defined\n    at eval (eval at evaluate (:234:30), <anonymous>:1:20)\n    at eval (<anonymous>)\n    at UtilityScript.evaluate (<anonymous>:234:30)\n    at UtilityScript.<anonymous> (<anonymous>:1:44)', '/data/artifacts/35/preview_20260412173413495.png', '2026-04-12 09:33:01', NULL, '2026-04-12 17:34:14', '2026-04-12 17:34:14');
INSERT INTO `task_runs` VALUES (36, 18, 5, 1, '', 'failed', 0, 1, 'scroll_feed', '下滑一屏', 'https://www.tiktok.com/', '{\"error\":\"ReferenceError: arguments is not defined\\n    at eval (eval at evaluate (:234:30), \\u003Canonymous\\u003E:1:20)\\n    at eval (\\u003Canonymous\\u003E)\\n    at UtilityScript.evaluate (\\u003Canonymous\\u003E:234:30)\\n    at UtilityScript.\\u003Canonymous\\u003E (\\u003Canonymous\\u003E:1:44)\"}', 'executor_error', 'ReferenceError: arguments is not defined\n    at eval (eval at evaluate (:234:30), <anonymous>:1:20)\n    at eval (<anonymous>)\n    at UtilityScript.evaluate (<anonymous>:234:30)\n    at UtilityScript.<anonymous> (<anonymous>:1:44)', '/data/artifacts/36/preview_20260412173423010.png', '2026-04-12 09:37:24', NULL, '2026-04-12 17:34:23', '2026-04-12 17:34:23');
INSERT INTO `task_runs` VALUES (37, 19, 5, 1, '', 'completed', 0, 1, 'done', '完成', 'https://old.reddit.com/', '{}', '', '', '/data/artifacts/37/final_20260412173427941.png', '2026-04-12 09:38:02', NULL, '2026-04-12 17:34:28', '2026-04-12 17:34:28');
INSERT INTO `task_runs` VALUES (38, 19, 5, 1, '', 'completed', 0, 1, 'done', '完成', 'https://old.reddit.com/', '{}', '', '', '/data/artifacts/38/final_20260412173430117.png', '2026-04-12 09:38:06', NULL, '2026-04-12 17:34:30', '2026-04-12 17:34:30');
INSERT INTO `task_runs` VALUES (39, 20, 9, 1, '', 'failed', 0, 0, 'tiktok_session', 'workbench session', 'about:blank', '{\"error\":\"net::ERR_CONNECTION_REFUSED at http://localhost:3001/login\\nCall log:\\n  - navigating to \\u0022http://localhost:3001/login\\u0022, waiting until \\u0022load\\u0022\"}', 'executor_error', 'net::ERR_CONNECTION_REFUSED at http://localhost:3001/login\nCall log:\n  - navigating to \"http://localhost:3001/login\", waiting until \"load\"', '/data/artifacts/39/preview_20260412173358307.png', '2026-04-12 09:38:16', NULL, '2026-04-12 17:33:59', '2026-04-12 17:34:01');

-- ----------------------------
-- Table structure for task_templates
-- ----------------------------
DROP TABLE IF EXISTS `task_templates`;
CREATE TABLE `task_templates`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '任务模板主键ID',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '任务模板名称',
  `definition_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '任务模板定义JSON',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '任务模板表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of task_templates
-- ----------------------------
INSERT INTO `task_templates` VALUES (2, 'Facebook Login + Like + Comment Template', '{\r\n    \"steps\":[\r\n      { \"id\":\"open_login\", \"type\":\"open\", \"data\":{\"label\":\"打开登录页\",\"url\":\"http://localhost:3000/login\"} },\r\n      { \"id\":\"wait_login_form\", \"type\":\"wait_for_element\", \"data\":{\"label\":\"等待登录表单\",\"selector\":\"form[action=\'/login\']\",\"timeout\":10000} },\r\n      { \"id\":\"type_username\", \"type\":\"type\", \"data\":{\"label\":\"输入用户名\",\"selector\":\"input[name=\'username\']\",\"text\":\"alice\"} },\r\n      { \"id\":\"type_password\", \"type\":\"type\", \"data\":{\"label\":\"输入密码\",\"selector\":\"input[name=\'password\']\",\"text\":\"123456\"} },\r\n      { \"id\":\"click_login\", \"type\":\"click\", \"data\":{\"label\":\"点击登录\",\"selector\":\"button[type=\'submit\']\"} },\r\n      { \"id\":\"wait_feed\", \"type\":\"wait_for_element\", \"data\":{\"label\":\"等待Feed\",\"selector\":\"[data-testid=\'fb-feed\'], .feed, main\",\"timeout\":10000} },\r\n      { \"id\":\"click_comment_toggle\", \"type\":\"click\", \"data\":{\"label\":\"展开评论\",\"selector\":\"[data-testid=\'comment-toggle\'], .comment-btn\"} },\r\n      { \"id\":\"type_comment\", \"type\":\"type\", \"data\":{\"label\":\"输入评论\",\"selector\":\"[data-testid=\'comment-input\'], .comment-input\",\"text\":\"hello\"} },\r\n      { \"id\":\"submit_comment\", \"type\":\"click\", \"data\":{\"label\":\"提交评论\",\"selector\":\"[data-testid=\'comment-submit\'], .submit-comment, .submit\"} },\r\n      { \"id\":\"done\", \"type\":\"end_success\", \"data\":{\"label\":\"完成\"} }\r\n    ],\r\n    \"edges\":[\r\n      {\"source\":\"open_login\",\"target\":\"wait_login_form\"},\r\n      {\"source\":\"wait_login_form\",\"target\":\"type_username\"},\r\n      {\"source\":\"type_username\",\"target\":\"type_password\"},\r\n      {\"source\":\"type_password\",\"target\":\"click_login\"},\r\n      {\"source\":\"click_login\",\"target\":\"wait_feed\"},\r\n      {\"source\":\"wait_feed\",\"target\":\"click_comment_toggle\"},\r\n      {\"source\":\"click_comment_toggle\",\"target\":\"type_comment\"},\r\n      {\"source\":\"type_comment\",\"target\":\"submit_comment\"},\r\n      {\"source\":\"submit_comment\",\"target\":\"done\"}\r\n    ]\r\n  }', '2026-03-29 13:44:21');

-- ----------------------------
-- Table structure for tasks
-- ----------------------------
DROP TABLE IF EXISTS `tasks`;
CREATE TABLE `tasks`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '任务主键ID',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '任务名称',
  `browser_profile_id` bigint(0) NOT NULL COMMENT '绑定执行的Profile ID',
  `scheduling_strategy` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'profile_owner' COMMENT '调度策略：profile_owner / preferred_agent / least_loaded',
  `preferred_agent_id` bigint(0) NULL DEFAULT NULL COMMENT '优先Agent ID',
  `status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'queued' COMMENT '状态：queued / leased / running / completed / failed / cancelled',
  `payload_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '任务编排与参数JSON',
  `retry_policy_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `priority` int(0) NOT NULL DEFAULT 100 COMMENT '任务优先级，数值越小可表示越高优先级',
  `timeout_seconds` int(0) NOT NULL DEFAULT 900,
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  `account_id` bigint(0) NULL DEFAULT NULL,
  `is_enabled` tinyint(1) NOT NULL DEFAULT 1,
  `schedule_type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'manual',
  `schedule_config_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `next_run_at` datetime(0) NULL DEFAULT NULL,
  `last_run_at` datetime(0) NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_tasks_browser_profile_id`(`browser_profile_id`) USING BTREE,
  INDEX `idx_tasks_status`(`status`) USING BTREE,
  INDEX `idx_tasks_preferred_agent_id`(`preferred_agent_id`) USING BTREE,
  INDEX `idx_tasks_status_priority_created`(`status`, `priority`, `created_at`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 18 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '工作流任务表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of tasks
-- ----------------------------
INSERT INTO `tasks` VALUES (9, 'TK Daily Browse', 5, 'preferred_agent', 1, 'failed', '{\n  \"name\": \"TK Daily Browse\",\n  \"browserProfileId\": 1,\n  \"accountId\": 1,\n  \"schedulingStrategy\": \"least_loaded\",\n  \"preferredAgentId\": null,\n  \"isEnabled\": true,\n  \"scheduleType\": \"daily_window_random\",\n  \"scheduleConfigJson\": \"{\\\"timezone\\\":\\\"UTC\\\",\\\"windowStart\\\":\\\"01:00\\\",\\\"windowEnd\\\":\\\"03:00\\\",\\\"maxRunsPerDay\\\":1,\\\"randomMinuteStep\\\":5}\",\n  \"payloadJson\": \"{\\\"steps\\\":[{\\\"id\\\":\\\"tiktok_session\\\",\\\"type\\\":\\\"tiktok_mock_session\\\",\\\"data\\\":{\\\"label\\\":\\\"daily session\\\",\\\"baseUrl\\\":\\\"http://localhost:3001\\\",\\\"username\\\":\\\"alice\\\",\\\"password\\\":\\\"123456\\\",\\\"minVideos\\\":2,\\\"maxVideos\\\":4,\\\"minWatchMs\\\":2500,\\\"maxWatchMs\\\":7000,\\\"minLikes\\\":1,\\\"maxLikes\\\":2,\\\"minComments\\\":1,\\\"maxComments\\\":2,\\\"behaviorProfile\\\":\\\"balanced\\\",\\\"commentProvider\\\":\\\"deepseek\\\"}},{\\\"id\\\":\\\"done\\\",\\\"type\\\":\\\"end_success\\\",\\\"data\\\":{\\\"label\\\":\\\"完成\\\"}}],\\\"edges\\\":[{\\\"source\\\":\\\"tiktok_session\\\",\\\"target\\\":\\\"done\\\"}]}\",\n  \"priority\": 100,\n  \"timeoutSeconds\": 300,\n  \"retryPolicyJson\": \"{\\\"maxRetries\\\":1}\"\n}', '{\"maxRetries\":1}', 100, 300, '2026-04-05 10:01:47', 1, 1, 'daily_window_random', '{\"timezone\":\"UTC\",\"windowStart\":\"09:00\",\"windowEnd\":\"18:00\",\"maxRunsPerDay\":1,\"randomMinuteStep\":5}', '2026-04-13 09:00:00', '2026-04-12 09:33:01');
INSERT INTO `tasks` VALUES (14, 'DEMO Task Queued', 8, 'preferred_agent', 2, 'queued', '{\r\n  \"steps\": [\r\n    { \"id\": \"open\", \"type\": \"open\", \"data\": { \"label\": \"打开示例站点\", \"url\": \"https://example.com\" } },\r\n    { \"id\": \"wait\", \"type\": \"wait_for_timeout\", \"data\": { \"label\": \"等待页面稳定\", \"timeout\": 1200 } },\r\n    { \"id\": \"done\", \"type\": \"end_success\", \"data\": { \"label\": \"结束\" } }\r\n  ],\r\n  \"edges\": [\r\n    { \"source\": \"open\", \"target\": \"wait\" },\r\n    { \"source\": \"wait\", \"target\": \"done\" }\r\n  ]\r\n}', '{\"maxRetries\":1}', 200, 300, '2026-04-06 03:53:29', NULL, 1, 'manual', '{}', NULL, NULL);
INSERT INTO `tasks` VALUES (15, 'DEMO Task Completed', 9, 'least_loaded', NULL, 'completed', '{\r\n  \"steps\": [\r\n    { \"id\": \"open\", \"type\": \"open\", \"data\": { \"label\": \"打开 HTTPBin\", \"url\": \"https://httpbin.org/get\" } },\r\n    { \"id\": \"extract\", \"type\": \"extract_text\", \"data\": { \"label\": \"提取页面标题\", \"selector\": \"body\" } },\r\n    { \"id\": \"done\", \"type\": \"end_success\", \"data\": { \"label\": \"结束\" } }\r\n  ],\r\n  \"edges\": [\r\n    { \"source\": \"open\", \"target\": \"extract\" },\r\n    { \"source\": \"extract\", \"target\": \"done\" }\r\n  ]\r\n}', '{\"maxRetries\":0}', 100, 180, '2026-04-06 03:53:29', NULL, 1, 'manual', '{}', NULL, NULL);
INSERT INTO `tasks` VALUES (16, 'DEMO Task Failed', 8, 'profile_owner', NULL, 'failed', '{\r\n  \"steps\": [\r\n    { \"id\": \"open\", \"type\": \"open\", \"data\": { \"label\": \"打开不可达域名\", \"url\": \"https://invalid.domain.example\" } },\r\n    { \"id\": \"done\", \"type\": \"end_fail\", \"data\": { \"label\": \"结束失败\" } }\r\n  ],\r\n  \"edges\": [\r\n    { \"source\": \"open\", \"target\": \"done\" }\r\n  ]\r\n}', '{\"maxRetries\":2}', 80, 120, '2026-04-06 03:53:29', NULL, 1, 'manual', '{}', NULL, NULL);
INSERT INTO `tasks` VALUES (17, 'DEMO Task Baidu Search', 9, 'profile_owner', NULL, 'failed', '{\r\n  \"steps\": [\r\n    { \"id\": \"step_open\", \"type\": \"open\", \"data\": { \"label\": \"打开百度首页\", \"url\": \"https://www.baidu.com\" } },\r\n    { \"id\": \"step_wait_input\", \"type\": \"wait_for_element\", \"data\": { \"label\": \"等待搜索输入框\", \"selector\": \"textarea[name=\\\"wd\\\"]\", \"timeout\": 15000 } },\r\n    { \"id\": \"step_type_keyword\", \"type\": \"type\", \"data\": { \"label\": \"输入关键词\", \"selector\": \"textarea[name=\\\"wd\\\"]\", \"value\": \"BrowserAgentPlatform 自动化测试\" } },\r\n    { \"id\": \"step_click_search\", \"type\": \"click\", \"data\": { \"label\": \"点击搜索按钮\", \"selector\": \"#su\" } },\r\n    { \"id\": \"step_wait_result\", \"type\": \"wait_for_element\", \"data\": { \"label\": \"等待结果区域\", \"selector\": \"#content_left\", \"timeout\": 15000 } },\r\n    { \"id\": \"step_extract_title\", \"type\": \"extract_text\", \"data\": { \"label\": \"提取首条结果标题\", \"selector\": \"#content_left h3\" } },\r\n    { \"id\": \"step_done\", \"type\": \"end_success\", \"data\": { \"label\": \"完成\" } }\r\n  ],\r\n  \"edges\": [\r\n    { \"source\": \"step_open\", \"target\": \"step_wait_input\" },\r\n    { \"source\": \"step_wait_input\", \"target\": \"step_type_keyword\" },\r\n    { \"source\": \"step_type_keyword\", \"target\": \"step_click_search\" },\r\n    { \"source\": \"step_click_search\", \"target\": \"step_wait_result\" },\r\n    { \"source\": \"step_wait_result\", \"target\": \"step_extract_title\" },\r\n    { \"source\": \"step_extract_title\", \"target\": \"step_done\" }\r\n  ]\r\n}', '{\"maxRetries\":1}', 220, 240, '2026-04-06 03:53:29', NULL, 1, 'manual', '{}', NULL, NULL);
INSERT INTO `tasks` VALUES (18, 'tk自动浏览', 5, 'preferred_agent', 1, 'failed', '{\n  \"steps\": [\n    {\n      \"id\": \"open_home\",\n      \"type\": \"open\",\n      \"data\": {\n        \"label\": \"打开 TikTok 首页\",\n        \"url\": \"https://www.tiktok.com/\"\n      }\n    },\n    {\n      \"id\": \"wait_page\",\n      \"type\": \"wait_for_element\",\n      \"data\": {\n        \"label\": \"等待页面主体加载\",\n        \"selector\": \"body\",\n        \"timeout\": 20000\n      }\n    },\n    {\n      \"id\": \"warmup_wait\",\n      \"type\": \"wait_for_timeout\",\n      \"data\": {\n        \"label\": \"首屏稳定等待\",\n        \"timeout\": 4000\n      }\n    },\n    {\n      \"id\": \"scroll_feed\",\n      \"type\": \"scroll\",\n      \"data\": {\n        \"label\": \"下滑一屏\",\n        \"deltaY\": 900\n      }\n    },\n    {\n      \"id\": \"watch_wait\",\n      \"type\": \"wait_for_timeout\",\n      \"data\": {\n        \"label\": \"观看停留\",\n        \"timeout\": 3500\n      }\n    },\n    {\n      \"id\": \"loop_control\",\n      \"type\": \"loop\",\n      \"data\": {\n        \"label\": \"循环 20 次\",\n        \"count\": 20\n      }\n    },\n    {\n      \"id\": \"collect_result\",\n      \"type\": \"execute_js\",\n      \"data\": {\n        \"label\": \"收集浏览结果\",\n        \"script\": \"(() => { const title = document.title || \'\'; const url = location.href; const videoLinks = Array.from(document.querySelectorAll(\'a[href*=\\\"/video/\\\"]\')).slice(0, 10).map(a => a.href); return { title, url, sampledVideoLinks: videoLinks, sampledCount: videoLinks.length, ts: new Date().toISOString() }; })()\"\n      }\n    },\n    {\n      \"id\": \"done\",\n      \"type\": \"end_success\",\n      \"data\": {\n        \"label\": \"完成\"\n      }\n    }\n  ],\n  \"edges\": [\n    { \"source\": \"open_home\", \"target\": \"wait_page\" },\n    { \"source\": \"wait_page\", \"target\": \"warmup_wait\" },\n    { \"source\": \"warmup_wait\", \"target\": \"scroll_feed\" },\n    { \"source\": \"scroll_feed\", \"target\": \"watch_wait\" },\n    { \"source\": \"watch_wait\", \"target\": \"loop_control\" },\n    { \"source\": \"loop_control\", \"sourceHandle\": \"loop\", \"target\": \"scroll_feed\" },\n    { \"source\": \"loop_control\", \"sourceHandle\": \"done\", \"target\": \"collect_result\" },\n    { \"source\": \"collect_result\", \"target\": \"done\" }\n  ]\n}', '{\"maxRetries\":1}', 100, 300, '2026-04-06 03:55:19', NULL, 1, 'daily_window_random', '{\"timezone\":\"UTC\",\"windowStart\":\"09:00\",\"windowEnd\":\"18:00\",\"maxRunsPerDay\":1,\"randomMinuteStep\":5}', '2026-04-13 09:00:00', '2026-04-12 09:37:24');
INSERT INTO `tasks` VALUES (19, 'testreddit', 5, 'least_loaded', NULL, 'completed', '{\n  \"steps\": [\n    {\n      \"id\": \"open_reddit_home\",\n      \"type\": \"open\",\n      \"data\": {\n        \"label\": \"打开 Reddit 首页\",\n        \"url\": \"https://old.reddit.com/\"\n      }\n    },\n    {\n      \"id\": \"wait_home_ready\",\n      \"type\": \"wait_for_element\",\n      \"data\": {\n        \"label\": \"等待首页内容加载\",\n        \"selector\": \"body\",\n        \"timeout\": 20000\n      }\n    },\n    {\n      \"id\": \"done\",\n      \"type\": \"end_success\",\n      \"data\": {\n        \"label\": \"完成\"\n      }\n    }\n  ],\n  \"edges\": [\n    { \"source\": \"open_reddit_home\", \"target\": \"wait_home_ready\" },\n    { \"source\": \"wait_home_ready\", \"target\": \"done\" }\n  ]\n}\n', '{\"maxRetries\":1}', 100, 300, '2026-04-12 09:38:01', NULL, 1, 'manual', '{}', NULL, '2026-04-12 09:38:06');
INSERT INTO `tasks` VALUES (20, 'closed-loop-workbench', 9, 'preferred_agent', 1, 'failed', '{\"steps\":[{\"id\":\"tiktok_session\",\"type\":\"tiktok_mock_session\",\"data\":{\"label\":\"workbench session\",\"baseUrl\":\"http://localhost:3001\",\"username\":\"alice\",\"password\":\"123456\",\"minVideos\":2,\"maxVideos\":4,\"minWatchMs\":2500,\"maxWatchMs\":7000,\"minLikes\":1,\"maxLikes\":2,\"minComments\":1,\"maxComments\":2,\"behaviorProfile\":\"balanced\",\"commentProvider\":\"deepseek\"}},{\"id\":\"done\",\"type\":\"end_success\",\"data\":{\"label\":\"完成\"}}],\"edges\":[{\"source\":\"tiktok_session\",\"target\":\"done\"}]}', '{\"maxRetries\":0}', 999, 300, '2026-04-12 09:38:16', NULL, 1, 'manual', '{}', NULL, NULL);

-- ----------------------------
-- Table structure for users
-- ----------------------------
DROP TABLE IF EXISTS `users`;
CREATE TABLE `users`  (
  `id` bigint(0) NOT NULL AUTO_INCREMENT COMMENT '用户主键ID',
  `username` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '登录用户名，系统唯一',
  `password_hash` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '密码哈希值',
  `display_name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '' COMMENT '显示名称',
  `role` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'admin' COMMENT '角色：admin / user',
  `created_at` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uk_users_username`(`username`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '系统用户表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of users
-- ----------------------------
INSERT INTO `users` VALUES (1, 'admin', '$2a$11$5L9x0zd4zRMvxBmreQ8wUeXPmikTVzZGZklvnUBPnDiYOBgdvI/16', 'Admin', 'admin', '2026-03-28 07:27:18');

SET FOREIGN_KEY_CHECKS = 1;
