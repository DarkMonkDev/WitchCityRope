using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmergencyContactFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                schema: "public",
                table: "EventAttendees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                schema: "public",
                table: "EventAttendees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                schema: "public",
                table: "EventAttendees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                schema: "public",
                table: "EventAttendees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
