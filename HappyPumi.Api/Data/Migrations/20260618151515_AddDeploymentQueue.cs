using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Deployments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "Deployments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobToken",
                table: "Deployments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "Deployments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Deployments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DeploymentLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeploymentId = table.Column<string>(type: "text", nullable: false),
                    Step = table.Column<int>(type: "integer", nullable: false),
                    Line = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_JobId",
                table: "Deployments",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_Status",
                table: "Deployments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentLogs_DeploymentId",
                table: "DeploymentLogs",
                column: "DeploymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeploymentLogs");

            migrationBuilder.DropIndex(
                name: "IX_Deployments_JobId",
                table: "Deployments");

            migrationBuilder.DropIndex(
                name: "IX_Deployments_Status",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "JobToken",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Deployments");
        }
    }
}
