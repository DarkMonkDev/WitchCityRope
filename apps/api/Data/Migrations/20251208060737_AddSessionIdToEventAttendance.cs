using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIdToEventAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "public",
                table: "EventAttendances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_SessionId",
                schema: "public",
                table: "EventAttendances",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_SessionId_Status_AttendanceType",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "SessionId", "Status", "AttendanceType" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_UserId_SessionId_Status",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "UserId", "SessionId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_EventAttendances_Sessions_SessionId",
                schema: "public",
                table: "EventAttendances",
                column: "SessionId",
                principalSchema: "public",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventAttendances_Sessions_SessionId",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropIndex(
                name: "IX_EventAttendances_SessionId",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropIndex(
                name: "IX_EventAttendances_SessionId_Status_AttendanceType",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropIndex(
                name: "IX_EventAttendances_UserId_SessionId_Status",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "public",
                table: "EventAttendances");
        }
    }
}
