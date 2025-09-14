using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStripeWithPayPal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EncryptedStripeRefundId",
                schema: "public",
                table: "Payments",
                newName: "VenmoUsername");

            migrationBuilder.RenameColumn(
                name: "EncryptedStripePaymentIntentId",
                schema: "public",
                table: "Payments",
                newName: "EncryptedPayPalRefundId");

            migrationBuilder.RenameColumn(
                name: "EncryptedStripeCustomerId",
                schema: "public",
                table: "Payments",
                newName: "EncryptedPayPalPayerId");

            migrationBuilder.RenameColumn(
                name: "EncryptedStripeRefundId",
                schema: "public",
                table: "PaymentRefunds",
                newName: "EncryptedPayPalRefundId");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPayPalOrderId",
                schema: "public",
                table: "Payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPayPalOrderId",
                schema: "public",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "VenmoUsername",
                schema: "public",
                table: "Payments",
                newName: "EncryptedStripeRefundId");

            migrationBuilder.RenameColumn(
                name: "EncryptedPayPalRefundId",
                schema: "public",
                table: "Payments",
                newName: "EncryptedStripePaymentIntentId");

            migrationBuilder.RenameColumn(
                name: "EncryptedPayPalPayerId",
                schema: "public",
                table: "Payments",
                newName: "EncryptedStripeCustomerId");

            migrationBuilder.RenameColumn(
                name: "EncryptedPayPalRefundId",
                schema: "public",
                table: "PaymentRefunds",
                newName: "EncryptedStripeRefundId");
        }
    }
}
