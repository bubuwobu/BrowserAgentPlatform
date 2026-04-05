using BrowserAgentPlatform.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Services;

public class LeaseReaperBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaseReaperBackgroundService> _logger;

    public LeaseReaperBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<LeaseReaperBackgroundService> logger)
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

                var expiredLocks = await db.BrowserProfileLocks
                    .Where(x => (x.Status == "reserved" || x.Status == "leased") && x.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                foreach (var lockRow in expiredLocks)
                {
                    lockRow.Status = "released";
                    if (lockRow.TaskRunId.HasValue)
                    {
                        var run = await db.TaskRuns.FindAsync(new object?[] { lockRow.TaskRunId.Value }, cancellationToken: stoppingToken);
                        if (run is not null && run.Status == "leased")
                        {
                            run.Status = "queued";
                            run.AssignedAgentId = null;
                            run.LeaseToken = "";
                            run.HeartbeatAt = null;
                        }
                    }
                }

                if (expiredLocks.Count > 0) await db.SaveChangesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected cancellation during graceful shutdown.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lease reaper iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
        }
    }
}
