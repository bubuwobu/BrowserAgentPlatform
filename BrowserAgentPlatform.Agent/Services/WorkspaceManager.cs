using System.Text.Json;
using BrowserAgentPlatform.Agent.Models;
using Microsoft.Extensions.Options;

namespace BrowserAgentPlatform.Agent.Services;

public class WorkspaceManager
{
    private readonly AgentOptions _options;

    public WorkspaceManager(IOptions<AgentOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(_options.ProfilesRoot);
    }

    public WorkspaceDescriptor EnsureWorkspace(long profileId, RuntimeIdentityDescriptor? runtimeIdentity)
    {
        var workspace = runtimeIdentity?.Workspace;
        if (workspace is null)
        {
            var root = Path.Combine(_options.ProfilesRoot, $"profile_{profileId}");
            workspace = new WorkspaceDescriptor(
                $"profile_{profileId}",
                root,
                root,
                root,
                Path.Combine(root, "downloads"),
                Path.Combine(root, "artifacts"),
                Path.Combine(root, "logs"),
                Path.Combine(root, "temp"),
                Path.Combine(root, "state", "identity.json")
            );
        }

        foreach (var path in new[]
        {
            workspace.WorkspaceRootPath, workspace.ProfileRootPath, workspace.StorageRootPath, workspace.DownloadRootPath,
            workspace.ArtifactRootPath, workspace.LogRootPath, workspace.TempRootPath, Path.GetDirectoryName(workspace.StateFilePath) ?? string.Empty
        })
        {
            if (!string.IsNullOrWhiteSpace(path)) Directory.CreateDirectory(path);
        }

        try
        {
            var state = new
            {
                workspace.WorkspaceKey,
                workspace.WorkspaceRootPath,
                workspace.ProfileRootPath,
                workspace.StorageRootPath,
                workspace.DownloadRootPath,
                runtimeIdentity?.IdentityKey,
                runtimeIdentity?.AccountId,
                runtimeIdentity?.ProfileId,
                generatedAtUtc = DateTime.UtcNow
            };
            File.WriteAllText(workspace.StateFilePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }

        return workspace;
    }
}
