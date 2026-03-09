# Email Template Test Send Feature - Design Specification

**Created**: 2026-03-08
**Status**: Approved for Implementation

## Overview

Add the ability for admins to send test emails for any email template, with configurable test data that persists in the database. This is a **Hybrid approach** (Option C): a global "Test Data" tab for managing all default variable values, plus an inline "Send Test" section in the template editor showing only that template's variables.

---

## Architecture Decisions

### Storage
- **Use the existing `Settings` table** with key prefix `EmailTestData:` (e.g., `EmailTestData:scene_name` = `"Dark Phoenix"`)
- Individual Setting rows per variable — fits within existing `MaxLength(500)` constraint
- **NO new database tables or migrations needed**
- Settings are seeded with reasonable defaults via `SettingsSeeder`

### Behavior Rules
1. **Inline overrides auto-save**: When a user changes a variable value in the inline Send Test section and sends, those override values are automatically saved back to the defaults in the Settings table
2. **Save-then-send**: When the user hits "Send Test" while editing a template, the system FIRST saves the current template content (title, subject, htmlBody), THEN sends the saved version from the database. This ensures what you test is what's persisted.
3. **Admin-only**: All endpoints and UI require Administrator role

### Email Sending
- Uses the existing `EmailService.SendEmailAsync()` method (the raw HTML version, NOT `SendTemplatedEmailAsync`)
- Variable substitution is done manually before calling SendEmailAsync — the backend fetches the template, loads test data, applies overrides, substitutes variables, then sends
- Works in staging and production (SendGrid configured in both)
- In development mode (no SendGrid), emails are logged to console only

---

## Backend Implementation

### 1. Make SubstituteVariables Accessible

**File**: `apps/api/Features/Shared/Services/EmailService.cs`

Change the existing `SubstituteVariables` method from `private static` to `internal static` so it can be reused by the test send logic without duplicating code.

```csharp
// CHANGE FROM:
private static string SubstituteVariables(string template, Dictionary<string, string> variables)

// CHANGE TO:
internal static string SubstituteVariables(string template, Dictionary<string, string> variables)
```

**DO NOT** copy, duplicate, or rewrite this method. Just change the access modifier.

### 2. Add Upsert to SettingsService

**File**: `apps/api/Features/Admin/Settings/Services/SettingsService.cs`
**Interface**: `apps/api/Features/Admin/Settings/Interfaces/ISettingsService.cs`

Add a new method `UpsertMultipleSettingsAsync` that creates Setting rows if they don't exist, or updates them if they do.

```csharp
/// <summary>
/// Creates or updates multiple settings. If a setting with the given key doesn't exist, it is created.
/// If it already exists, its value is updated. Used by email template test data feature.
/// </summary>
public async Task<(bool Success, string Error)> UpsertMultipleSettingsAsync(
    Dictionary<string, string> settings,
    CancellationToken cancellationToken = default)
{
    try
    {
        var keys = settings.Keys.ToList();
        var existingSettings = await _context.Settings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        // Update existing settings
        foreach (var existing in existingSettings)
        {
            existing.Value = settings[existing.Key];
            existing.UpdatedAt = now;
        }

        // Create new settings for keys that don't exist yet
        var existingKeys = existingSettings.Select(s => s.Key).ToHashSet();
        var newSettings = settings
            .Where(kvp => !existingKeys.Contains(kvp.Key))
            .Select(kvp => new Setting
            {
                Id = Guid.NewGuid(),
                Key = kvp.Key,
                Value = kvp.Value,
                Description = $"Email template test data for variable: {kvp.Key.Replace("EmailTestData:", "")}",
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        if (newSettings.Count > 0)
        {
            _context.Settings.AddRange(newSettings);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Upserted {Count} settings ({New} new, {Updated} updated)",
            settings.Count, newSettings.Count, existingSettings.Count);

        return (true, string.Empty);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error upserting settings");
        return (false, "Failed to upsert settings");
    }
}
```

