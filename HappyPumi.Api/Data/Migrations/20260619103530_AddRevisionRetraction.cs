using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRevisionRetraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetractReason",
                table: "EnvironmentRevisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RetractReplacement",
                table: "EnvironmentRevisions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Retracted",
                table: "EnvironmentRevisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetractedAt",
                table: "EnvironmentRevisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetractedByLogin",
                table: "EnvironmentRevisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetractedByName",
                table: "EnvironmentRevisions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetractReason",
                table: "EnvironmentRevisions");

            migrationBuilder.DropColumn(
                name: "RetractReplacement",
                table: "EnvironmentRevisions");

            migrationBuilder.DropColumn(
                name: "Retracted",
                table: "EnvironmentRevisions");

            migrationBuilder.DropColumn(
                name: "RetractedAt",
                table: "EnvironmentRevisions");

            migrationBuilder.DropColumn(
                name: "RetractedByLogin",
                table: "EnvironmentRevisions");

            migrationBuilder.DropColumn(
                name: "RetractedByName",
                table: "EnvironmentRevisions");
        }
    }
}
