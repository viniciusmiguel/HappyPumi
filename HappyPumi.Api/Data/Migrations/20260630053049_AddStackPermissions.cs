using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStackPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StackPermissions",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: false),
                    SubjectKind = table.Column<string>(type: "text", nullable: false),
                    SubjectName = table.Column<string>(type: "text", nullable: false),
                    Permission = table.Column<long>(type: "bigint", nullable: false),
                    IsCreator = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StackPermissions", x => new { x.Org, x.Project, x.Stack, x.SubjectKind, x.SubjectName });
                    table.ForeignKey(
                        name: "FK_StackPermissions_Stacks_Org_Project_Stack",
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
                name: "StackPermissions");
        }
    }
}
