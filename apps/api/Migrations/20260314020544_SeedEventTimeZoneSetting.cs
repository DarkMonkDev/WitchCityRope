using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <summary>
    /// One-time data fix: Inserts the missing EventTimeZone setting into the Settings table.
    /// Production and staging databases are missing this setting because the seeder skips all
    /// settings if any already exist. The Settings page throws "Settings not found: EventTimeZone"
    /// when trying to save without this row present. No schema changes are made.
    /// </summary>
    public partial class SeedEventTimeZoneSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "public"."Settings" ("Id", "Key", "Value", "Description", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), 'EventTimeZone', 'America/New_York', 'IANA timezone ID for event scheduling (Eastern Time)', NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM "public"."Settings" WHERE "Key" = 'EventTimeZone'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "public"."Settings" WHERE "Key" = 'EventTimeZone';
                """);
        }
    }
}
