using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardcodeStaticEmailVariables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update Vetting templates - replace {{contact_email}} with info@witchcityrope.com
            // Note: Vetting = Category 0 (NOT 1)
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""HtmlBody"" = REPLACE(""HtmlBody"", '{{contact_email}}', 'info@witchcityrope.com'),
                    ""PlainTextBody"" = REPLACE(""PlainTextBody"", '{{contact_email}}', 'info@witchcityrope.com'),
                    ""UpdatedAt"" = NOW()
                WHERE ""Category"" = 0;
            ");

            // Remove {{contact_email}} from Variables JSONB array for Vetting templates
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""Variables"" = (
                    SELECT jsonb_agg(elem)
                    FROM jsonb_array_elements(""Variables"") AS elem
                    WHERE elem::text != '""{{contact_email}}""'
                )
                WHERE ""Category"" = 0 AND ""Variables""::text LIKE '%{{contact_email}}%';
            ");

            // Update Events templates - replace {{organizer_email}} with events@witchcityrope.com
            // Note: Events = Category 1 (NOT 0)
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""HtmlBody"" = REPLACE(""HtmlBody"", '{{organizer_email}}', 'events@witchcityrope.com'),
                    ""PlainTextBody"" = REPLACE(""PlainTextBody"", '{{organizer_email}}', 'events@witchcityrope.com'),
                    ""UpdatedAt"" = NOW()
                WHERE ""Category"" = 1;
            ");

            // Remove {{organizer_email}} from Variables JSONB array for Events templates
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""Variables"" = (
                    SELECT jsonb_agg(elem)
                    FROM jsonb_array_elements(""Variables"") AS elem
                    WHERE elem::text != '""{{organizer_email}}""'
                )
                WHERE ""Category"" = 1 AND ""Variables""::text LIKE '%{{organizer_email}}%';
            ");

            // Update Admin templates - replace {{support_email}} with support@witchcityrope.com
            // Note: Admin = Category 2 (correct)
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""HtmlBody"" = REPLACE(""HtmlBody"", '{{support_email}}', 'support@witchcityrope.com'),
                    ""PlainTextBody"" = REPLACE(""PlainTextBody"", '{{support_email}}', 'support@witchcityrope.com'),
                    ""UpdatedAt"" = NOW()
                WHERE ""Category"" = 2;
            ");

            // Remove {{support_email}} from Variables JSONB array for Admin templates
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""Variables"" = (
                    SELECT jsonb_agg(elem)
                    FROM jsonb_array_elements(""Variables"") AS elem
                    WHERE elem::text != '""{{support_email}}""'
                )
                WHERE ""Category"" = 2 AND ""Variables""::text LIKE '%{{support_email}}%';
            ");

            // Update Admin AccountCreated template - replace {{system_url}} with https://witchcityrope.com
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""HtmlBody"" = REPLACE(""HtmlBody"", '{{system_url}}', 'https://witchcityrope.com'),
                    ""PlainTextBody"" = REPLACE(""PlainTextBody"", '{{system_url}}', 'https://witchcityrope.com'),
                    ""UpdatedAt"" = NOW()
                WHERE ""Category"" = 2 AND ""TemplateType"" = 'AccountCreated';
            ");

            // Remove {{system_url}} from Variables JSONB array for AccountCreated template
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""Variables"" = (
                    SELECT jsonb_agg(elem)
                    FROM jsonb_array_elements(""Variables"") AS elem
                    WHERE elem::text != '""{{system_url}}""'
                )
                WHERE ""Category"" = 2 AND ""TemplateType"" = 'AccountCreated' AND ""Variables""::text LIKE '%{{system_url}}%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback - restore variables for Vetting templates
            // Note: Vetting = Category 0 (NOT 1)
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""HtmlBody"" = REPLACE(""HtmlBody"", 'info@witchcityrope.com', '{{contact_email}}'),
                    ""PlainTextBody"" = REPLACE(""PlainTextBody"", 'info@witchcityrope.com', '{{contact_email}}'),
                    ""UpdatedAt"" = NOW()
                WHERE ""Category"" = 0;
            ");

            // Rollback - restore variables for Events templates
            // Note: Events = Category 1 (NOT 0)
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""HtmlBody"" = REPLACE(""HtmlBody"", 'events@witchcityrope.com', '{{organizer_email}}'),
                    ""PlainTextBody"" = REPLACE(""PlainTextBody"", 'events@witchcityrope.com', '{{organizer_email}}'),
                    ""UpdatedAt"" = NOW()
                WHERE ""Category"" = 1;
            ");

            // Rollback - restore variables for Admin templates
            // Note: Admin = Category 2 (correct)
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""HtmlBody"" = REPLACE(""HtmlBody"", 'support@witchcityrope.com', '{{support_email}}'),
                    ""PlainTextBody"" = REPLACE(""PlainTextBody"", 'support@witchcityrope.com', '{{support_email}}'),
                    ""UpdatedAt"" = NOW()
                WHERE ""Category"" = 2;
            ");

            // Rollback - restore variables for AccountCreated template
            migrationBuilder.Sql(@"
                UPDATE ""GlobalEmailTemplates""
                SET ""HtmlBody"" = REPLACE(""HtmlBody"", 'https://witchcityrope.com', '{{system_url}}'),
                    ""PlainTextBody"" = REPLACE(""PlainTextBody"", 'https://witchcityrope.com', '{{system_url}}'),
                    ""UpdatedAt"" = NOW()
                WHERE ""Category"" = 2 AND ""TemplateType"" = 'AccountCreated';
            ");
        }
    }
}
