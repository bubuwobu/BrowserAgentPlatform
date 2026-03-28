using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;

namespace BrowserAgentPlatform.Api.Services;

public class AgentRequestSecurityService
{
    private readonly IConfiguration _configuration;
    private static readonly ConcurrentDictionary<string, long> SeenNonces = new();

    public AgentRequestSecurityService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (bool ok, string reason) Validate(HttpRequest request, string agentKey)
    {
        var sharedSecret = _configuration["AgentSecurity:SharedSecret"];
        if (string.IsNullOrWhiteSpace(sharedSecret))
        {
            return (true, "disabled");
        }

        var tsHeader = request.Headers["x-agent-ts"].FirstOrDefault();
        var sigHeader = request.Headers["x-agent-signature"].FirstOrDefault();
        var nonceHeader = request.Headers["x-agent-nonce"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tsHeader) || string.IsNullOrWhiteSpace(sigHeader) || string.IsNullOrWhiteSpace(nonceHeader))
        {
            return (false, "missing security headers");
        }

        if (!long.TryParse(tsHeader, out var unixTs))
        {
            return (false, "invalid timestamp");
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - unixTs) > 300)
        {
            return (false, "timestamp outside allowed skew");
        }

        CleanupExpiredNonces(now);
        var nonceKey = $"{agentKey}:{unixTs}:{nonceHeader}";
        if (!SeenNonces.TryAdd(nonceKey, now + 300))
        {
            return (false, "replayed nonce");
        }

        var payload = $"{agentKey}:{unixTs}:{nonceHeader}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        if (!string.Equals(expected, sigHeader.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            return (false, "signature mismatch");
        }

        return (true, "ok");
    }

    private static void CleanupExpiredNonces(long now)
    {
        foreach (var item in SeenNonces)
        {
            if (item.Value < now) SeenNonces.TryRemove(item.Key, out _);
        }
    }
}
