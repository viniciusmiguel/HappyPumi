using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEscOperationalState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnvironmentDrafts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Draft = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentDrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentOpenRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Request = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentOpenRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentRotationEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Event = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentRotationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentSchedules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentWebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    HookName = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivery = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentWebhookDeliveries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentDrafts_Org_Project_Name",
                table: "EnvironmentDrafts",
                columns: new[] { "Org", "Project", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentOpenRequests_Org_Project_Name",
                table: "EnvironmentOpenRequests",
                columns: new[] { "Org", "Project", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentRotationEvents_Org_Project_Name",
                table: "EnvironmentRotationEvents",
                columns: new[] { "Org", "Project", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentSchedules_Org_Project_Name",
                table: "EnvironmentSchedules",
                columns: new[] { "Org", "Project", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentWebhookDeliveries_Org_Project_Name_HookName",
                table: "EnvironmentWebhookDeliveries",
                columns: new[] { "Org", "Project", "Name", "HookName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvironmentDrafts");

            migrationBuilder.DropTable(
                name: "EnvironmentOpenRequests");

            migrationBuilder.DropTable(
                name: "EnvironmentRotationEvents");

            migrationBuilder.DropTable(
                name: "EnvironmentSchedules");

            migrationBuilder.DropTable(
                name: "EnvironmentWebhookDeliveries");
        }
    }
}
