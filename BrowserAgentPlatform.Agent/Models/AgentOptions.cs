namespace BrowserAgentPlatform.Agent.Models;
public class AgentOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5216";
    public string AgentKey { get; set; } = "agent-local-001";
    public string Name { get; set; } = "本地执行器";
    public string MachineName { get; set; } = "DEV-PC";
    public int MaxParallelRuns { get; set; } = 1;
    public string SchedulerTags { get; set; } = "";
    public string ProfilesRoot { get; set; } = "data/profiles";
}
