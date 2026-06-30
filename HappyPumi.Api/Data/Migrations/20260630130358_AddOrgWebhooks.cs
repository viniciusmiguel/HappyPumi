using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgWebhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrgWebhooks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    PayloadUrl = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: true),
                    Secret = table.Column<string>(type: "text", nullable: true),
                    Filters = table.Column<string>(type: "jsonb", nullable: true),
                    Groups = table.Column<string>(type: "jsonb", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgWebhooks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgWebhooks_Org_Name",
                table: "OrgWebhooks",
                columns: new[] { "Org", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgWebhooks");
        }
    }
}
