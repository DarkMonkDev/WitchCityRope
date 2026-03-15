using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeVolunteerPositionSessionIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, cancel any volunteer signups for event-wide positions (SessionId IS NULL)
            // before deleting those positions. Event-wide positions are no longer supported —
            // all volunteer positions must belong to a specific session.
            migrationBuilder.Sql("""
                UPDATE public."VolunteerSignups"
                SET "Status" = 3, "UpdatedAt" = NOW()
                WHERE "VolunteerPositionId" IN (
                    SELECT "Id" FROM public."VolunteerPositions" WHERE "SessionId" IS NULL
                )
                AND "Status" = 1;
            """);

            // Delete event-wide volunteer positions that have no session assigned.
            // These are not valid under the new constraint.
            migrationBuilder.Sql("""
                DELETE FROM public."VolunteerPositions" WHERE "SessionId" IS NULL;
            """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                schema: "public",
                table: "VolunteerPositions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                schema: "public",
                table: "VolunteerPositions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
