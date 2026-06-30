using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerManagedKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerManagedKeys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    KeyType = table.Column<string>(type: "text", nullable: false),
                    KeyArn = table.Column<string>(type: "text", nullable: true),
                    RoleArn = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerManagedKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyMigrations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyMigrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerManagedKeys_Org_Id",
                table: "CustomerManagedKeys",
                columns: new[] { "Org", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_KeyMigrations_Org_Created",
                table: "KeyMigrations",
                columns: new[] { "Org", "Created" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerManagedKeys");

            migrationBuilder.DropTable(
                name: "KeyMigrations");
        }
    }
}
