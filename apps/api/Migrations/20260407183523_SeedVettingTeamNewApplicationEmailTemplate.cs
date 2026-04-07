using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <summary>
    /// Seeds the VettingTeamNewApplication email template.
    /// Sent to all VettingTeam role members when a new vetting application is submitted.
    /// Contains no applicant PII — just a generic notification with a link to the admin vetting page.
    /// Category 0 = EmailCategory.Vetting, TriggerType 1 = FixedEvent (triggered by business event).
    /// </summary>
    public partial class SeedVettingTeamNewApplicationEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO public."GlobalEmailTemplates" (
                    "Id", "Category", "TemplateType", "Title", "Subject",
                    "HtmlBody", "PlainTextBody",
                    "TriggerType", "SendingEnabled",
                    "TimingOffsetDays", "TimingOffsetHours", "RecipientGroup",
                    "IsActive", "Version", "CreatedAt", "UpdatedAt", "UpdatedBy"
                )
                SELECT
                    gen_random_uuid(),
                    0, -- Vetting category
                    'VettingTeamNewApplication',
                    'New Vetting Application Submitted',
                    'New Vetting Application Submitted',
                    '<p style="margin-bottom: 16px;">Hello,</p>
                <p style="margin-bottom: 16px;">A new vetting application has been submitted and is ready for review.</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Action Required</h2>
                <p style="margin-bottom: 16px;">Please log in to the admin dashboard to review the application at your earliest convenience.</p>
                <p style="margin-bottom: 16px;"><a href="{{vetting_admin_link}}" style="display: inline-block; padding: 12px 24px; background-color: #880124; color: #ffffff; text-decoration: none; border-radius: 4px; font-weight: bold;">Review Vetting Applications</a></p>
                <p style="margin-bottom: 16px;">Best regards,<br>WitchCityRope System</p>
                <p style="margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;">This is an automated notification sent to members of the Vetting Team.</p>',
                    E'Hello,\n\nA new vetting application has been submitted and is ready for review.\n\nPlease log in to the admin dashboard to review the application:\n{{vetting_admin_link}}\n\nBest regards,\nWitchCityRope System\n\nThis is an automated notification sent to members of the Vetting Team.',
                    1, -- FixedEvent trigger (triggered by application submission)
                    true, -- SendingEnabled
                    NULL, -- TimingOffsetDays (not time-based)
                    NULL, -- TimingOffsetHours
                    NULL, -- RecipientGroup (resolved by VettingEmailService, not scheduler)
                    true, 1,
                    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC',
                    u."Id"
                FROM public."Users" u
                WHERE u."Email" IN ('admin@witchcityrope.com', 'ropemaster@witchcityrope.com')
                  AND NOT EXISTS (
                    SELECT 1 FROM public."GlobalEmailTemplates"
                    WHERE "Category" = 0 AND "TemplateType" = 'VettingTeamNewApplication'
                  )
                LIMIT 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM public."GlobalEmailTemplates"
                WHERE "Category" = 0 AND "TemplateType" = 'VettingTeamNewApplication';
                """);
        }
    }
}
