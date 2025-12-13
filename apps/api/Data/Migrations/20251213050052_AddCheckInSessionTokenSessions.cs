using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInSessionTokenSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckInSessionTokens_Sessions_SessionId",
                schema: "public",
                table: "CheckInSessionTokens");

            migrationBuilder.DropColumn(
                name: "EventType",
                schema: "public",
                table: "Events");

            migrationBuilder.AddColumn<bool>(
                name: "AllowRsvps",
                schema: "public",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireTicketPurchase",
                schema: "public",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "VettedMembersOnly",
                schema: "public",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                schema: "public",
                table: "CheckInSessionTokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "CheckInSessionTokenSessions",
                schema: "public",
                columns: table => new
                {
                    TokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInSessionTokenSessions", x => new { x.TokenId, x.SessionId });
                    table.ForeignKey(
                        name: "FK_CheckInSessionTokenSessions_CheckInSessionTokens_TokenId",
                        column: x => x.TokenId,
                        principalSchema: "public",
                        principalTable: "CheckInSessionTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckInSessionTokenSessions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "public",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInSessionTokenSessions_SessionId",
                schema: "public",
                table: "CheckInSessionTokenSessions",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInSessionTokens_Sessions_SessionId",
                schema: "public",
                table: "CheckInSessionTokens",
                column: "SessionId",
                principalSchema: "public",
                principalTable: "Sessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckInSessionTokens_Sessions_SessionId",
                schema: "public",
                table: "CheckInSessionTokens");

            migrationBuilder.DropTable(
                name: "CheckInSessionTokenSessions",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "AllowRsvps",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RequireTicketPurchase",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VettedMembersOnly",
                schema: "public",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                schema: "public",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                schema: "public",
                table: "CheckInSessionTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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
    }
}