Also add the interface method to `ISettingsService.cs`:
```csharp
Task<(bool Success, string Error)> UpsertMultipleSettingsAsync(
    Dictionary<string, string> settings,
    CancellationToken cancellationToken = default);
```

**IMPORTANT**: You need to add `using WitchCityRope.Api.Core.Entities;` to `SettingsService.cs` if not already present, since we're creating `new Setting` objects.

### 3. New API Endpoints

**File**: `apps/api/Features/EmailTemplates/Endpoints/EmailTemplateEndpoints.cs`

Add these 3 endpoints to the EXISTING `MapEmailTemplateEndpoints` method. Add them at the end of the method, before the closing brace.

#### GET /api/email-templates/test-data

```csharp
// ── Email Template Test Data ──────────────────────────────────────
// Returns all saved test data variable values for email template testing.
// Values are stored in the Settings table with "EmailTestData:" prefix.
group.MapGet("/test-data", async (
    ISettingsService settingsService,
    CancellationToken cancellationToken) =>
{
    var allSettings = await settingsService.GetAllSettingsAsync(cancellationToken);

    // Filter to only EmailTestData: prefixed settings and strip the prefix for the response
    var testData = allSettings
        .Where(kvp => kvp.Key.StartsWith("EmailTestData:"))
        .ToDictionary(
            kvp => kvp.Key.Replace("EmailTestData:", ""),
            kvp => kvp.Value);

    return Results.Ok(testData);
})
.RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
.WithName("GetEmailTestData")
.WithSummary("Get all email template test data variable values")
.WithDescription("Returns saved default values for all email template variables, used for test sends")
.Produces<Dictionary<string, string>>(200);
```

#### PUT /api/email-templates/test-data

```csharp
// Saves test data variable values for email template testing.
// Uses upsert — creates new entries if they don't exist, updates existing ones.
group.MapPut("/test-data", async (
    Dictionary<string, string> testData,
    ISettingsService settingsService,
    CancellationToken cancellationToken) =>
{
    // Prefix all keys with "EmailTestData:" for storage
    var prefixedData = testData.ToDictionary(
        kvp => $"EmailTestData:{kvp.Key}",
        kvp => kvp.Value);

    var (success, error) = await settingsService.UpsertMultipleSettingsAsync(
        prefixedData, cancellationToken);

    if (!success)
    {
        return Results.Problem(
            title: "Save Failed",
            detail: error,
            statusCode: 500);
    }

    return Results.Ok(new { message = "Test data saved successfully" });
})
.RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
.WithName("SaveEmailTestData")
.WithSummary("Save email template test data variable values")
.WithDescription("Creates or updates default variable values used for test email sends")
.Produces<object>(200)
.Produces(500);
```

#### POST /api/email-templates/{id}/send-test

