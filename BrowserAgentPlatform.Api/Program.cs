using System.Text;
using BrowserAgentPlatform.Api.Data;
using BrowserAgentPlatform.Api.Hubs;
using BrowserAgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing connection string.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

var corsOriginsRaw = builder.Configuration["Cors:WebOrigins"]
                     ?? builder.Configuration["Cors:WebOrigin"]
                     ?? "http://localhost:5173,http://127.0.0.1:5173";

var corsOrigins = corsOriginsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing JWT key.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs/live") || path.StartsWithSegments("/hubs/picker")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SchedulerService>();
builder.Services.AddScoped<ArtifactService>();
builder.Services.AddScoped<LiveHubNotifier>();
builder.Services.AddScoped<IsolationPolicyService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ObservabilityService>();
builder.Services.AddScoped<ClosedLoopValidationService>();
builder.Services.AddScoped<ProfileLifecycleService>();
builder.Services.AddSingleton<AgentRequestSecurityService>();
builder.Services.AddSingleton<PickerSessionService>();
builder.Services.AddHostedService<QueueScanBackgroundService>();
builder.Services.AddHostedService<LeaseReaperBackgroundService>();
builder.Services.AddHostedService<RunWatchdogBackgroundService>();
builder.Services.AddHostedService<TaskScheduleBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var enableDemoData = configuration.GetValue<bool>("Seed:EnableDemoData");
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(db, enableDemoData);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LiveHub>("/hubs/live");
app.MapHub<PickerHub>("/hubs/picker");
app.MapGet("/", () => Results.Ok(new { ok = true, service = "BrowserAgentPlatform.Api" }));

app.Run();
