using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropDeadPaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRefunds_Payments_PaymentId",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropTable(
                name: "PaymentAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentFailures",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentMethods",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRefunds_PaymentId",
                schema: "public",
                table: "PaymentRefunds");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                schema: "public",
                table: "PaymentRefunds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                schema: "public",
                table: "PaymentRefunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    EncryptedStripeErrorDetails = table.Column<string>(type: "text", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    FailureCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FailureMessage = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
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
                name: "PaymentMethods",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardBrand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    EncryptedStripePaymentMethodId = table.Column<string>(type: "text", nullable: false),
                    ExpiryMonth = table.Column<int>(type: "integer", nullable: false),
                    ExpiryYear = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastFourDigits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentId",
                schema: "public",
                table: "PaymentRefunds",
                column: "PaymentId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRefunds_Payments_PaymentId",
                schema: "public",
                table: "PaymentRefunds",
                column: "PaymentId",
                principalSchema: "public",
                principalTable: "Payments",
                principalColumn: "Id");
        }
    }
}
