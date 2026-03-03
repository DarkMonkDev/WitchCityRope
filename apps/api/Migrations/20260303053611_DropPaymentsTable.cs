using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropPaymentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments",
                schema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    EncryptedPayPalCaptureId = table.Column<string>(type: "text", nullable: true),
                    EncryptedPayPalOrderId = table.Column<string>(type: "text", nullable: true),
                    EncryptedPayPalPayerId = table.Column<string>(type: "text", nullable: true),
                    EncryptedPayPalRefundId = table.Column<string>(type: "text", nullable: true),
                    EventRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'"),
                    PaymentMethodType = table.Column<int>(type: "integer", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    RefundAmountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    RefundCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    RefundReason = table.Column<string>(type: "text", maxLength: 200, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    SlidingScalePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0.00m),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    VenmoUsername = table.Column<string>(type: "text", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CHK_Payments_AmountValue_NonNegative", "\"AmountValue\" >= 0");
                    table.CheckConstraint("CHK_Payments_Currency_Valid", "\"Currency\" IN ('USD', 'EUR', 'GBP', 'CAD')");
                    table.CheckConstraint("CHK_Payments_CurrencyConsistency", "(\"RefundCurrency\" IS NULL) OR (\"RefundCurrency\" = \"Currency\")");
                    table.CheckConstraint("CHK_Payments_RefundAmount_NotExceedOriginal", "\"RefundAmountValue\" IS NULL OR \"RefundAmountValue\" <= \"AmountValue\"");
                    table.CheckConstraint("CHK_Payments_RefundCurrency_Valid", "\"RefundCurrency\" IS NULL OR \"RefundCurrency\" IN ('USD', 'EUR', 'GBP', 'CAD')");
                    table.CheckConstraint("CHK_Payments_RefundRequiresOriginalPayment", "(\"RefundAmountValue\" IS NULL AND \"RefundedAt\" IS NULL) OR (\"RefundAmountValue\" IS NOT NULL AND \"RefundedAt\" IS NOT NULL)");
                    table.CheckConstraint("CHK_Payments_SlidingScalePercentage_Range", "\"SlidingScalePercentage\" >= 0 AND \"SlidingScalePercentage\" <= 75.00");
                    table.ForeignKey(
                        name: "FK_Payments_Users_RefundedByUserId",
                        column: x => x.RefundedByUserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_FailedStatus",
                schema: "public",
                table: "Payments",
                column: "ProcessedAt",
                filter: "\"Status\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Metadata_Gin",
                schema: "public",
                table: "Payments",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PendingStatus",
                schema: "public",
                table: "Payments",
                column: "CreatedAt",
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RefundedByUserId",
                schema: "public",
                table: "Payments",
                column: "RefundedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RefundedStatus",
                schema: "public",
                table: "Payments",
                column: "RefundedAt",
                filter: "\"RefundedAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SlidingScalePercentage",
                schema: "public",
                table: "Payments",
                column: "SlidingScalePercentage");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                schema: "public",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                schema: "public",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_Payments_EventRegistration_Completed",
                schema: "public",
                table: "Payments",
                column: "EventRegistrationId",
                unique: true,
                filter: "\"Status\" = 1");
        }
    }
}
