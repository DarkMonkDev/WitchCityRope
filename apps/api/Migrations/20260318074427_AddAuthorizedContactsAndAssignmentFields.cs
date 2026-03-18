using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizedContactsAndAssignmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_EventAttendances_User_Event_Type_Session_Active",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_EventAttendances_Status",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_AttendanceHistory_ActionType",
                schema: "public",
                table: "AttendanceHistory");

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantityPerPurchase",
                schema: "public",
                table: "TicketTypes",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchasedForUserId",
                schema: "public",
                table: "TicketPurchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                schema: "public",
                table: "EventAttendances",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                schema: "public",
                table: "EventAttendances",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedByUserId",
                schema: "public",
                table: "EventAttendances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeclinedAt",
                schema: "public",
                table: "EventAttendances",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeclinedReason",
                schema: "public",
                table: "EventAttendances",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                schema: "public",
                table: "EventAttendances",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthorizedContacts",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizedContacts", x => x.Id);
                    table.CheckConstraint("CHK_AuthorizedContacts_NoSelfAuthorization", "\"PrincipalId\" != \"DelegateId\"");
                    table.ForeignKey(
                        name: "FK_AuthorizedContacts_Users_DelegateId",
                        column: x => x.DelegateId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorizedContacts_Users_PrincipalId",
                        column: x => x.PrincipalId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CHK_TicketTypes_MaxQuantityPerPurchase",
                schema: "public",
                table: "TicketTypes",
                sql: "\"MaxQuantityPerPurchase\" >= 1 AND \"MaxQuantityPerPurchase\" <= 10");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_PurchasedForUserId",
                schema: "public",
                table: "TicketPurchases",
                column: "PurchasedForUserId",
                filter: "\"PurchasedForUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_AssignedByUserId",
                schema: "public",
                table: "EventAttendances",
                column: "AssignedByUserId",
                filter: "\"AssignedByUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_Status_ReminderSentAt",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "Status", "ReminderSentAt" },
                filter: "\"Status\" = 6 AND \"ReminderSentAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_EventAttendances_User_Event_Type_Session_Active",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "UserId", "EventId", "AttendanceType", "SessionId" },
                unique: true,
                filter: "\"Status\" IN (1, 6)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_EventAttendances_Status",
                schema: "public",
                table: "EventAttendances",
                sql: "\"Status\" IN (1, 2, 3, 4, 5, 6)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_AttendanceHistory_ActionType",
                schema: "public",
                table: "AttendanceHistory",
                sql: "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded', 'StatusChanged', 'PaymentUpdated', 'TicketAssigned', 'TicketReassigned', 'TicketAccepted', 'TicketDeclined', 'ProxyRSVPCreated', 'ProxyRSVPAccepted', 'ProxyRSVPDeclined', 'ReminderSent')");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizedContacts_DelegateId",
                schema: "public",
                table: "AuthorizedContacts",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizedContacts_DelegateId_RevokedAt",
                schema: "public",
                table: "AuthorizedContacts",
                columns: new[] { "DelegateId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizedContacts_PrincipalId",
                schema: "public",
                table: "AuthorizedContacts",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "UQ_AuthorizedContacts_Principal_Delegate_Active",
                schema: "public",
                table: "AuthorizedContacts",
                columns: new[] { "PrincipalId", "DelegateId" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_EventAttendances_Users_AssignedByUserId",
                schema: "public",
                table: "EventAttendances",
                column: "AssignedByUserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketPurchases_Users_PurchasedForUserId",
                schema: "public",
                table: "TicketPurchases",
                column: "PurchasedForUserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventAttendances_Users_AssignedByUserId",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketPurchases_Users_PurchasedForUserId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropTable(
                name: "AuthorizedContacts",
                schema: "public");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_TicketTypes_MaxQuantityPerPurchase",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.DropIndex(
                name: "IX_TicketPurchases_PurchasedForUserId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropIndex(
                name: "IX_EventAttendances_AssignedByUserId",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropIndex(
                name: "IX_EventAttendances_Status_ReminderSentAt",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropIndex(
                name: "UQ_EventAttendances_User_Event_Type_Session_Active",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_EventAttendances_Status",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_AttendanceHistory_ActionType",
                schema: "public",
                table: "AttendanceHistory");

            migrationBuilder.DropColumn(
                name: "MaxQuantityPerPurchase",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.DropColumn(
                name: "PurchasedForUserId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropColumn(
                name: "DeclinedAt",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropColumn(
                name: "DeclinedReason",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.CreateIndex(
                name: "UQ_EventAttendances_User_Event_Type_Session_Active",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "UserId", "EventId", "AttendanceType", "SessionId" },
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_EventAttendances_Status",
                schema: "public",
                table: "EventAttendances",
                sql: "\"Status\" IN (1, 2, 3, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_AttendanceHistory_ActionType",
                schema: "public",
                table: "AttendanceHistory",
                sql: "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded', 'StatusChanged', 'PaymentUpdated')");
        }
    }
}
