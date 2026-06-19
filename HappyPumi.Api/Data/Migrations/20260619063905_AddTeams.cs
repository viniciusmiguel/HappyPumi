using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
