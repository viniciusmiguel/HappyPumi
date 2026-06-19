using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateRequestedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedByLogin",
                table: "Updates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByName",
                table: "Updates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByLogin",
                table: "StackUpdates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByName",
                table: "StackUpdates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedByLogin",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "RequestedByName",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "RequestedByLogin",
                table: "StackUpdates");

            migrationBuilder.DropColumn(
                name: "RequestedByName",
                table: "StackUpdates");
        }
    }
}
