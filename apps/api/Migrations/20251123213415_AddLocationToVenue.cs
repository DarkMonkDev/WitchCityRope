using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToVenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "public",
                table: "Venues",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                schema: "public",
                table: "Venues");
        }
    }
}
