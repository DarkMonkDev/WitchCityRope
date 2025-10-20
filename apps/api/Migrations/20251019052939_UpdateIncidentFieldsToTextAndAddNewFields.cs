using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIncidentFieldsToTextAndAddNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DesiredOutcomes",
                schema: "public",
                table: "SafetyIncidents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnonymityPreferenceDuringProcess",
                schema: "public",
                table: "SafetyIncidents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FutureInteractionPreference",
                schema: "public",
                table: "SafetyIncidents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonymityPreferenceDuringProcess",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "FutureInteractionPreference",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.AlterColumn<int>(
                name: "DesiredOutcomes",
                schema: "public",
                table: "SafetyIncidents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
