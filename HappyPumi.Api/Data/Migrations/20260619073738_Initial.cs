using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentPools",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRules",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StackPattern = table.Column<string>(type: "text", nullable: false),
                    RequiredApprovals = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRules", x => new { x.Org, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Event = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ActorName = table.Column<string>(type: "text", nullable: false),
                    SourceIp = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CloudAccounts",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudAccounts", x => new { x.Org, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Deployments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Operation = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    JobId = table.Column<string>(type: "text", nullable: true),
                    JobToken = table.Column<string>(type: "text", nullable: true),
                    RequestedByLogin = table.Column<string>(type: "text", nullable: true),
                    RequestedByName = table.Column<string>(type: "text", nullable: true),
                    TemplateRef = table.Column<string>(type: "text", nullable: true),
                    Jobs = table.Column<string>(type: "jsonb", nullable: false),
                    Updates = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deployments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentSettings",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    Settings = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentSettings", x => new { x.Org, x.Project, x.Stack });
                });

            migrationBuilder.CreateTable(
                name: "Environments",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OwnerLogin = table.Column<string>(type: "text", nullable: false),
                    OwnerName = table.Column<string>(type: "text", nullable: false),
                    DeletionProtected = table.Column<bool>(type: "boolean", nullable: false),
                    Yaml = table.Column<string>(type: "text", nullable: false),
                    CurrentRevision = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => new { x.Org, x.Project, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    UserLogin = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => new { x.Org, x.UserLogin });
                });

            migrationBuilder.CreateTable(
                name: "OidcIssuers",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OidcIssuers", x => new { x.Org, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Source = table.Column<string>(type: "text", nullable: false),
                    Publisher = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    Readme = table.Column<string>(type: "text", nullable: true),
                    Nav = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => new { x.Source, x.Publisher, x.Name, x.Version });
                });

            migrationBuilder.CreateTable(
                name: "PolicyFindings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Finding = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyGroups",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsOrgDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Stacks = table.Column<string>(type: "jsonb", nullable: false),
                    AppliedPolicyPacks = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyGroups", x => new { x.Org, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "PolicyPackVersions",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    VersionTag = table.Column<string>(type: "text", nullable: true),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    Policies = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyPackVersions", x => new { x.Org, x.Name, x.Version });
                });

            migrationBuilder.CreateTable(
                name: "RegistryArtifacts",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistryArtifacts", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsOrgDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ResourceType = table.Column<string>(type: "text", nullable: true),
                    UxPurpose = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    Schedule = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Items = table.Column<string>(type: "jsonb", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => new { x.Org, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Stacks",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    Config = table.Column<string>(type: "jsonb", nullable: true),
                    Deployment = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stacks", x => new { x.Org, x.Project, x.Stack });
                });

            migrationBuilder.CreateTable(
                name: "TeamRoles",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamRoles", x => new { x.Org, x.TeamName, x.RoleId });
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Members = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => new { x.Org, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Source = table.Column<string>(type: "text", nullable: false),
                    Publisher = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Published = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => new { x.Source, x.Publisher, x.Name, x.Version });
                });

            migrationBuilder.CreateTable(
                name: "VcsConnections",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VcsConnections", x => new { x.Org, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Webhooks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    Webhook = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeploymentId = table.Column<string>(type: "text", nullable: false),
                    Step = table.Column<int>(type: "integer", nullable: false),
                    Header = table.Column<string>(type: "text", nullable: true),
                    Line = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentLogs_Deployments_DeploymentId",
                        column: x => x.DeploymentId,
                        principalTable: "Deployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentRevisions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorLogin = table.Column<string>(type: "text", nullable: false),
                    CreatorName = table.Column<string>(type: "text", nullable: false),
                    Yaml = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentRevisions_Environments_Org_Project_Name",
                        columns: x => new { x.Org, x.Project, x.Name },
                        principalTable: "Environments",
                        principalColumns: new[] { "Org", "Project", "Name" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StackUpdates",
                columns: table => new
                {
                    UpdateId = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<long>(type: "bigint", nullable: false),
                    EndTime = table.Column<long>(type: "bigint", nullable: false),
                    Config = table.Column<string>(type: "jsonb", nullable: false),
                    RequestedByLogin = table.Column<string>(type: "text", nullable: true),
                    RequestedByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StackUpdates", x => x.UpdateId);
                    table.ForeignKey(
                        name: "FK_StackUpdates_Stacks_Org_Project_Stack",
                        columns: x => new { x.Org, x.Project, x.Stack },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Org", "Project", "Stack" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Updates",
                columns: table => new
                {
                    UpdateId = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<long>(type: "bigint", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    RequestedByLogin = table.Column<string>(type: "text", nullable: true),
                    RequestedByName = table.Column<string>(type: "text", nullable: true),
                    Config = table.Column<string>(type: "jsonb", nullable: true),
                    Checkpoint = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Updates", x => x.UpdateId);
                    table.ForeignKey(
                        name: "FK_Updates_Stacks_Org_Project_Stack",
                        columns: x => new { x.Org, x.Project, x.Stack },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Org", "Project", "Stack" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentPools_Org",
                table: "AgentPools",
                column: "Org");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPools_Token",
                table: "AgentPools",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Org_Timestamp",
                table: "AuditLogs",
                columns: new[] { "Org", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentLogs_DeploymentId",
                table: "DeploymentLogs",
                column: "DeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_JobId",
                table: "Deployments",
                column: "JobId",
                unique: true,
                filter: "\"JobId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_Org_Project_Stack",
                table: "Deployments",
                columns: new[] { "Org", "Project", "Stack" });

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_Status_Created",
                table: "Deployments",
                columns: new[] { "Status", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentRevisions_Org_Project_Name_Number",
                table: "EnvironmentRevisions",
                columns: new[] { "Org", "Project", "Name", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyFindings_Org",
                table: "PolicyFindings",
                column: "Org");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyPackVersions_Org_Name_VersionTag",
                table: "PolicyPackVersions",
                columns: new[] { "Org", "Name", "VersionTag" },
                unique: true,
                filter: "\"VersionTag\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Org",
                table: "Roles",
                column: "Org");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Org_Project_Stack",
                table: "Schedules",
                columns: new[] { "Org", "Project", "Stack" });

            migrationBuilder.CreateIndex(
                name: "IX_StackUpdates_Org_Project_Stack_Version",
                table: "StackUpdates",
                columns: new[] { "Org", "Project", "Stack", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamRoles_Org_RoleId",
                table: "TeamRoles",
                columns: new[] { "Org", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Updates_Org_Project_Stack",
                table: "Updates",
                columns: new[] { "Org", "Project", "Stack" });

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_Org_Project_Stack",
                table: "Webhooks",
                columns: new[] { "Org", "Project", "Stack" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentPools");

            migrationBuilder.DropTable(
                name: "ApprovalRules");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CloudAccounts");

            migrationBuilder.DropTable(
                name: "DeploymentLogs");

            migrationBuilder.DropTable(
                name: "DeploymentSettings");

            migrationBuilder.DropTable(
                name: "EnvironmentRevisions");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "OidcIssuers");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "PolicyFindings");

            migrationBuilder.DropTable(
                name: "PolicyGroups");

            migrationBuilder.DropTable(
                name: "PolicyPackVersions");

            migrationBuilder.DropTable(
                name: "RegistryArtifacts");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "StackUpdates");

            migrationBuilder.DropTable(
                name: "TeamRoles");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "Updates");

            migrationBuilder.DropTable(
                name: "VcsConnections");

            migrationBuilder.DropTable(
                name: "Webhooks");

            migrationBuilder.DropTable(
                name: "Deployments");

            migrationBuilder.DropTable(
                name: "Environments");

            migrationBuilder.DropTable(
                name: "Stacks");
        }
    }
}
