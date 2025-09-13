using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventAttendees",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationStatus = table.Column<string>(type: "text", nullable: false),
                    TicketNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WaitlistPosition = table.Column<int>(type: "integer", nullable: true),
                    IsFirstTime = table.Column<bool>(type: "boolean", nullable: false),
                    DietaryRestrictions = table.Column<string>(type: "text", nullable: true),
                    AccessibilityNeeds = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HasCompletedWaiver = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttendees", x => x.Id);
                    table.CheckConstraint("CHK_EventAttendees_RegistrationStatus", "\"RegistrationStatus\" IN ('confirmed', 'waitlist', 'checked-in', 'no-show', 'cancelled')");
                    table.CheckConstraint("CHK_EventAttendees_WaitlistPosition", "\"WaitlistPosition\" > 0 OR \"WaitlistPosition\" IS NULL");
                    table.ForeignKey(
                        name: "FK_EventAttendees_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventAttendees_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventAttendees_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventAttendees_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfflineSyncQueue",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionData = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    LocalTimestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    SyncStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineSyncQueue", x => x.Id);
                    table.CheckConstraint("CHK_OfflineSyncQueue_ActionType", "\"ActionType\" IN ('check-in', 'manual-entry', 'status-update', 'capacity-override')");
                    table.CheckConstraint("CHK_OfflineSyncQueue_RetryCount", "\"RetryCount\" >= 0 AND \"RetryCount\" <= 10");
                    table.CheckConstraint("CHK_OfflineSyncQueue_SyncedAt", "(\"SyncStatus\" = 'completed' AND \"SyncedAt\" IS NOT NULL) OR (\"SyncStatus\" != 'completed' AND \"SyncedAt\" IS NULL)");
                    table.CheckConstraint("CHK_OfflineSyncQueue_SyncStatus", "\"SyncStatus\" IN ('pending', 'syncing', 'completed', 'failed', 'conflict')");
                    table.ForeignKey(
                        name: "FK_OfflineSyncQueue_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfflineSyncQueue_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheckInAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventAttendeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInAuditLog", x => x.Id);
                    table.CheckConstraint("CHK_CheckInAuditLog_ActionType", "\"ActionType\" IN ('check-in', 'manual-entry', 'capacity-override', 'status-change', 'data-update')");
                    table.ForeignKey(
                        name: "FK_CheckInAuditLog_EventAttendees_EventAttendeeId",
                        column: x => x.EventAttendeeId,
                        principalSchema: "public",
                        principalTable: "EventAttendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CheckInAuditLog_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckInAuditLog_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CheckIns",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventAttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsManualEntry = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideCapacity = table.Column<bool>(type: "boolean", nullable: false),
                    ManualEntryData = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckIns", x => x.Id);
                    table.CheckConstraint("CHK_CheckIns_ManualEntryData", "(\"IsManualEntry\" = true AND \"ManualEntryData\" IS NOT NULL) OR (\"IsManualEntry\" = false AND \"ManualEntryData\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_CheckIns_EventAttendees_EventAttendeeId",
                        column: x => x.EventAttendeeId,
                        principalSchema: "public",
                        principalTable: "EventAttendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckIns_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckIns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CheckIns_Users_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_ActionType",
                schema: "public",
                table: "CheckInAuditLog",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_CreatedAt",
                schema: "public",
                table: "CheckInAuditLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_Event_Time",
                schema: "public",
                table: "CheckInAuditLog",
                columns: new[] { "EventId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_EventAttendeeId",
                schema: "public",
                table: "CheckInAuditLog",
                column: "EventAttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_EventId",
                schema: "public",
                table: "CheckInAuditLog",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_NewValues",
                schema: "public",
                table: "CheckInAuditLog",
                column: "NewValues",
                filter: "\"NewValues\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_OldValues",
                schema: "public",
                table: "CheckInAuditLog",
                column: "OldValues",
                filter: "\"OldValues\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_User_Time",
                schema: "public",
                table: "CheckInAuditLog",
                columns: new[] { "CreatedBy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_CapacityOverride",
                schema: "public",
                table: "CheckIns",
                column: "OverrideCapacity",
                filter: "\"OverrideCapacity\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_CheckInTime",
                schema: "public",
                table: "CheckIns",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_CreatedBy",
                schema: "public",
                table: "CheckIns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_Event_Time",
                schema: "public",
                table: "CheckIns",
                columns: new[] { "EventId", "CheckInTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_EventId",
                schema: "public",
                table: "CheckIns",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_ManualEntry",
                schema: "public",
                table: "CheckIns",
                column: "IsManualEntry",
                filter: "\"IsManualEntry\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_ManualEntryData",
                schema: "public",
                table: "CheckIns",
                column: "ManualEntryData",
                filter: "\"ManualEntryData\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_StaffMemberId",
                schema: "public",
                table: "CheckIns",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "UQ_CheckIns_EventAttendee",
                schema: "public",
                table: "CheckIns",
                column: "EventAttendeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_CreatedBy",
                schema: "public",
                table: "EventAttendees",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_Event_Status",
                schema: "public",
                table: "EventAttendees",
                columns: new[] { "EventId", "RegistrationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_Event_Waitlist",
                schema: "public",
                table: "EventAttendees",
                columns: new[] { "EventId", "WaitlistPosition" },
                filter: "\"RegistrationStatus\" = 'waitlist'");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_EventId",
                schema: "public",
                table: "EventAttendees",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_FirstTime",
                schema: "public",
                table: "EventAttendees",
                column: "IsFirstTime",
                filter: "\"IsFirstTime\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_Metadata",
                schema: "public",
                table: "EventAttendees",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_RegistrationStatus",
                schema: "public",
                table: "EventAttendees",
                column: "RegistrationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_TicketNumber_Unique",
                schema: "public",
                table: "EventAttendees",
                column: "TicketNumber",
                unique: true,
                filter: "\"TicketNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_UpdatedBy",
                schema: "public",
                table: "EventAttendees",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_UserId",
                schema: "public",
                table: "EventAttendees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_Waiver",
                schema: "public",
                table: "EventAttendees",
                column: "HasCompletedWaiver",
                filter: "\"HasCompletedWaiver\" = false");

            migrationBuilder.CreateIndex(
                name: "UQ_EventAttendees_EventUser",
                schema: "public",
                table: "EventAttendees",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_ActionData",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "ActionData")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_EventId",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_Failed",
                schema: "public",
                table: "OfflineSyncQueue",
                columns: new[] { "RetryCount", "CreatedAt" },
                filter: "\"SyncStatus\" = 'failed' AND \"RetryCount\" < 5");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_LocalTimestamp",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "LocalTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_Pending",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "CreatedAt",
                filter: "\"SyncStatus\" = 'pending'");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_SyncStatus",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_UserId",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CheckIns",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OfflineSyncQueue",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventAttendees",
                schema: "public");
        }
    }
}
