# Backend Developer Handoff - Hardcode Static Email Variables

**Date**: 2025-11-17
**Agent**: backend-developer
**Estimated Time**: 2-4 hours
**Priority**: High

## Context

Email templates currently use variables like `{{support_email}}`, `{{contact_email}}`, and `{{system_url}}` that never change. This creates unnecessary complexity - every service must populate these same values, and admins see placeholder text instead of actual contact information.

## Your Tasks

### Phase 1: Update Email Template Seeder (1-2 hours)

**File**: `apps/api/Services/Seeding/EmailTemplateSeeder.cs`

**What to do**:
1. Replace all instances of static variables with hardcoded values in template HTML and plain text
2. Update the `Variables` JSON field to remove static variables (only keep dynamic ones)
3. Create a database migration to update existing GlobalEmailTemplates records

**Static Variables to Replace**:

| Variable | Replacement | Templates Affected |
|----------|-------------|-------------------|
| `{{support_email}}` | `support@witchcityrope.com` | Admin (4), Incident (4) |
| `{{contact_email}}` | `info@witchcityrope.com` | Vetting (6) |
| `{{organizer_email}}` | `events@witchcityrope.com` | Events (7) |
| `{{system_url}}` | `https://witchcityrope.com` | Admin (1) |

**Example - Before** (line 316 in EmailTemplateSeeder.cs):
```csharp
HtmlBody = "<p>Hi {{user_name}},</p><p>Your WitchCityRope account has been created!</p><p><strong>Email:</strong> {{account_email}}</p><p>You can log in at {{system_url}}</p><p>If you have any questions, contact us at {{support_email}}</p>",
PlainTextBody = "Hi {{user_name}},\n\nYour WitchCityRope account has been created!\n\nEmail: {{account_email}}\n\nYou can log in at {{system_url}}\n\nIf you have any questions, contact us at {{support_email}}",
Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{account_email}}", "{{system_url}}", "{{support_email}}" }),
```

**Example - After**:
```csharp
HtmlBody = "<p>Hi {{user_name}},</p><p>Your WitchCityRope account has been created!</p><p><strong>Email:</strong> {{account_email}}</p><p>You can log in at https://witchcityrope.com</p><p>If you have any questions, contact us at support@witchcityrope.com</p>",
PlainTextBody = "Hi {{user_name}},\n\nYour WitchCityRope account has been created!\n\nEmail: {{account_email}}\n\nYou can log in at https://witchcityrope.com\n\nIf you have any questions, contact us at support@witchcityrope.com",
Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{account_email}}" }),
```

**All Templates to Update** (23 total):

**Vetting (6 templates)** - Replace `{{contact_email}}` with `info@witchcityrope.com`:
- ApplicationReceived (lines 72-86)
- InterviewApproved (lines 87-101)
- VettingApproved (lines 102-116)
- ApplicationOnHold (lines 117-131)
- ApplicationStatusUpdate (lines 132-146)
- InterviewReminder (lines 147-161)

**Events (7 templates)** - Replace `{{organizer_email}}` with `events@witchcityrope.com`:
- Confirmation (lines 187-200)
- Reminder1Week (line 201-214)
- Reminder1Day (line 215-228)
- Reminder2Hours (line 229-242)
- Cancellation (line 243-256)
- SessionChange (line 257-270)
- ThankYou (line 271-284)

**Admin (6 templates)** - Replace `{{support_email}}` with `support@witchcityrope.com`:
- AccountCreated (lines 310-323) - Also replace `{{system_url}}` with `https://witchcityrope.com`
- PasswordReset (lines 324-337)
- RoleChanged (lines 338-351)
- SystemNotification (lines 352-365)
- EmailVerification (lines 366-379)
- RefundConfirmation (lines 380-461)

**Incident (4 templates)** - No static variables currently used (skip)
- ReportReceived (lines 486-500)
- StatusUpdate (lines 501-515)
- AssignmentNotification (lines 516-528)
- Resolved (lines 529-542)

**Ad Hoc (1 template)** - No static variables (skip)
- General (lines 562-579)

**Migration Needed**:
Create migration to update existing records in database:

