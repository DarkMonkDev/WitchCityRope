using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TestPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "VolunteerSignups");

            migrationBuilder.DropColumn(
                name: "Requirements",
                schema: "public",
                table: "VolunteerPositions");

            migrationBuilder.DropColumn(
                name: "RequiresExperience",
                schema: "public",
                table: "VolunteerPositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "VolunteerSignups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                schema: "public",
                table: "VolunteerPositions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresExperience",
                schema: "public",
                table: "VolunteerPositions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
