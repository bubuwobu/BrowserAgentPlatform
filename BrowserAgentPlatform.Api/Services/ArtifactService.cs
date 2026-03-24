using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Data.Entities;

namespace BrowserAgentPlatform.Api.Services;

public class ArtifactService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _db;

    public ArtifactService(IConfiguration config, IWebHostEnvironment env, AppDbContext db)
    {
        _config = config;
        _env = env;
        _db = db;
    }

    public async Task<string> SavePreviewAsync(long taskRunId, string? base64, string filePrefix = "preview")
    {
        if (string.IsNullOrWhiteSpace(base64)) return string.Empty;
        var relativeRoot = _config["Storage:ArtifactsPath"] ?? "data/artifacts";
        var physicalRoot = Path.Combine(_env.ContentRootPath, relativeRoot, taskRunId.ToString());
        Directory.CreateDirectory(physicalRoot);

        var bytes = Convert.FromBase64String(base64);
        var fileName = $"{filePrefix}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.png";
        var filePath = Path.Combine(physicalRoot, fileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        var relativePath = $"{relativeRoot}/{taskRunId}/{fileName}".Replace("\\", "/");
        _db.BrowserArtifacts.Add(new BrowserArtifact
        {
            TaskRunId = taskRunId,
            ArtifactType = "screenshot",
            FileName = fileName,
            FilePath = relativePath
        });
        await _db.SaveChangesAsync();
        return "/" + relativePath;
    }
}
