using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceContactPhoneWithContactName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedContactPhone",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedContactName",
                schema: "public",
                table: "SafetyIncidents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedContactName",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedContactPhone",
                schema: "public",
                table: "SafetyIncidents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
