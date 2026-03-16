using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSentAdHocEmailSuccessFailureCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailureCount",
                schema: "public",
                table: "SentAdHocEmails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuccessCount",
                schema: "public",
                table: "SentAdHocEmails",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureCount",
                schema: "public",
                table: "SentAdHocEmails");

            migrationBuilder.DropColumn(
                name: "SuccessCount",
                schema: "public",
                table: "SentAdHocEmails");
        }
    }
}
