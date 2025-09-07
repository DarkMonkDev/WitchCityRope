using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSessionMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.AddColumn<Guid>(
                name: "EventTicketTypeId",
                table: "Registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventSessions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionIdentifier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSessions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketTypes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TicketType = table.Column<int>(type: "integer", nullable: false),
                    MinPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MaxPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    QuantityAvailable = table.Column<int>(type: "integer", nullable: true),
                    SalesEndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketTypes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RSVPs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedInBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConfirmationCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RSVPs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RSVPs_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RSVPs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketTypeSessions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketTypeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketTypeSessions_EventSessions_EventSessionId",
                        column: x => x.EventSessionId,
                        principalSchema: "public",
                        principalTable: "EventSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventTicketTypeSessions_EventTicketTypes_EventTicketTypeId",
                        column: x => x.EventTicketTypeId,
                        principalSchema: "public",
                        principalTable: "EventTicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventTicketTypeId",
                table: "Registrations",
                column: "EventTicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSessions_EventId_IsActive",
                schema: "public",
                table: "EventSessions",
                columns: new[] { "EventId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_EventSessions_EventId_SessionIdentifier",
                schema: "public",
                table: "EventSessions",
                columns: new[] { "EventId", "SessionIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypes_EventId_IsActive",
                schema: "public",
                table: "EventTicketTypes",
                columns: new[] { "EventId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypeSessions_EventSessionId",
                schema: "public",
                table: "EventTicketTypeSessions",
                column: "EventSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypeSessions_EventTicketTypeId",
                schema: "public",
                table: "EventTicketTypeSessions",
                column: "EventTicketTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_EventTicketTypeSessions_TicketType_Session",
                schema: "public",
                table: "EventTicketTypeSessions",
                columns: new[] { "EventTicketTypeId", "EventSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RSVPs_CheckedInAt",
                table: "RSVPs",
                column: "CheckedInAt");

            migrationBuilder.CreateIndex(
                name: "IX_RSVPs_ConfirmationCode",
                table: "RSVPs",
                column: "ConfirmationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RSVPs_CreatedAt",
                table: "RSVPs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RSVPs_EventId",
                table: "RSVPs",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_RSVPs_Status",
                table: "RSVPs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RSVPs_UserId",
                table: "RSVPs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RSVPs_UserId_EventId",
                table: "RSVPs",
                columns: new[] { "UserId", "EventId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_EventTicketTypes_EventTicketTypeId",
                table: "Registrations",
                column: "EventTicketTypeId",
                principalSchema: "public",
                principalTable: "EventTicketTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_EventTicketTypes_EventTicketTypeId",
                table: "Registrations");

            migrationBuilder.DropTable(
                name: "EventTicketTypeSessions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RSVPs");

            migrationBuilder.DropTable(
                name: "EventSessions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventTicketTypes",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventTicketTypeId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "EventTicketTypeId",
                table: "Registrations");
        }
    }
}
