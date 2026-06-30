using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPumi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSamlConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SamlConfigs",
                columns: table => new
                {
                    Org = table.Column<string>(type: "text", nullable: false),
                    IdpMetadataXml = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    SsoUrl = table.Column<string>(type: "text", nullable: true),
                    Certificate = table.Column<string>(type: "text", nullable: true),
                    NameIdFormat = table.Column<string>(type: "text", nullable: true),
                    ValidUntil = table.Column<string>(type: "text", nullable: true),
                    ValidationError = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Admins = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlConfigs", x => x.Org);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SamlConfigs");
        }
    }
}
