namespace BrowserAgentPlatform.Api.Data.Entities;
public class ProxyConfig
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Protocol { get; set; } = "http";
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
