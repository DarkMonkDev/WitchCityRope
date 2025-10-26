using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingTypeToTicketType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRsvpMode",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.AddColumn<string>(
                name: "PricingType",
                schema: "public",
                table: "TicketTypes",
                type: "text",
                nullable: false,
                defaultValue: "fixed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricingType",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.AddColumn<bool>(
                name: "IsRsvpMode",
                schema: "public",
                table: "TicketTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
