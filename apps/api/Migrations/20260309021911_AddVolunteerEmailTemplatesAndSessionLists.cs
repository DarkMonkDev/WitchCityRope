using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <summary>
    /// Adds two new volunteer email templates (VolunteerReminder, VolunteerThankYou) to the
    /// GlobalEmailTemplates table, updates the existing Confirmation and Cancellation templates
    /// to include session list variables, and seeds test data for new template variables.
    ///
    /// New templates:
    /// - VolunteerReminder: TimeBased, 2 days before session, targets SessionVolunteers
    /// - VolunteerThankYou: TimeBased, 1 day after session, targets SessionVolunteers
    ///
    /// Updated templates:
    /// - Confirmation: Adds {{ticket_sessions_list}} variable for multi-session support
    /// - Cancellation: Adds {{cancelled_sessions_list}} variable and updates wording
    ///
    /// All inserts use ON CONFLICT DO NOTHING to preserve existing customizations.
    /// Template updates use conditional UPDATEs that only apply if body still matches the
    /// original seeded content (preserving admin edits).
    /// </summary>
    public partial class AddVolunteerEmailTemplatesAndSessionLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================================
            // 1. INSERT NEW VOLUNTEER TEMPLATES
            // ============================================================================
            // Uses subquery to get admin user ID since GUIDs differ across environments.
            // ON CONFLICT prevents duplicate creation if migration runs on a database
            // that already has these templates (e.g., from a fresh seed).
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
                    'VolunteerReminder',
                    'Volunteer Reminder: {{event_title}}',
                    'Volunteer Reminder: {{event_title}} - {{session_name}}',
                    '<p>Hi {{volunteer_name}},</p><p>This is a reminder that you are volunteering for <strong>{{event_title}}</strong>!</p><h3>Your Volunteer Details</h3><p><strong>Role:</strong> {{volunteer_role}}<br><strong>Session:</strong> {{session_name}}<br><strong>Date:</strong> {{event_date}}<br><strong>Time:</strong> {{event_time}}<br><strong>Shift:</strong> {{shift_start}} - {{shift_end}}</p><p><strong>Location:</strong><br>{{venue_name}}<br>{{venue_address}}</p><p>Thank you for volunteering! If you can no longer make it, please update your status as soon as possible so we can find a replacement.</p><p>Questions? Email events@witchcityrope.com</p>',
                    E'Hi {{volunteer_name}},\n\nThis is a reminder that you are volunteering for {{event_title}}!\n\nYour Volunteer Details:\nRole: {{volunteer_role}}\nSession: {{session_name}}\nDate: {{event_date}}\nTime: {{event_time}}\nShift: {{shift_start}} - {{shift_end}}\n\nLocation:\n{{venue_name}}\n{{venue_address}}\n\nThank you for volunteering! If you can no longer make it, please update your status as soon as possible so we can find a replacement.\n\nQuestions? Email events@witchcityrope.com',
                    '["{{volunteer_name}}","{{event_title}}","{{session_name}}","{{event_date}}","{{event_time}}","{{volunteer_role}}","{{shift_start}}","{{shift_end}}","{{venue_name}}","{{venue_address}}"]'::jsonb,
                    2, -- TimeBased trigger
                    true,
                    2, -- 2 days before session
                    NULL,
                    2, -- SessionVolunteers recipient group
                    true,
                    1,
                    NOW(), NOW(),
                    u."Id"
                FROM "AspNetUsers" u
                WHERE u."Email" IN ('admin@witchcityrope.com', 'ropemaster@witchcityrope.com')
                AND NOT EXISTS (
                    SELECT 1 FROM "GlobalEmailTemplates"
                    WHERE "Category" = 1 AND "TemplateType" = 'VolunteerReminder'
                )
                LIMIT 1;
                """);

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
                    'VolunteerThankYou',
                    'Thank you for volunteering: {{event_title}}',
                    'Thank you for volunteering at {{event_title}}!',
                    '<p>Hi {{volunteer_name}},</p><p>Thank you so much for volunteering at <strong>{{event_title}}</strong>!</p><p><strong>Your Role:</strong> {{volunteer_role}}<br><strong>Session:</strong> {{session_name}}<br><strong>Date:</strong> {{event_date}}</p><p>Our events wouldn''t be possible without dedicated volunteers like you. We truly appreciate your time and effort!</p><p>We hope to see you at future events. If you have any feedback about your volunteer experience, please reach out to events@witchcityrope.com</p><p>With gratitude,<br>The Witch City Rope Team</p>',
                    E'Hi {{volunteer_name}},\n\nThank you so much for volunteering at {{event_title}}!\n\nYour Role: {{volunteer_role}}\nSession: {{session_name}}\nDate: {{event_date}}\n\nOur events wouldn''t be possible without dedicated volunteers like you. We truly appreciate your time and effort!\n\nWe hope to see you at future events. If you have any feedback about your volunteer experience, please reach out to events@witchcityrope.com\n\nWith gratitude,\nThe Witch City Rope Team',
                    '["{{volunteer_name}}","{{event_title}}","{{session_name}}","{{event_date}}","{{volunteer_role}}"]'::jsonb,
                    2, -- TimeBased trigger
                    true,
                    -1, -- 1 day after session
                    NULL,
                    2, -- SessionVolunteers recipient group
                    true,
                    1,
                    NOW(), NOW(),
                    u."Id"
                FROM "AspNetUsers" u
                WHERE u."Email" IN ('admin@witchcityrope.com', 'ropemaster@witchcityrope.com')
                AND NOT EXISTS (
                    SELECT 1 FROM "GlobalEmailTemplates"
                    WHERE "Category" = 1 AND "TemplateType" = 'VolunteerThankYou'
                )
                LIMIT 1;
                """);

            // ============================================================================
            // 2. UPDATE CONFIRMATION TEMPLATE
            // ============================================================================
            // Adds {{ticket_sessions_list}} to show purchased sessions grouped by ticket type.
            // Only updates if the body still contains the original seeded content
            // (preserves admin customizations). Also updates Variables JSON to include
            // the new variables so the admin UI shows them as available placeholders.
            migrationBuilder.Sql(
                """
                UPDATE "GlobalEmailTemplates"
                SET
                    "HtmlBody" = '<p>Hi {{attendee_name}},</p><p>Thank you for registering for <strong>{{event_title}}</strong>!</p><p><strong>Event Details:</strong><br>Venue: {{venue_name}}<br>Address: {{venue_address}}</p><h3>Your Sessions</h3>{{ticket_sessions_list}}<p><strong>Total Paid:</strong> {{total_paid}}<br><strong>Confirmation Number:</strong> {{confirmation_number}}</p><p>We look forward to seeing you!</p><p>Questions? Email events@witchcityrope.com</p>',
                    "PlainTextBody" = E'Hi {{attendee_name}},\n\nThank you for registering for {{event_title}}!\n\nEvent Details:\nVenue: {{venue_name}}\nAddress: {{venue_address}}\n\nYour Sessions:\n{{ticket_sessions_list_text}}\n\nTotal Paid: {{total_paid}}\nConfirmation Number: {{confirmation_number}}\n\nWe look forward to seeing you!\n\nQuestions? Email events@witchcityrope.com',
                    "Variables" = '["{{attendee_name}}","{{event_title}}","{{event_date}}","{{event_time}}","{{venue_name}}","{{venue_address}}","{{ticket_type}}","{{total_paid}}","{{confirmation_number}}","{{ticket_sessions_list}}","{{ticket_sessions_list_text}}"]'::jsonb,
                    "UpdatedAt" = NOW(),
                    "Version" = "Version" + 1
                WHERE "Category" = 1
                    AND "TemplateType" = 'Confirmation'
                    AND "HtmlBody" LIKE '%Date: {{event_date}}<br>Time: {{event_time}}%'
                    AND "HtmlBody" NOT LIKE '%ticket_sessions_list%';
                """);

            // ============================================================================
            // 3. UPDATE CANCELLATION TEMPLATE
            // ============================================================================
            // Adds {{cancelled_sessions_list}} to show which sessions were cancelled.
            // Updates wording from "Event Cancelled" to "Cancellation Confirmation"
            // since this template is for user ticket cancellations, not event cancellations.
            // Only updates if body still matches original seeded content.
            migrationBuilder.Sql(
                """
                UPDATE "GlobalEmailTemplates"
                SET
                    "Title" = 'Cancellation Confirmation: {{event_title}}',
                    "Subject" = 'Cancellation Confirmation: {{event_title}}',
                    "HtmlBody" = '<p>Hi {{attendee_name}},</p><p>Your registration for <strong>{{event_title}}</strong> has been cancelled.</p><h3>Cancelled Sessions</h3>{{cancelled_sessions_list}}<p>{{custom_message}}</p><p>If you have any questions, please contact events@witchcityrope.com</p>',
                    "PlainTextBody" = E'Hi {{attendee_name}},\n\nYour registration for {{event_title}} has been cancelled.\n\nCancelled Sessions:\n{{cancelled_sessions_list_text}}\n\n{{custom_message}}\n\nIf you have any questions, please contact events@witchcityrope.com',
                    "Variables" = '["{{attendee_name}}","{{event_title}}","{{event_date}}","{{custom_message}}","{{cancelled_sessions_list}}","{{cancelled_sessions_list_text}}"]'::jsonb,
                    "UpdatedAt" = NOW(),
                    "Version" = "Version" + 1
                WHERE "Category" = 1
                    AND "TemplateType" = 'Cancellation'
                    AND "HtmlBody" LIKE '%We regret to inform you%'
                    AND "HtmlBody" NOT LIKE '%cancelled_sessions_list%';
                """);

            // ============================================================================
            // 4. SEED TEST DATA FOR NEW VARIABLES
            // ============================================================================
            // Adds test data values for the new template variables so admins can preview
            // volunteer and session list templates in the email template editor.
            migrationBuilder.Sql(
                """
                INSERT INTO "Settings" ("Id", "Key", "Value", "Description", "CreatedAt", "UpdatedAt")
                VALUES
                    (gen_random_uuid(), 'EmailTestData:volunteer_name', 'Jane Volunteer', 'Email template test data: volunteer_name', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:volunteer_role', 'Door Monitor', 'Email template test data: volunteer_role', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:shift_start', '6:30 PM', 'Email template test data: shift_start', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:shift_end', '10:00 PM', 'Email template test data: shift_end', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:ticket_sessions_list', '<p><strong>Full Weekend Pass</strong></p><ul><li>Day 1 - Saturday, March 15, 2026 at 2:00 PM ET</li><li>Day 2 - Sunday, March 16, 2026 at 2:00 PM ET</li></ul>', 'Email template test data: ticket_sessions_list (HTML)', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:ticket_sessions_list_text', E'Full Weekend Pass\n  - Day 1 - Saturday, March 15, 2026 at 2:00 PM ET\n  - Day 2 - Sunday, March 16, 2026 at 2:00 PM ET', 'Email template test data: ticket_sessions_list_text (plain text)', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:cancelled_sessions_list', '<p><strong>General Admission</strong></p><ul><li>Day 1 - Saturday, March 15, 2026 at 2:00 PM ET</li></ul>', 'Email template test data: cancelled_sessions_list (HTML)', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:cancelled_sessions_list_text', E'General Admission\n  - Day 1 - Saturday, March 15, 2026 at 2:00 PM ET', 'Email template test data: cancelled_sessions_list_text (plain text)', NOW(), NOW())
                ON CONFLICT ("Key") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove volunteer templates
            migrationBuilder.Sql(
                """
                DELETE FROM "GlobalEmailTemplates"
                WHERE "Category" = 1
                    AND "TemplateType" IN ('VolunteerReminder', 'VolunteerThankYou');
                """);

            // Revert Confirmation template to original (only if it matches our updated content)
            migrationBuilder.Sql(
                """
                UPDATE "GlobalEmailTemplates"
                SET
                    "HtmlBody" = '<p>Hi {{attendee_name}},</p><p>Thank you for registering for <strong>{{event_title}}</strong>!</p><p><strong>Event Details:</strong><br>Date: {{event_date}}<br>Time: {{event_time}}<br>Venue: {{venue_name}}<br>Address: {{venue_address}}</p><p><strong>Ticket Type:</strong> {{ticket_type}}<br><strong>Total Paid:</strong> {{total_paid}}<br><strong>Confirmation Number:</strong> {{confirmation_number}}</p><p>We look forward to seeing you!</p><p>Questions? Email events@witchcityrope.com</p>',
                    "PlainTextBody" = E'Hi {{attendee_name}},\n\nThank you for registering for {{event_title}}!\n\nEvent Details:\nDate: {{event_date}}\nTime: {{event_time}}\nVenue: {{venue_name}}\nAddress: {{venue_address}}\n\nTicket Type: {{ticket_type}}\nTotal Paid: {{total_paid}}\nConfirmation Number: {{confirmation_number}}\n\nWe look forward to seeing you!\n\nQuestions? Email events@witchcityrope.com',
                    "Variables" = '["{{attendee_name}}","{{event_title}}","{{event_date}}","{{event_time}}","{{venue_name}}","{{venue_address}}","{{ticket_type}}","{{total_paid}}","{{confirmation_number}}"]'::jsonb,
                    "UpdatedAt" = NOW(),
                    "Version" = "Version" + 1
                WHERE "Category" = 1
                    AND "TemplateType" = 'Confirmation'
                    AND "HtmlBody" LIKE '%ticket_sessions_list%';
                """);

            // Revert Cancellation template to original
            migrationBuilder.Sql(
                """
                UPDATE "GlobalEmailTemplates"
                SET
                    "Title" = 'Event Cancelled: {{event_title}}',
                    "Subject" = 'Event Cancelled: {{event_title}}',
                    "HtmlBody" = '<p>Hi {{attendee_name}},</p><p>We regret to inform you that <strong>{{event_title}}</strong> scheduled for {{event_date}} has been cancelled.</p><p>{{custom_message}}</p><p>If you have any questions, please contact events@witchcityrope.com</p>',
                    "PlainTextBody" = E'Hi {{attendee_name}},\n\nWe regret to inform you that {{event_title}} scheduled for {{event_date}} has been cancelled.\n\n{{custom_message}}\n\nIf you have any questions, please contact events@witchcityrope.com',
                    "Variables" = '["{{attendee_name}}","{{event_title}}","{{event_date}}","{{custom_message}}"]'::jsonb,
                    "UpdatedAt" = NOW(),
                    "Version" = "Version" + 1
                WHERE "Category" = 1
                    AND "TemplateType" = 'Cancellation'
                    AND "HtmlBody" LIKE '%cancelled_sessions_list%';
                """);

            // Remove test data
            migrationBuilder.Sql(
                """
                DELETE FROM "Settings"
                WHERE "Key" IN (
                    'EmailTestData:volunteer_name',
                    'EmailTestData:volunteer_role',
                    'EmailTestData:shift_start',
                    'EmailTestData:shift_end',
                    'EmailTestData:ticket_sessions_list',
                    'EmailTestData:ticket_sessions_list_text',
                    'EmailTestData:cancelled_sessions_list',
                    'EmailTestData:cancelled_sessions_list_text'
                );
                """);
        }
    }
}
