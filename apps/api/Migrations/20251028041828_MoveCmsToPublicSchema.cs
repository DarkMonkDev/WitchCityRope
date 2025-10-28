using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class MoveCmsToPublicSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Move ContentRevisions from cms to public schema
            migrationBuilder.RenameTable(
                name: "ContentRevisions",
                schema: "cms",
                newName: "ContentRevisions",
                newSchema: "public");

            // Move ContentPages from cms to public schema
            migrationBuilder.RenameTable(
                name: "ContentPages",
                schema: "cms",
                newName: "ContentPages",
                newSchema: "public");

            // Drop the now-empty cms schema
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS cms;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cms");

            migrationBuilder.RenameTable(
                name: "ContentRevisions",
                schema: "public",
                newName: "ContentRevisions",
                newSchema: "cms");

            migrationBuilder.RenameTable(
                name: "ContentPages",
                schema: "public",
                newName: "ContentPages",
                newSchema: "cms");
        }
    }
}
