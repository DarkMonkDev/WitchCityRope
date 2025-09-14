using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRSVPSupportForSocialEvents : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RSVPs");
        }
    }
}