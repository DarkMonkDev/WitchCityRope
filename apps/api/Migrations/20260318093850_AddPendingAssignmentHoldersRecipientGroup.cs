using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingAssignmentHoldersRecipientGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_GlobalEmailTemplates_RecipientGroup",
                schema: "public",
                table: "GlobalEmailTemplates");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_GlobalEmailTemplates_RecipientGroup",
                schema: "public",
                table: "GlobalEmailTemplates",
                sql: "\"RecipientGroup\" IS NULL OR \"RecipientGroup\" IN (0, 1, 2, 3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_GlobalEmailTemplates_RecipientGroup",
                schema: "public",
                table: "GlobalEmailTemplates");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_GlobalEmailTemplates_RecipientGroup",
                schema: "public",
                table: "GlobalEmailTemplates",
                sql: "\"RecipientGroup\" IS NULL OR \"RecipientGroup\" IN (0, 1, 2, 3)");
        }
    }
}
