using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class VettingSystemEmailTemplatesAndBulkOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VettingApplications_ApplicantId",
                schema: "public",
                table: "VettingApplications");

            migrationBuilder.RenameTable(
                name: "VettingApplicationNotes",
                schema: "public",
                newName: "VettingApplicationNotes");

            migrationBuilder.AddColumn<Guid>(
                name: "VettingEmailTemplateId",
                schema: "public",
                table: "VettingNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomatic",
                table: "VettingApplicationNotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NoteCategory",
                table: "VettingApplicationNotes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateTable(
                name: "VettingBulkOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PerformedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailureCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingBulkOperations", x => x.Id);
                    table.CheckConstraint("CHK_VettingBulkOperations_Counts", "\"SuccessCount\" + \"FailureCount\" <= \"TotalItems\"");
                    table.CheckConstraint("CHK_VettingBulkOperations_TotalItems", "\"TotalItems\" > 0");
                    table.ForeignKey(
                        name: "FK_VettingBulkOperations_Users_PerformedBy",
                        column: x => x.PerformedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VettingEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateType = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingEmailTemplates", x => x.Id);
                    table.CheckConstraint("CHK_VettingEmailTemplates_Body_Length", "LENGTH(\"Body\") >= 10");
                    table.CheckConstraint("CHK_VettingEmailTemplates_Subject_Length", "LENGTH(\"Subject\") BETWEEN 5 AND 200");
                    table.ForeignKey(
                        name: "FK_VettingEmailTemplates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VettingBulkOperationItems",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BulkOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    RetryAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingBulkOperationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingBulkOperationItems_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "public",
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VettingBulkOperationItems_VettingBulkOperations_BulkOperati~",
                        column: x => x.BulkOperationId,
                        principalTable: "VettingBulkOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingBulkOperationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BulkOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LogLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Context = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingBulkOperationLogs", x => x.Id);
                    table.CheckConstraint("CHK_VettingBulkOperationLogs_LogLevel", "\"LogLevel\" IN ('Debug', 'Info', 'Warning', 'Error', 'Critical')");
                    table.ForeignKey(
                        name: "FK_VettingBulkOperationLogs_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "public",
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VettingBulkOperationLogs_VettingBulkOperations_BulkOperatio~",
                        column: x => x.BulkOperationId,
                        principalTable: "VettingBulkOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_VettingEmailTemplateId",
                schema: "public",
                table: "VettingNotifications",
                column: "VettingEmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "UQ_VettingApplications_ApplicantId_Active",
                schema: "public",
                table: "VettingApplications",
                column: "ApplicantId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"ApplicantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationNotes_Application_Automatic_Created",
                table: "VettingApplicationNotes",
                columns: new[] { "ApplicationId", "IsAutomatic", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationNotes_IsAutomatic",
                table: "VettingApplicationNotes",
                column: "IsAutomatic");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationNotes_NoteCategory",
                table: "VettingApplicationNotes",
                column: "NoteCategory");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_VettingApplicationNotes_NoteCategory",
                table: "VettingApplicationNotes",
                sql: "\"NoteCategory\" IN ('Manual', 'StatusChange', 'BulkOperation', 'System', 'EmailSent', 'EmailFailed')");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationItems_ApplicationId",
                schema: "public",
                table: "VettingBulkOperationItems",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationItems_BulkOperationId",
                schema: "public",
                table: "VettingBulkOperationItems",
                column: "BulkOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationItems_RetryAt",
                schema: "public",
                table: "VettingBulkOperationItems",
                column: "RetryAt",
                filter: "\"RetryAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationItems_Success_ProcessedAt",
                schema: "public",
                table: "VettingBulkOperationItems",
                columns: new[] { "Success", "ProcessedAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_VettingBulkOperationItems_Operation_Application",
                schema: "public",
                table: "VettingBulkOperationItems",
                columns: new[] { "BulkOperationId", "ApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationLogs_ApplicationId",
                table: "VettingBulkOperationLogs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationLogs_BulkOperationId_CreatedAt",
                table: "VettingBulkOperationLogs",
                columns: new[] { "BulkOperationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationLogs_Context",
                table: "VettingBulkOperationLogs",
                column: "Context")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationLogs_LogLevel",
                table: "VettingBulkOperationLogs",
                column: "LogLevel",
                filter: "\"LogLevel\" IN ('Error', 'Critical')");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperations_OperationType_PerformedAt",
                table: "VettingBulkOperations",
                columns: new[] { "OperationType", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperations_Parameters",
                table: "VettingBulkOperations",
                column: "Parameters")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperations_PerformedBy_PerformedAt",
                table: "VettingBulkOperations",
                columns: new[] { "PerformedBy", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperations_Status",
                table: "VettingBulkOperations",
                column: "Status",
                filter: "\"Status\" = 1");

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
                name: "UQ_VettingEmailTemplates_TemplateType",
                table: "VettingEmailTemplates",
                column: "TemplateType",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VettingNotifications_VettingEmailTemplates_VettingEmailTemp~",
                schema: "public",
                table: "VettingNotifications",
                column: "VettingEmailTemplateId",
                principalTable: "VettingEmailTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VettingNotifications_VettingEmailTemplates_VettingEmailTemp~",
                schema: "public",
                table: "VettingNotifications");

            migrationBuilder.DropTable(
                name: "VettingBulkOperationItems",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingBulkOperationLogs");

            migrationBuilder.DropTable(
                name: "VettingEmailTemplates");

            migrationBuilder.DropTable(
                name: "VettingBulkOperations");

            migrationBuilder.DropIndex(
                name: "IX_VettingNotifications_VettingEmailTemplateId",
                schema: "public",
                table: "VettingNotifications");

            migrationBuilder.DropIndex(
                name: "UQ_VettingApplications_ApplicantId_Active",
                schema: "public",
                table: "VettingApplications");

            migrationBuilder.DropIndex(
                name: "IX_VettingApplicationNotes_Application_Automatic_Created",
                table: "VettingApplicationNotes");

            migrationBuilder.DropIndex(
                name: "IX_VettingApplicationNotes_IsAutomatic",
                table: "VettingApplicationNotes");

            migrationBuilder.DropIndex(
                name: "IX_VettingApplicationNotes_NoteCategory",
                table: "VettingApplicationNotes");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_VettingApplicationNotes_NoteCategory",
                table: "VettingApplicationNotes");

            migrationBuilder.DropColumn(
                name: "VettingEmailTemplateId",
                schema: "public",
                table: "VettingNotifications");

            migrationBuilder.DropColumn(
                name: "IsAutomatic",
                table: "VettingApplicationNotes");

            migrationBuilder.DropColumn(
                name: "NoteCategory",
                table: "VettingApplicationNotes");

            migrationBuilder.RenameTable(
                name: "VettingApplicationNotes",
                newName: "VettingApplicationNotes",
                newSchema: "public");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_ApplicantId",
                schema: "public",
                table: "VettingApplications",
                column: "ApplicantId");
        }
    }
}
