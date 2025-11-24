using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateSceneNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_SceneName",
                schema: "public",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SceneName",
                schema: "public",
                table: "Users",
                column: "SceneName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_SceneName",
                schema: "public",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SceneName",
                schema: "public",
                table: "Users",
                column: "SceneName",
                unique: true);
        }
    }
}
