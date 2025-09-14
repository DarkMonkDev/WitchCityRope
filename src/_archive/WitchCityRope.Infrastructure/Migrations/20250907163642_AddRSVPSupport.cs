using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRSVPSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RSVPs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DietaryRestrictions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConfirmationCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RSVPs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RSVPs_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RSVPs_Registrations_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RSVPs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "IX_RSVPs_TicketId",
                table: "RSVPs",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RSVPs_UserId_EventId_Active",
                table: "RSVPs",
                columns: new[] { "UserId", "EventId" },
                unique: true,
                filter: "\"Status\" != 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RSVPs");
        }
    }
}
