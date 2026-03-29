using System.Collections.Concurrent;
using BrowserAgentPlatform.Api.Models;

namespace BrowserAgentPlatform.Api.Services;

public class PickerSessionService
{
    private readonly ConcurrentDictionary<string, PickerSessionDto> _sessions = new();

    public PickerSessionDto CreateOrRestore(
        long profileId,
        string? pageUrl,
        string? nodeId,
        string? nodeType,
        bool continuous,
        bool resumeIfExists,
        out bool restored,
        long? agentId = null)
    {
        restored = false;

        if (resumeIfExists)
        {
            var existing = _sessions.Values
                .Where(x => x.ProfileId == profileId && x.Status is "started" or "paused")
                .OrderByDescending(x => x.LastEventAtUtc ?? x.CreatedAtUtc)
                .FirstOrDefault();

            if (existing is not null)
            {
                existing.PageUrl = pageUrl ?? existing.PageUrl;
                existing.NodeId = nodeId ?? existing.NodeId;
                existing.NodeType = nodeType ?? existing.NodeType;
                existing.Continuous = continuous;
                existing.IsPaused = false;
                existing.Status = "started";
                existing.LastEventAtUtc = DateTime.UtcNow;
                restored = true;
                return existing;
            }
        }

        var session = new PickerSessionDto
        {
            SessionId = Guid.NewGuid().ToString("N"),
            ProfileId = profileId,
            AgentId = agentId,
            PageUrl = pageUrl,
            NodeId = nodeId,
            NodeType = nodeType,
            Continuous = continuous,
            Status = "created",
            CreatedAtUtc = DateTime.UtcNow,
            LastEventAtUtc = DateTime.UtcNow
        };

        _sessions[session.SessionId] = session;
        return session;
    }

    public bool TryGet(string sessionId, out PickerSessionDto? session)
    {
        var ok = _sessions.TryGetValue(sessionId, out var found);
        session = found;
        return ok;
    }

    public void MarkStarted(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = "started";
            session.IsPaused = false;
            session.LastEventAtUtc = DateTime.UtcNow;
        }
    }

    public void MarkPicked(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = "picked";
            session.LastEventAtUtc = DateTime.UtcNow;
            session.PickCount += 1;
            if (session.Continuous)
                session.Status = "started";
        }
    }

    public void Pause(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = "paused";
            session.IsPaused = true;
            session.LastEventAtUtc = DateTime.UtcNow;
        }
    }

    public void Stop(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = "stopped";
            session.IsPaused = false;
            session.LastEventAtUtc = DateTime.UtcNow;
        }
    }

    public PickerStateSnapshotDto? Snapshot(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return null;

        return new PickerStateSnapshotDto
        {
            SessionId = session.SessionId,
            ProfileId = session.ProfileId,
            Status = session.Status,
            Continuous = session.Continuous,
            IsPaused = session.IsPaused,
            PickCount = session.PickCount,
            LastEventAtUtc = session.LastEventAtUtc
        };
    }
}
