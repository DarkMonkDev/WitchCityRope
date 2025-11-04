using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInSessionTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckInSessionTokens",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInSessionTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInSessionTokens_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokens_CreatedByUserId",
                schema: "public",
                table: "CheckInSessionTokens",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokens_EventId",
                schema: "public",
                table: "CheckInSessionTokens",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokens_EventId_ExpiresAt_Active",
                schema: "public",
                table: "CheckInSessionTokens",
                columns: new[] { "EventId", "ExpiresAt" },
                filter: "\"IsRevoked\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokens_Token",
                schema: "public",
                table: "CheckInSessionTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokens_Token_ExpiresAt_IsRevoked",
                schema: "public",
                table: "CheckInSessionTokens",
                columns: new[] { "Token", "ExpiresAt", "IsRevoked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInSessionTokens",
                schema: "public");
        }
    }
}
