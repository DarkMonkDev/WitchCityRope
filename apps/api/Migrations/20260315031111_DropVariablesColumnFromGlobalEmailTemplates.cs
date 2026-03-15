using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropVariablesColumnFromGlobalEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GlobalEmailTemplates_Variables_Gin",
                schema: "public",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropColumn(
                name: "Variables",
                schema: "public",
                table: "GlobalEmailTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Variables",
                schema: "public",
                table: "GlobalEmailTemplates",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalEmailTemplates_Variables_Gin",
                schema: "public",
                table: "GlobalEmailTemplates",
                column: "Variables")
                .Annotation("Npgsql:IndexMethod", "gin");
        }
    }
}
