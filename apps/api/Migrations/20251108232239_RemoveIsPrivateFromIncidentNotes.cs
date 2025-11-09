using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsPrivateFromIncidentNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrivate",
                schema: "public",
                table: "IncidentNotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                schema: "public",
                table: "IncidentNotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
