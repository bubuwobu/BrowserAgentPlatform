using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Services;

public class QueueScanBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public QueueScanBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tasks = await db.Tasks
                .Where(x => x.Status == "queued" && !db.TaskRuns.Any(r => r.TaskId == x.Id && (r.Status == "queued" || r.Status == "leased" || r.Status == "running")))
                .ToListAsync(stoppingToken);

            foreach (var task in tasks)
            {
                db.TaskRuns.Add(new TaskRun
                {
                    TaskId = task.Id,
                    BrowserProfileId = task.BrowserProfileId,
                    Status = "queued"
                });
            }

            if (tasks.Count > 0) await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
