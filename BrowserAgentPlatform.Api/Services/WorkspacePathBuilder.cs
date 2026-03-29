using BrowserAgentPlatform.Api.Data.Entities;
using BrowserAgentPlatform.Api.Models;

namespace BrowserAgentPlatform.Api.Services;

public class WorkspacePathBuilder
{
    public WorkspaceDescriptor Build(Account? account, BrowserProfile profile)
    {
        var accountKey = account is null ? $"profile_{profile.Id}" : $"acc_{account.Id}";
        var workspaceRoot = string.IsNullOrWhiteSpace(profile.StorageRootPath)
            ? Path.Combine("runtime", "accounts", accountKey)
            : profile.StorageRootPath;
        var profileRoot = string.IsNullOrWhiteSpace(profile.LocalProfilePath)
            ? Path.Combine(workspaceRoot, "profile")
            : profile.LocalProfilePath;
        var downloadRoot = string.IsNullOrWhiteSpace(profile.DownloadRootPath)
            ? Path.Combine(workspaceRoot, "downloads")
            : profile.DownloadRootPath;
        var artifactRoot = Path.Combine(workspaceRoot, "artifacts");
        var logRoot = Path.Combine(workspaceRoot, "logs");
        var tempRoot = Path.Combine(workspaceRoot, "temp");
        var stateFile = Path.Combine(workspaceRoot, "state", "identity.json");

        return new WorkspaceDescriptor(
            accountKey,
            workspaceRoot,
            profileRoot,
            workspaceRoot,
            downloadRoot,
            artifactRoot,
            logRoot,
            tempRoot,
            stateFile
        );
    }
}
