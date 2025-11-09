using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixVettingSeedDataMismatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VettingNotifications_VettingEmailTemplates_TemplateId",
                schema: "public",
                table: "VettingNotifications");

            migrationBuilder.DropTable(
                name: "VettingEmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_VettingNotifications_TemplateId",
                schema: "public",
                table: "VettingNotifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VettingEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastModified = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TemplateType = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Variables = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingEmailTemplates", x => x.Id);
                    table.CheckConstraint("CHK_VettingEmailTemplates_HtmlBody_Length", "LENGTH(\"HtmlBody\") >= 10");
                    table.CheckConstraint("CHK_VettingEmailTemplates_PlainTextBody_Length", "LENGTH(\"PlainTextBody\") >= 10");
                    table.CheckConstraint("CHK_VettingEmailTemplates_Subject_Length", "LENGTH(\"Subject\") BETWEEN 5 AND 200");
                    table.ForeignKey(
                        name: "FK_VettingEmailTemplates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_TemplateId",
                schema: "public",
                table: "VettingNotifications",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_IsActive",
                table: "VettingEmailTemplates",
                column: "IsActive",
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_UpdatedAt",
                table: "VettingEmailTemplates",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_UpdatedBy",
                table: "VettingEmailTemplates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_Variables",
                table: "VettingEmailTemplates",
                column: "Variables")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "UQ_VettingEmailTemplates_TemplateType",
                table: "VettingEmailTemplates",
                column: "TemplateType",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VettingNotifications_VettingEmailTemplates_TemplateId",
                schema: "public",
                table: "VettingNotifications",
                column: "TemplateId",
                principalTable: "VettingEmailTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
