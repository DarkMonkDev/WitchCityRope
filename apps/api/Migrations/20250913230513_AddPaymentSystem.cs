using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedStripePaymentMethodId = table.Column<string>(type: "text", nullable: false),
                    LastFourDigits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    CardBrand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiryMonth = table.Column<int>(type: "integer", nullable: false),
                    ExpiryYear = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.CheckConstraint("CHK_PaymentMethods_CardBrand", "\"CardBrand\" IN ('Visa', 'MasterCard', 'American Express', 'Discover', 'JCB', 'Diners Club')");
                    table.CheckConstraint("CHK_PaymentMethods_ExpiryMonth_Range", "\"ExpiryMonth\" >= 1 AND \"ExpiryMonth\" <= 12");
                    table.CheckConstraint("CHK_PaymentMethods_ExpiryYear_Future", "\"ExpiryYear\" >= EXTRACT(YEAR FROM NOW())");
                    table.CheckConstraint("CHK_PaymentMethods_LastFourDigits", "\"LastFourDigits\" ~ '^\\d{4}$'");
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    SlidingScalePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0.00m),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethodType = table.Column<int>(type: "integer", nullable: false),
                    EncryptedStripePaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    EncryptedStripeCustomerId = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    RefundAmountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    RefundCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    EncryptedStripeRefundId = table.Column<string>(type: "text", nullable: true),
                    RefundReason = table.Column<string>(type: "text", nullable: true),
                    RefundedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false, defaultValue: new Dictionary<string, object>())
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

            migrationBuilder.CreateTable(
                name: "PaymentAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    NewValues = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAuditLog", x => x.Id);
                    table.CheckConstraint("CHK_PaymentAuditLog_ActionType", "\"ActionType\" IN ('PaymentInitiated', 'PaymentProcessed', 'PaymentCompleted', 'PaymentFailed', 'PaymentRetried', 'RefundInitiated', 'RefundCompleted', 'RefundFailed', 'StatusChanged', 'MetadataUpdated', 'SystemAction')");
                    table.ForeignKey(
                        name: "FK_PaymentAuditLog_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "public",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentAuditLog_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PaymentFailures",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FailureMessage = table.Column<string>(type: "text", nullable: false),
                    EncryptedStripeErrorDetails = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentFailures", x => x.Id);
                    table.CheckConstraint("CHK_PaymentFailures_RetryCount_NonNegative", "\"RetryCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_PaymentFailures_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "public",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRefunds",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundAmountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    RefundCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    RefundReason = table.Column<string>(type: "text", nullable: false),
                    RefundStatus = table.Column<int>(type: "integer", nullable: false),
                    EncryptedStripeRefundId = table.Column<string>(type: "text", nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false, defaultValue: new Dictionary<string, object>())
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                    table.CheckConstraint("CHK_PaymentRefunds_Currency", "\"RefundCurrency\" IN ('USD', 'EUR', 'GBP', 'CAD')");
                    table.CheckConstraint("CHK_PaymentRefunds_ReasonRequired", "LENGTH(TRIM(\"RefundReason\")) >= 10");
                    table.CheckConstraint("CHK_PaymentRefunds_RefundAmountValue_Positive", "\"RefundAmountValue\" > 0");
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Payments_OriginalPaymentId",
                        column: x => x.OriginalPaymentId,
                        principalSchema: "public",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_ActionType",
                schema: "public",
                table: "PaymentAuditLog",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_FailedActions",
                schema: "public",
                table: "PaymentAuditLog",
                column: "CreatedAt",
                filter: "\"ActionType\" IN ('PaymentFailed', 'RefundFailed')");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_NewValues_Gin",
                schema: "public",
                table: "PaymentAuditLog",
                column: "NewValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_OldValues_Gin",
                schema: "public",
                table: "PaymentAuditLog",
                column: "OldValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_PaymentId_CreatedAt",
                schema: "public",
                table: "PaymentAuditLog",
                columns: new[] { "PaymentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_UserId_CreatedAt",
                schema: "public",
                table: "PaymentAuditLog",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailures_FailedAt",
                schema: "public",
                table: "PaymentFailures",
                column: "FailedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailures_FailureCode",
                schema: "public",
                table: "PaymentFailures",
                column: "FailureCode");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailures_PaymentId",
                schema: "public",
                table: "PaymentFailures",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailures_RetryCount",
                schema: "public",
                table: "PaymentFailures",
                column: "RetryCount",
                filter: "\"RetryCount\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsActive",
                schema: "public",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsActive" },
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsDefault",
                schema: "public",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsDefault" },
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentMethods_UserDefault",
                schema: "public",
                table: "PaymentMethods",
                column: "UserId",
                unique: true,
                filter: "\"IsDefault\" = true AND \"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_Metadata_Gin",
                schema: "public",
                table: "PaymentRefunds",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_OriginalPaymentId",
                schema: "public",
                table: "PaymentRefunds",
                column: "OriginalPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_ProcessedAt",
                schema: "public",
                table: "PaymentRefunds",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_ProcessedByUserId",
                schema: "public",
                table: "PaymentRefunds",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_RefundStatus",
                schema: "public",
                table: "PaymentRefunds",
                column: "RefundStatus");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentFailures",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentMethods",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentRefunds",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "public");
        }
    }
}