```csharp
// Sends a test email for a specific template. Loads the template from the database,
// merges saved test data with any provided overrides, substitutes variables, and sends.
// Any override values are automatically saved back to the defaults.
group.MapPost("/{id}/send-test", async (
    string id,
    SendTestEmailRequest request,
    ApplicationDbContext context,
    ISettingsService settingsService,
    IEmailService emailService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    // Validate email address
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { error = "Email address is required" });
    }

    // Fetch the template from the database
    if (!Guid.TryParse(id, out var templateId))
    {
        return Results.BadRequest(new { error = "Invalid template ID" });
    }

    var template = await context.GlobalEmailTemplates
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

    if (template == null)
    {
        return Results.NotFound(new { error = "Template not found" });
    }

    // Load saved test data defaults from Settings
    var allSettings = await settingsService.GetAllSettingsAsync(cancellationToken);
    var testData = allSettings
        .Where(kvp => kvp.Key.StartsWith("EmailTestData:"))
        .ToDictionary(
            kvp => kvp.Key.Replace("EmailTestData:", ""),
            kvp => kvp.Value);

    // Merge with overrides (overrides take precedence)
    if (request.VariableOverrides != null)
    {
        foreach (var kvp in request.VariableOverrides)
        {
            testData[kvp.Key] = kvp.Value;
        }

        // Auto-save overrides back to defaults
        var prefixedOverrides = request.VariableOverrides.ToDictionary(
            kvp => $"EmailTestData:{kvp.Key}",
            kvp => kvp.Value);

        await settingsService.UpsertMultipleSettingsAsync(prefixedOverrides, cancellationToken);
    }

    // Substitute variables in template content
    var subject = EmailService.SubstituteVariables(template.Subject, testData);
    var htmlBody = EmailService.SubstituteVariables(template.HtmlBody, testData);
    var plainTextBody = EmailService.SubstituteVariables(template.PlainTextBody, testData);

    // Send the email using the raw send method (not templated, since we already resolved the template)
    var result = await emailService.SendEmailAsync(
        request.Email,
        subject,
        htmlBody,
        plainTextBody,
        cancellationToken);

    if (result.IsSuccess)
    {
        logger.LogInformation(
            "Test email sent: TemplateId={TemplateId}, TemplateType={TemplateType}, To={Email}",
            template.Id, template.TemplateType, request.Email);

        return Results.Ok(new
        {
            message = "Test email sent successfully",
            templateType = template.TemplateType,
            sentTo = request.Email
        });
    }

    return Results.Problem(
        title: "Send Failed",
        detail: result.Error,
        statusCode: 500);
})
.RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
.WithName("SendTestEmail")
.WithSummary("Send a test email for a specific template")
.WithDescription("Sends a test email with test data variable substitution to a specified email address")
.Produces<object>(200)
.Produces(400)
.Produces(404)
.Produces(500);
```

### 4. Request Model

**New File**: `apps/api/Features/EmailTemplates/Models/SendTestEmailRequest.cs`

```csharp
namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for sending a test email for a specific template.
/// The email field is the recipient address, and variableOverrides allows
/// overriding saved test data defaults for this specific send.
/// Overrides are automatically saved back to the defaults.
/// </summary>
public class SendTestEmailRequest
{
    /// <summary>
    /// Email address to send the test email to
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional variable value overrides. Keys are variable names WITHOUT braces
    /// (e.g., "scene_name" not "{{scene_name}}"). Values provided here take precedence
    /// over saved defaults and are auto-saved back to defaults after sending.
    /// </summary>
    public Dictionary<string, string>? VariableOverrides { get; set; }
}
```

### 5. Seed Test Data Defaults

**File**: `apps/api/Services/Seeding/SettingsSeeder.cs`

Add a new method `SeedEmailTestDataAsync` and call it from the existing seeding flow. This method should check if EmailTestData settings already exist and only seed if they don't (don't overwrite user customizations).

The seed data should include these variable groups with reasonable sample values:

