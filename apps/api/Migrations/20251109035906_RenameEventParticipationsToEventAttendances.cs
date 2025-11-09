using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameEventParticipationsToEventAttendances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticipationHistory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventParticipations",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "Sold",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.DropColumn(
                name: "CurrentAttendees",
                schema: "public",
                table: "Sessions");

            migrationBuilder.CreateTable(
                name: "EventAttendances",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TicketPurchaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttendances", x => x.Id);
                    table.CheckConstraint("CHK_EventAttendances_AttendanceType", "\"AttendanceType\" IN (1, 2)");
                    table.CheckConstraint("CHK_EventAttendances_CancelledAt_Logic", "(\"Status\" IN (2, 3) AND \"CancelledAt\" IS NOT NULL) OR (\"Status\" NOT IN (2, 3) AND \"CancelledAt\" IS NULL)");
                    table.CheckConstraint("CHK_EventAttendances_Status", "\"Status\" IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_EventAttendances_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventAttendances_TicketPurchases_TicketPurchaseId",
                        column: x => x.TicketPurchaseId,
                        principalSchema: "public",
                        principalTable: "TicketPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventAttendances_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventAttendances_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventAttendances_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceHistory",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceHistory", x => x.Id);
                    table.CheckConstraint("CHK_AttendanceHistory_ActionType", "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded', 'StatusChanged', 'PaymentUpdated')");
                    table.ForeignKey(
                        name: "FK_AttendanceHistory_EventAttendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalSchema: "public",
                        principalTable: "EventAttendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceHistory_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceHistory_ActionType",
                schema: "public",
                table: "AttendanceHistory",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceHistory_AttendanceId_CreatedAt",
                schema: "public",
                table: "AttendanceHistory",
                columns: new[] { "AttendanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceHistory_ChangedBy",
                schema: "public",
                table: "AttendanceHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceHistory_NewValues_Gin",
                schema: "public",
                table: "AttendanceHistory",
                column: "NewValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceHistory_OldValues_Gin",
                schema: "public",
                table: "AttendanceHistory",
                column: "OldValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_CreatedAt",
                schema: "public",
                table: "EventAttendances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_CreatedBy",
                schema: "public",
                table: "EventAttendances",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_EventId_Status",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_Metadata_Gin",
                schema: "public",
                table: "EventAttendances",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_TicketPurchaseId",
                schema: "public",
                table: "EventAttendances",
                column: "TicketPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_UpdatedBy",
                schema: "public",
                table: "EventAttendances",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendances_UserId_Status",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_EventAttendances_User_Event_Type_Active",
                schema: "public",
                table: "EventAttendances",
                columns: new[] { "UserId", "EventId", "AttendanceType" },
                unique: true,
                filter: "\"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceHistory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventAttendances",
                schema: "public");

            migrationBuilder.AddColumn<int>(
                name: "Sold",
                schema: "public",
                table: "TicketTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentAttendees",
                schema: "public",
                table: "Sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EventParticipations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ParticipationType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipations", x => x.Id);
                    table.CheckConstraint("CHK_EventParticipations_CancelledAt_Logic", "(\"Status\" IN (2, 3) AND \"CancelledAt\" IS NOT NULL) OR (\"Status\" NOT IN (2, 3) AND \"CancelledAt\" IS NULL)");
                    table.CheckConstraint("CHK_EventParticipations_ParticipationType", "\"ParticipationType\" IN (1, 2)");
                    table.CheckConstraint("CHK_EventParticipations_Status", "\"Status\" IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_EventParticipations_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventParticipations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventParticipations_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventParticipations_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipationHistory",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ParticipationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipationHistory", x => x.Id);
                    table.CheckConstraint("CHK_ParticipationHistory_ActionType", "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded', 'StatusChanged', 'PaymentUpdated')");
                    table.ForeignKey(
                        name: "FK_ParticipationHistory_EventParticipations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalSchema: "public",
                        principalTable: "EventParticipations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipationHistory_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_CreatedAt",
                schema: "public",
                table: "EventParticipations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_CreatedBy",
                schema: "public",
                table: "EventParticipations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_EventId_Status",
                schema: "public",
                table: "EventParticipations",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_Metadata_Gin",
                schema: "public",
                table: "EventParticipations",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_UpdatedBy",
                schema: "public",
                table: "EventParticipations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_UserId_Status",
                schema: "public",
                table: "EventParticipations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_EventParticipations_User_Event_Type_Active",
                schema: "public",
                table: "EventParticipations",
                columns: new[] { "UserId", "EventId", "ParticipationType" },
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_ActionType",
                schema: "public",
                table: "ParticipationHistory",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_ChangedBy",
                schema: "public",
                table: "ParticipationHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_NewValues_Gin",
                schema: "public",
                table: "ParticipationHistory",
                column: "NewValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_OldValues_Gin",
                schema: "public",
                table: "ParticipationHistory",
                column: "OldValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_ParticipationId_CreatedAt",
                schema: "public",
                table: "ParticipationHistory",
                columns: new[] { "ParticipationId", "CreatedAt" });
        }
    }
}
