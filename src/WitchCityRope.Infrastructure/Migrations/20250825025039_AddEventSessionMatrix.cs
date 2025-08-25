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
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "UserAuthentications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenCreatedAt",
                table: "UserAuthentications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "UserAuthentications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "UserAuthentications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PronouncedName",
                table: "UserAuthentications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pronouns",
                table: "UserAuthentications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionIdentifier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RegisteredCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSessions", x => x.Id);
                    table.UniqueConstraint("AK_EventSessions_EventId_SessionIdentifier", x => new { x.EventId, x.SessionIdentifier });
                    table.CheckConstraint("CK_EventSessions_Capacity", "Capacity > 0");
                    table.CheckConstraint("CK_EventSessions_RegisteredCount", "RegisteredCount >= 0");
                    table.CheckConstraint("CK_EventSessions_RegisteredCount_LTE_Capacity", "RegisteredCount <= Capacity");
                    table.ForeignKey(
                        name: "FK_EventSessions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MinPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantityAvailable = table.Column<int>(type: "integer", nullable: true),
                    SalesEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRsvpMode = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketTypes", x => x.Id);
                    table.CheckConstraint("CK_EventTicketTypes_MaxPrice", "MaxPrice >= 0");
                    table.CheckConstraint("CK_EventTicketTypes_MinPrice", "MinPrice >= 0");
                    table.CheckConstraint("CK_EventTicketTypes_Price_Range", "MinPrice <= MaxPrice");
                    table.CheckConstraint("CK_EventTicketTypes_QuantityAvailable", "QuantityAvailable IS NULL OR QuantityAvailable > 0");
                    table.ForeignKey(
                        name: "FK_EventTicketTypes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketTypeSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionIdentifier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventSessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketTypeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketTypeSessions_EventSessions_EventSessionId",
                        column: x => x.EventSessionId,
                        principalTable: "EventSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventTicketTypeSessions_EventTicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "EventTicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventSessions_Date_StartTime",
                table: "EventSessions",
                columns: new[] { "Date", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSessions_EventId",
                table: "EventSessions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSessions_EventId_Date",
                table: "EventSessions",
                columns: new[] { "EventId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSessions_EventId_SessionIdentifier",
                table: "EventSessions",
                columns: new[] { "EventId", "SessionIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypes_EventId",
                table: "EventTicketTypes",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypes_EventId_IsActive",
                table: "EventTicketTypes",
                columns: new[] { "EventId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypes_EventId_Name",
                table: "EventTicketTypes",
                columns: new[] { "EventId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypes_SalesEndDate",
                table: "EventTicketTypes",
                column: "SalesEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypeSessions_EventSessionId",
                table: "EventTicketTypeSessions",
                column: "EventSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypeSessions_SessionIdentifier",
                table: "EventTicketTypeSessions",
                column: "SessionIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypeSessions_TicketTypeId",
                table: "EventTicketTypeSessions",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypeSessions_TicketTypeId_SessionIdentifier",
                table: "EventTicketTypeSessions",
                columns: new[] { "TicketTypeId", "SessionIdentifier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventTicketTypeSessions");

            migrationBuilder.DropTable(
                name: "EventSessions");

            migrationBuilder.DropTable(
                name: "EventTicketTypes");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "UserAuthentications");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenCreatedAt",
                table: "UserAuthentications");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "UserAuthentications");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "UserAuthentications");

            migrationBuilder.DropColumn(
                name: "PronouncedName",
                table: "UserAuthentications");

            migrationBuilder.DropColumn(
                name: "Pronouns",
                table: "UserAuthentications");
        }
    }
}
