using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentGitSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitBranch",
                table: "Deployments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitRepoDir",
                table: "Deployments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitRepoUrl",
                table: "Deployments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitBranch",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "GitRepoDir",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "GitRepoUrl",
                table: "Deployments");
        }
    }
}
