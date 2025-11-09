using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplatesSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    GlobalTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: false),
                    TargetSessions = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[0]),
                    RecipientGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsCustomized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEmailTemplates", x => x.Id);
                    table.CheckConstraint("CHK_EventEmailTemplates_HtmlBody_NotEmpty", "LENGTH(TRIM(\"HtmlBody\")) > 0");
                    table.CheckConstraint("CHK_EventEmailTemplates_PlainTextBody_NotEmpty", "LENGTH(TRIM(\"PlainTextBody\")) > 0");
                    table.CheckConstraint("CHK_EventEmailTemplates_Subject_NotEmpty", "LENGTH(TRIM(\"Subject\")) > 0");
                    table.ForeignKey(
                        name: "FK_EventEmailTemplates_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEmailTemplates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GlobalEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    TemplateType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: false),
                    Variables = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalEmailTemplates", x => x.Id);
                    table.CheckConstraint("CHK_GlobalEmailTemplates_Category", "\"Category\" IN (0, 1, 2, 3, 4)");
                    table.CheckConstraint("CHK_GlobalEmailTemplates_HtmlBody_NotEmpty", "LENGTH(TRIM(\"HtmlBody\")) > 0");
                    table.CheckConstraint("CHK_GlobalEmailTemplates_PlainTextBody_NotEmpty", "LENGTH(TRIM(\"PlainTextBody\")) > 0");
                    table.CheckConstraint("CHK_GlobalEmailTemplates_Subject_NotEmpty", "LENGTH(TRIM(\"Subject\")) > 0");
                    table.CheckConstraint("CHK_GlobalEmailTemplates_Version", "\"Version\" >= 1");
                    table.ForeignKey(
                        name: "FK_GlobalEmailTemplates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SentAdHocEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: false),
                    RecipientGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecipientEmails = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[0]),
                    RecipientCount = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    SendGridMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    SentAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    SentBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentAdHocEmails", x => x.Id);
                    table.CheckConstraint("CHK_SentAdHocEmails_DeliveryStatus", "\"DeliveryStatus\" IN ('Pending', 'Sent', 'Delivered', 'Failed', 'Bounced')");
                    table.CheckConstraint("CHK_SentAdHocEmails_RecipientCount", "\"RecipientCount\" >= 0");
                    table.CheckConstraint("CHK_SentAdHocEmails_Subject_NotEmpty", "LENGTH(TRIM(\"Subject\")) > 0");
                    table.ForeignKey(
                        name: "FK_SentAdHocEmails_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SentAdHocEmails_Users_SentBy",
                        column: x => x.SentBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_EventId",
                table: "EventEmailTemplates",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_UpdatedAt",
                table: "EventEmailTemplates",
                column: "UpdatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_UpdatedBy",
                table: "EventEmailTemplates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_EventEmailTemplates_EventId_Type",
                table: "EventEmailTemplates",
                columns: new[] { "EventId", "TemplateType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_Category",
                table: "GlobalEmailTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_UpdatedAt",
                table: "GlobalEmailTemplates",
                column: "UpdatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_UpdatedBy",
                table: "GlobalEmailTemplates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_Variables_Gin",
                table: "GlobalEmailTemplates",
                column: "Variables")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "UQ_GlobalEmailTemplates_Category_Type",
                table: "GlobalEmailTemplates",
                columns: new[] { "Category", "TemplateType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_DeliveryStatus",
                table: "SentAdHocEmails",
                column: "DeliveryStatus",
                filter: "\"DeliveryStatus\" IN ('Pending', 'Failed')");

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_EventId",
                table: "SentAdHocEmails",
                column: "EventId",
                filter: "\"EventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_SentAt",
                table: "SentAdHocEmails",
                column: "SentAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_SentBy",
                table: "SentAdHocEmails",
                column: "SentBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventEmailTemplates");

            migrationBuilder.DropTable(
                name: "GlobalEmailTemplates");

            migrationBuilder.DropTable(
                name: "SentAdHocEmails");
        }
    }
}
