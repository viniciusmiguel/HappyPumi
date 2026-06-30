using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStackSettingsAndAnnotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationSettings",
                table: "Stacks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "Stacks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StackAnnotations",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StackAnnotations", x => new { x.Org, x.Project, x.Stack, x.Kind });
                    table.ForeignKey(
                        name: "FK_StackAnnotations_Stacks_Org_Project_Stack",
                        columns: x => new { x.Org, x.Project, x.Stack },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Org", "Project", "Stack" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StackAnnotations");

            migrationBuilder.DropColumn(
                name: "NotificationSettings",
                table: "Stacks");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "Stacks");
        }
    }
}
