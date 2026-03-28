using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BrowserAgentPlatform.Api.Services;

public record IsolationCheckResult(bool Ok, List<string> Errors, List<string> Warnings, string EffectivePolicyJson);

public class IsolationPolicyService
{
    private readonly AppDbContext _db;

    public IsolationPolicyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IsolationCheckResult> CheckProfileAsync(BrowserProfile profile, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        JsonDocument? parsedPolicy = null;

        if (!string.IsNullOrWhiteSpace(profile.IsolationPolicyJson))
        {
            try
            {
                parsedPolicy = JsonDocument.Parse(profile.IsolationPolicyJson);
            }
            catch
            {
                errors.Add("IsolationPolicyJson is not valid JSON.");
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.StartupArgsJson))
        {
            try
            {
                using var startupArgsDoc = JsonDocument.Parse(profile.StartupArgsJson);
                if (startupArgsDoc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    errors.Add("StartupArgsJson must be a JSON array.");
                }
            }
            catch
            {
                errors.Add("StartupArgsJson is not valid JSON.");
            }
        }

        if (string.IsNullOrWhiteSpace(profile.LocalProfilePath))
        {
            errors.Add("LocalProfilePath is required.");
        }

        if (!string.IsNullOrWhiteSpace(profile.StorageRootPath) && !Path.IsPathRooted(profile.StorageRootPath))
        {
            warnings.Add("StorageRootPath is not absolute.");
        }

        if (!string.IsNullOrWhiteSpace(profile.DownloadRootPath) && !Path.IsPathRooted(profile.DownloadRootPath))
        {
            warnings.Add("DownloadRootPath is not absolute.");
        }

        ProxyConfig? proxy = null;
        if (profile.ProxyId.HasValue)
        {
            proxy = await _db.Proxies.FindAsync(new object?[] { profile.ProxyId.Value }, cancellationToken);
            if (proxy is null) errors.Add("ProxyId points to a missing proxy.");
        }

        FingerprintTemplate? fingerprint = null;
        if (profile.FingerprintTemplateId.HasValue)
        {
            fingerprint = await _db.FingerprintTemplates.FindAsync(new object?[] { profile.FingerprintTemplateId.Value }, cancellationToken);
            if (fingerprint is null) errors.Add("FingerprintTemplateId points to a missing fingerprint template.");
        }

        var effectivePolicy = new
        {
            level = string.IsNullOrWhiteSpace(profile.IsolationLevel) ? "strict" : profile.IsolationLevel,
            localProfilePath = profile.LocalProfilePath,
            storageRootPath = profile.StorageRootPath,
            downloadRootPath = profile.DownloadRootPath,
            proxy = proxy is null ? null : new
            {
                proxy.Protocol,
                proxy.Host,
                proxy.Port,
                hasAuth = !string.IsNullOrWhiteSpace(proxy.Username)
            },
            fingerprint = fingerprint is null ? null : new
            {
                fingerprint.Id,
                fingerprint.Name
            },
            rawPolicy = parsedPolicy?.RootElement
        };

        var effectivePolicyJson = JsonSerializer.Serialize(effectivePolicy);
        return new IsolationCheckResult(errors.Count == 0, errors, warnings, effectivePolicyJson);
    }
}
