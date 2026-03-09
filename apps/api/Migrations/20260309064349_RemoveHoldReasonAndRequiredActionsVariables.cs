using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHoldReasonAndRequiredActionsVariables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================================
            // 1. UPDATE ApplicationOnHold TEMPLATE
            // ============================================================================
            // Replaces {{hold_reason}} and {{required_actions}} with {{custom_message}}
            // for consistency with other templates that use custom_message.
            migrationBuilder.Sql(
                """
                UPDATE "GlobalEmailTemplates"
                SET
                    "HtmlBody" = '<p style="margin-bottom: 16px;">Hi {{scene_name}},</p><p style="margin-bottom: 16px;">Your vetting application is currently on hold as we need some additional information to proceed.</p><h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Application Status</h2><p style="margin-bottom: 16px;"><strong>Application Number:</strong> {{application_number}}<br><strong>Status:</strong> On Hold</p><p style="margin-bottom: 16px;">{{custom_message}}</p><h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Important Deadline</h2><p style="margin-bottom: 16px;">Please provide the requested information within <strong>30 days</strong> to avoid application expiration.</p><p style="margin-bottom: 16px;">If you have any questions about what''s needed, please don''t hesitate to contact us.</p><p style="margin-bottom: 16px;">Best regards,<br>The Witch City Rope Vetting Team</p><p style="margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;">Questions? Contact us at <a href="mailto:info@witchcityrope.com" style="color: #880124;">info@witchcityrope.com</a></p>',
                    "PlainTextBody" = E'Dear {{scene_name}},\n\nYour vetting application is currently on hold as we need some additional information to proceed.\n\nApplication Number: {{application_number}}\n\n{{custom_message}}\n\nPlease provide the requested information within 30 days to avoid application expiration.\n\nIf you have any questions about what''s needed, please contact us.\n\nBest regards,\nThe WitchCityRope Vetting Team',
                    "Variables" = '["{{scene_name}}","{{application_number}}","{{custom_message}}","{{submission_date}}","{{application_date}}","{{status_change_date}}","{{current_status}}"]'::jsonb,
                    "UpdatedAt" = NOW(),
                    "Version" = "Version" + 1
                WHERE "Category" = 0
                    AND "TemplateType" = 'ApplicationOnHold';
                """);

            // ============================================================================
            // 2. REMOVE OBSOLETE TEST DATA
            // ============================================================================
            // Delete hold_reason and required_actions test data entries from Settings
            // since these variables no longer exist.
            migrationBuilder.Sql(
                """
                DELETE FROM "Settings"
                WHERE "Key" IN ('EmailTestData:hold_reason', 'EmailTestData:required_actions');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore original ApplicationOnHold template with hold_reason and required_actions
            migrationBuilder.Sql(
                """
                UPDATE "GlobalEmailTemplates"
                SET
                    "HtmlBody" = '<p style="margin-bottom: 16px;">Hi {{scene_name}},</p><p style="margin-bottom: 16px;">Your vetting application is currently on hold as we need some additional information to proceed.</p><h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Application Status</h2><p style="margin-bottom: 16px;"><strong>Application Number:</strong> {{application_number}}<br><strong>Status:</strong> On Hold<br><strong>Reason:</strong> {{hold_reason}}</p><h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Required Actions</h2><p style="margin-bottom: 16px;">{{required_actions}}</p><h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Important Deadline</h2><p style="margin-bottom: 16px;">Please provide the requested information within <strong>30 days</strong> to avoid application expiration.</p><p style="margin-bottom: 16px;">If you have any questions about what''s needed, please don''t hesitate to contact us.</p><p style="margin-bottom: 16px;">Best regards,<br>The Witch City Rope Vetting Team</p><p style="margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;">Questions? Contact us at <a href="mailto:info@witchcityrope.com" style="color: #880124;">info@witchcityrope.com</a></p>',
                    "PlainTextBody" = E'Dear {{scene_name}},\n\nYour vetting application is currently on hold as we need some additional information to proceed.\n\nApplication Number: {{application_number}}\nReason: {{hold_reason}}\n\nRequired Actions:\n{{required_actions}}\n\nPlease provide the requested information within 30 days to avoid application expiration.\n\nIf you have any questions about what''s needed, please contact us.\n\nBest regards,\nThe WitchCityRope Vetting Team',
                    "Variables" = '["{{scene_name}}","{{application_number}}","{{hold_reason}}","{{required_actions}}","{{submission_date}}","{{application_date}}","{{status_change_date}}","{{current_status}}"]'::jsonb,
                    "UpdatedAt" = NOW(),
                    "Version" = "Version" + 1
                WHERE "Category" = 0
                    AND "TemplateType" = 'ApplicationOnHold';
                """);

            // Restore test data
            migrationBuilder.Sql(
                """
                INSERT INTO "Settings" ("Id", "Key", "Value", "Description", "CreatedAt", "UpdatedAt")
                VALUES
                    (gen_random_uuid(), 'EmailTestData:hold_reason', 'Additional references needed', 'Email template test data: hold_reason', NOW(), NOW()),
                    (gen_random_uuid(), 'EmailTestData:required_actions', 'Please provide two community references', 'Email template test data: required_actions', NOW(), NOW());
                """);
        }
    }
}
