using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAnonymityPreferenceWithBooleans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonymityPreferenceDuringProcess",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.AddColumn<bool>(
                name: "AnonymousDuringInvestigation",
                schema: "public",
                table: "SafetyIncidents",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AnonymousInFinalReport",
                schema: "public",
                table: "SafetyIncidents",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonymousDuringInvestigation",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "AnonymousInFinalReport",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.AddColumn<int>(
                name: "AnonymityPreferenceDuringProcess",
                schema: "public",
                table: "SafetyIncidents",
                type: "integer",
                nullable: true);
        }
    }
}
