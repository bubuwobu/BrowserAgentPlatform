using BrowserAgentPlatform.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Services;

public class ObservabilityService
{
    private readonly AppDbContext _db;

    public ObservabilityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var last24h = now.AddHours(-24);

        var queued = await _db.TaskRuns.CountAsync(x => x.Status == "queued", cancellationToken);
        var leased = await _db.TaskRuns.CountAsync(x => x.Status == "leased", cancellationToken);
        var running = await _db.TaskRuns.CountAsync(x => x.Status == "running", cancellationToken);
        var completed24h = await _db.TaskRuns.CountAsync(x => x.Status == "completed" && x.FinishedAt >= last24h, cancellationToken);
        var failed24h = await _db.TaskRuns.CountAsync(x => (x.Status == "failed" || x.Status == "dead" || x.Status == "timeout") && x.FinishedAt >= last24h, cancellationToken);
        var onlineAgents = await _db.Agents.CountAsync(x => x.LastHeartbeatAt >= now.AddMinutes(-2), cancellationToken);

        var durations = await _db.TaskRuns
            .Where(x => x.FinishedAt.HasValue && x.StartedAt.HasValue && x.FinishedAt >= last24h)
            .Select(x => new { x.StartedAt, x.FinishedAt })
            .ToListAsync(cancellationToken);
        var avgDurationSeconds = durations.Count == 0
            ? 0
            : durations.Average(x => (x.FinishedAt!.Value - x.StartedAt!.Value).TotalSeconds);

        return new
        {
            timestamp = now,
            queue = new { queued, leased, running },
            reliability = new
            {
                completed24h,
                failed24h,
                successRate24h = completed24h + failed24h == 0 ? 1 : (double)completed24h / (completed24h + failed24h),
                avgDurationSeconds24h = avgDurationSeconds
            },
            agents = new { onlineAgents }
        };
    }
}
