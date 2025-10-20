using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWireframeFieldsToSafetyIncident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DesiredOutcomes",
                schema: "public",
                table: "SafetyIncidents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventName",
                schema: "public",
                table: "SafetyIncidents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HasSpokenToPerson",
                schema: "public",
                table: "SafetyIncidents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "public",
                table: "SafetyIncidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhereOccurred",
                schema: "public",
                table: "SafetyIncidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesiredOutcomes",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "EventName",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "HasSpokenToPerson",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "WhereOccurred",
                schema: "public",
                table: "SafetyIncidents");
        }
    }
}
