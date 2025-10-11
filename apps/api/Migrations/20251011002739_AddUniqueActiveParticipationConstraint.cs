using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveParticipationConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_EventParticipations_User_Event_Active",
                schema: "public",
                table: "EventParticipations");

            migrationBuilder.CreateIndex(
                name: "UQ_EventParticipations_User_Event_Active",
                schema: "public",
                table: "EventParticipations",
                columns: new[] { "UserId", "EventId" },
                unique: true,
                filter: "\"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_EventParticipations_User_Event_Active",
                schema: "public",
                table: "EventParticipations");

            migrationBuilder.CreateIndex(
                name: "UQ_EventParticipations_User_Event_Active",
                schema: "public",
                table: "EventParticipations",
                columns: new[] { "UserId", "EventId" },
                unique: true);
        }
    }
}
