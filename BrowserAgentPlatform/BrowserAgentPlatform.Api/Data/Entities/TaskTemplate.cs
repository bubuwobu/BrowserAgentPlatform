namespace BrowserAgentPlatform.Api.Data.Entities;
public class TaskTemplate
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string DefinitionJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
