using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Jobs",
                table: "Deployments",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedByLogin",
                table: "Deployments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByName",
                table: "Deployments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Updates",
                table: "Deployments",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Header",
                table: "DeploymentLogs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Jobs",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "RequestedByLogin",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "RequestedByName",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "Updates",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "Header",
                table: "DeploymentLogs");
        }
    }
}
