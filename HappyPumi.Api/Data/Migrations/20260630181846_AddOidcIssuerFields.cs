using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOidcIssuerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "OidcIssuers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsed",
                table: "OidcIssuers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MaxExpiration",
                table: "OidcIssuers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "OidcIssuers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Thumbprints",
                table: "OidcIssuers",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OidcIssuers_Org_Id",
                table: "OidcIssuers",
                columns: new[] { "Org", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OidcIssuers_Org_Id",
                table: "OidcIssuers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "OidcIssuers");

            migrationBuilder.DropColumn(
                name: "LastUsed",
                table: "OidcIssuers");

            migrationBuilder.DropColumn(
                name: "MaxExpiration",
                table: "OidcIssuers");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "OidcIssuers");

            migrationBuilder.DropColumn(
                name: "Thumbprints",
                table: "OidcIssuers");
        }
    }
}
