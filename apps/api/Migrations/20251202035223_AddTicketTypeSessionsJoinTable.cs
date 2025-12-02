using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTypeSessionsJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the new join table FIRST
            migrationBuilder.CreateTable(
                name: "TicketTypeSessions",
                schema: "public",
                columns: table => new
                {
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTypeSessions", x => new { x.TicketTypeId, x.SessionId });
                    table.ForeignKey(
                        name: "FK_TicketTypeSessions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "public",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketTypeSessions_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalSchema: "public",
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypeSessions_SessionId",
                schema: "public",
                table: "TicketTypeSessions",
                column: "SessionId");

            // 2. Migrate existing data: Copy SessionId relationships to the join table
            // For ticket types that had a SessionId, create an entry in the join table
            migrationBuilder.Sql(@"
                INSERT INTO public.""TicketTypeSessions"" (""TicketTypeId"", ""SessionId"")
                SELECT ""Id"", ""SessionId""
                FROM public.""TicketTypes""
                WHERE ""SessionId"" IS NOT NULL;
            ");

            // 3. Now safely drop the old foreign key, index, and column
            migrationBuilder.DropForeignKey(
                name: "FK_TicketTypes_Sessions_SessionId",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.DropIndex(
                name: "IX_TicketTypes_SessionId",
                schema: "public",
                table: "TicketTypes");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "public",
                table: "TicketTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Add back the SessionId column
            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "public",
                table: "TicketTypes",
                type: "uuid",
                nullable: true);

            // 2. Migrate data back: Copy the first session from join table to SessionId
            // Note: Multi-session tickets will lose their additional sessions in the rollback
            migrationBuilder.Sql(@"
                UPDATE public.""TicketTypes"" tt
                SET ""SessionId"" = (
                    SELECT tts.""SessionId""
                    FROM public.""TicketTypeSessions"" tts
                    WHERE tts.""TicketTypeId"" = tt.""Id""
                    ORDER BY tts.""SessionId""
                    LIMIT 1
                );
            ");

            // 3. Recreate the index and foreign key
            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_SessionId",
                schema: "public",
                table: "TicketTypes",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketTypes_Sessions_SessionId",
                schema: "public",
                table: "TicketTypes",
                column: "SessionId",
                principalSchema: "public",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // 4. Drop the join table
            migrationBuilder.DropTable(
                name: "TicketTypeSessions",
                schema: "public");
        }
    }
}
