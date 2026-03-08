using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleToGlobalEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Title column with temporary empty default so NOT NULL constraint can be applied
            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "public",
                table: "GlobalEmailTemplates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Populate Title for existing rows using their Subject line (truncated to 100 chars)
            migrationBuilder.Sql(
                """
                UPDATE "public"."GlobalEmailTemplates"
                SET "Title" = LEFT("Subject", 100)
                WHERE "Title" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                schema: "public",
                table: "GlobalEmailTemplates");
        }
    }
}