```csharp
/// <summary>
/// Seeds default test data values for email template testing.
/// Only creates settings that don't already exist (preserves user customizations).
/// </summary>
public async Task SeedEmailTestDataAsync(CancellationToken cancellationToken = default)
{
    var testDataDefaults = new Dictionary<string, string>
    {
        // ── Global Variables ──
        ["EmailTestData:user_name"] = "Jane Doe",
        ["EmailTestData:system_url"] = "https://witchcityrope.com",
        ["EmailTestData:custom_message"] = "This is a test custom message for preview purposes.",
        ["EmailTestData:custom_content"] = "This is test custom content for preview purposes.",

        // ── Vetting Variables ──
        ["EmailTestData:scene_name"] = "Dark Phoenix",
        ["EmailTestData:application_number"] = "APP-2026-0042",
        ["EmailTestData:submission_date"] = "March 1, 2026",
        ["EmailTestData:application_date"] = "March 1, 2026",
        ["EmailTestData:status_change_date"] = "March 5, 2026",
        ["EmailTestData:current_status"] = "Under Review",
        ["EmailTestData:interview_link"] = "https://witchcityrope.com/vetting/interview/sample-link",
        ["EmailTestData:approval_date"] = "March 8, 2026",
        ["EmailTestData:hold_reason"] = "Additional references needed",
        ["EmailTestData:required_actions"] = "Please provide two community references",
        ["EmailTestData:review_date"] = "March 10, 2026",

        // ── Events Variables ──
        ["EmailTestData:attendee_name"] = "Jane Doe",
        ["EmailTestData:event_title"] = "Introduction to Shibari",
        ["EmailTestData:event_date"] = "Saturday, March 15, 2026",
        ["EmailTestData:event_time"] = "7:00 PM EST",
        ["EmailTestData:venue_name"] = "The Witch City Studio",
        ["EmailTestData:venue_address"] = "123 Essex Street, Salem, MA 01970",
        ["EmailTestData:ticket_type"] = "General Admission",
        ["EmailTestData:total_paid"] = "$35.00",
        ["EmailTestData:confirmation_number"] = "WCR-2026-1234",
        ["EmailTestData:session_name"] = "Beginner Ties - Session A",

        // ── Admin Variables ──
        ["EmailTestData:account_email"] = "testuser@example.com",
        ["EmailTestData:reset_url"] = "https://witchcityrope.com/auth/reset-password?token=sample-token",
        ["EmailTestData:action_required"] = "Please update your profile information",
        ["EmailTestData:deadline_date"] = "March 20, 2026",
        ["EmailTestData:verification_url"] = "https://witchcityrope.com/auth/verify-email?token=sample-token",
        ["EmailTestData:refund_amount"] = "$35.00",
        ["EmailTestData:original_amount"] = "$45.00",
        ["EmailTestData:payment_method"] = "PayPal",
        ["EmailTestData:timing_message"] = "Your refund will be processed within 5-10 business days.",
        ["EmailTestData:refund_reason"] = "Event cancelled by organizer",
        ["EmailTestData:refund_id"] = "REF-2026-5678",

        // ── Incident Variables ──
        ["EmailTestData:reporter_name"] = "Jane Doe",
        ["EmailTestData:incident_number"] = "INC-2026-0001",
        ["EmailTestData:incident_date"] = "March 8, 2026",
        ["EmailTestData:coordinator_name"] = "Safety Coordinator",
        ["EmailTestData:next_steps"] = "An investigation has been initiated. You will be contacted within 48 hours.",
        ["EmailTestData:status"] = "Under Investigation",

        // ── Ad Hoc Variables ──
        ["EmailTestData:recipient_name"] = "Community Member",
    };

    // Only seed settings that don't already exist (preserve user customizations)
    var existingKeys = await _context.Settings
        .Where(s => s.Key.StartsWith("EmailTestData:"))
        .Select(s => s.Key)
        .ToListAsync(cancellationToken);

    var newSettings = testDataDefaults
        .Where(kvp => !existingKeys.Contains(kvp.Key))
        .Select(kvp => new Setting
        {
            Id = Guid.NewGuid(),
            Key = kvp.Key,
            Value = kvp.Value,
            Description = $"Email template test data: {kvp.Key.Replace("EmailTestData:", "")}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        })
        .ToList();

    if (newSettings.Count > 0)
    {
        _context.Settings.AddRange(newSettings);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} email test data defaults", newSettings.Count);
    }
    else
    {
        _logger.LogInformation("Email test data already seeded - skipping");
    }
}
```

### 6. Required Using Statements / DI

The send-test endpoint needs these injected:
- `ApplicationDbContext context` — to fetch the template
- `ISettingsService settingsService` — to load/save test data
- `IEmailService emailService` — to send the email
- `ILogger<Program> logger` — for logging

The endpoint file needs these using statements added (if not already present):
- `using WitchCityRope.Api.Features.Admin.Settings.Interfaces;`
- `using WitchCityRope.Api.Features.Shared.Services;`
- `using Microsoft.EntityFrameworkCore;`

---

## Frontend Implementation

