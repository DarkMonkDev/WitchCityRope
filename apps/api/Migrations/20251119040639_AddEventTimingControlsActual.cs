using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTimingControlsActual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CancellationCloseHours",
                schema: "public",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationOpenHours",
                schema: "public",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationCloseHours",
                schema: "public",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationOpenHours",
                schema: "public",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolunteerCancellationCloseHours",
                schema: "public",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolunteerRegistrationCloseHours",
                schema: "public",
                table: "Events",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CancellationOpenHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationOpenHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VolunteerCancellationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VolunteerRegistrationCloseHours",
                schema: "public",
                table: "Events");
        }
    }
}
