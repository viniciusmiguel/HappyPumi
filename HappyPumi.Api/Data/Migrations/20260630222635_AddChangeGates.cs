using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeGates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangeGates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Org = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    RuleType = table.Column<string>(type: "text", nullable: false),
                    NumApprovalsRequired = table.Column<long>(type: "bigint", nullable: false),
                    AllowSelfApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequireReapprovalOnChange = table.Column<bool>(type: "boolean", nullable: false),
                    EligibleApprovers = table.Column<string>(type: "jsonb", nullable: false),
                    TargetEntityType = table.Column<string>(type: "text", nullable: false),
                    ActionTypes = table.Column<string>(type: "jsonb", nullable: false),
                    QualifiedName = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeGates", x => new { x.Org, x.Id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeGates");
        }
    }
}
