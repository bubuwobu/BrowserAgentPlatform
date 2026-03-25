using BCrypt.Net;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Backward-compatible bootstrap for environments that were created before
        // new tables were introduced (EnsureCreated does not evolve existing schema).
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS `audit_events` (
              `id` BIGINT NOT NULL AUTO_INCREMENT,
              `event_type` longtext NOT NULL,
              `actor_type` longtext NOT NULL,
              `actor_id` longtext NOT NULL,
              `target_type` longtext NOT NULL,
              `target_id` longtext NOT NULL,
              `details_json` longtext NOT NULL,
              `created_at` datetime(6) NOT NULL,
              CONSTRAINT `PK_audit_events` PRIMARY KEY (`id`)
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS `run_isolation_reports` (
              `id` BIGINT NOT NULL AUTO_INCREMENT,
              `task_run_id` BIGINT NOT NULL,
              `browser_profile_id` BIGINT NOT NULL,
              `proxy_snapshot_json` longtext NOT NULL,
              `fingerprint_snapshot_json` longtext NOT NULL,
              `storage_check_json` longtext NOT NULL,
              `network_check_json` longtext NOT NULL,
              `result` longtext NOT NULL,
              `created_at` datetime(6) NOT NULL,
              CONSTRAINT `PK_run_isolation_reports` PRIMARY KEY (`id`)
            );
            """);

        if (!await db.Users.AnyAsync())
        {
            db.Users.Add(new AppUser
            {
                Username = "admin",
                DisplayName = "Admin",
                Role = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456")
            });
        }

        if (!await db.FingerprintTemplates.AnyAsync())
        {
            db.FingerprintTemplates.Add(new FingerprintTemplate
            {
                Name = "Default Desktop Chrome",
                ConfigJson = """
                {
                  "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123 Safari/537.36",
                  "viewport": { "width": 1366, "height": 768 },
                  "locale": "zh-CN",
                  "timezoneId": "Asia/Singapore"
                }
                """
            });
        }

        var demoAgent1 = await db.Agents.FirstOrDefaultAsync(x => x.AgentKey == "demo-agent-1");
        if (demoAgent1 is null)
        {
            demoAgent1 = new AgentNode
            {
                AgentKey = "demo-agent-1",
                Name = "Demo Agent 1",
                MachineName = "demo-machine-1",
                Status = "online",
                MaxParallelRuns = 2,
                CurrentRuns = 0,
                SchedulerTags = "demo,default",
                LastHeartbeatAt = DateTime.UtcNow.AddSeconds(-10)
            };
            db.Agents.Add(demoAgent1);
        }

        var demoAgent2 = await db.Agents.FirstOrDefaultAsync(x => x.AgentKey == "demo-agent-2");
        if (demoAgent2 is null)
        {
            demoAgent2 = new AgentNode
            {
                AgentKey = "demo-agent-2",
                Name = "Demo Agent 2",
                MachineName = "demo-machine-2",
                Status = "offline",
                MaxParallelRuns = 1,
                CurrentRuns = 0,
                SchedulerTags = "demo,backup",
                LastHeartbeatAt = DateTime.UtcNow.AddMinutes(-30)
            };
            db.Agents.Add(demoAgent2);
        }

        var demoProxyNoAuth = await db.Proxies.FirstOrDefaultAsync(x => x.Name == "Demo NoAuth Proxy");
        if (demoProxyNoAuth is null)
        {
            demoProxyNoAuth = new ProxyConfig
            {
                Name = "Demo NoAuth Proxy",
                Protocol = "http",
                Host = "127.0.0.1",
                Port = 8080,
                Notes = "Local demo proxy for UI testing"
            };
            db.Proxies.Add(demoProxyNoAuth);
        }

        var demoProxyAuth = await db.Proxies.FirstOrDefaultAsync(x => x.Name == "Demo Auth Proxy");
        if (demoProxyAuth is null)
        {
            demoProxyAuth = new ProxyConfig
            {
                Name = "Demo Auth Proxy",
                Protocol = "socks5",
                Host = "127.0.0.1",
                Port = 1080,
                Username = "demo_user",
                Password = "demo_pass",
                Notes = "Authenticated demo proxy"
            };
            db.Proxies.Add(demoProxyAuth);
        }

        await db.SaveChangesAsync();

        var defaultFingerprint = await db.FingerprintTemplates.OrderBy(x => x.Id).FirstAsync();
        var firstAgent = await db.Agents.OrderBy(x => x.Id).FirstAsync();
        var firstProxy = await db.Proxies.OrderBy(x => x.Id).FirstOrDefaultAsync();

        var demoProfileIsolated = await db.BrowserProfiles.FirstOrDefaultAsync(x => x.Name == "DEMO Profile Isolated");
        if (demoProfileIsolated is null)
        {
            demoProfileIsolated = new BrowserProfile
            {
                Name = "DEMO Profile Isolated",
                OwnerAgentId = firstAgent.Id,
                ProxyId = firstProxy?.Id,
                FingerprintTemplateId = defaultFingerprint.Id,
                Status = "idle",
                IsolationLevel = "strict",
                LocalProfilePath = "/tmp/bap/demo/profile-isolated",
                StorageRootPath = "/tmp/bap/demo/storage-isolated",
                DownloadRootPath = "/tmp/bap/demo/download-isolated",
                StartupArgsJson = "[\"--start-maximized\"]",
                IsolationPolicyJson = "{\"timezone\":\"Asia/Shanghai\",\"locale\":\"zh-CN\",\"webrtc\":\"disabled\"}",
                RuntimeMetaJson = "{}",
                LastIsolationCheckAt = DateTime.UtcNow.AddMinutes(-15),
                LastUsedAt = DateTime.UtcNow.AddHours(-3)
            };
            db.BrowserProfiles.Add(demoProfileIsolated);
        }

        var demoProfileStandard = await db.BrowserProfiles.FirstOrDefaultAsync(x => x.Name == "DEMO Profile Standard");
        if (demoProfileStandard is null)
        {
            demoProfileStandard = new BrowserProfile
            {
                Name = "DEMO Profile Standard",
                OwnerAgentId = null,
                ProxyId = null,
                FingerprintTemplateId = defaultFingerprint.Id,
                Status = "idle",
                IsolationLevel = "standard",
                LocalProfilePath = "/tmp/bap/demo/profile-standard",
                StorageRootPath = "/tmp/bap/demo/storage-standard",
                DownloadRootPath = "/tmp/bap/demo/download-standard",
                StartupArgsJson = "[]",
                IsolationPolicyJson = "{\"timezone\":\"UTC\",\"locale\":\"en-US\"}",
                RuntimeMetaJson = "{}",
                LastIsolationCheckAt = DateTime.UtcNow.AddHours(-1),
                LastUsedAt = DateTime.UtcNow.AddHours(-1)
            };
            db.BrowserProfiles.Add(demoProfileStandard);
        }

        await db.SaveChangesAsync();

        var hasDemoTasks = await db.Tasks.AnyAsync(x => x.Name.StartsWith("DEMO Task "));
        if (!hasDemoTasks)
        {
            var isolatedProfile = await db.BrowserProfiles.FirstAsync(x => x.Name == "DEMO Profile Isolated");
            var standardProfile = await db.BrowserProfiles.FirstAsync(x => x.Name == "DEMO Profile Standard");
            var onlineAgent = await db.Agents.FirstOrDefaultAsync(x => x.AgentKey == "demo-agent-1") ?? firstAgent;
            var queuedPayloadJson = """
            {
              "steps": [
                { "id": "open", "type": "open", "data": { "label": "打开示例站点", "url": "https://example.com" } },
                { "id": "wait", "type": "wait_for_timeout", "data": { "label": "等待页面稳定", "timeout": 1200 } },
                { "id": "done", "type": "end_success", "data": { "label": "结束" } }
              ],
              "edges": [
                { "source": "open", "target": "wait" },
                { "source": "wait", "target": "done" }
              ]
            }
            """;
            var completedPayloadJson = """
            {
              "steps": [
                { "id": "open", "type": "open", "data": { "label": "打开 HTTPBin", "url": "https://httpbin.org/get" } },
                { "id": "extract", "type": "extract_text", "data": { "label": "提取页面标题", "selector": "body" } },
                { "id": "done", "type": "end_success", "data": { "label": "结束" } }
              ],
              "edges": [
                { "source": "open", "target": "extract" },
                { "source": "extract", "target": "done" }
              ]
            }
            """;
            var failedPayloadJson = """
            {
              "steps": [
                { "id": "open", "type": "open", "data": { "label": "打开不可达域名", "url": "https://invalid.domain.example" } },
                { "id": "done", "type": "end_fail", "data": { "label": "结束失败" } }
              ],
              "edges": [
                { "source": "open", "target": "done" }
              ]
            }
            """;
            var baiduPayloadJson = """
            {
              "steps": [
                { "id": "step_open", "type": "open", "data": { "label": "打开百度首页", "url": "https://www.baidu.com" } },
                { "id": "step_wait_input", "type": "wait_for_element", "data": { "label": "等待搜索输入框", "selector": "#kw", "timeout": 15000 } },
                { "id": "step_type_keyword", "type": "type", "data": { "label": "输入关键词", "selector": "#kw", "value": "BrowserAgentPlatform 自动化测试" } },
                { "id": "step_click_search", "type": "click", "data": { "label": "点击搜索按钮", "selector": "#su" } },
                { "id": "step_wait_result", "type": "wait_for_element", "data": { "label": "等待结果区域", "selector": "#content_left", "timeout": 15000 } },
                { "id": "step_extract_title", "type": "extract_text", "data": { "label": "提取首条结果标题", "selector": "#content_left h3" } },
                { "id": "step_done", "type": "end_success", "data": { "label": "完成" } }
              ],
              "edges": [
                { "source": "step_open", "target": "step_wait_input" },
                { "source": "step_wait_input", "target": "step_type_keyword" },
                { "source": "step_type_keyword", "target": "step_click_search" },
                { "source": "step_click_search", "target": "step_wait_result" },
                { "source": "step_wait_result", "target": "step_extract_title" },
                { "source": "step_extract_title", "target": "step_done" }
              ]
            }
            """;

            var queuedTask = new WorkflowTask
            {
                Name = "DEMO Task Queued",
                BrowserProfileId = isolatedProfile.Id,
                SchedulingStrategy = "preferred_agent",
                PreferredAgentId = onlineAgent.Id,
                Status = "queued",
                PayloadJson = queuedPayloadJson,
                RetryPolicyJson = "{\"maxRetries\":1}",
                Priority = 200,
                TimeoutSeconds = 300
            };

            var completedTask = new WorkflowTask
            {
                Name = "DEMO Task Completed",
                BrowserProfileId = standardProfile.Id,
                SchedulingStrategy = "least_loaded",
                Status = "completed",
                PayloadJson = completedPayloadJson,
                RetryPolicyJson = "{\"maxRetries\":0}",
                Priority = 100,
                TimeoutSeconds = 180
            };

            var failedTask = new WorkflowTask
            {
                Name = "DEMO Task Failed",
                BrowserProfileId = isolatedProfile.Id,
                SchedulingStrategy = "profile_owner",
                Status = "failed",
                PayloadJson = failedPayloadJson,
                RetryPolicyJson = "{\"maxRetries\":2}",
                Priority = 80,
                TimeoutSeconds = 120
            };

            var baiduTask = new WorkflowTask
            {
                Name = "DEMO Task Baidu Search",
                BrowserProfileId = standardProfile.Id,
                SchedulingStrategy = "profile_owner",
                Status = "queued",
                PayloadJson = baiduPayloadJson,
                RetryPolicyJson = "{\"maxRetries\":1}",
                Priority = 220,
                TimeoutSeconds = 240
            };

            db.Tasks.AddRange(queuedTask, completedTask, failedTask, baiduTask);
            await db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var queuedRun = new TaskRun
            {
                TaskId = queuedTask.Id,
                BrowserProfileId = queuedTask.BrowserProfileId,
                Status = "queued",
                CreatedAt = now.AddMinutes(-5),
                RetryCount = 0,
                MaxRetries = 1
            };
            var completedRun = new TaskRun
            {
                TaskId = completedTask.Id,
                BrowserProfileId = completedTask.BrowserProfileId,
                Status = "completed",
                AssignedAgentId = onlineAgent.Id,
                CreatedAt = now.AddHours(-2),
                StartedAt = now.AddHours(-2).AddMinutes(1),
                HeartbeatAt = now.AddHours(-2).AddMinutes(3),
                FinishedAt = now.AddHours(-2).AddMinutes(4),
                CurrentStepId = "done",
                CurrentStepLabel = "完成",
                CurrentUrl = "https://httpbin.org/get",
                ResultJson = "{\"ok\":true}",
                LastPreviewPath = "",
                RetryCount = 0,
                MaxRetries = 0
            };
            var failedRun = new TaskRun
            {
                TaskId = failedTask.Id,
                BrowserProfileId = failedTask.BrowserProfileId,
                Status = "failed",
                AssignedAgentId = onlineAgent.Id,
                CreatedAt = now.AddHours(-1),
                StartedAt = now.AddHours(-1).AddMinutes(1),
                HeartbeatAt = now.AddHours(-1).AddMinutes(2),
                FinishedAt = now.AddHours(-1).AddMinutes(3),
                CurrentStepId = "open",
                CurrentStepLabel = "打开页面",
                CurrentUrl = "https://invalid.domain.example",
                ResultJson = "{\"ok\":false}",
                ErrorCode = "dns_error",
                ErrorMessage = "Domain not found",
                RetryCount = 1,
                MaxRetries = 2
            };

            db.TaskRuns.AddRange(queuedRun, completedRun, failedRun);
            await db.SaveChangesAsync();

            db.TaskRunLogs.AddRange(
                new TaskRunLog { TaskRunId = completedRun.Id, Level = "info", StepId = "open", Message = "Opened target URL successfully", CreatedAt = now.AddHours(-2).AddMinutes(2) },
                new TaskRunLog { TaskRunId = completedRun.Id, Level = "info", StepId = "done", Message = "Run completed", CreatedAt = now.AddHours(-2).AddMinutes(4) },
                new TaskRunLog { TaskRunId = failedRun.Id, Level = "error", StepId = "open", Message = "DNS resolve failed", CreatedAt = now.AddHours(-1).AddMinutes(2) }
            );

            db.RunIsolationReports.Add(new RunIsolationReport
            {
                TaskRunId = completedRun.Id,
                BrowserProfileId = completedRun.BrowserProfileId,
                ProxySnapshotJson = "{\"host\":\"127.0.0.1\",\"port\":8080}",
                FingerprintSnapshotJson = "{\"name\":\"Default Desktop Chrome\"}",
                StorageCheckJson = "{\"ok\":true}",
                NetworkCheckJson = "{\"ok\":true}",
                Result = "pass",
                CreatedAt = now.AddHours(-2).AddMinutes(4)
            });

            db.AuditEvents.AddRange(
                new AuditEvent
                {
                    EventType = "demo_seed",
                    ActorType = "system",
                    ActorId = "db_seeder",
                    TargetType = "task_run",
                    TargetId = completedRun.Id.ToString(),
                    DetailsJson = "{\"note\":\"seed completed run\"}",
                    CreatedAt = now.AddHours(-2).AddMinutes(4)
                },
                new AuditEvent
                {
                    EventType = "demo_seed",
                    ActorType = "system",
                    ActorId = "db_seeder",
                    TargetType = "task_run",
                    TargetId = failedRun.Id.ToString(),
                    DetailsJson = "{\"note\":\"seed failed run\"}",
                    CreatedAt = now.AddHours(-1).AddMinutes(3)
                }
            );

            await db.SaveChangesAsync();
        }
    }
}
