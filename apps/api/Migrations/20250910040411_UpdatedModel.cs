using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "UserTokens",
                schema: "auth",
                newName: "UserTokens",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "auth",
                newName: "Users",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                schema: "auth",
                newName: "UserRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "UserLogins",
                schema: "auth",
                newName: "UserLogins",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "UserClaims",
                schema: "auth",
                newName: "UserClaims",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "auth",
                newName: "Roles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "RoleClaims",
                schema: "auth",
                newName: "RoleClaims",
                newSchema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.RenameTable(
                name: "UserTokens",
                schema: "public",
                newName: "UserTokens",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "public",
                newName: "Users",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                schema: "public",
                newName: "UserRoles",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "UserLogins",
                schema: "public",
                newName: "UserLogins",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "UserClaims",
                schema: "public",
                newName: "UserClaims",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "public",
                newName: "Roles",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "RoleClaims",
                schema: "public",
                newName: "RoleClaims",
                newSchema: "auth");
        }
    }
}
