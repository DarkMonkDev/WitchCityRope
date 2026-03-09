using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <summary>
    /// Adds the RSVPConfirmation email template for RSVP confirmation emails.
    /// Sent when a user manually RSVPs to a social/free event.
    /// NOT sent for auto-RSVPs created during ticket purchase (those get ticket confirmation).
    /// </summary>
    public partial class AddRsvpConfirmationEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert RSVPConfirmation template for existing databases.
            // Uses the same pattern as the RSVP cancellation templates migration:
            // - Finds admin user by email (handles both dev/staging and production admin emails)
            // - NOT EXISTS check prevents duplicates if template already exists
            // - LIMIT 1 prevents duplicate inserts if both admin emails exist in the database
            migrationBuilder.Sql(
                """
                INSERT INTO "GlobalEmailTemplates" (
                    "Id", "Category", "TemplateType", "Title", "Subject",
                    "HtmlBody", "PlainTextBody", "Variables",
                    "TriggerType", "TriggerEnabled", "TimingOffsetDays", "TimingOffsetHours",
                    "RecipientGroup", "IsActive", "Version",
                    "CreatedAt", "UpdatedAt", "UpdatedBy"
                )
                SELECT
                    gen_random_uuid(),
                    1, -- Events category
                    'RSVPConfirmation',
                    'RSVP Confirmation',
                    'You''re RSVPed! {{event_title}}',
                    '<p>Hi {{attendee_name}},</p><p>You''re all set! Your RSVP for <strong>{{event_title}}</strong> has been confirmed.</p><p><strong>Event Details:</strong><br>Date: {{event_date}}<br>Time: {{event_time}}<br>Venue: {{venue_name}}<br>Address: {{venue_address}}</p><p>We look forward to seeing you there!</p><p>Questions? Email events@witchcityrope.com</p>',
                    E'Hi {{attendee_name}},\n\nYou''re all set! Your RSVP for {{event_title}} has been confirmed.\n\nEvent Details:\nDate: {{event_date}}\nTime: {{event_time}}\nVenue: {{venue_name}}\nAddress: {{venue_address}}\n\nWe look forward to seeing you there!\n\nQuestions? Email events@witchcityrope.com',
                    '["{{attendee_name}}","{{event_title}}","{{event_date}}","{{event_time}}","{{venue_name}}","{{venue_address}}"]'::jsonb,
                    1, -- FixedEvent trigger (sent immediately on RSVP creation)
                    true,
                    NULL, -- No timing offset (not time-based)
                    NULL,
                    1, -- RSVPTicketHolders recipient group
                    true,
                    1,
                    NOW(), NOW(),
                    u."Id"
                FROM "public"."Users" u
                WHERE u."Email" IN ('admin@witchcityrope.com', 'ropemaster@witchcityrope.com')
                AND NOT EXISTS (
                    SELECT 1 FROM "GlobalEmailTemplates"
                    WHERE "Category" = 1 AND "TemplateType" = 'RSVPConfirmation'
                )
                LIMIT 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "GlobalEmailTemplates"
                WHERE "Category" = 1 AND "TemplateType" = 'RSVPConfirmation';
                """);
        }
    }
}
