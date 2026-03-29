using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BrowserAgentPlatform.Api.Services;

public class ProfileLifecycleService
{
    private readonly AppDbContext _db;

    public ProfileLifecycleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task MarkLeasedAsync(long profileId, long taskRunId, long? taskId, string leaseToken, CancellationToken cancellationToken = default)
    {
        var profile = await _db.BrowserProfiles.FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);
        if (profile is null) return;

        profile.Status = "leased";
        profile.LifecycleState = "leased";
        profile.LastStartedAt ??= DateTime.UtcNow;
        profile.LastUsedAt = DateTime.UtcNow;
        profile.RuntimeMetaJson = MergeRuntimeMeta(profile.RuntimeMetaJson, new
        {
            lifecycle = new
            {
                state = "leased",
                updatedAt = DateTime.UtcNow,
                taskRunId,
                taskId,
                leaseToken
            },
            workspace = BuildWorkspace(profile)
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkRunningAsync(long profileId, long taskRunId, string currentStepId, string currentStepLabel, string currentUrl, CancellationToken cancellationToken = default)
    {
        var profile = await _db.BrowserProfiles.FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);
        if (profile is null) return;

        profile.Status = "running";
        profile.LifecycleState = "running";
        profile.LastStartedAt ??= DateTime.UtcNow;
        profile.LastUsedAt = DateTime.UtcNow;
        profile.RuntimeMetaJson = MergeRuntimeMeta(profile.RuntimeMetaJson, new
        {
            lifecycle = new
            {
                state = "running",
                updatedAt = DateTime.UtcNow,
                taskRunId,
                currentStepId,
                currentStepLabel,
                currentUrl
            },
            workspace = BuildWorkspace(profile)
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCompletedAsync(long profileId, string finalState, CancellationToken cancellationToken = default)
    {
        var profile = await _db.BrowserProfiles.FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);
        if (profile is null) return;

        profile.Status = finalState == "failed" ? "error" : "idle";
        profile.LifecycleState = finalState == "failed" ? "broken" : "ready";
        profile.LastStoppedAt = DateTime.UtcNow;
        profile.LastUsedAt = DateTime.UtcNow;
        profile.RuntimeMetaJson = MergeRuntimeMeta(profile.RuntimeMetaJson, new
        {
            lifecycle = new
            {
                state = profile.LifecycleState,
                updatedAt = DateTime.UtcNow,
                finalState
            },
            workspace = BuildWorkspace(profile)
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkUnlockedAsync(long profileId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.BrowserProfiles.FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);
        if (profile is null) return;
        profile.Status = "idle";
        profile.LifecycleState = "ready";
        profile.LastStoppedAt = DateTime.UtcNow;
        profile.RuntimeMetaJson = MergeRuntimeMeta(profile.RuntimeMetaJson, new
        {
            lifecycle = new { state = "ready", updatedAt = DateTime.UtcNow },
            workspace = BuildWorkspace(profile)
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public static object BuildWorkspace(BrowserProfile profile) => new
    {
        workspaceKey = profile.WorkspaceKey,
        profileRootPath = profile.ProfileRootPath,
        localProfilePath = profile.LocalProfilePath,
        storageRootPath = profile.StorageRootPath,
        downloadRootPath = profile.DownloadRootPath,
        artifactRootPath = profile.ArtifactRootPath,
        tempRootPath = profile.TempRootPath
    };

    private static string MergeRuntimeMeta(string? existingJson, object patch)
    {
        try
        {
            using var patchDoc = JsonDocument.Parse(JsonSerializer.Serialize(patch));
            var root = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(existingJson) && existingJson!.TrimStart().StartsWith("{"))
            {
                using var existingDoc = JsonDocument.Parse(existingJson);
                foreach (var property in existingDoc.RootElement.EnumerateObject())
                {
                    root[property.Name] = JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
                }
            }
            foreach (var property in patchDoc.RootElement.EnumerateObject())
            {
                root[property.Name] = JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
            }
            return JsonSerializer.Serialize(root);
        }
        catch
        {
            return JsonSerializer.Serialize(patch);
        }
    }
}
