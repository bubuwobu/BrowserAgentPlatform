using BrowserAgentPlatform.Agent.Models;
using BrowserAgentPlatform.Agent.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Platform"));
builder.Services.AddHttpClient<PlatformApiClient>();
builder.Services.AddHttpClient<ElementPickerService>();
builder.Services.AddSingleton<ProfileRuntimeManager>();
builder.Services.AddSingleton<TaskExecutor>();
builder.Services.AddHostedService<AgentWorker>();
var host = builder.Build();
host.Run();
