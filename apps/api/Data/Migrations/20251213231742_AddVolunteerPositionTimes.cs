using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerPositionTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndTime",
                schema: "public",
                table: "VolunteerPositions",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartTime",
                schema: "public",
                table: "VolunteerPositions",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                schema: "public",
                table: "VolunteerPositions");

            migrationBuilder.DropColumn(
                name: "StartTime",
                schema: "public",
                table: "VolunteerPositions");
        }
    }
}
