using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSlidingScalePricingToTicketTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                schema: "public",
                table: "TicketTypes",
                type: "numeric(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPrice",
                schema: "public",
                table: "TicketTypes",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                schema: "public",
                table: "TicketTypes",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinPrice",
                schema: "public",
                table: "TicketTypes",
                type: "numeric(10,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPrice",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.DropColumn(
                name: "MaxPrice",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.DropColumn(
                name: "MinPrice",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                schema: "public",
                table: "TicketTypes",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldNullable: true);
        }
    }
}
