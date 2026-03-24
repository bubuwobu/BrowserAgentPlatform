using BrowserAgentPlatform.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Services;

public class LeaseReaperBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LeaseReaperBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
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
                    }
                }
            }

            if (expiredLocks.Count > 0) await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
