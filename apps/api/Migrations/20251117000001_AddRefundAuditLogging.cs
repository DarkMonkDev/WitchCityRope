using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Migration to add comprehensive audit logging for PayPal refund operations.
    ///
    /// Phase 2 of PayPal Refund Enhancement Plan (2025-11-17):
    /// - Adds retry tracking for resilient error handling
    /// - Adds error message logging for debugging and compliance
    /// - Adds PayPal response storage for audit trail and forensics
    ///
    /// Changes:
    /// 1. PaymentRefunds table: Add RetryCount column (tracks retry attempts)
    /// 2. PaymentRefunds table: Add ErrorMessage column (stores last error for debugging)
    /// 3. PaymentRefunds table: Add PayPalResponse column (stores full PayPal API response as JSONB)
    ///
    /// References:
    /// - Implementation Plan: /docs/functional-areas/payment-paypal-venmo/implementation-plans/2025-11-16-paypal-refund-enhancement-plan.md
    /// - PayPal API: Full response stored for audit compliance
    /// - Retry Strategy: Exponential backoff with max retry tracking
    /// </summary>
    public partial class AddRefundAuditLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========================================
            // PAYMENT REFUNDS TABLE: AUDIT LOGGING
            // ========================================

            // Add RetryCount column
            // Tracks number of retry attempts for failed refund requests
            // Used for exponential backoff and max retry enforcement
            // Default 0 for new records
            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "public",
                table: "PaymentRefunds",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Number of retry attempts for this refund request (used for exponential backoff)");

            // Add ErrorMessage column
            // Stores the last error message from PayPal or internal processing
            // Nullable because errors don't always occur
            // Critical for debugging failed refunds and compliance reporting
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "public",
                table: "PaymentRefunds",
                type: "text",
                nullable: true,
                comment: "Last error message from PayPal or internal processing (for debugging and compliance)");

            // Add PayPalResponse column as JSONB
            // Stores complete PayPal API response for audit trail
            // JSONB type enables efficient querying of response fields
            // Nullable because response only exists after PayPal API call
            // Essential for forensic analysis and dispute resolution
            migrationBuilder.AddColumn<string>(
                name: "PayPalResponse",
                schema: "public",
                table: "PaymentRefunds",
                type: "jsonb",
                nullable: true,
                comment: "Complete PayPal API response (stored as JSONB for audit trail and forensics)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ========================================
            // ROLLBACK: REMOVE AUDIT LOGGING COLUMNS
            // ========================================

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropColumn(
                name: "PayPalResponse",
                schema: "public",
                table: "PaymentRefunds");
        }
    }
}
