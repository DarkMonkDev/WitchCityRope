using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SentAdHocEmails",
                newName: "SentAdHocEmails",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "GlobalEmailTemplates",
                newName: "GlobalEmailTemplates",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "EventEmailTemplates",
                newName: "EventEmailTemplates",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "EmailTriggerLogs",
                newName: "EmailTriggerLogs",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AdHocEmailTemplates",
                newName: "AdHocEmailTemplates",
                newSchema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SentAdHocEmails",
                schema: "public",
                newName: "SentAdHocEmails");

            migrationBuilder.RenameTable(
                name: "GlobalEmailTemplates",
                schema: "public",
                newName: "GlobalEmailTemplates");

            migrationBuilder.RenameTable(
                name: "EventEmailTemplates",
                schema: "public",
                newName: "EventEmailTemplates");

            migrationBuilder.RenameTable(
                name: "EmailTriggerLogs",
                schema: "public",
                newName: "EmailTriggerLogs");

            migrationBuilder.RenameTable(
                name: "AdHocEmailTemplates",
                schema: "public",
                newName: "AdHocEmailTemplates");
        }
    }
}
