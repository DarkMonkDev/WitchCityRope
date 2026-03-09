using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <summary>
    /// Seeds default test data values for email template testing into the Settings table.
    /// Uses INSERT ... ON CONFLICT DO NOTHING to preserve any existing user customizations.
    /// These values are used when admins send test emails from the Email Templates admin page.
    /// </summary>
    public partial class SeedEmailTestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert email test data settings, skipping any that already exist.
            // Each row is a default variable value used when sending test emails.
            migrationBuilder.Sql(
                """
                INSERT INTO "public"."Settings" ("Id", "Key", "Value", "Description", "CreatedAt", "UpdatedAt")
                VALUES
                    -- Global Variables
                    (gen_random_uuid(), 'EmailTestData:user_name', 'Jane Doe', 'Email template test data: user_name', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:system_url', 'https://witchcityrope.com', 'Email template test data: system_url', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:custom_message', 'This is a test custom message for preview purposes.', 'Email template test data: custom_message', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:custom_content', 'This is test custom content for preview purposes.', 'Email template test data: custom_content', NOW(), NOW()),

                    -- Vetting Variables
                    (gen_random_uuid(), 'EmailTestData:scene_name', 'Dark Phoenix', 'Email template test data: scene_name', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:application_number', 'APP-2026-0042', 'Email template test data: application_number', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:submission_date', 'March 1, 2026', 'Email template test data: submission_date', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:application_date', 'March 1, 2026', 'Email template test data: application_date', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:status_change_date', 'March 5, 2026', 'Email template test data: status_change_date', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:current_status', 'Under Review', 'Email template test data: current_status', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:interview_link', 'https://witchcityrope.com/vetting/interview/sample-link', 'Email template test data: interview_link', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:approval_date', 'March 8, 2026', 'Email template test data: approval_date', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:hold_reason', 'Additional references needed', 'Email template test data: hold_reason', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:required_actions', 'Please provide two community references', 'Email template test data: required_actions', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:review_date', 'March 10, 2026', 'Email template test data: review_date', NOW(), NOW()),

                    -- Events Variables
                    (gen_random_uuid(), 'EmailTestData:attendee_name', 'Jane Doe', 'Email template test data: attendee_name', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:event_title', 'Introduction to Shibari', 'Email template test data: event_title', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:event_date', 'Saturday, March 15, 2026', 'Email template test data: event_date', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:event_time', '7:00 PM EST', 'Email template test data: event_time', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:venue_name', 'The Witch City Studio', 'Email template test data: venue_name', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:venue_address', '123 Essex Street, Salem, MA 01970', 'Email template test data: venue_address', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:ticket_type', 'General Admission', 'Email template test data: ticket_type', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:total_paid', '$35.00', 'Email template test data: total_paid', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:confirmation_number', 'WCR-2026-1234', 'Email template test data: confirmation_number', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:session_name', 'Beginner Ties - Session A', 'Email template test data: session_name', NOW(), NOW()),

                    -- Admin Variables
                    (gen_random_uuid(), 'EmailTestData:account_email', 'testuser@example.com', 'Email template test data: account_email', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:reset_url', 'https://witchcityrope.com/auth/reset-password?token=sample-token', 'Email template test data: reset_url', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:action_required', 'Please update your profile information', 'Email template test data: action_required', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:deadline_date', 'March 20, 2026', 'Email template test data: deadline_date', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:verification_url', 'https://witchcityrope.com/auth/verify-email?token=sample-token', 'Email template test data: verification_url', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:refund_amount', '$35.00', 'Email template test data: refund_amount', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:original_amount', '$45.00', 'Email template test data: original_amount', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:payment_method', 'PayPal', 'Email template test data: payment_method', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:timing_message', 'Your refund will be processed within 5-10 business days.', 'Email template test data: timing_message', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:refund_reason', 'Event cancelled by organizer', 'Email template test data: refund_reason', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:refund_id', 'REF-2026-5678', 'Email template test data: refund_id', NOW(), NOW()),

                    -- Incident Variables
                    (gen_random_uuid(), 'EmailTestData:reporter_name', 'Jane Doe', 'Email template test data: reporter_name', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:incident_number', 'INC-2026-0001', 'Email template test data: incident_number', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:incident_date', 'March 8, 2026', 'Email template test data: incident_date', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:coordinator_name', 'Safety Coordinator', 'Email template test data: coordinator_name', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:next_steps', 'An investigation has been initiated. You will be contacted within 48 hours.', 'Email template test data: next_steps', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:status', 'Under Investigation', 'Email template test data: status', NOW(), NOW()),

                    -- Ad Hoc Variables
                    (gen_random_uuid(), 'EmailTestData:recipient_name', 'Community Member', 'Email template test data: recipient_name', NOW(), NOW())
                ON CONFLICT ("Key") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all email test data settings
            migrationBuilder.Sql(
                """
                DELETE FROM "public"."Settings"
                WHERE "Key" LIKE 'EmailTestData:%';
                """);
        }
    }
}
