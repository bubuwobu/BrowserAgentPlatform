using BrowserAgentPlatform.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        var behaviorRuns = await _db.TaskRuns
            .Where(x => x.Status == "completed" && x.FinishedAt >= last24h)
            .OrderByDescending(x => x.Id)
            .Take(200)
            .Select(x => x.ResultJson)
            .ToListAsync(cancellationToken);

        var typingDelayMetrics = new List<double>();
        var commentDuplicateRates = new List<double>();
        var anomalyRates = new List<double>();
        var providerDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var profileDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var resultJson in behaviorRuns)
        {
            if (string.IsNullOrWhiteSpace(resultJson)) continue;
            try
            {
                using var doc = JsonDocument.Parse(resultJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.Object) continue;
                    if (!prop.Value.TryGetProperty("behaviorMetrics", out var behavior) || behavior.ValueKind != JsonValueKind.Object) continue;
                    if (behavior.TryGetProperty("avgTypingDelayMs", out var typing) && typing.ValueKind == JsonValueKind.Number) typingDelayMetrics.Add(typing.GetDouble());
                    if (behavior.TryGetProperty("commentDuplicateRate", out var duplicateRate) && duplicateRate.ValueKind == JsonValueKind.Number) commentDuplicateRates.Add(duplicateRate.GetDouble());
                    if (behavior.TryGetProperty("anomalyRate", out var anomalyRate) && anomalyRate.ValueKind == JsonValueKind.Number) anomalyRates.Add(anomalyRate.GetDouble());
                    if (behavior.TryGetProperty("commentProvider", out var provider) && provider.ValueKind == JsonValueKind.String)
                    {
                        var key = provider.GetString() ?? "unknown";
                        providerDistribution[key] = providerDistribution.TryGetValue(key, out var count) ? count + 1 : 1;
                    }
                    if (behavior.TryGetProperty("behaviorProfile", out var profile) && profile.ValueKind == JsonValueKind.String)
                    {
                        var key = profile.GetString() ?? "unknown";
                        profileDistribution[key] = profileDistribution.TryGetValue(key, out var count) ? count + 1 : 1;
                    }
                }
            }
            catch
            {
                // ignore malformed legacy result json
            }
        }

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
            behaviorQuality = new
            {
                sampledRuns24h = typingDelayMetrics.Count,
                avgTypingDelayMs24h = typingDelayMetrics.Count == 0 ? 0 : typingDelayMetrics.Average(),
                p50TypingDelayMs24h = Percentile(typingDelayMetrics, 0.5),
                p90TypingDelayMs24h = Percentile(typingDelayMetrics, 0.9),
                avgCommentDuplicateRate24h = commentDuplicateRates.Count == 0 ? 0 : commentDuplicateRates.Average(),
                avgAnomalyRate24h = anomalyRates.Count == 0 ? 0 : anomalyRates.Average()
            },
            behaviorDimensions = new
            {
                providerDistribution,
                profileDistribution
            },
            agents = new { onlineAgents }
        };
    }

    private static double Percentile(List<double> values, double percentile)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        index = Math.Clamp(index, 0, sorted.Count - 1);
        return sorted[index];
    }
}
