using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVettingEmailTemplatesEnhancementsAndLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_VettingEmailTemplates_Body_Length",
                table: "VettingEmailTemplates");

            migrationBuilder.RenameColumn(
                name: "Body",
                table: "VettingEmailTemplates",
                newName: "PlainTextBody");

            migrationBuilder.AddColumn<string>(
                name: "HtmlBody",
                table: "VettingEmailTemplates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Variables",
                table: "VettingEmailTemplates",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateTable(
                name: "VettingEmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateType = table.Column<int>(type: "integer", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    DeliveryStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SendGridMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastRetryAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingEmailLogs", x => x.Id);
                    table.CheckConstraint("CHK_VettingEmailLogs_RetryCount", "\"RetryCount\" >= 0 AND \"RetryCount\" <= 10");
                    table.ForeignKey(
                        name: "FK_VettingEmailLogs_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_Variables",
                table: "VettingEmailTemplates",
                column: "Variables")
                .Annotation("Npgsql:IndexMethod", "gin");

            // Copy PlainTextBody to HtmlBody for existing rows (data migration)
            migrationBuilder.Sql(
                @"UPDATE ""VettingEmailTemplates""
                  SET ""HtmlBody"" = ""PlainTextBody""
                  WHERE ""HtmlBody"" = '';");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_VettingEmailTemplates_HtmlBody_Length",
                table: "VettingEmailTemplates",
                sql: "LENGTH(\"HtmlBody\") >= 10");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_VettingEmailTemplates_PlainTextBody_Length",
                table: "VettingEmailTemplates",
                sql: "LENGTH(\"PlainTextBody\") >= 10");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_ApplicationId",
                table: "VettingEmailLogs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_DeliveryStatus",
                table: "VettingEmailLogs",
                column: "DeliveryStatus");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_DeliveryStatus_RetryCount_SentAt",
                table: "VettingEmailLogs",
                columns: new[] { "DeliveryStatus", "RetryCount", "SentAt" },
                filter: "\"DeliveryStatus\" IN (3, 4) AND \"RetryCount\" < 5");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_SendGridMessageId",
                table: "VettingEmailLogs",
                column: "SendGridMessageId",
                filter: "\"SendGridMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_SentAt",
                table: "VettingEmailLogs",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VettingEmailLogs");

            migrationBuilder.DropIndex(
                name: "IX_VettingEmailTemplates_Variables",
                table: "VettingEmailTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_VettingEmailTemplates_HtmlBody_Length",
                table: "VettingEmailTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_VettingEmailTemplates_PlainTextBody_Length",
                table: "VettingEmailTemplates");

            migrationBuilder.DropColumn(
                name: "HtmlBody",
                table: "VettingEmailTemplates");

            migrationBuilder.DropColumn(
                name: "Variables",
                table: "VettingEmailTemplates");

            migrationBuilder.RenameColumn(
                name: "PlainTextBody",
                table: "VettingEmailTemplates",
                newName: "Body");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_VettingEmailTemplates_Body_Length",
                table: "VettingEmailTemplates",
                sql: "LENGTH(\"Body\") >= 10");
        }
    }
}
