using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidatePaymentTrackingToTicketPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRefunds_Payments_OriginalPaymentId",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.RenameColumn(
                name: "OriginalPaymentId",
                schema: "public",
                table: "PaymentRefunds",
                newName: "TicketPurchaseId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentRefunds_OriginalPaymentId",
                schema: "public",
                table: "PaymentRefunds",
                newName: "IX_PaymentRefunds_TicketPurchaseId");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPayPalCaptureId",
                schema: "public",
                table: "TicketPurchases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPayPalOrderId",
                schema: "public",
                table: "TicketPurchases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPayPalPayerId",
                schema: "public",
                table: "TicketPurchases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "public",
                table: "TicketPurchases",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                schema: "public",
                table: "TicketPurchases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SlidingScalePercentage",
                schema: "public",
                table: "TicketPurchases",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                schema: "public",
                table: "PaymentRefunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentId",
                schema: "public",
                table: "PaymentRefunds",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRefunds_Payments_PaymentId",
                schema: "public",
                table: "PaymentRefunds",
                column: "PaymentId",
                principalSchema: "public",
                principalTable: "Payments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRefunds_TicketPurchases_TicketPurchaseId",
                schema: "public",
                table: "PaymentRefunds",
                column: "TicketPurchaseId",
                principalSchema: "public",
                principalTable: "TicketPurchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRefunds_Payments_PaymentId",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRefunds_TicketPurchases_TicketPurchaseId",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRefunds_PaymentId",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropColumn(
                name: "EncryptedPayPalCaptureId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "EncryptedPayPalOrderId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "EncryptedPayPalPayerId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "SlidingScalePercentage",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.RenameColumn(
                name: "TicketPurchaseId",
                schema: "public",
                table: "PaymentRefunds",
                newName: "OriginalPaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentRefunds_TicketPurchaseId",
                schema: "public",
                table: "PaymentRefunds",
                newName: "IX_PaymentRefunds_OriginalPaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRefunds_Payments_OriginalPaymentId",
                schema: "public",
                table: "PaymentRefunds",
                column: "OriginalPaymentId",
                principalSchema: "public",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