### 1. API Client Additions

**File**: `apps/web/src/services/emailTemplates.api.ts`

Add 3 new methods to the existing `emailTemplatesApi` object:

```typescript
/** Fetch all saved test data variable values */
getTestData: async (): Promise<Record<string, string>> => {
  const response = await apiClient.get('/api/email-templates/test-data');
  return response.data;
},

/** Save test data variable values (upsert) */
saveTestData: async (testData: Record<string, string>): Promise<void> => {
  await apiClient.put('/api/email-templates/test-data', testData);
},

/** Send a test email for a specific template */
sendTestEmail: async (
  templateId: string,
  request: { email: string; variableOverrides?: Record<string, string> }
): Promise<{ message: string; templateType: string; sentTo: string }> => {
  const response = await apiClient.post(
    `/api/email-templates/${templateId}/send-test`,
    request
  );
  return response.data;
},
```

### 2. Test Data Tab Component

**New File**: `apps/web/src/components/email-templates/EmailTestDataTab.tsx`

This component displays ALL email template variables organized by category with editable text inputs, pre-filled from the database. It has a "Save Defaults" button.

**Layout Requirements**:
- Use Mantine `Paper`, `Stack`, `SimpleGrid`, `TextInput`, `Button`, `Text`, `Divider`, `Group`, `Alert` components
- Follow the existing style patterns from `EmailCategoryPanel.tsx` (burgundy color scheme, same border radius, same spacing)
- Group variables into sections with section headers using `Text` component (fw={600}, c="burgundy")
- Use `SimpleGrid cols={{ base: 1, sm: 2 }}` for the variable inputs within each section to keep the layout compact
- Show a loading state with `Loader` while fetching
- Show success/error notifications via `@mantine/notifications`
- Use `useQuery` for fetching test data and `useMutation` for saving

**Variable Groups** (display in this order):

1. **Global** — `user_name`, `system_url`, `custom_message`, `custom_content`
2. **Vetting** — `scene_name`, `application_number`, `submission_date`, `application_date`, `status_change_date`, `current_status`, `interview_link`, `approval_date`, `hold_reason`, `required_actions`, `review_date`
3. **Events** — `attendee_name`, `event_title`, `event_date`, `event_time`, `venue_name`, `venue_address`, `ticket_type`, `total_paid`, `confirmation_number`, `session_name`
4. **Admin** — `account_email`, `reset_url`, `action_required`, `deadline_date`, `verification_url`, `refund_amount`, `original_amount`, `payment_method`, `timing_message`, `refund_reason`, `refund_id`
5. **Incident** — `reporter_name`, `incident_number`, `incident_date`, `coordinator_name`, `next_steps`, `status`
6. **Ad Hoc** — `recipient_name`

**Label formatting**: Convert snake_case variable names to Title Case for labels (e.g., `scene_name` -> "Scene Name", `event_title` -> "Event Title").

**State management**:
- Load saved values via `useQuery(['email-test-data'], emailTemplatesApi.getTestData)`
- Store local edits in a `Record<string, string>` state
- On "Save Defaults", call `emailTemplatesApi.saveTestData(localValues)` via `useMutation`
- Invalidate `['email-test-data']` query on save success

### 3. Send Test Email Component

**New File**: `apps/web/src/components/email-templates/SendTestEmail.tsx`

This component is rendered INSIDE the template editor panel in `EmailCategoryPanel.tsx`, below the Save/Cancel action buttons, ONLY when a template is selected.

**Props**:
```typescript
interface SendTestEmailProps {
  /** The currently selected template (to know which variables to show and which template to send) */
  template: GlobalEmailTemplateDto;
  /** Current edited subject (may differ from saved) */
  currentSubject: string;
  /** Current edited HTML body (may differ from saved) */
  currentHtmlBody: string;
  /** Current edited title (may differ from saved) */
  currentTitle: string;
  /** Callback to trigger saving the template before sending test */
  onSaveTemplate: () => Promise<void>;
  /** Whether the template has unsaved changes */
  hasUnsavedChanges: boolean;
}
```

