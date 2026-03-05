using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingPaymentAttendanceStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_EventAttendances_Status",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_EventAttendances_Status",
                schema: "public",
                table: "EventAttendances",
                sql: "\"Status\" IN (1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_EventAttendances_Status",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_EventAttendances_Status",
                schema: "public",
                table: "EventAttendances",
                sql: "\"Status\" IN (1, 2, 3, 4)");
        }
    }
}
