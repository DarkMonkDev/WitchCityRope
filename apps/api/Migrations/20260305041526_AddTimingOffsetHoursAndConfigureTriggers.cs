using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTimingOffsetHoursAndConfigureTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimingOffsetHours",
                schema: "public",
                table: "GlobalEmailTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverrideTimingOffsetHours",
                schema: "public",
                table: "EventEmailTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CHK_GlobalEmailTemplates_TimingOffsetHours",
                schema: "public",
                table: "GlobalEmailTemplates",
                sql: "\"TimingOffsetHours\" IS NULL OR (\"TimingOffsetHours\" >= -23 AND \"TimingOffsetHours\" <= 23)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_EventEmailTemplates_OverrideTimingOffsetHours",
                schema: "public",
                table: "EventEmailTemplates",
                sql: "\"OverrideTimingOffsetHours\" IS NULL OR (\"OverrideTimingOffsetHours\" >= -23 AND \"OverrideTimingOffsetHours\" <= 23)");

            // Data migration: Configure trigger types and recipient groups for Events templates
            // Confirmation — FixedEvent trigger, fired on purchase
            migrationBuilder.Sql(@"
                UPDATE public.""GlobalEmailTemplates""
                SET ""TriggerType"" = 1, ""RecipientGroup"" = 1
                WHERE ""Category"" = 1 AND ""TemplateType"" = 'Confirmation';");

            // Reminder1Week — TimeBased, 7 days before
            migrationBuilder.Sql(@"
                UPDATE public.""GlobalEmailTemplates""
                SET ""TriggerType"" = 2, ""TimingOffsetDays"" = 7, ""RecipientGroup"" = 1
                WHERE ""Category"" = 1 AND ""TemplateType"" = 'Reminder1Week';");

            // Reminder1Day — TimeBased, 1 day before
            migrationBuilder.Sql(@"
                UPDATE public.""GlobalEmailTemplates""
                SET ""TriggerType"" = 2, ""TimingOffsetDays"" = 1, ""RecipientGroup"" = 1
                WHERE ""Category"" = 1 AND ""TemplateType"" = 'Reminder1Day';");

            // Reminder2Hours — TimeBased, 2 hours before (uses new TimingOffsetHours column)
            migrationBuilder.Sql(@"
                UPDATE public.""GlobalEmailTemplates""
                SET ""TriggerType"" = 2, ""TimingOffsetDays"" = 0, ""TimingOffsetHours"" = 2, ""RecipientGroup"" = 1
                WHERE ""Category"" = 1 AND ""TemplateType"" = 'Reminder2Hours';");

            // ThankYou — TimeBased, 1 day after
            migrationBuilder.Sql(@"
                UPDATE public.""GlobalEmailTemplates""
                SET ""TriggerType"" = 2, ""TimingOffsetDays"" = -1, ""RecipientGroup"" = 1
                WHERE ""Category"" = 1 AND ""TemplateType"" = 'ThankYou';");

            // Cancellation — FixedEvent trigger
            migrationBuilder.Sql(@"
                UPDATE public.""GlobalEmailTemplates""
                SET ""TriggerType"" = 1, ""RecipientGroup"" = 1
                WHERE ""Category"" = 1 AND ""TemplateType"" = 'Cancellation';");

            // SessionChange — FixedEvent trigger
            migrationBuilder.Sql(@"
                UPDATE public.""GlobalEmailTemplates""
                SET ""TriggerType"" = 1, ""RecipientGroup"" = 1
                WHERE ""Category"" = 1 AND ""TemplateType"" = 'SessionChange';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_GlobalEmailTemplates_TimingOffsetHours",
                schema: "public",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_EventEmailTemplates_OverrideTimingOffsetHours",
                schema: "public",
                table: "EventEmailTemplates");

            migrationBuilder.DropColumn(
                name: "TimingOffsetHours",
                schema: "public",
                table: "GlobalEmailTemplates");

            migrationBuilder.DropColumn(
                name: "OverrideTimingOffsetHours",
                schema: "public",
                table: "EventEmailTemplates");
        }
    }
}
