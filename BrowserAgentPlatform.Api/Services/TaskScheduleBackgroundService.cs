using System.Text.Json;
using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Services;

public class TaskScheduleBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskScheduleBackgroundService> _logger;

    public TaskScheduleBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TaskScheduleBackgroundService> logger)
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
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task scheduler tick failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var tasks = await db.Tasks
            .Where(x => x.IsEnabled && x.ScheduleType == "daily_window_random")
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            var next = task.NextRunAt ?? CalculateNextRun(task.ScheduleConfigJson, now);
            if (!task.NextRunAt.HasValue)
            {
                task.NextRunAt = next;
            }

            if (next.HasValue && next.Value <= now)
            {
                var maxRunsPerDay = ReadMaxRunsPerDay(task.ScheduleConfigJson);
                var todayStart = now.Date;
                var todayEnd = todayStart.AddDays(1);

                var todayCount = await db.TaskRuns
                    .Where(x => x.TaskId == task.Id && x.CreatedAt >= todayStart && x.CreatedAt < todayEnd)
                    .CountAsync(cancellationToken);

                if (todayCount < maxRunsPerDay)
                {
                    db.TaskRuns.Add(new TaskRun
                    {
                        TaskId = task.Id,
                        BrowserProfileId = task.BrowserProfileId,
                        Status = "queued",
                        MaxRetries = 1
                    });
                    task.LastRunAt = now;
                }

                task.NextRunAt = CalculateNextRun(task.ScheduleConfigJson, now.AddMinutes(1));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static int ReadMaxRunsPerDay(string? scheduleConfigJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(scheduleConfigJson) ? "{}" : scheduleConfigJson);
            var root = doc.RootElement;
            return root.TryGetProperty("maxRunsPerDay", out var maxEl) ? Math.Max(1, maxEl.GetInt32()) : 1;
        }
        catch
        {
            return 1;
        }
    }

    private static DateTime? CalculateNextRun(string? scheduleConfigJson, DateTime nowUtc)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(scheduleConfigJson) ? "{}" : scheduleConfigJson);
            var root = doc.RootElement;

            var start = root.TryGetProperty("windowStart", out var startEl) ? startEl.GetString() ?? "09:00" : "09:00";
            var end = root.TryGetProperty("windowEnd", out var endEl) ? endEl.GetString() ?? "18:00" : "18:00";
            var step = root.TryGetProperty("randomMinuteStep", out var stepEl) ? Math.Max(1, stepEl.GetInt32()) : 5;

            var startParts = start.Split(':');
            var endParts = end.Split(':');

            var startHour = int.Parse(startParts[0]);
            var startMinute = int.Parse(startParts[1]);
            var endHour = int.Parse(endParts[0]);
            var endMinute = int.Parse(endParts[1]);

            var baseDay = nowUtc.Date;
            var baseStart = new DateTime(baseDay.Year, baseDay.Month, baseDay.Day, startHour, startMinute, 0, DateTimeKind.Utc);
            var baseEnd = new DateTime(baseDay.Year, baseDay.Month, baseDay.Day, endHour, endMinute, 0, DateTimeKind.Utc);

            if (baseEnd <= baseStart)
                baseEnd = baseEnd.AddHours(1);

            if (nowUtc > baseEnd)
            {
                baseStart = baseStart.AddDays(1);
                baseEnd = baseEnd.AddDays(1);
            }

            var totalMinutes = Math.Max(step, (int)(baseEnd - baseStart).TotalMinutes);
            var bucketCount = Math.Max(1, totalMinutes / step);

            var attempts = Enumerable.Range(0, bucketCount)
                .Select(i => baseStart.AddMinutes(i * step))
                .Where(x => x > nowUtc)
                .ToList();

            if (attempts.Count == 0)
                return baseStart.AddDays(1);

            return attempts[Random.Shared.Next(0, attempts.Count)];
        }
        catch
        {
            return nowUtc.AddMinutes(5);
        }
    }
}
