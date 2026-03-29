namespace BrowserAgentPlatform.Api.Data.Entities;

public class Account
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Platform { get; set; } = "generic";
    public string Username { get; set; } = "";
    public string Status { get; set; } = "active";
    public long? BrowserProfileId { get; set; }
    public string CredentialJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