**Layout**:
- Wrapped in a `Paper` component with the same styling as the editor panel (shadow="sm", radius="md", border)
- Section header: "Send Test Email" in burgundy
- Show ONLY the variables that belong to this template (from `template.variables` array)
- Each variable gets a `TextInput`, pre-filled from the saved test data defaults
- Email address `TextInput` with label "Send to" and placeholder "Enter email address..."
- `Button` "Send Test" with loading state
- Info `Alert` below: "Sends this template with the above values to the specified email address. Variable overrides will be saved as new defaults."

**Behavior**:
1. On mount, fetch saved test data via `useQuery(['email-test-data'])`
2. Parse `template.variables` to get the list of variable names for this template (strip `{{` and `}}` braces)
3. Pre-fill each variable input from the saved test data
4. User can override any value inline
5. When "Send Test" is clicked:
   a. If `hasUnsavedChanges` is true, call `onSaveTemplate()` first and wait for it to complete
   b. Collect all variable values (including any overrides)
   c. Call `emailTemplatesApi.sendTestEmail(template.id, { email, variableOverrides })` where `variableOverrides` contains ALL the variable values shown (not just changed ones — this ensures the test data stays in sync)
   d. Invalidate `['email-test-data']` query on success (since overrides are auto-saved)
   e. Show success/error notification

### 4. Integrate into EmailCategoryPanel

**File**: `apps/web/src/components/email-templates/EmailCategoryPanel.tsx`

**Changes needed**:

1. Import `SendTestEmail` component
2. Add state tracking for unsaved changes:
   ```typescript
   const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
   ```
3. Update `setSubject`, `setHtmlBody`, `setTitle` handlers to also set `hasUnsavedChanges = true`
4. Reset `hasUnsavedChanges` to false in the save mutation `onSuccess` callback and in `handleCancel`
5. Extract save logic into an async function that can be awaited:
   ```typescript
   const handleSaveAsync = async (): Promise<void> => {
     const plainText = generatePlainText(htmlBody);
     await saveMutation.mutateAsync({
       title,
       subject,
       htmlBody,
       plainTextBody: plainText,
     });
   };
   ```
6. Render `SendTestEmail` component AFTER the action buttons Group, still inside the editor Paper, only when `selectedTemplate` is not null:
   ```tsx
   {selectedTemplate && (
     <SendTestEmail
       template={selectedTemplate}
       currentTitle={title}
       currentSubject={subject}
       currentHtmlBody={htmlBody}
       onSaveTemplate={handleSaveAsync}
       hasUnsavedChanges={hasUnsavedChanges}
     />
   )}
   ```

### 5. Integrate Test Data Tab into Admin Page

**File**: `apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx`

**Changes needed**:

1. Import `EmailTestDataTab` component
2. Add a new tab after "Ad Hoc":
   ```tsx
   <Tabs.Tab value="testdata">Test Data</Tabs.Tab>
   ```
3. Add the tab panel:
   ```tsx
   <Tabs.Panel value="testdata" pt="xl">
     <EmailTestDataTab />
   </Tabs.Panel>
   ```

---

## Complete Variable Reference