```csharp
public partial class HardcodeStaticEmailVariables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Update Vetting templates - replace {{contact_email}}
        migrationBuilder.Sql(@"
            UPDATE ""GlobalEmailTemplates""
            SET ""HtmlBody"" = REPLACE(""HtmlBody"", '{{contact_email}}', 'info@witchcityrope.com'),
                ""PlainTextBody"" = REPLACE(""PlainTextBody"", '{{contact_email}}', 'info@witchcityrope.com'),
                ""Variables"" = REPLACE(""Variables"", ',""{{contact_email}}""', ''),
                ""Variables"" = REPLACE(""Variables"", '""{{contact_email}}"",', ''),
                ""UpdatedAt"" = NOW()
            WHERE ""Category"" = 1; -- EmailCategory.Vetting
        ");

        // Update Events templates - replace {{organizer_email}}
        migrationBuilder.Sql(@"
            UPDATE ""GlobalEmailTemplates""
            SET ""HtmlBody"" = REPLACE(""HtmlBody"", '{{organizer_email}}', 'events@witchcityrope.com'),
                ""PlainTextBody"" = REPLACE(""PlainTextBody"", '{{organizer_email}}', 'events@witchcityrope.com'),
                ""Variables"" = REPLACE(""Variables"", ',""{{organizer_email}}""', ''),
                ""Variables"" = REPLACE(""Variables"", '""{{organizer_email}}"",', ''),
                ""UpdatedAt"" = NOW()
            WHERE ""Category"" = 0; -- EmailCategory.Events
        ");

        // Update Admin templates - replace {{support_email}}
        migrationBuilder.Sql(@"
            UPDATE ""GlobalEmailTemplates""
            SET ""HtmlBody"" = REPLACE(""HtmlBody"", '{{support_email}}', 'support@witchcityrope.com'),
                ""PlainTextBody"" = REPLACE(""PlainTextBody"", '{{support_email}}', 'support@witchcityrope.com'),
                ""Variables"" = REPLACE(""Variables"", ',""{{support_email}}""', ''),
                ""Variables"" = REPLACE(""Variables"", '""{{support_email}}"",', ''),
                ""UpdatedAt"" = NOW()
            WHERE ""Category"" = 2; -- EmailCategory.Admin
        ");

        // Update Admin AccountCreated template - replace {{system_url}}
        migrationBuilder.Sql(@"
            UPDATE ""GlobalEmailTemplates""
            SET ""HtmlBody"" = REPLACE(""HtmlBody"", '{{system_url}}', 'https://witchcityrope.com'),
                ""PlainTextBody"" = REPLACE(""PlainTextBody"", '{{system_url}}', 'https://witchcityrope.com'),
                ""Variables"" = REPLACE(""Variables"", ',""{{system_url}}""', ''),
                ""Variables"" = REPLACE(""Variables"", '""{{system_url}}"",', ''),
                ""UpdatedAt"" = NOW()
            WHERE ""Category"" = 2 AND ""TemplateType"" = 'AccountCreated';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback - restore variables
        migrationBuilder.Sql(@"
            UPDATE ""GlobalEmailTemplates""
            SET ""HtmlBody"" = REPLACE(""HtmlBody"", 'info@witchcityrope.com', '{{contact_email}}'),
                ""PlainTextBody"" = REPLACE(""PlainTextBody"", 'info@witchcityrope.com', '{{contact_email}}'),
                ""UpdatedAt"" = NOW()
            WHERE ""Category"" = 1;
        ");

        migrationBuilder.Sql(@"
            UPDATE ""GlobalEmailTemplates""
            SET ""HtmlBody"" = REPLACE(""HtmlBody"", 'events@witchcityrope.com', '{{organizer_email}}'),
                ""PlainTextBody"" = REPLACE(""PlainTextBody"", 'events@witchcityrope.com', '{{organizer_email}}'),
                ""UpdatedAt"" = NOW()
            WHERE ""Category"" = 0;
        ");

        migrationBuilder.Sql(@"
            UPDATE ""GlobalEmailTemplates""
            SET ""HtmlBody"" = REPLACE(""HtmlBody"", 'support@witchcityrope.com', '{{support_email}}'),
                ""PlainTextBody"" = REPLACE(""PlainTextBody"", 'support@witchcityrope.com', '{{support_email}}'),
                ""UpdatedAt"" = NOW()
            WHERE ""Category"" = 2;
        ");

        migrationBuilder.Sql(@"
            UPDATE ""GlobalEmailTemplates""
            SET ""HtmlBody"" = REPLACE(""HtmlBody"", 'https://witchcityrope.com', '{{system_url}}'),
                ""PlainTextBody"" = REPLACE(""PlainTextBody"", 'https://witchcityrope.com', '{{system_url}}'),
                ""UpdatedAt"" = NOW()
            WHERE ""Category"" = 2 AND ""TemplateType"" = 'AccountCreated';
        ");
    }
}
```

