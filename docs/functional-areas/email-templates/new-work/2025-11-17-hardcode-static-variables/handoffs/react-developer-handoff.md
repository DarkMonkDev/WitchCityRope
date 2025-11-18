# React Developer Handoff - Update Email Template Editor UI

**Date**: 2025-11-17
**Agent**: react-developer
**Estimated Time**: 1 hour
**Priority**: Medium (can run in parallel with backend Phase 2)

## Context

We're removing static variables (`{{support_email}}`, `{{contact_email}}`, `{{organizer_email}}`, `{{system_url}}`) from email templates. Admins should see actual hardcoded values in templates instead of placeholder variables.

The email template editor UI currently shows these static variables in documentation/help text. We need to update the UI to reflect the new approach.

## Your Tasks

### Task 1: Update Available Variables Display

**Files to Review**:
- `apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx`
- `apps/web/src/components/email-templates/EmailCategoryPanel.tsx`

**What to Look For**:
1. Any "Available Variables" help text or documentation
2. Variable lists shown to admins
3. Tooltips or hints about using variables

**What to Update**:
Remove references to static variables from any documentation. For example:

**Before** (if exists):
```typescript
const availableVariables = {
  vetting: [
    { name: '{{scene_name}}', description: 'Applicant scene name' },
    { name: '{{contact_email}}', description: 'Organization contact email' }, // REMOVE
    { name: '{{application_number}}', description: 'Unique application ID' },
  ],
  admin: [
    { name: '{{user_name}}', description: 'User name' },
    { name: '{{support_email}}', description: 'Support email address' }, // REMOVE
    { name: '{{system_url}}', description: 'System URL' }, // REMOVE
  ],
};
```

**After**:
```typescript
const availableVariables = {
  vetting: [
    { name: '{{scene_name}}', description: 'Applicant scene name' },
    { name: '{{application_number}}', description: 'Unique application ID' },
    // contact_email is now hardcoded as info@witchcityrope.com
  ],
  admin: [
    { name: '{{user_name}}', description: 'User name' },
    // support_email is now hardcoded as support@witchcityrope.com
    // system_url is now hardcoded as https://witchcityrope.com
  ],
};
```

**Add helpful note** (if there's a help section):
```typescript
<Alert variant="info" mt="md">
  <Text size="sm">
    <strong>Variables:</strong> Use {'{{'} and {'}}}'} to insert dynamic content.
    Static values like contact emails are already included in templates.
  </Text>
  <Text size="sm" mt="xs">
    <strong>Available variables:</strong> {availableVariables.join(', ')}
  </Text>
</Alert>
```

### Task 2: Update Variables Field Display

**Context**: Templates have a `Variables` JSON field that lists available variables. This field is now smaller (no static variables).

**What to Check**:
- Does the UI display the `Variables` field from the template?
- Is it read-only or editable?
- Does it need any updates?

**Current Behavior** (backend updated):
```typescript
// Template DTO from API now returns smaller Variables array
{
  id: "...",
  templateType: "ApplicationReceived",
  variables: ["{{scene_name}}", "{{application_number}}", "{{submission_date}}"],
  // NO LONGER includes: "{{contact_email}}"
}
```

**What to Verify**:
- UI correctly displays the updated Variables array
- No hardcoded lists that need updating
- If UI has its own variable definitions, update them to match backend

### Task 3: Search for Hardcoded References

Run searches to find any hardcoded mentions of removed variables:

```bash
# From apps/web directory
grep -rn "support_email" src/pages/admin/
grep -rn "contact_email" src/pages/admin/
grep -rn "organizer_email" src/pages/admin/
grep -rn "system_url" src/pages/admin/

# Check components
grep -rn "support_email" src/components/email-templates/
grep -rn "contact_email" src/components/email-templates/
```

Update any documentation strings, comments, or hardcoded lists.

## Testing Your Changes

**Manual UI Testing**:
1. Navigate to Email Templates admin page
2. Click through each category (Vetting, Events, Admin, Incident)
3. Verify "Available Variables" help text doesn't mention removed variables
4. Open a template for editing
5. Verify you can see hardcoded email addresses in the template body (e.g., `support@witchcityrope.com`)
6. Verify Variables list is smaller (no static variables)

**Visual Regression Check**:
- Take screenshots before/after changes
- Ensure UI layout still looks good with smaller variable lists
- Verify no broken tooltips or help text

## Expected Outcomes

**Before** (what admin currently sees):
```
Available Variables for Vetting Templates:
- {{scene_name}} - Applicant scene name
- {{application_number}} - Application ID
- {{contact_email}} - Organization contact email  ← REMOVE
- {{submission_date}} - Date submitted
```

**After** (what admin should see):
```
Available Variables for Vetting Templates:
- {{scene_name}} - Applicant scene name
- {{application_number}} - Application ID
- {{submission_date}} - Date submitted

Note: Contact email (info@witchcityrope.com) is hardcoded in templates.
```

## Success Criteria

- ✅ No references to `{{support_email}}`, `{{contact_email}}`, `{{organizer_email}}`, `{{system_url}}` in UI documentation
- ✅ Available variables lists updated to show only dynamic variables
- ✅ Help text or tooltips updated (if applicable)
- ✅ Admin can see hardcoded email addresses in template editor
- ✅ UI tests pass (if any exist for email template pages)

## Files You May Modify

**Likely**:
- `apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx`
- `apps/web/src/components/email-templates/EmailCategoryPanel.tsx`

**Possibly** (if they exist):
- Any constants files that define variable lists
- Any help/documentation components

## Integration Points

**Backend Changes** (backend-developer is handling):
- Template `Variables` field now excludes static variables
- Templates contain hardcoded email addresses in HTML/text body

**Your Changes Should**:
- Reflect the updated Variables arrays from API
- Not break if admin edits template with hardcoded values
- Make it clear to admin that contact info is hardcoded (not variable)

## Questions or Issues?

If you encounter:
- **Variable lists in multiple places**: Update all of them for consistency
- **Generated documentation**: If variables list is auto-generated from template data, it will auto-update
- **Unclear if something should be updated**: Err on the side of removing references to static variables

## Next Steps After Completion

1. Commit your UI changes
2. Test manually by navigating through email template admin
3. Report back with screenshots showing updated UI
4. Note any UX improvements discovered during implementation

**Implementation Plan**: `/docs/functional-areas/email-templates/new-work/2025-11-17-hardcode-static-variables/implementation-plan.md`
