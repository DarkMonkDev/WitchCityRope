using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdminNotesFromVettingApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "VettingApplications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "VettingApplications",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }
    }
}
