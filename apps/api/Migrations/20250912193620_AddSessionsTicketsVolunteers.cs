using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionsTicketsVolunteers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sessions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    CurrentAttendees = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketTypes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Available = table.Column<int>(type: "integer", nullable: false),
                    Sold = table.Column<int>(type: "integer", nullable: false),
                    IsRsvpMode = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketTypes_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketTypes_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "public",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerPositions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SlotsNeeded = table.Column<int>(type: "integer", nullable: false),
                    SlotsFilled = table.Column<int>(type: "integer", nullable: false),
                    RequiresExperience = table.Column<bool>(type: "boolean", nullable: false),
                    Requirements = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerPositions_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerPositions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "public",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TicketPurchases",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketPurchases_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalSchema: "public",
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketPurchases_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EventId",
                schema: "public",
                table: "Sessions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EventId_SessionCode",
                schema: "public",
                table: "Sessions",
                columns: new[] { "EventId", "SessionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_PaymentStatus",
                schema: "public",
                table: "TicketPurchases",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_TicketTypeId",
                schema: "public",
                table: "TicketPurchases",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_UserId",
                schema: "public",
                table: "TicketPurchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_EventId",
                schema: "public",
                table: "TicketTypes",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_SessionId",
                schema: "public",
                table: "TicketTypes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPositions_EventId",
                schema: "public",
                table: "VolunteerPositions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPositions_SessionId",
                schema: "public",
                table: "VolunteerPositions",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketPurchases",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VolunteerPositions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TicketTypes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Sessions",
                schema: "public");
        }
    }
}
