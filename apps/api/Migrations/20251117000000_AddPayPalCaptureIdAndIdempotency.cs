using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Migration to add PayPal Capture ID and Idempotency Key support for refund enhancements.
    ///
    /// Phase 1 of PayPal Refund Enhancement Plan (2025-11-16):
    /// - Fixes critical bug: PayPal refunds require Capture ID, not Order ID
    /// - Adds idempotency support to prevent duplicate refunds
    ///
    /// Changes:
    /// 1. Payments table: Add EncryptedPayPalCaptureId column (stores encrypted Capture ID from PayPal API)
    /// 2. Payments table: Add IdempotencyKey column (tracks transaction idempotency)
    /// 3. PaymentRefunds table: Add IdempotencyKey column (tracks refund request idempotency)
    /// 4. Add indexes for performance optimization
    ///
    /// References:
    /// - Implementation Plan: /docs/functional-areas/payment-paypal-venmo/implementation-plans/2025-11-16-paypal-refund-enhancement-plan.md
    /// - PayPal API: Capture ID from purchase_units[0].payments.captures[0].id
    /// - PayPal Idempotency: PayPal-Request-Id header for refund deduplication
    /// </summary>
    public partial class AddPayPalCaptureIdAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========================================
            // PAYMENTS TABLE ENHANCEMENTS
            // ========================================

            // Add EncryptedPayPalCaptureId column
            // Stores encrypted Capture ID from PayPal's purchase_units[0].payments.captures[0].id
            // Nullable because existing payments don't have this (will be populated via migration script)
            migrationBuilder.AddColumn<string>(
                name: "EncryptedPayPalCaptureId",
                schema: "public",
                table: "Payments",
                type: "text",
                nullable: true,
                comment: "Encrypted PayPal Capture ID (required for refunds) - from purchase_units[0].payments.captures[0].id");

            // Add IdempotencyKey column to Payments
            // Tracks the idempotency key for this payment transaction
            // Used to prevent duplicate payment processing
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "public",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "Idempotency key for payment transaction (prevents duplicate payments)");

            // Create index on EncryptedPayPalCaptureId for fast lookup during refund operations
            migrationBuilder.CreateIndex(
                name: "IX_Payments_EncryptedPayPalCaptureId",
                schema: "public",
                table: "Payments",
                column: "EncryptedPayPalCaptureId");

            // Create index on IdempotencyKey for fast deduplication checks
            migrationBuilder.CreateIndex(
                name: "IX_Payments_IdempotencyKey",
                schema: "public",
                table: "Payments",
                column: "IdempotencyKey");

            // ========================================
            // PAYMENT REFUNDS TABLE ENHANCEMENTS
            // ========================================

            // Add IdempotencyKey column to PaymentRefunds
            // Stores the idempotency key sent to PayPal (PayPal-Request-Id header)
            // Ensures duplicate refund requests return the same result
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "public",
                table: "PaymentRefunds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "Idempotency key for refund request (maps to PayPal-Request-Id header)");

            // Create index on IdempotencyKey for fast deduplication checks
            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_IdempotencyKey",
                schema: "public",
                table: "PaymentRefunds",
                column: "IdempotencyKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ========================================
            // ROLLBACK: REMOVE PAYMENT REFUNDS INDEXES
            // ========================================

            migrationBuilder.DropIndex(
                name: "IX_PaymentRefunds_IdempotencyKey",
                schema: "public",
                table: "PaymentRefunds");

            // ========================================
            // ROLLBACK: REMOVE PAYMENTS INDEXES
            // ========================================

            migrationBuilder.DropIndex(
                name: "IX_Payments_IdempotencyKey",
                schema: "public",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_EncryptedPayPalCaptureId",
                schema: "public",
                table: "Payments");

            // ========================================
            // ROLLBACK: REMOVE PAYMENT REFUNDS COLUMNS
            // ========================================

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "public",
                table: "PaymentRefunds");

            // ========================================
            // ROLLBACK: REMOVE PAYMENTS COLUMNS
            // ========================================

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "public",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EncryptedPayPalCaptureId",
                schema: "public",
                table: "Payments");
        }
    }
}
