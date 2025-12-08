using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUniqueConstraintForMultiSessionTicketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_EventAttendances_User_Event_Type_Active",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.CreateIndex(
                name: "UQ_EventAttendances_User_Event_Type_Session_Active",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "UserId", "EventId", "AttendanceType", "SessionId" },
                unique: true,
                filter: "\"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_EventAttendances_User_Event_Type_Session_Active",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.CreateIndex(
                name: "UQ_EventAttendances_User_Event_Type_Active",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "UserId", "EventId", "AttendanceType" },
                unique: true,
                filter: "\"Status\" = 1");
        }
    }
}
