# CRITICAL: Safety Incident Report Form - Bug Analysis

## Status: BLOCKING BUG - Form Submit Does Nothing

**Date**: November 11, 2025  
**Severity**: CRITICAL (PRODUCTION BUG)  
**Impact**: Users cannot submit safety incident reports - button click has no effect

---

## SYMPTOMS REPORTED
1. ✗ Clicking "Submit Safety Report" button produces NO response
2. ✗ No console activity logged
3. ✗ No error messages displayed
4. ✗ No validation error summary shown when validation fails
5. ✗ Form appears to be unresponsive

---

## ROOT CAUSE ANALYSIS

### BUG #1: Agreement Checkbox Creates Silent Submission Blocker
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/features/safety/components/IncidentReportForm.tsx`

**Lines 113-124** - The `handleSubmit` function:
```typescript
const handleSubmit = useCallback(async (values: IncidentFormData) => {
  if (!agreementChecked) {
    return;  // ⚠️ SILENT RETURN - NO ERROR MESSAGE!
  }

  try {
    const result = await submitIncident(values, user?.id);
    onSubmissionComplete?.((result as any).referenceNumber);
  } catch (error) {
    console.error('Submission failed:', error);
  }
}, [submitIncident, user?.id, agreementChecked, onSubmissionComplete]);
```

**Problem**: When `agreementChecked` is `false`, the function returns silently with NO validation error displayed. Users don't know WHY the form won't submit.

**Line 446** - Button disabled logic:
```typescript
disabled={!agreementChecked}
```

**Problem**: Button is disabled if agreement is unchecked, BUT there's no visual feedback about WHAT needs to be agreed to. Users click the button, nothing happens, they don't understand why.

---

### BUG #2: No Error Summary Display
**Location**: Lines 433-439 only show API submission errors

```typescript
{error && (
  <Alert variant="light" color="red" mb="md">
    <Text size="sm">
      Failed to submit report: {error instanceof Error ? error.message : 'Unknown error occurred'}
    </Text>
  </Alert>
)}
```

**Problem**: This ONLY displays API errors. It does NOT display Mantine form validation errors (missing required fields, character limits, etc.). The form has built-in validators (lines 83-109) but their errors are never displayed to users.

---

### BUG #3: Form Validation Errors Not Displayed Anywhere
**Location**: Lines 83-109 define validation rules:

```typescript
validate: {
  incidentDate: (value) => {
    if (!value) return 'Incident date is required';
    return null;
  },
  location: (value) => {
    if (!value || value.length < 3) {
      return 'Location details are required (minimum 3 characters)';
    }
    return null;
  },
  description: (value) => {
    if (!value || value.length < 50) {
      return 'Description must be at least 50 characters';
    }
    if (value.length > 5000) {
      return 'Description must be less than 5000 characters';
    }
    return null;
  },
  contactEmail: (value, values) => {
    if (!values.isAnonymous && (!value || !value.includes('@'))) {
      return 'Valid email address is required for identified reports';
    }
    return null;
  }
}
```

**Problem**: These validation messages are defined but NEVER shown to users. Mantine form input components (`MantineTextInput`, `MantineTextarea`, `MantineSelect`) have error display built-in via the `error` prop from `form.getInputProps()`, but the form component doesn't include visual feedback for validation errors.

---

## AFFECTED FILES

### Primary Form Component
- **Path**: `/home/chad/repos/witchcityrope/apps/web/src/features/safety/components/IncidentReportForm.tsx`
- **Problematic Lines**:
  - **113-124**: `handleSubmit` callback with silent return on unchecked agreement
  - **171**: Form submission with no validation error handling
  - **433-439**: Error display that only shows API errors, not validation errors
  - **446**: Button disabled state with no visual explanation

### Related Hook (No bugs - works correctly)
- **Path**: `/home/chad/repos/witchcityrope/apps/web/src/features/safety/hooks/useSubmitIncident.ts`
- **Status**: ✓ Correctly awaits mutation and sets submission result
- **Status**: ✓ Correctly catches and logs errors

### Related Hook (No bugs - works correctly)
- **Path**: `/home/chad/repos/witchcityrope/apps/web/src/features/safety/hooks/useSafetyIncidents.ts`
- **Status**: ✓ Correctly implements React Query mutation
- **Status**: ✓ Correctly handles success and error callbacks

### API Service (No bugs - works correctly)
- **Path**: `/home/chad/repos/witchcityrope/apps/web/src/features/safety/api/safetyApi.ts`
- **Status**: ✓ Correctly posts to `/api/safety/incidents`
- **Status**: ✓ Correctly handles response and errors

---

## DETAILED FLOW BREAKDOWN

### Current (Broken) Flow:
1. User fills out form
2. User checks "I understand this report may trigger safety team investigation" checkbox
3. User clicks "Submit Safety Report" button
4. Form calls `form.onSubmit(handleSubmit)` 
5. **NO VALIDATION ERROR CHECK** - Mantine just calls handleSubmit if form.onSubmit is triggered
6. handleSubmit checks if `agreementChecked === true`
7. **IF FALSE**: Silent return - nothing happens, no console log, no error message
8. **IF TRUE**: Proceeds to call submitIncident
9. If API fails: Error Alert appears (lines 433-439)

### Issue: Validation errors are never checked before submission
Mantine's `form.onSubmit()` doesn't validate automatically - you must check `form.values` against the validation rules manually or Mantine must be configured to validate on submit.

---

## MISSING ERROR DISPLAYS

### Missing #1: Validation Error Summary
No component displays validation errors from `form.errors` object. The form has validators but no error summary.

**Example of what's missing**: An error summary component that shows all validation errors when submit is attempted.

### Missing #2: Agreement Requirement Indication
The agreement checkbox has no:
- Visual indicator that it's REQUIRED
- Error message when unchecked
- Disabled button explanation

### Missing #3: Field-Level Error Messages
Mantine `TextInput`, `Textarea`, and `Select` components receive `error` prop via `form.getInputProps()`, but there's no guarantee these are being displayed by the custom `MantineTextInput` component.

---

## SUBMISSION FLOW (from working components)

**NOTE**: These components work correctly - the bug is in how the form handles submission:

1. User clicks submit
2. `form.onSubmit(handleSubmit)` is called
3. handleSubmit receives form values
4. `submitIncident(values, user?.id)` is called (line 119)
5. Hook calls `submitMutation.mutateAsync(request)` (useSubmitIncident.ts:59)
6. API client calls `POST /api/safety/incidents` (safetyApi.ts:28-31)
7. Response is returned and submission result is set
8. Component shows success state (SubmissionConfirmation)

**This flow IS WORKING** - the issue is the form isn't getting values to it due to the silent return bug.

---

## CODE EXHIBITS

### Exhibit A: Silent Return (Line 113-116)
```typescript
const handleSubmit = useCallback(async (values: IncidentFormData) => {
  if (!agreementChecked) {
    return;  // ⚠️ BUG: Silent return, no error feedback
  }
```

**Expected Behavior**: Should either:
- Make agreement part of form validation (not state)
- Display error if agreement not checked
- Prevent button from being clickable AND show why

**Current Behavior**: Button appears clickable (it's not, disabled), but user has no feedback.

### Exhibit B: Button Disabled With No Explanation (Line 446)
```typescript
<Button
  type="submit"
  size="lg"
  loading={isSubmitting}
  disabled={!agreementChecked}  // ⚠️ Why is this disabled? No tooltip or message
  style={{
    background: 'linear-gradient(135deg, #FFBF00 0%, #DAA520 100%)',
    border: 'none'
  }}
>
  Submit Safety Report
</Button>
```

**Expected**: Disabled state should have a title or tooltip explaining why.

### Exhibit C: No Validation Error Display
The form validators (lines 83-109) are defined but never displayed anywhere on the form.

---

## MANTINE FORM VALIDATION PATTERN

**Standard Mantine Pattern**:
```typescript
// Form validation happens automatically in form.getInputProps()
<TextInput 
  label="Email"
  error={form.errors.contactEmail}  // ✓ Shows validation error
  {...form.getInputProps('contactEmail')}
/>

// OR display error summary
{Object.keys(form.errors).length > 0 && (
  <Alert color="red">
    <Text>Please fix the following errors:</Text>
    <ul>
      {Object.entries(form.errors).map(([field, error]) => (
        <li key={field}>{error}</li>
      ))}
    </ul>
  </Alert>
)}
```

**Current Implementation**: Form has validators but no error display.

---

## REQUIRED FIXES (PRIORITY ORDER)

### FIX #1: Display Validation Errors
Add error summary alert above submit button showing all validation errors.

### FIX #2: Make Agreement Part of Form Validation
Move agreement checkbox validation to the form's validate rules, so it shows an error message.

### FIX #3: Add Tooltip to Disabled Button
When button is disabled, show user WHY (missing agreement, validation errors, etc.).

### FIX #4: Console Logging
Add console.log statements when:
- User clicks submit
- Form validation fails
- handleSubmit is called
- Agreement check fails
- API submission starts
- API submission succeeds/fails

---

## CONSOLE DEBUGGING CHECKLIST

When investigating, check:

1. Open DevTools Console (F12)
2. Fill form with valid data
3. Click Submit
4. **Expected console output**:
   - "Form submission started"
   - "Form validation passed/failed"
   - "Agreement checked: true/false"
   - "API call to /api/safety/incidents starting"
   - "Incident submitted successfully: [refNum]" (from useSafetyIncidents.ts:42)

5. **If nothing appears**: The handleSubmit function is never being called

---

## AFFECTED PAGES

1. `/safety-report` - Public safety report submission (uses IncidentReportForm)
2. `/admin/incidents` - Incident list view (doesn't submit but displays incidents)
3. `/my-reports` - User's submitted reports (view only)

---

## TESTING CHECKLIST

After fixes, test:

- [ ] ✓ Fill form incompletely, click submit - validation errors appear
- [ ] ✓ Leave agreement unchecked, click submit - specific error message appears
- [ ] ✓ Check agreement, fill form completely, click submit - API call made
- [ ] ✓ Console shows proper debug logs
- [ ] ✓ Disabled button shows tooltip on hover
- [ ] ✓ API error displays in error alert
- [ ] ✓ Success page appears after submission

---

## QUICK REFERENCE

| Component | Status | Location |
|-----------|--------|----------|
| IncidentReportForm | ✗ BUG: No error display | `/features/safety/components/IncidentReportForm.tsx` |
| handleSubmit | ✗ BUG: Silent return | Lines 113-124 |
| useSubmitIncident | ✓ WORKS | `/features/safety/hooks/useSubmitIncident.ts` |
| useSafetyIncidents | ✓ WORKS | `/features/safety/hooks/useSafetyIncidents.ts` |
| safetyApi | ✓ WORKS | `/features/safety/api/safetyApi.ts` |

