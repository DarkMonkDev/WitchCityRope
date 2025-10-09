using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                schema: "public",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscordName",
                schema: "public",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FetLifeName",
                schema: "public",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "public",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "public",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DiscordName",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FetLifeName",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "public",
                table: "Users");
        }
    }
}
