using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTriggerEnabledToSendingEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GlobalEmailTemplates_TriggerType_Enabled",
                schema: "public",
                table: "GlobalEmailTemplates");

            migrationBuilder.RenameColumn(
                name: "TriggerEnabled",
                schema: "public",
                table: "GlobalEmailTemplates",
                newName: "SendingEnabled");

            migrationBuilder.RenameColumn(
                name: "OverrideTriggerEnabled",
                schema: "public",
                table: "EventEmailTemplates",
                newName: "OverrideSendingEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_TriggerType_SendingEnabled",
                schema: "public",
                table: "GlobalEmailTemplates",
                columns: new[] { "TriggerType", "SendingEnabled" },
                filter: "\"SendingEnabled\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GlobalEmailTemplates_TriggerType_SendingEnabled",
                schema: "public",
                table: "GlobalEmailTemplates");

            migrationBuilder.RenameColumn(
                name: "SendingEnabled",
                schema: "public",
                table: "GlobalEmailTemplates",
                newName: "TriggerEnabled");

            migrationBuilder.RenameColumn(
                name: "OverrideSendingEnabled",
                schema: "public",
                table: "EventEmailTemplates",
                newName: "OverrideTriggerEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_TriggerType_Enabled",
                schema: "public",
                table: "GlobalEmailTemplates",
                columns: new[] { "TriggerType", "TriggerEnabled" },
                filter: "\"TriggerEnabled\" = TRUE");
        }
    }
}
