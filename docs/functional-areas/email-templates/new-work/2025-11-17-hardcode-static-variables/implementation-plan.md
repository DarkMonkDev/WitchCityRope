# Email Template Static Variables Hardcoding - Implementation Plan

**Date**: 2025-11-17
**Status**: Ready for Implementation
**Complexity**: Medium

## Problem Statement

Currently, email templates use variables like `{{support_email}}`, `{{contact_email}}`, and `{{system_url}}` that never change. Every service that sends emails must populate these same static values in the variables dictionary, leading to:

- Repetitive code across all email-sending services
- Error-prone (easy to forget to include static variables)
- Confusing UX (admin sees `{{support_email}}` instead of actual email address)
- Unnecessary complexity (no need for variables that never change)

## Solution

Hardcode static configuration values directly in email templates. Keep variables only for truly dynamic content that changes per email.

## Static Variables to Hardcode

| Variable | Hardcoded Value | Used In |
|----------|----------------|---------|
| `{{support_email}}` | `support@witchcityrope.com` | Admin, Incident templates |
| `{{contact_email}}` | `info@witchcityrope.com` | Vetting templates |
| `{{organizer_email}}` | `events@witchcityrope.com` | Events templates |
| `{{system_url}}` | `https://witchcityrope.com` | Admin templates |

## Dynamic Variables to Keep

These change per email and should remain as variables:
- `{{scene_name}}`, `{{user_name}}`, `{{attendee_name}}` - recipient names
- `{{event_title}}`, `{{event_date}}`, `{{event_time}}` - event details
- `{{application_number}}`, `{{incident_number}}` - unique IDs
- `{{submission_date}}`, `{{approval_date}}`, etc. - contextual dates
- `{{refund_amount}}`, `{{ticket_type}}` - transaction details
- `{{custom_message}}`, `{{action_required}}` - dynamic content

## Implementation Phases

### Phase 1: Backend - Email Template Seeder
**Agent**: backend-developer
**Priority**: High (blocks other work)

**Tasks**:
1. Update `EmailTemplateSeeder.cs` - replace static variables in all 23 templates
2. Update `Variables` JSON field to remove static variables (documentation only)
3. Create migration to update existing database records

**Files**:
- `apps/api/Services/Seeding/EmailTemplateSeeder.cs`
- New migration file

**Expected outcome**: All seeded templates have hardcoded values, Variables field updated

### Phase 2: Backend - Email Services
**Agent**: backend-developer
**Priority**: High (follows Phase 1)

**Tasks**:
1. Update `VettingEmailService.cs` - remove static variables from dictionaries
2. Update `AuthenticationService.cs` - remove static variables from dictionaries
3. Update `RefundService.cs` - remove static variables from dictionaries
4. Update any other email-sending services

**Files**:
- `apps/api/Features/Vetting/Services/VettingEmailService.cs`
- `apps/api/Features/Authentication/Services/AuthenticationService.cs`
- `apps/api/Features/Payments/Services/RefundService.cs`
- Search for all `SendTemplatedEmailAsync` calls

**Expected outcome**: No services populate static variables in dictionaries

### Phase 3: Frontend - Template Editor UI
**Agent**: react-developer
**Priority**: Medium

**Tasks**:
1. Update email template admin page to show updated variable lists
2. Remove static variables from "Available Variables" documentation/help text
3. Update any tooltips or help text that mentions static variables

**Files**:
- `apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx`
- `apps/web/src/components/email-templates/EmailCategoryPanel.tsx`

**Expected outcome**: UI no longer suggests using static variables

### Phase 4: Testing
**Agent**: test-developer
**Priority**: Medium

**Tasks**:
1. Update unit tests that mock `SendTemplatedEmailAsync` to remove static variables
2. Update integration tests for email sending
3. Add test to verify templates contain hardcoded values (not variables)

**Files**:
- `tests/unit/api/Features/Auth/AuthenticationServiceTests.cs`
- `tests/unit/api/Features/Payments/Services/RefundServiceEmailTests.cs`
- `tests/integration/api/Features/EmailTemplates/EmailTemplateEndpointsIntegrationTests.cs`

**Expected outcome**: All tests pass with hardcoded values

## Testing Strategy

**Unit Tests**:
- Verify services no longer pass static variables
- Verify EmailService.SubstituteVariables still works for dynamic variables

**Integration Tests**:
- Send test emails and verify hardcoded values appear correctly
- Verify templates load from database with hardcoded values

**Manual Testing**:
1. Restart database with new seeded templates
2. Send test email from each category
3. Verify emails contain correct hardcoded values
4. Admin edits template - verify can see/edit hardcoded values

## Rollback Plan

If issues discovered:
1. Database migration can be rolled back
2. Old seeder values preserved in git history
3. Services can re-add static variables if needed

## Success Criteria

- ✅ All 23 email templates have static values hardcoded
- ✅ No services populate `support_email`, `contact_email`, `organizer_email`, `system_url` in variables
- ✅ Email template editor UI doesn't suggest using static variables
- ✅ All tests pass
- ✅ Test emails sent successfully with correct contact information

## Risk Assessment

**Low Risk**:
- Simple string replacement in templates
- No functional logic changes
- Easy to rollback via migration

**Mitigation**:
- Comprehensive testing before deployment
- Manual smoke test of each email category
- Keep old values in git history

## Timeline

**Estimated**: 4-6 hours total
- Phase 1 (Backend Seeder): 1-2 hours
- Phase 2 (Backend Services): 1-2 hours
- Phase 3 (Frontend UI): 1 hour
- Phase 4 (Testing): 1-2 hours

## Dependencies

- Phase 2 depends on Phase 1 (need updated templates in database)
- Phase 3 can run in parallel with Phase 2
- Phase 4 runs after Phases 1-3 complete

## Notes

- This is a simplification, not a feature addition
- Reduces cognitive load for developers writing email-sending code
- Makes templates more transparent for admins
- No user-facing changes (emails look identical)
