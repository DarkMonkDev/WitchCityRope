using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameVettingApplicationStatusToWorkflowStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "VettingApplications",
                newName: "WorkflowStatus");

            migrationBuilder.RenameIndex(
                name: "IX_VettingApplications_Status",
                table: "VettingApplications",
                newName: "IX_VettingApplications_WorkflowStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WorkflowStatus",
                table: "VettingApplications",
                newName: "Status");

            migrationBuilder.RenameIndex(
                name: "IX_VettingApplications_WorkflowStatus",
                table: "VettingApplications",
                newName: "IX_VettingApplications_Status");
        }
    }
}
