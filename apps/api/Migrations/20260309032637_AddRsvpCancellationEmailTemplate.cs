using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <summary>
    /// Adds the RSVPCancellation email template for RSVP cancellation confirmations.
    /// This is separate from the ticket Cancellation template — RSVPs are for social/free events
    /// and don't have ticket types or session lists to display.
    /// </summary>
    public partial class AddRsvpCancellationEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert RSVPCancellation template for existing databases.
            // Uses the same pattern as the volunteer templates migration:
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
                    'RSVPCancellation',
                    'RSVP Cancelled: {{event_title}}',
                    'RSVP Cancelled: {{event_title}}',
                    '<p>Hi {{attendee_name}},</p><p>Your RSVP for <strong>{{event_title}}</strong> on {{event_date}} has been cancelled.</p><p>{{custom_message}}</p><p>If you''d like to attend, you can RSVP again from the event page.</p><p>Questions? Email events@witchcityrope.com</p>',
                    E'Hi {{attendee_name}},\n\nYour RSVP for {{event_title}} on {{event_date}} has been cancelled.\n\n{{custom_message}}\n\nIf you''d like to attend, you can RSVP again from the event page.\n\nQuestions? Email events@witchcityrope.com',
                    '["{{attendee_name}}","{{event_title}}","{{event_date}}","{{event_time}}","{{venue_name}}","{{venue_address}}","{{custom_message}}"]'::jsonb,
                    1, -- FixedEvent trigger (sent immediately on RSVP cancellation)
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
                    WHERE "Category" = 1 AND "TemplateType" = 'RSVPCancellation'
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
                WHERE "Category" = 1 AND "TemplateType" = 'RSVPCancellation';
                """);
        }
    }
}
