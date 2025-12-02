using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTriggerEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledSendAt",
                table: "SentAdHocEmails",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecipientGroup",
                table: "GlobalEmailTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimingOffsetDays",
                table: "GlobalEmailTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TriggerEnabled",
                table: "GlobalEmailTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "TriggerType",
                table: "GlobalEmailTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OverrideRecipientGroup",
                table: "EventEmailTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverrideTimingOffsetDays",
                table: "EventEmailTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideTriggerEnabled",
                table: "EventEmailTemplates",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdHocEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TemplateName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdHocEmailTemplates", x => x.Id);
                    table.CheckConstraint("CHK_AdHocEmailTemplates_HtmlBody_NotEmpty", "LENGTH(TRIM(\"HtmlBody\")) > 0");
                    table.CheckConstraint("CHK_AdHocEmailTemplates_PlainTextBody_NotEmpty", "LENGTH(TRIM(\"PlainTextBody\")) > 0");
                    table.CheckConstraint("CHK_AdHocEmailTemplates_Subject_NotEmpty", "LENGTH(TRIM(\"Subject\")) > 0");
                    table.CheckConstraint("CHK_AdHocEmailTemplates_TemplateName_NotEmpty", "LENGTH(TRIM(\"TemplateName\")) > 0");
                    table.ForeignKey(
                        name: "FK_AdHocEmailTemplates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailTriggerLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecipientGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecipientCount = table.Column<int>(type: "integer", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    SentAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Sent"),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTriggerLogs", x => x.Id);
                    table.CheckConstraint("CHK_EmailTriggerLogs_RecipientCount", "\"RecipientCount\" >= 0");
                    table.CheckConstraint("CHK_EmailTriggerLogs_Status", "\"Status\" IN ('Sent', 'Failed', 'Skipped')");
                    table.ForeignKey(
                        name: "FK_EmailTriggerLogs_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailTriggerLogs_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "public",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_Scheduled_Pending",
                table: "SentAdHocEmails",
                columns: new[] { "ScheduledSendAt", "DeliveryStatus" },
                filter: "\"ScheduledSendAt\" IS NOT NULL AND \"DeliveryStatus\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_Category_TriggerType",
                table: "GlobalEmailTemplates",
                columns: new[] { "Category", "TriggerType" });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_TriggerType_Enabled",
                table: "GlobalEmailTemplates",
                columns: new[] { "TriggerType", "TriggerEnabled" },
                filter: "\"TriggerEnabled\" = TRUE");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_GlobalEmailTemplates_RecipientGroup",
                table: "GlobalEmailTemplates",
                sql: "\"RecipientGroup\" IS NULL OR \"RecipientGroup\" IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_GlobalEmailTemplates_TimingOffsetDays",
                table: "GlobalEmailTemplates",
                sql: "\"TimingOffsetDays\" IS NULL OR (\"TimingOffsetDays\" >= -365 AND \"TimingOffsetDays\" <= 365)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_GlobalEmailTemplates_TriggerType",
                table: "GlobalEmailTemplates",
                sql: "\"TriggerType\" IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_EventEmailTemplates_OverrideRecipientGroup",
                table: "EventEmailTemplates",
                sql: "\"OverrideRecipientGroup\" IS NULL OR \"OverrideRecipientGroup\" IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_EventEmailTemplates_OverrideTimingOffsetDays",
                table: "EventEmailTemplates",
                sql: "\"OverrideTimingOffsetDays\" IS NULL OR (\"OverrideTimingOffsetDays\" >= -365 AND \"OverrideTimingOffsetDays\" <= 365)");

            migrationBuilder.CreateIndex(
                name: "IX_AdHocEmailTemplates_CreatedAt",
                table: "AdHocEmailTemplates",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AdHocEmailTemplates_CreatedBy",
                table: "AdHocEmailTemplates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AdHocEmailTemplates_TemplateName",
                table: "AdHocEmailTemplates",
                column: "TemplateName");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTriggerLogs_EventId_SessionId",
                table: "EmailTriggerLogs",
                columns: new[] { "EventId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTriggerLogs_Failed_TriggeredAt",
                table: "EmailTriggerLogs",
                columns: new[] { "Status", "TriggeredAt" },
                filter: "\"Status\" = 'Failed'");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTriggerLogs_SessionId",
                table: "EmailTriggerLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTriggerLogs_Status",
                table: "EmailTriggerLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTriggerLogs_TemplateId",
                table: "EmailTriggerLogs",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTriggerLogs_TriggeredAt",
                table: "EmailTriggerLogs",
                column: "TriggeredAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UQ_EmailTriggerLogs_Idempotency",
                table: "EmailTriggerLogs",
                columns: new[] { "TemplateId", "SessionId", "TemplateType" },
                unique: true,
                filter: "\"SessionId\" IS NOT NULL AND \"Status\" = 'Sent'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdHocEmailTemplates");

            migrationBuilder.DropTable(
                name: "EmailTriggerLogs");

            migrationBuilder.DropIndex(
                name: "IX_SentAdHocEmails_Scheduled_Pending",
                table: "SentAdHocEmails");

            migrationBuilder.DropIndex(
                name: "IX_GlobalEmailTemplates_Category_TriggerType",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_GlobalEmailTemplates_TriggerType_Enabled",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_GlobalEmailTemplates_RecipientGroup",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_GlobalEmailTemplates_TimingOffsetDays",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_GlobalEmailTemplates_TriggerType",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_EventEmailTemplates_OverrideRecipientGroup",
                table: "EventEmailTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_EventEmailTemplates_OverrideTimingOffsetDays",
                table: "EventEmailTemplates");

            migrationBuilder.DropColumn(
                name: "ScheduledSendAt",
                table: "SentAdHocEmails");

            migrationBuilder.DropColumn(
                name: "RecipientGroup",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropColumn(
                name: "TimingOffsetDays",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropColumn(
                name: "TriggerEnabled",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropColumn(
                name: "OverrideRecipientGroup",
                table: "EventEmailTemplates");

            migrationBuilder.DropColumn(
                name: "OverrideTimingOffsetDays",
                table: "EventEmailTemplates");

            migrationBuilder.DropColumn(
                name: "OverrideTriggerEnabled",
                table: "EventEmailTemplates");
        }
    }
}
