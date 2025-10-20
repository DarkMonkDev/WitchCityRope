using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHasVettingApplicationToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasVettingApplication",
                schema: "public",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Set HasVettingApplication = true for users who already have vetting applications
            migrationBuilder.Sql(@"
                UPDATE public.""Users"" u
                SET ""HasVettingApplication"" = true
                FROM public.""VettingApplications"" va
                WHERE va.""UserId"" = u.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasVettingApplication",
                schema: "public",
                table: "Users");
        }
    }
}
