#nullable enable

using HappyPumi.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data;

/// <summary>
/// The PostgreSQL persistence context (ADR-0005). Schema is derived from the wire contracts: scalar
/// columns for the fields endpoints query/sort on, jsonb for nested contract payloads (configs,
/// checkpoints, role descriptors, deployment settings, webhooks, schedules).
/// </summary>
public sealed class HappyPumiDbContext(DbContextOptions<HappyPumiDbContext> options) : DbContext(options)
{
    public DbSet<StackRow> Stacks => Set<StackRow>();
    public DbSet<StackUpdateRow> StackUpdates => Set<StackUpdateRow>();
    public DbSet<UpdateRow> Updates => Set<UpdateRow>();
    public DbSet<MemberRow> Members => Set<MemberRow>();
    public DbSet<RoleRow> Roles => Set<RoleRow>();
    public DbSet<TeamRoleRow> TeamRoles => Set<TeamRoleRow>();
    public DbSet<PackageVersionRow> Packages => Set<PackageVersionRow>();
    public DbSet<TemplateVersionRow> Templates => Set<TemplateVersionRow>();
    public DbSet<PolicyGroupRow> PolicyGroups => Set<PolicyGroupRow>();
    public DbSet<PolicyPackVersionRow> PolicyPackVersions => Set<PolicyPackVersionRow>();
    public DbSet<DeploymentSettingsRow> DeploymentSettings => Set<DeploymentSettingsRow>();
    public DbSet<DeploymentRow> Deployments => Set<DeploymentRow>();
    public DbSet<DeploymentLogRow> DeploymentLogs => Set<DeploymentLogRow>();
    public DbSet<AgentPoolRow> AgentPools => Set<AgentPoolRow>();
    public DbSet<ScheduleRow> Schedules => Set<ScheduleRow>();
    public DbSet<WebhookRow> Webhooks => Set<WebhookRow>();
    public DbSet<EnvironmentRow> Environments => Set<EnvironmentRow>();
    public DbSet<EnvironmentRevisionRow> EnvironmentRevisions => Set<EnvironmentRevisionRow>();
    public DbSet<RegistryArtifactRow> RegistryArtifacts => Set<RegistryArtifactRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<StackRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Project, x.Stack });
            e.Property(x => x.Tags).AsJsonb();
            e.Property(x => x.Config).AsJsonb();
            e.Property(x => x.Deployment).AsJsonb();
        });

        b.Entity<StackUpdateRow>(e =>
        {
            e.HasKey(x => x.UpdateId);
            e.HasIndex(x => new { x.Org, x.Project, x.Stack });
            e.Property(x => x.Config).AsJsonb();
        });

        b.Entity<UpdateRow>(e =>
        {
            e.HasKey(x => x.UpdateId);
            e.Property(x => x.Config).AsJsonb();
            e.Property(x => x.Checkpoint).AsJsonb();
        });

        b.Entity<MemberRow>(e => e.HasKey(x => new { x.Org, x.UserLogin }));

        b.Entity<RoleRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Org);
            e.Property(x => x.Details).AsJsonb();
        });

        b.Entity<TeamRoleRow>(e => e.HasKey(x => new { x.Org, x.TeamName, x.RoleId }));

        b.Entity<PackageVersionRow>(e =>
        {
            e.HasKey(x => new { x.Source, x.Publisher, x.Name, x.Version });
            e.Property(x => x.Nav).AsJsonb();
        });
        b.Entity<TemplateVersionRow>(e => e.HasKey(x => new { x.Source, x.Publisher, x.Name, x.Version }));

        b.Entity<PolicyGroupRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Name });
            e.Property(x => x.Stacks).AsJsonb();
            e.Property(x => x.AppliedPolicyPacks).AsJsonb();
        });

        b.Entity<PolicyPackVersionRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Name, x.Version });
            e.Property(x => x.Policies).AsJsonb();
        });

        b.Entity<DeploymentSettingsRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Project, x.Stack });
            e.Property(x => x.Settings).AsJsonb();
        });

        b.Entity<DeploymentRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Stack });
            e.HasIndex(x => x.Status);       // poll claims the oldest not-started row
            e.HasIndex(x => x.JobId);         // runner callbacks look up by job id
            e.Property(x => x.Jobs).AsJsonb();
            e.Property(x => x.Updates).AsJsonb();
        });

        b.Entity<DeploymentLogRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => x.DeploymentId);
        });

        b.Entity<AgentPoolRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Org);
            e.HasIndex(x => x.Token).IsUnique();  // pool-token lookup on every agent request
        });

        b.Entity<ScheduleRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Stack });
            e.Property(x => x.Schedule).AsJsonb();
        });

        b.Entity<WebhookRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Stack });
            e.Property(x => x.Webhook).AsJsonb();
        });

        b.Entity<EnvironmentRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Project, x.Name });
            e.Property(x => x.Tags).AsJsonb();
        });

        b.Entity<EnvironmentRevisionRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Name });
            e.Property(x => x.Tags).AsJsonb();
        });

        b.Entity<RegistryArtifactRow>(e => e.HasKey(x => x.Key));
    }
}
