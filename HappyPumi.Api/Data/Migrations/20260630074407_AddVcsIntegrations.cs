using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVcsIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VcsIntegrations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    BaseUrl = table.Column<string>(type: "text", nullable: true),
                    AccountName = table.Column<string>(type: "text", nullable: true),
                    AccountId = table.Column<long>(type: "bigint", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    AzureProject = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Settings = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VcsIntegrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VcsIntegrations_Org_Kind",
                table: "VcsIntegrations",
                columns: new[] { "Org", "Kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VcsIntegrations");
        }
    }
}
