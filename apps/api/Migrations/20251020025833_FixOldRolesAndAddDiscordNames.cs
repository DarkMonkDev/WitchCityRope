using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixOldRolesAndAddDiscordNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix old roles from before refactoring
            // "Member" and "Attendee" roles were removed - set to empty string (no special permissions)
            migrationBuilder.Sql(@"
                UPDATE public.""Users""
                SET ""Role"" = ''
                WHERE ""Role"" IN ('Member', 'Attendee');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
