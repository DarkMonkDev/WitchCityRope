using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotesAndRequirementsFromVolunteerSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove Notes column from VolunteerSignups table
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "VolunteerSignups");

            // Remove RequiresExperience column from VolunteerPositions table
            migrationBuilder.DropColumn(
                name: "RequiresExperience",
                table: "VolunteerPositions");

            // Remove Requirements column from VolunteerPositions table
            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "VolunteerPositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add Notes column to VolunteerSignups table
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "VolunteerSignups",
                type: "text",
                nullable: true);

            // Re-add RequiresExperience column to VolunteerPositions table
            migrationBuilder.AddColumn<bool>(
                name: "RequiresExperience",
                table: "VolunteerPositions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Re-add Requirements column to VolunteerPositions table
            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "VolunteerPositions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
