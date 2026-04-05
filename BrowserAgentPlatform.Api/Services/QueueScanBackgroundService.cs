using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BrowserAgentPlatform.Api.Services;

public class QueueScanBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueueScanBackgroundService> _logger;

    public QueueScanBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<QueueScanBackgroundService> logger)
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
                var tasks = await db.Tasks
                    .Where(x => x.Status == "queued" && !db.TaskRuns.Any(r => r.TaskId == x.Id && (r.Status == "queued" || r.Status == "leased" || r.Status == "running")))
                    .ToListAsync(stoppingToken);

                foreach (var task in tasks)
                {
                    var maxRetries = 0;
                    if (!string.IsNullOrWhiteSpace(task.RetryPolicyJson))
                    {
                        try
                        {
                            using var policyDoc = JsonDocument.Parse(task.RetryPolicyJson);
                            if (policyDoc.RootElement.TryGetProperty("maxRetries", out var maxRetriesProp) && maxRetriesProp.TryGetInt32(out var parsed))
                            {
                                maxRetries = Math.Max(0, parsed);
                            }
                        }
                        catch
                        {
                            maxRetries = 0;
                        }
                    }

                    db.TaskRuns.Add(new TaskRun
                    {
                        TaskId = task.Id,
                        BrowserProfileId = task.BrowserProfileId,
                        Status = "queued",
                        RetryCount = 0,
                        MaxRetries = maxRetries
                    });
                }

                if (tasks.Count > 0) await db.SaveChangesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue scanner iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
        }
    }
}
