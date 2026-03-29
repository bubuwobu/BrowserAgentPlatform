namespace BrowserAgentPlatform.Api.Data.Entities;
public class FingerprintTemplate
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string ConfigJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
