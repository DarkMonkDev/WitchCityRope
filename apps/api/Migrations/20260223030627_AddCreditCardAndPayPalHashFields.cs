using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardAndPayPalHashFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditCardLastFour",
                schema: "public",
                table: "TicketPurchases",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreditCardType",
                schema: "public",
                table: "TicketPurchases",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedAuthNetTransactionId",
                schema: "public",
                table: "TicketPurchases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPalOrderIdHash",
                schema: "public",
                table: "TicketPurchases",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_PayPalOrderIdHash",
                schema: "public",
                table: "TicketPurchases",
                column: "PayPalOrderIdHash",
                filter: "\"PayPalOrderIdHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketPurchases_PayPalOrderIdHash",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "CreditCardLastFour",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "CreditCardType",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "EncryptedAuthNetTransactionId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "PayPalOrderIdHash",
                schema: "public",
                table: "TicketPurchases");
        }
    }
}