| Variable Name | Category | Sample Value |
|--------------|----------|--------------|
| user_name | Global | Jane Doe |
| system_url | Global | https://witchcityrope.com |
| custom_message | Global | This is a test custom message... |
| custom_content | Global | This is test custom content... |
| scene_name | Vetting | Dark Phoenix |
| application_number | Vetting | APP-2026-0042 |
| submission_date | Vetting | March 1, 2026 |
| application_date | Vetting | March 1, 2026 |
| status_change_date | Vetting | March 5, 2026 |
| current_status | Vetting | Under Review |
| interview_link | Vetting | https://witchcityrope.com/vetting/interview/sample-link |
| approval_date | Vetting | March 8, 2026 |
| hold_reason | Vetting | Additional references needed |
| required_actions | Vetting | Please provide two community references |
| review_date | Vetting | March 10, 2026 |
| attendee_name | Events | Jane Doe |
| event_title | Events | Introduction to Shibari |
| event_date | Events | Saturday, March 15, 2026 |
| event_time | Events | 7:00 PM EST |
| venue_name | Events | The Witch City Studio |
| venue_address | Events | 123 Essex Street, Salem, MA 01970 |
| ticket_type | Events | General Admission |
| total_paid | Events | $35.00 |
| confirmation_number | Events | WCR-2026-1234 |
| session_name | Events | Beginner Ties - Session A |
| account_email | Admin | testuser@example.com |
| reset_url | Admin | https://witchcityrope.com/auth/reset-password?token=sample-token |
| action_required | Admin | Please update your profile information |
| deadline_date | Admin | March 20, 2026 |
| verification_url | Admin | https://witchcityrope.com/auth/verify-email?token=sample-token |
| refund_amount | Admin | $35.00 |
| original_amount | Admin | $45.00 |
| payment_method | Admin | PayPal |
| timing_message | Admin | Your refund will be processed within 5-10 business days. |
| refund_reason | Admin | Event cancelled by organizer |
| refund_id | Admin | REF-2026-5678 |
| reporter_name | Incident | Jane Doe |
| incident_number | Incident | INC-2026-0001 |
| incident_date | Incident | March 8, 2026 |
| coordinator_name | Incident | Safety Coordinator |
| next_steps | Incident | An investigation has been initiated... |
| status | Incident | Under Investigation |
| recipient_name | Ad Hoc | Community Member |

---

## Files Modified (Summary)

### Backend (7 files)
1. `apps/api/Features/Shared/Services/EmailService.cs` — Change `SubstituteVariables` to `internal static`
2. `apps/api/Features/Admin/Settings/Interfaces/ISettingsService.cs` — Add `UpsertMultipleSettingsAsync`
3. `apps/api/Features/Admin/Settings/Services/SettingsService.cs` — Implement `UpsertMultipleSettingsAsync`
4. `apps/api/Features/EmailTemplates/Endpoints/EmailTemplateEndpoints.cs` — Add 3 endpoints
5. `apps/api/Features/EmailTemplates/Models/SendTestEmailRequest.cs` — NEW file
6. `apps/api/Services/Seeding/SettingsSeeder.cs` — Add `SeedEmailTestDataAsync`
7. `apps/api/Services/Seeding/SeedCoordinator.cs` — Call `SeedEmailTestDataAsync` (check if seeder is called from here)

### Frontend (5 files)
1. `apps/web/src/services/emailTemplates.api.ts` — Add 3 API client methods
2. `apps/web/src/components/email-templates/EmailTestDataTab.tsx` — NEW file
3. `apps/web/src/components/email-templates/SendTestEmail.tsx` — NEW file
4. `apps/web/src/components/email-templates/EmailCategoryPanel.tsx` — Add SendTestEmail integration
5. `apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx` — Add Test Data tab

---

## Style Guide Reference

All new UI components MUST follow these existing patterns from `EmailCategoryPanel.tsx`:

- **Primary color**: burgundy (`c="burgundy"`, `var(--mantine-color-burgundy-6)`)
- **Borders**: `rgba(136, 1, 36, 0.1)`
- **Background highlights**: `rgba(136, 1, 36, 0.05)`
- **Border radius**: `12px` for cards, `md` for Paper
- **Section headers**: `Text` with `fw={600}` and `c="burgundy"`
- **Paper shadows**: `shadow="sm"` with `radius="md"`
- **Spacing**: `gap="md"` inside Stack, `pt="xl"` for tab panels
- **Buttons**: Use default Mantine Button (inherits theme), with `fw={600}` for primary actions
- **Notifications**: Use `@mantine/notifications` `notifications.show()` with `color: 'green'` for success, `color: 'red'` for errors
