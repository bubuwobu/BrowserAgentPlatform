using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AgentNode> Agents => Set<AgentNode>();
    public DbSet<ProxyConfig> Proxies => Set<ProxyConfig>();
    public DbSet<FingerprintTemplate> FingerprintTemplates => Set<FingerprintTemplate>();
    public DbSet<BrowserProfile> BrowserProfiles => Set<BrowserProfile>();
    public DbSet<BrowserProfileLock> BrowserProfileLocks => Set<BrowserProfileLock>();
    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();
    public DbSet<WorkflowTask> Tasks => Set<WorkflowTask>();
    public DbSet<TaskRun> TaskRuns => Set<TaskRun>();
    public DbSet<TaskRunLog> TaskRunLogs => Set<TaskRunLog>();
    public DbSet<BrowserArtifact> BrowserArtifacts => Set<BrowserArtifact>();
    public DbSet<AgentCommand> AgentCommands => Set<AgentCommand>();
    public DbSet<RunIsolationReport> RunIsolationReports => Set<RunIsolationReport>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Username).HasColumnName("username");
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash");
            entity.Property(x => x.DisplayName).HasColumnName("display_name");
            entity.Property(x => x.Role).HasColumnName("role");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<AgentNode>(entity =>
        {
            entity.ToTable("agents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.AgentKey).HasColumnName("agent_key");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.MachineName).HasColumnName("machine_name");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.MaxParallelRuns).HasColumnName("max_parallel_runs");
            entity.Property(x => x.CurrentRuns).HasColumnName("current_runs");
            entity.Property(x => x.SchedulerTags).HasColumnName("scheduler_tags");
            entity.Property(x => x.LastHeartbeatAt).HasColumnName("last_heartbeat_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => x.AgentKey).IsUnique();
        });

        modelBuilder.Entity<ProxyConfig>(entity =>
        {
            entity.ToTable("proxies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Protocol).HasColumnName("protocol");
            entity.Property(x => x.Host).HasColumnName("host");
            entity.Property(x => x.Port).HasColumnName("port");
            entity.Property(x => x.Username).HasColumnName("username");
            entity.Property(x => x.Password).HasColumnName("password");
            entity.Property(x => x.Notes).HasColumnName("notes");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<FingerprintTemplate>(entity =>
        {
            entity.ToTable("fingerprint_templates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.ConfigJson).HasColumnName("config_json").HasColumnType("longtext");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<BrowserProfile>(entity =>
        {
            entity.ToTable("browser_profiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.OwnerAgentId).HasColumnName("owner_agent_id");
            entity.Property(x => x.ProxyId).HasColumnName("proxy_id");
            entity.Property(x => x.FingerprintTemplateId).HasColumnName("fingerprint_template_id");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.IsolationLevel).HasColumnName("isolation_level");
            entity.Property(x => x.LocalProfilePath).HasColumnName("local_profile_path");
            entity.Property(x => x.StorageRootPath).HasColumnName("storage_root_path");
            entity.Property(x => x.DownloadRootPath).HasColumnName("download_root_path");
            entity.Property(x => x.StartupArgsJson).HasColumnName("startup_args_json").HasColumnType("longtext");
            entity.Property(x => x.IsolationPolicyJson).HasColumnName("isolation_policy_json").HasColumnType("longtext");
            entity.Property(x => x.RuntimeMetaJson).HasColumnName("runtime_meta_json").HasColumnType("longtext");
            entity.Property(x => x.LastUsedAt).HasColumnName("last_used_at");
            entity.Property(x => x.LastIsolationCheckAt).HasColumnName("last_isolation_check_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<BrowserProfileLock>(entity =>
        {
            entity.ToTable("browser_profile_locks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ProfileId).HasColumnName("profile_id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.TaskRunId).HasColumnName("task_run_id");
            entity.Property(x => x.AgentId).HasColumnName("agent_id");
            entity.Property(x => x.LeaseToken).HasColumnName("lease_token");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => new { x.ProfileId, x.Status });
        });

        modelBuilder.Entity<TaskTemplate>(entity =>
        {
            entity.ToTable("task_templates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.DefinitionJson).HasColumnName("definition_json").HasColumnType("longtext");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<WorkflowTask>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.BrowserProfileId).HasColumnName("browser_profile_id");
            entity.Property(x => x.SchedulingStrategy).HasColumnName("scheduling_strategy");
            entity.Property(x => x.PreferredAgentId).HasColumnName("preferred_agent_id");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("longtext");
            entity.Property(x => x.RetryPolicyJson).HasColumnName("retry_policy_json").HasColumnType("longtext");
            entity.Property(x => x.Priority).HasColumnName("priority");
            entity.Property(x => x.TimeoutSeconds).HasColumnName("timeout_seconds");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<TaskRun>(entity =>
        {
            entity.ToTable("task_runs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.BrowserProfileId).HasColumnName("browser_profile_id");
            entity.Property(x => x.AssignedAgentId).HasColumnName("assigned_agent_id");
            entity.Property(x => x.LeaseToken).HasColumnName("lease_token");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.RetryCount).HasColumnName("retry_count");
            entity.Property(x => x.MaxRetries).HasColumnName("max_retries");
            entity.Property(x => x.CurrentStepId).HasColumnName("current_step_id");
            entity.Property(x => x.CurrentStepLabel).HasColumnName("current_step_label");
            entity.Property(x => x.CurrentUrl).HasColumnName("current_url");
            entity.Property(x => x.ResultJson).HasColumnName("result_json").HasColumnType("longtext");
            entity.Property(x => x.ErrorCode).HasColumnName("error_code");
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message");
            entity.Property(x => x.LastPreviewPath).HasColumnName("last_preview_path");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.StartedAt).HasColumnName("started_at");
            entity.Property(x => x.HeartbeatAt).HasColumnName("heartbeat_at");
            entity.Property(x => x.FinishedAt).HasColumnName("finished_at");

            entity.HasIndex(x => new { x.Status, x.AssignedAgentId });
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
            entity.HasIndex(x => x.LeaseToken);
        });

        modelBuilder.Entity<TaskRunLog>(entity =>
        {
            entity.ToTable("task_run_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskRunId).HasColumnName("task_run_id");
            entity.Property(x => x.Level).HasColumnName("level");
            entity.Property(x => x.StepId).HasColumnName("step_id");
            entity.Property(x => x.Message).HasColumnName("message");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<BrowserArtifact>(entity =>
        {
            entity.ToTable("browser_artifacts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskRunId).HasColumnName("task_run_id");
            entity.Property(x => x.ArtifactType).HasColumnName("artifact_type");
            entity.Property(x => x.FilePath).HasColumnName("file_path");
            entity.Property(x => x.FileName).HasColumnName("file_name");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<AgentCommand>(entity =>
        {
            entity.ToTable("agent_commands");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.AgentId).HasColumnName("agent_id");
            entity.Property(x => x.ProfileId).HasColumnName("profile_id");
            entity.Property(x => x.CommandType).HasColumnName("command_type");
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("longtext");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => new { x.AgentId, x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<RunIsolationReport>(entity =>
        {
            entity.ToTable("run_isolation_reports");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskRunId).HasColumnName("task_run_id");
            entity.Property(x => x.BrowserProfileId).HasColumnName("browser_profile_id");
            entity.Property(x => x.ProxySnapshotJson).HasColumnName("proxy_snapshot_json").HasColumnType("longtext");
            entity.Property(x => x.FingerprintSnapshotJson).HasColumnName("fingerprint_snapshot_json").HasColumnType("longtext");
            entity.Property(x => x.StorageCheckJson).HasColumnName("storage_check_json").HasColumnType("longtext");
            entity.Property(x => x.NetworkCheckJson).HasColumnName("network_check_json").HasColumnType("longtext");
            entity.Property(x => x.Result).HasColumnName("result");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => x.TaskRunId);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EventType).HasColumnName("event_type");
            entity.Property(x => x.ActorType).HasColumnName("actor_type");
            entity.Property(x => x.ActorId).HasColumnName("actor_id");
            entity.Property(x => x.TargetType).HasColumnName("target_type");
            entity.Property(x => x.TargetId).HasColumnName("target_id");
            entity.Property(x => x.DetailsJson).HasColumnName("details_json").HasColumnType("longtext");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => new { x.EventType, x.CreatedAt });
            entity.HasIndex(x => new { x.ActorType, x.ActorId, x.CreatedAt });
        });
    }
}
