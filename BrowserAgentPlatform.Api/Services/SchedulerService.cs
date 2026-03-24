using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Services;

public class SchedulerService
{
    private readonly AppDbContext _db;

    public SchedulerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(TaskRun run, BrowserProfileLock profileLock)?> LeaseNextRunForAgentAsync(string agentKey)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(x => x.AgentKey == agentKey);
        if (agent is null) return null;

        // Return already leased run first
        var leased = await _db.TaskRuns
            .Where(x => x.AssignedAgentId == agent.Id && x.Status == "leased")
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();
        if (leased is not null)
        {
            var existingLock = await _db.BrowserProfileLocks
                .Where(x => x.TaskRunId == leased.Id && x.Status == "leased")
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
            if (existingLock is not null) return (leased, existingLock);
        }

        if (agent.CurrentRuns >= agent.MaxParallelRuns) return null;

        var queuedRuns = await _db.TaskRuns
            .Where(x => x.Status == "queued")
            .OrderBy(x => x.Id)
            .ToListAsync();

        foreach (var run in queuedRuns)
        {
            var task = await _db.Tasks.FindAsync(run.TaskId);
            if (task is null) continue;
            var profile = await _db.BrowserProfiles.FindAsync(run.BrowserProfileId);
            if (profile is null) continue;

            var activeLock = await _db.BrowserProfileLocks
                .Where(x => x.ProfileId == profile.Id && (x.Status == "reserved" || x.Status == "leased") && x.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();
            if (activeLock is not null) continue;

            var matched = task.SchedulingStrategy switch
            {
                "preferred_agent" => task.PreferredAgentId == agent.Id,
                "least_loaded" => true,
                _ => profile.OwnerAgentId == null || profile.OwnerAgentId == agent.Id
            };
            if (!matched) continue;

            var leaseToken = Guid.NewGuid().ToString("N");
            var lockRow = new BrowserProfileLock
            {
                ProfileId = profile.Id,
                TaskId = task.Id,
                TaskRunId = run.Id,
                AgentId = agent.Id,
                LeaseToken = leaseToken,
                Status = "leased",
                ExpiresAt = DateTime.UtcNow.AddMinutes(20)
            };
            _db.BrowserProfileLocks.Add(lockRow);

            run.Status = "leased";
            run.AssignedAgentId = agent.Id;
            agent.CurrentRuns += 1;
            profile.Status = "leased";
            await _db.SaveChangesAsync();
            return (run, lockRow);
        }

        return null;
    }

    public async Task RefreshLeaseAsync(long taskRunId, string leaseToken)
    {
        var lease = await _db.BrowserProfileLocks
            .Where(x => x.TaskRunId == taskRunId && x.LeaseToken == leaseToken && x.Status == "leased")
            .FirstOrDefaultAsync();
        if (lease is null) return;
        lease.ExpiresAt = DateTime.UtcNow.AddMinutes(20);
        await _db.SaveChangesAsync();
    }

    public async Task ReleaseRunAsync(long taskRunId)
    {
        var run = await _db.TaskRuns.FindAsync(taskRunId);
        if (run is null) return;

        var lockRow = await _db.BrowserProfileLocks
            .Where(x => x.TaskRunId == taskRunId && x.Status == "leased")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();
        if (lockRow is not null)
        {
            lockRow.Status = "released";
        }

        if (run.AssignedAgentId.HasValue)
        {
            var agent = await _db.Agents.FindAsync(run.AssignedAgentId.Value);
            if (agent is not null && agent.CurrentRuns > 0) agent.CurrentRuns -= 1;
        }

        var profile = await _db.BrowserProfiles.FindAsync(run.BrowserProfileId);
        if (profile is not null)
        {
            profile.Status = run.Status is "running" or "leased" ? "busy" : "idle";
            profile.LastUsedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
