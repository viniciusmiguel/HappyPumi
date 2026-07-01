using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditExportConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditExportConfigs",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IamRoleArn = table.Column<string>(type: "text", nullable: true),
                    S3BucketName = table.Column<string>(type: "text", nullable: true),
                    S3PathPrefix = table.Column<string>(type: "text", nullable: true),
                    LastResultMessage = table.Column<string>(type: "text", nullable: true),
                    LastResultTimestamp = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditExportConfigs", x => x.Org);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditExportConfigs");
        }
    }
}
