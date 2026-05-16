using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthNetRefundTransactionIdToPaymentRefund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedAuthNetRefundTransactionId",
                schema: "public",
                table: "PaymentRefunds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasVoided",
                schema: "public",
                table: "PaymentRefunds",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedAuthNetRefundTransactionId",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropColumn(
                name: "WasVoided",
                schema: "public",
                table: "PaymentRefunds");
        }
    }
}