### Phase 2: Update Email-Sending Services (1-2 hours)

Remove static variables from all services that call `SendTemplatedEmailAsync()`.

**Files to Update**:

1. **VettingEmailService.cs** (`apps/api/Features/Vetting/Services/VettingEmailService.cs`)

**Remove from lines 40-49** (SendApplicationConfirmationAsync):
```csharp
// REMOVE THIS LINE:
{ "contact_email", _configuration["Vetting:FromEmail"] ?? "info@witchcityrope.com" },
```

**Remove from lines 114-128** (SendStatusUpdateAsync):
```csharp
// REMOVE THIS LINE:
{ "contact_email", _configuration["Vetting:FromEmail"] ?? "info@witchcityrope.com" },
```

**Remove from lines 175-185** (SendReminderAsync):
```csharp
// REMOVE THIS LINE:
{ "contact_email", _configuration["Vetting:FromEmail"] ?? "info@witchcityrope.com" },
```

2. **AuthenticationService.cs** (`apps/api/Features/Authentication/Services/AuthenticationService.cs`)

Search for `SendTemplatedEmailAsync` calls and remove:
```csharp
// REMOVE:
{ "support_email", "support@witchcityrope.com" }
{ "system_url", "https://witchcityrope.com" }
```

3. **RefundService.cs** (`apps/api/Features/Payments/Services/RefundService.cs`)

Search for email sending and remove:
```csharp
// REMOVE:
{ "support_email", "support@witchcityrope.com" }
```

4. **Find all other usages**:
```bash
# Search for all SendTemplatedEmailAsync calls
grep -rn "SendTemplatedEmailAsync" apps/api/Features/ --include="*.cs"

# Look for static variable assignments
grep -rn '"support_email"' apps/api/Features/ --include="*.cs"
grep -rn '"contact_email"' apps/api/Features/ --include="*.cs"
grep -rn '"organizer_email"' apps/api/Features/ --include="*.cs"
grep -rn '"system_url"' apps/api/Features/ --include="*.cs"
```

## Testing Your Changes

**After Phase 1**:
```bash
# Create and apply migration
dotnet ef migrations add HardcodeStaticEmailVariables -p apps/api
dotnet ef database update -p apps/api

# Verify templates in database
# Connect to database and check:
SELECT "TemplateType", "HtmlBody", "Variables"
FROM "GlobalEmailTemplates"
WHERE "Category" = 1 -- Vetting
LIMIT 1;

# HtmlBody should contain "info@witchcityrope.com" (not {{contact_email}})
# Variables should NOT contain "{{contact_email}}"
```

**After Phase 2**:
```bash
# Run unit tests
dotnet test apps/api/

# Look for failing tests that expect static variables in dictionaries
# Update those tests (test-developer will handle comprehensively)
```

**Manual Testing**:
1. Trigger a vetting application submission
2. Check email logs (development mode logs to console)
3. Verify email contains "info@witchcityrope.com" (not "{{contact_email}}")

## Success Criteria

- ✅ All 23 templates in EmailTemplateSeeder.cs have hardcoded values
- ✅ Migration created and applied successfully
- ✅ Database templates updated (no {{support_email}}, {{contact_email}}, {{organizer_email}}, {{system_url}})
- ✅ No services populate removed variables in dictionaries
- ✅ Existing unit tests still pass (may need minor updates)

## Files You'll Modify

**New**:
- `apps/api/Migrations/[timestamp]_HardcodeStaticEmailVariables.cs` (create)

**Modified**:
- `apps/api/Services/Seeding/EmailTemplateSeeder.cs`
- `apps/api/Features/Vetting/Services/VettingEmailService.cs`
- `apps/api/Features/Authentication/Services/AuthenticationService.cs`
- `apps/api/Features/Payments/Services/RefundService.cs`
- Any other services found via grep

## Questions or Issues?

If you encounter:
- **Templates with other static-looking variables**: Check if they're truly static. If yes, hardcode them too.
- **Configuration values that might change**: Leave as-is for now (this is phase 1, we can iterate)
- **Tests failing**: Note which tests need updates - test-developer will handle comprehensive test updates

## Next Steps After Completion

Once you complete both phases:
1. Commit your changes with descriptive message
2. Report back with list of modified files
3. Note any issues or edge cases discovered
4. test-developer will update tests
5. react-developer will update UI in parallel

**Implementation Plan**: `/docs/functional-areas/email-templates/new-work/2025-11-17-hardcode-static-variables/implementation-plan.md`
