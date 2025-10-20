using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeverityFromIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SafetyIncidents_Severity",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropIndex(
                name: "IX_SafetyIncidents_Status_Severity_ReportedAt",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "Severity",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_Status_ReportedAt",
                schema: "public",
                table: "SafetyIncidents",
                columns: new[] { "Status", "ReportedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SafetyIncidents_Status_ReportedAt",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                schema: "public",
                table: "SafetyIncidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_Severity",
                schema: "public",
                table: "SafetyIncidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_Status_Severity_ReportedAt",
                schema: "public",
                table: "SafetyIncidents",
                columns: new[] { "Status", "Severity", "ReportedAt" });
        }
    }
}
