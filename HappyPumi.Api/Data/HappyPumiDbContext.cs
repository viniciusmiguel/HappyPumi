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
    public DbSet<StackAnnotationRow> StackAnnotations => Set<StackAnnotationRow>();
    public DbSet<StackPermissionRow> StackPermissions => Set<StackPermissionRow>();
    public DbSet<UpdateRow> Updates => Set<UpdateRow>();
    public DbSet<MemberRow> Members => Set<MemberRow>();
    public DbSet<RoleRow> Roles => Set<RoleRow>();
    public DbSet<TeamRoleRow> TeamRoles => Set<TeamRoleRow>();
    public DbSet<TeamRow> Teams => Set<TeamRow>();
    public DbSet<PackageVersionRow> Packages => Set<PackageVersionRow>();
    public DbSet<TemplateVersionRow> Templates => Set<TemplateVersionRow>();
    public DbSet<PolicyGroupRow> PolicyGroups => Set<PolicyGroupRow>();
    public DbSet<PolicyPackVersionRow> PolicyPackVersions => Set<PolicyPackVersionRow>();
    public DbSet<PolicyFindingRow> PolicyFindings => Set<PolicyFindingRow>();
    public DbSet<AuditLogRow> AuditLogs => Set<AuditLogRow>();
    public DbSet<ServiceRow> Services => Set<ServiceRow>();
    public DbSet<CloudAccountRow> CloudAccounts => Set<CloudAccountRow>();
    public DbSet<VcsConnectionRow> VcsConnections => Set<VcsConnectionRow>();
    public DbSet<OidcIssuerRow> OidcIssuers => Set<OidcIssuerRow>();
    public DbSet<ApprovalRuleRow> ApprovalRules => Set<ApprovalRuleRow>();
    public DbSet<DeploymentSettingsRow> DeploymentSettings => Set<DeploymentSettingsRow>();
    public DbSet<DeploymentRow> Deployments => Set<DeploymentRow>();
    public DbSet<DeploymentLogRow> DeploymentLogs => Set<DeploymentLogRow>();
    public DbSet<AgentPoolRow> AgentPools => Set<AgentPoolRow>();
    public DbSet<ScheduleRow> Schedules => Set<ScheduleRow>();
    public DbSet<WebhookRow> Webhooks => Set<WebhookRow>();
    public DbSet<WebhookDeliveryRow> WebhookDeliveries => Set<WebhookDeliveryRow>();
    public DbSet<OrgWebhookRow> OrgWebhooks => Set<OrgWebhookRow>();
    public DbSet<EnvironmentRow> Environments => Set<EnvironmentRow>();
    public DbSet<EnvironmentRevisionRow> EnvironmentRevisions => Set<EnvironmentRevisionRow>();
    public DbSet<EnvironmentWebhookRow> EnvironmentWebhooks => Set<EnvironmentWebhookRow>();
    public DbSet<EnvironmentScheduleRow> EnvironmentSchedules => Set<EnvironmentScheduleRow>();
    public DbSet<EnvironmentDraftRow> EnvironmentDrafts => Set<EnvironmentDraftRow>();
    public DbSet<EnvironmentOpenRequestRow> EnvironmentOpenRequests => Set<EnvironmentOpenRequestRow>();
    public DbSet<EnvironmentRotationEventRow> EnvironmentRotationEvents => Set<EnvironmentRotationEventRow>();
    public DbSet<EnvironmentWebhookDeliveryRow> EnvironmentWebhookDeliveries => Set<EnvironmentWebhookDeliveryRow>();
    public DbSet<RegistryArtifactRow> RegistryArtifacts => Set<RegistryArtifactRow>();
    public DbSet<VcsIntegrationRow> VcsIntegrations => Set<VcsIntegrationRow>();
    public DbSet<AccessTokenRow> AccessTokens => Set<AccessTokenRow>();
    public DbSet<CmkRow> CustomerManagedKeys => Set<CmkRow>();
    public DbSet<KeyMigrationRow> KeyMigrations => Set<KeyMigrationRow>();
    public DbSet<SamlConfigRow> SamlConfigs => Set<SamlConfigRow>();
    public DbSet<ConnectedCloudAccountRow> ConnectedCloudAccounts => Set<ConnectedCloudAccountRow>();
    public DbSet<ChangeGateRow> ChangeGates => Set<ChangeGateRow>();
    public DbSet<ChangeRequestRow> ChangeRequests => Set<ChangeRequestRow>();
    public DbSet<ChangeRequestEventRow> ChangeRequestEvents => Set<ChangeRequestEventRow>();
    public DbSet<TemplateSourceRow> TemplateSources => Set<TemplateSourceRow>();
    public DbSet<AuthPolicyRow> AuthPolicies => Set<AuthPolicyRow>();
    public DbSet<OrgSettingsRow> OrgSettings => Set<OrgSettingsRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<StackRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Project, x.Stack });
            e.Property(x => x.Tags).AsJsonb();
            e.Property(x => x.Config).AsJsonb();
            e.Property(x => x.Deployment).AsJsonb();
            e.Property(x => x.NotificationSettings).AsJsonb();
        });

        b.Entity<StackAnnotationRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Project, x.Stack, x.Kind });
            e.Property(x => x.Payload).HasColumnType("jsonb");
            // Annotations are cleaned up with their owning stack.
            e.HasOne<StackRow>().WithMany()
                .HasForeignKey(x => new { x.Org, x.Project, x.Stack })
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<StackUpdateRow>(e =>
        {
            e.HasKey(x => x.UpdateId);
            // At most one history entry per version per stack. The natural-key index leads with
            // (Org,Project,Stack), so it also serves the FK lookup — no separate index needed.
            e.HasIndex(x => new { x.Org, x.Project, x.Stack, x.Version }).IsUnique();
            e.HasOne<StackRow>().WithMany()
                .HasForeignKey(x => new { x.Org, x.Project, x.Stack })
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Config).AsJsonb();
        });

        b.Entity<StackPermissionRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Project, x.Stack, x.SubjectKind, x.SubjectName });
            // Grants are cleaned up with their owning stack.
            e.HasOne<StackRow>().WithMany()
                .HasForeignKey(x => new { x.Org, x.Project, x.Stack })
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UpdateRow>(e =>
        {
            e.HasKey(x => x.UpdateId);
            // FK to the owning stack (cascade cleans in-flight/finished updates when the stack is deleted);
            // the FK also provides the (Org,Project,Stack) lookup index.
            e.HasOne<StackRow>().WithMany()
                .HasForeignKey(x => new { x.Org, x.Project, x.Stack })
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Config).AsJsonb();
            e.Property(x => x.Checkpoint).AsJsonb();
            e.Property(x => x.Events).AsJsonb();
        });

        b.Entity<MemberRow>(e => e.HasKey(x => new { x.Org, x.UserLogin }));

        b.Entity<RoleRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Org);
            e.Property(x => x.Details).AsJsonb();
        });

        b.Entity<TeamRoleRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.TeamName, x.RoleId });
            e.HasIndex(x => new { x.Org, x.RoleId }); // "which teams hold role X" (ListTeamsWithRole)
        });
        b.Entity<TeamRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Name });
            e.Property(x => x.Members).AsJsonb();
        });

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
            // A version tag (e.g. "1.0.0") resolves to exactly one version within a pack.
            e.HasIndex(x => new { x.Org, x.Name, x.VersionTag })
                .IsUnique()
                .HasFilter("\"VersionTag\" IS NOT NULL");
            e.Property(x => x.Policies).AsJsonb();
        });

        b.Entity<PolicyFindingRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Org); // findings are listed per org (ListPolicyViolationsV2)
            e.Property(x => x.Finding).AsJsonb();
        });

        b.Entity<AuditLogRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Timestamp }); // listed per org, newest first
        });
        b.Entity<ServiceRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Name });
            e.Property(x => x.Items).AsJsonb();
        });
        b.Entity<CloudAccountRow>(e => e.HasKey(x => new { x.Org, x.Name }));
        b.Entity<VcsConnectionRow>(e => e.HasKey(x => new { x.Org, x.Name }));
        b.Entity<OidcIssuerRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Name });
            e.HasIndex(x => new { x.Org, x.Id }); // issuers are fetched/updated/deleted by their opaque GUID id
            e.Property(x => x.Thumbprints).AsJsonb();
        });
        b.Entity<ApprovalRuleRow>(e => e.HasKey(x => new { x.Org, x.Name }));

        b.Entity<DeploymentSettingsRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Project, x.Stack });
            e.Property(x => x.Settings).AsJsonb();
        });

        b.Entity<DeploymentRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Stack });
            e.HasIndex(x => new { x.Status, x.Created }); // poll claims the OLDEST not-started row
            // One deployment per claimed runner job. JobId is null until claimed, so the uniqueness is
            // enforced only over claimed rows (partial index).
            e.HasIndex(x => x.JobId).IsUnique().HasFilter("\"JobId\" IS NOT NULL");
            e.Property(x => x.Jobs).AsJsonb();
            e.Property(x => x.Updates).AsJsonb();
        });

        b.Entity<DeploymentLogRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => x.DeploymentId);
            e.HasOne<DeploymentRow>().WithMany()
                .HasForeignKey(x => x.DeploymentId)
                .OnDelete(DeleteBehavior.Cascade); // logs die with their deployment
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

        b.Entity<WebhookDeliveryRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ScopeKind, x.ScopeId, x.WebhookName }); // listed per (scope, webhook)
        });

        b.Entity<OrgWebhookRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Name }).IsUnique(); // one webhook name per org
            e.Property(x => x.Filters).AsJsonb();
            e.Property(x => x.Groups).AsJsonb();
        });

        b.Entity<EnvironmentRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Project, x.Name });
            e.Property(x => x.Tags).AsJsonb();
        });

        b.Entity<EnvironmentRevisionRow>(e =>
        {
            e.HasKey(x => x.Id);
            // Revision numbers are unique within an environment; this natural-key index also serves the FK.
            e.HasIndex(x => new { x.Org, x.Project, x.Name, x.Number }).IsUnique();
            e.HasOne<EnvironmentRow>().WithMany()
                .HasForeignKey(x => new { x.Org, x.Project, x.Name })
                .OnDelete(DeleteBehavior.Cascade); // revisions die with their environment
            e.Property(x => x.Tags).AsJsonb();
        });

        b.Entity<EnvironmentWebhookRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.EnvName, x.Name }).IsUnique();
            e.Property(x => x.Filters).AsJsonb();
            e.Property(x => x.Groups).AsJsonb();
        });

        // ESC operational state (previously in-memory): keyed by Id, scoped by (Org, Project, Name), payload jsonb.
        b.Entity<EnvironmentScheduleRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Name });
            e.Property(x => x.Action).AsJsonb();
        });
        b.Entity<EnvironmentDraftRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Name });
            e.Property(x => x.Draft).AsJsonb();
        });
        b.Entity<EnvironmentOpenRequestRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Name });
            e.Property(x => x.Request).AsJsonb();
        });
        b.Entity<EnvironmentRotationEventRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Name });
            e.Property(x => x.Event).AsJsonb();
        });
        b.Entity<EnvironmentWebhookDeliveryRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Project, x.Name, x.HookName });
            e.Property(x => x.Delivery).AsJsonb();
        });

        b.Entity<RegistryArtifactRow>(e => e.HasKey(x => x.Key));

        b.Entity<VcsIntegrationRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Kind }); // integrations are listed per org, filtered by kind
            e.Property(x => x.Settings).AsJsonb();
        });

        b.Entity<AccessTokenRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Scope, x.OwnerKey }); // tokens are listed/revoked per (scope, owner)
        });

        b.Entity<CmkRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Id }); // keys are listed/looked up per org
        });

        b.Entity<KeyMigrationRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.Created }); // migrations are listed per org, newest first
        });

        b.Entity<SamlConfigRow>(e =>
        {
            e.HasKey(x => x.Org); // one SAML configuration per org
            e.Property(x => x.Admins).AsJsonb();
        });

        b.Entity<ConnectedCloudAccountRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Provider }); // one connected record per org+cloud provider
            e.Property(x => x.Accounts).AsJsonb();
        });

        b.Entity<ChangeGateRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Id }); // gates are listed/looked up per org
            e.Property(x => x.EligibleApprovers).AsJsonb();
            e.Property(x => x.ActionTypes).AsJsonb();
        });

        b.Entity<ChangeRequestRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Id }); // change requests are listed/looked up per org
            e.Property(x => x.Approvers).AsJsonb();
        });

        b.Entity<ChangeRequestEventRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Org, x.ChangeRequestId }); // the events endpoint lists one CR's timeline
        });

        b.Entity<TemplateSourceRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.Id }); // sources are listed/looked up per org
        });

        b.Entity<AuthPolicyRow>(e =>
        {
            e.HasKey(x => new { x.Org, x.PolicyId }); // one auth policy per (org, OIDC-issuer id)
            e.Property(x => x.Policies).AsJsonb();
        });

        b.Entity<OrgSettingsRow>(e => e.HasKey(x => x.Org)); // one settings row per org
    }
}
