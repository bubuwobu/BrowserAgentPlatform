using System.Security.Cryptography;
using System.Text;

namespace BrowserAgentPlatform.Api.Services;

public class AgentRequestSecurityService
{
    private readonly IConfiguration _configuration;

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
        if (string.IsNullOrWhiteSpace(tsHeader) || string.IsNullOrWhiteSpace(sigHeader))
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

        var payload = $"{agentKey}:{unixTs}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        if (!string.Equals(expected, sigHeader.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            return (false, "signature mismatch");
        }

        return (true, "ok");
    }
}
