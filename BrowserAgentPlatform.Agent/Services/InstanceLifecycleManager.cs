using System.Collections.Concurrent;
using System.Text.Json;
using BrowserAgentPlatform.Agent.Models;

namespace BrowserAgentPlatform.Agent.Services;

public class InstanceLifecycleManager
{
    private readonly ConcurrentDictionary<long, string> _states = new();

    public void Mark(long profileId, string state, WorkspaceDescriptor? workspace = null, string? message = null)
    {
        _states[profileId] = state;
        if (workspace is null || string.IsNullOrWhiteSpace(workspace.StateFilePath)) return;

        try
        {
            var payload = new
            {
                profileId,
                state,
                message,
                updatedAtUtc = DateTime.UtcNow
            };
            var dir = Path.GetDirectoryName(workspace.StateFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "lifecycle.json"), JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch { }
    }

    public string Get(long profileId) => _states.TryGetValue(profileId, out var state) ? state : "unknown";
}
