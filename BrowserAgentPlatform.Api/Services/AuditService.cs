using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;

namespace BrowserAgentPlatform.Api.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(
        string eventType,
        string actorType,
        string actorId,
        string targetType,
        string targetId,
        string detailsJson = "{}",
        CancellationToken cancellationToken = default)
    {
        _db.Set<AuditEvent>().Add(new AuditEvent
        {
            EventType = eventType,
            ActorType = actorType,
            ActorId = actorId,
            TargetType = targetType,
            TargetId = targetId,
            DetailsJson = detailsJson
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
