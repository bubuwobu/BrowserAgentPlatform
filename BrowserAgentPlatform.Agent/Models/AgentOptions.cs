namespace BrowserAgentPlatform.Agent.Models;
public class AgentOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:12126";
    public string AgentKey { get; set; } = "agent-local-001";
    public string Name { get; set; } = "本地执行器";
    public string MachineName { get; set; } = "DEV-PC";
    public int MaxParallelRuns { get; set; } = 1;
    public string SchedulerTags { get; set; } = "";
    public string ProfilesRoot { get; set; } = "data/profiles";
    public bool RunHeaded { get; set; } = true;
    public string AgentSecuritySharedSecret { get; set; } = "";
    public CommentAiOptions CommentAi { get; set; } = new();
}

public class CommentAiOptions
{
    public string Provider { get; set; } = "rule"; // rule | openai | deepseek
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 12;
}
