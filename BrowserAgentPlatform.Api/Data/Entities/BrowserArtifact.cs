namespace BrowserAgentPlatform.Api.Data.Entities;
public class BrowserArtifact
{
    public long Id { get; set; }
    public long TaskRunId { get; set; }
    public string ArtifactType { get; set; } = "screenshot";
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
