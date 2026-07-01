using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedStacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeletedStacks",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    ProgramId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: true),
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    StackName = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    DeletedAtUnix = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedStacks", x => new { x.Org, x.ProgramId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeletedStacks_Org_DeletedAtUnix",
                table: "DeletedStacks",
                columns: new[] { "Org", "DeletedAtUnix" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedStacks");
        }
    }
}
