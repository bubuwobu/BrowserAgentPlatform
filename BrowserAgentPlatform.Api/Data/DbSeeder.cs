using BCrypt.Net;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
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

        if (!await db.Agents.AnyAsync())
        {
            db.Agents.AddRange(
                new AgentNode
                {
                    AgentKey = "demo-agent-1",
                    Name = "Demo Agent 1",
                    MachineName = "demo-machine-1",
                    Status = "online",
                    MaxParallelRuns = 2,
                    CurrentRuns = 0,
                    SchedulerTags = "demo,default",
                    LastHeartbeatAt = DateTime.UtcNow.AddSeconds(-10)
                },
                new AgentNode
                {
                    AgentKey = "demo-agent-2",
                    Name = "Demo Agent 2",
                    MachineName = "demo-machine-2",
                    Status = "offline",
                    MaxParallelRuns = 1,
                    CurrentRuns = 0,
                    SchedulerTags = "demo,backup",
                    LastHeartbeatAt = DateTime.UtcNow.AddMinutes(-30)
                }
            );
        }

        if (!await db.Proxies.AnyAsync())
        {
            db.Proxies.AddRange(
                new ProxyConfig
                {
                    Name = "Demo NoAuth Proxy",
                    Protocol = "http",
                    Host = "127.0.0.1",
                    Port = 8080,
                    Notes = "Local demo proxy for UI testing"
                },
                new ProxyConfig
                {
                    Name = "Demo Auth Proxy",
                    Protocol = "socks5",
                    Host = "127.0.0.1",
                    Port = 1080,
                    Username = "demo_user",
                    Password = "demo_pass",
                    Notes = "Authenticated demo proxy"
                }
            );
        }

        await db.SaveChangesAsync();

        // Seed demo profiles/tasks/runs only once (idempotent by fixed demo names).
        var hasDemoProfiles = await db.BrowserProfiles.AnyAsync(x => x.Name.StartsWith("DEMO Profile "));
        if (!hasDemoProfiles)
        {
            var defaultFingerprint = await db.FingerprintTemplates.OrderBy(x => x.Id).FirstAsync();
            var firstAgent = await db.Agents.OrderBy(x => x.Id).FirstAsync();
            var firstProxy = await db.Proxies.OrderBy(x => x.Id).FirstOrDefaultAsync();

            db.BrowserProfiles.AddRange(
                new BrowserProfile
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
                },
                new BrowserProfile
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
                }
            );
            await db.SaveChangesAsync();
        }

        var hasDemoTasks = await db.Tasks.AnyAsync(x => x.Name.StartsWith("DEMO Task "));
        if (!hasDemoTasks)
        {
            var isolatedProfile = await db.BrowserProfiles.FirstAsync(x => x.Name == "DEMO Profile Isolated");
            var standardProfile = await db.BrowserProfiles.FirstAsync(x => x.Name == "DEMO Profile Standard");
            var onlineAgent = await db.Agents.FirstAsync(x => x.AgentKey == "demo-agent-1");

            var queuedTask = new WorkflowTask
            {
                Name = "DEMO Task Queued",
                BrowserProfileId = isolatedProfile.Id,
                SchedulingStrategy = "preferred_agent",
                PreferredAgentId = onlineAgent.Id,
                Status = "queued",
                PayloadJson = "{\"steps\":[{\"id\":\"open\",\"type\":\"open\",\"url\":\"https://example.com\"}]}",
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
                PayloadJson = "{\"steps\":[{\"id\":\"open\",\"type\":\"open\",\"url\":\"https://httpbin.org/get\"}]}",
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
                PayloadJson = "{\"steps\":[{\"id\":\"open\",\"type\":\"open\",\"url\":\"https://invalid.domain.example\"}]}",
                RetryPolicyJson = "{\"maxRetries\":2}",
                Priority = 80,
                TimeoutSeconds = 120
            };

            db.Tasks.AddRange(queuedTask, completedTask, failedTask);
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
