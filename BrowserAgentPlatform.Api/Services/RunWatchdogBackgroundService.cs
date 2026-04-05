using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Services;

public class RunWatchdogBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RunWatchdogBackgroundService> _logger;

    public RunWatchdogBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RunWatchdogBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var scheduler = scope.ServiceProvider.GetRequiredService<SchedulerService>();

                var activeRuns = await db.TaskRuns
                    .Where(x => x.Status == "leased" || x.Status == "running")
                    .ToListAsync(stoppingToken);

                foreach (var run in activeRuns)
                {
                    var timedOutByHeartbeat = run.HeartbeatAt.HasValue && run.HeartbeatAt.Value < DateTime.UtcNow.AddMinutes(-5);
                    var timedOutByDuration = run.StartedAt.HasValue && run.StartedAt.Value < DateTime.UtcNow.AddHours(-2);
                    if (!timedOutByHeartbeat && !timedOutByDuration) continue;

                    run.Status = "timeout";
                    run.ErrorCode = "watchdog_timeout";
                    run.ErrorMessage = timedOutByHeartbeat ? "heartbeat timeout" : "max duration timeout";
                    run.FinishedAt = DateTime.UtcNow;

                    if (run.RetryCount < run.MaxRetries)
                    {
                        run.RetryCount += 1;
                        run.Status = "queued";
                        run.AssignedAgentId = null;
                        run.LeaseToken = "";
                        run.StartedAt = null;
                        run.HeartbeatAt = null;
                        run.FinishedAt = null;
                    }
                    else
                    {
                        run.Status = "dead";
                    }

                    db.TaskRunLogs.Add(new TaskRunLog
                    {
                        TaskRunId = run.Id,
                        Level = "error",
                        Message = $"Watchdog marked run as {run.Status}"
                    });

                    await db.SaveChangesAsync(stoppingToken);
                    await scheduler.ReleaseRunAsync(run.Id);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Run watchdog iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
        }
    }
}
