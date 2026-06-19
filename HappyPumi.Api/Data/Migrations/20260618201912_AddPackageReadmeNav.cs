using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageReadmeNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nav",
                table: "Packages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Readme",
                table: "Packages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nav",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "Readme",
                table: "Packages");
        }
    }
}
