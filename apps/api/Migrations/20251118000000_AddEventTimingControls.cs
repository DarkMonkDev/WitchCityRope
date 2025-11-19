using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTimingControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add 6 new nullable decimal columns to Events table for granular timing controls
            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationOpenHours",
                schema: "public",
                table: "Events",
                type: "numeric(7,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationCloseHours",
                schema: "public",
                table: "Events",
                type: "numeric(7,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationOpenHours",
                schema: "public",
                table: "Events",
                type: "numeric(7,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationCloseHours",
                schema: "public",
                table: "Events",
                type: "numeric(7,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolunteerRegistrationCloseHours",
                schema: "public",
                table: "Events",
                type: "numeric(7,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolunteerCancellationCloseHours",
                schema: "public",
                table: "Events",
                type: "numeric(7,1)",
                nullable: true);

            // Add check constraints to enforce -24 hour minimum (no more than 24 hours after event start)
            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_RegistrationOpenHours",
                schema: "public",
                table: "Events",
                sql: "\"RegistrationOpenHours\" IS NULL OR \"RegistrationOpenHours\" >= -24");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_RegistrationCloseHours",
                schema: "public",
                table: "Events",
                sql: "\"RegistrationCloseHours\" IS NULL OR \"RegistrationCloseHours\" >= -24");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_CancellationOpenHours",
                schema: "public",
                table: "Events",
                sql: "\"CancellationOpenHours\" IS NULL OR \"CancellationOpenHours\" >= -24");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_CancellationCloseHours",
                schema: "public",
                table: "Events",
                sql: "\"CancellationCloseHours\" IS NULL OR \"CancellationCloseHours\" >= -24");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_VolunteerRegistrationCloseHours",
                schema: "public",
                table: "Events",
                sql: "\"VolunteerRegistrationCloseHours\" IS NULL OR \"VolunteerRegistrationCloseHours\" >= -24");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_VolunteerCancellationCloseHours",
                schema: "public",
                table: "Events",
                sql: "\"VolunteerCancellationCloseHours\" IS NULL OR \"VolunteerCancellationCloseHours\" >= -24");

            // Add column comments to explain business logic
            migrationBuilder.Sql(@"
                COMMENT ON COLUMN public.""Events"".""RegistrationOpenHours"" IS
                  'Hours before/after event start when RSVP/Ticket registration opens. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (any time before event).';

                COMMENT ON COLUMN public.""Events"".""RegistrationCloseHours"" IS
                  'Hours before/after event start when RSVP/Ticket registration closes. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (until event starts).';

                COMMENT ON COLUMN public.""Events"".""CancellationOpenHours"" IS
                  'Hours before/after event start when RSVP/Ticket cancellation opens. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (any time before event).';

                COMMENT ON COLUMN public.""Events"".""CancellationCloseHours"" IS
                  'Hours before/after event start when RSVP/Ticket cancellation closes. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (until event starts).';

                COMMENT ON COLUMN public.""Events"".""VolunteerRegistrationCloseHours"" IS
                  'Hours before/after event start when volunteer signup closes. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (until event starts).';

                COMMENT ON COLUMN public.""Events"".""VolunteerCancellationCloseHours"" IS
                  'Hours before/after event start when volunteer assignment cancellation closes. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (until event starts).';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove check constraints
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_RegistrationOpenHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_RegistrationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_CancellationOpenHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_CancellationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_VolunteerRegistrationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_VolunteerCancellationCloseHours",
                schema: "public",
                table: "Events");

            // Remove columns
            migrationBuilder.DropColumn(
                name: "RegistrationOpenHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CancellationOpenHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CancellationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VolunteerRegistrationCloseHours",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VolunteerCancellationCloseHours",
                schema: "public",
                table: "Events");
        }
    }
}
