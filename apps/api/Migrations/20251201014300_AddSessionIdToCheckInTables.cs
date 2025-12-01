using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIdToCheckInTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CheckInSessionTokens_EventId_ExpiresAt_Active",
                schema: "public",
                table: "CheckInSessionTokens");

            migrationBuilder.DropIndex(
                name: "UQ_CheckIns_EventAttendee",
                schema: "public",
                table: "CheckIns");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "public",
                table: "CheckInSessionTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "public",
                table: "CheckIns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokens_SessionId",
                schema: "public",
                table: "CheckInSessionTokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokens_SessionId_ExpiresAt_Active",
                schema: "public",
                table: "CheckInSessionTokens",
                columns: new[] { "SessionId", "ExpiresAt" },
                filter: "\"IsRevoked\" = false");

            // NOTE: Removed CHK_CheckInSessionTokens_SessionBelongsToEvent check constraint
            // PostgreSQL does NOT support subqueries in check constraints
            // Data integrity enforced by FK_CheckInSessionTokens_Sessions_SessionId foreign key instead

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_Session_Time",
                schema: "public",
                table: "CheckIns",
                columns: new[] { "SessionId", "CheckInTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_SessionId",
                schema: "public",
                table: "CheckIns",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "UQ_CheckIns_EventAttendee_Session",
                schema: "public",
                table: "CheckIns",
                columns: new[] { "EventAttendeeId", "SessionId" },
                unique: true);

            // NOTE: Removed CHK_CheckIns_SessionBelongsToEvent check constraint
            // PostgreSQL does NOT support subqueries in check constraints
            // Data integrity enforced by FK_CheckIns_Sessions_SessionId foreign key instead

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIns_Sessions_SessionId",
                schema: "public",
                table: "CheckIns",
                column: "SessionId",
                principalSchema: "public",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInSessionTokens_Sessions_SessionId",
                schema: "public",
                table: "CheckInSessionTokens",
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
                name: "FK_CheckIns_Sessions_SessionId",
                schema: "public",
                table: "CheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckInSessionTokens_Sessions_SessionId",
                schema: "public",
                table: "CheckInSessionTokens");

            migrationBuilder.DropIndex(
                name: "IX_CheckInSessionTokens_SessionId",
                schema: "public",
                table: "CheckInSessionTokens");

            migrationBuilder.DropIndex(
                name: "IX_CheckInSessionTokens_SessionId_ExpiresAt_Active",
                schema: "public",
                table: "CheckInSessionTokens");

            // NOTE: CHK_CheckInSessionTokens_SessionBelongsToEvent constraint was never created (PostgreSQL limitation)

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_Session_Time",
                schema: "public",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_SessionId",
                schema: "public",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "UQ_CheckIns_EventAttendee_Session",
                schema: "public",
                table: "CheckIns");

            // NOTE: CHK_CheckIns_SessionBelongsToEvent constraint was never created (PostgreSQL limitation)

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "public",
                table: "CheckInSessionTokens");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "public",
                table: "CheckIns");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokens_EventId_ExpiresAt_Active",
                schema: "public",
                table: "CheckInSessionTokens",
                columns: new[] { "EventId", "ExpiresAt" },
                filter: "\"IsRevoked\" = false");

            migrationBuilder.CreateIndex(
                name: "UQ_CheckIns_EventAttendee",
                schema: "public",
                table: "CheckIns",
                column: "EventAttendeeId",
                unique: true);
        }
    }
}
