using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationFieldsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedPayPalCaptureId",
                schema: "public",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "public",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "public",
                table: "PaymentRefunds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "public",
                table: "PaymentRefunds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "public",
                table: "PaymentRefunds",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPayPalCaptureId",
                schema: "public",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "public",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "public",
                table: "PaymentRefunds");
        }
    }
}
