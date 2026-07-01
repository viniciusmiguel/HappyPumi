using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrgSettings",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    MembersCanCreateStacks = table.Column<bool>(type: "boolean", nullable: false),
                    MembersCanDeleteStacks = table.Column<bool>(type: "boolean", nullable: false),
                    MembersCanCreateTeams = table.Column<bool>(type: "boolean", nullable: false),
                    MembersCanTransferStacks = table.Column<bool>(type: "boolean", nullable: false),
                    MembersCanCreateAccounts = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultStackPermission = table.Column<long>(type: "bigint", nullable: false),
                    DefaultAccountPermission = table.Column<long>(type: "bigint", nullable: false),
                    DefaultEnvironmentPermission = table.Column<string>(type: "text", nullable: false),
                    DefaultRoleId = table.Column<string>(type: "text", nullable: true),
                    DefaultDeploymentRoleId = table.Column<string>(type: "text", nullable: true),
                    DefaultAgentPoolId = table.Column<string>(type: "text", nullable: true),
                    PreferredVcs = table.Column<string>(type: "text", nullable: false),
                    AiEnablement = table.Column<string>(type: "text", nullable: false),
                    NeoEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgSettings", x => x.Org);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgSettings");
        }
    }
}
