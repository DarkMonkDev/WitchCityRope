# Vetting Form Field Reference
**Last Updated**: 2025-12-02
**Purpose**: Single source of truth for vetting application form field names and test selectors

## Current Vetting Application Form Fields

**Location**: `/apps/web/src/features/vetting/components/VettingApplicationForm.tsx`

### Form Fields (with data-testid attributes)

#### Read-Only Fields (Pre-populated from user profile)
- **Scene Name**: `data-testid="scene-name-input"` (readonly/disabled)
- **Email**: `data-testid="email-input"` (readonly/disabled)

#### Required Fields
- **First Name**: `data-testid="first-name-input"`
  - Validation: Required, 1-50 characters
  - Message: "First name is required"

- **Last Name**: `data-testid="last-name-input"`
  - Validation: Required, 1-50 characters
  - Message: "Last name is required"

- **Why Join**: `data-testid="why-join-textarea"`
  - Type: Textarea
  - Validation: Required, max 2000 characters
  - Message: "Please explain why you would like to join Witch City Rope"
  - Placeholder: "Tell us why you would like to join Witch City Rope and what you hope to gain from being part of our community..."

- **Experience with Rope**: `data-testid="experience-with-rope-textarea"`
  - Type: Textarea
  - Validation: Required, max 2000 characters
  - Message: "Please describe your experience with rope bondage"
  - Placeholder: "Tell us about your experience with rope bondage, BDSM, or kink communities..."

- **Community Standards Agreement**: `data-testid="community-standards-checkbox"`
  - Type: Checkbox
  - Validation: Must be checked (true)
  - Message: "You must agree to all community standards to submit your application"

#### Optional Fields
- **Pronouns**: `data-testid="pronouns-input"`
  - Validation: Max 50 characters
  - Description: "How you'd like to be referred to (e.g., she/her, they/them)"

- **FetLife Handle**: `data-testid="fetlife-handle-input"`
  - Validation: Max 50 characters
  - Description: "Optional - helps us verify community connections"

- **Other Names**: `data-testid="other-names-textarea"`
  - Type: Textarea
  - Validation: Max 500 characters
  - Description: "Any other names, nicknames, or social media handles you have used in a kinky context"

#### Submit Button
- **Submit Application**: `data-testid="submit-application-button"`

---

## OLD Field Names (DEPRECATED - DO NOT USE)

These field names were from the previous vetting form implementation and are NO LONGER VALID:

### ❌ REMOVED FIELDS (do not use in tests):
- `realName` → Use `firstName` + `lastName` instead
- `experience` → Use `experienceWithRope` instead
- `agreedToGuidelines` → Use `agreeToCommunityStandards` instead
- `email-or-scenename-input` → Use `email-input` and `scene-name-input` separately

---

## Profile Settings Page Fields

**Location**: `/apps/web/src/pages/dashboard/ProfileSettingsPage.tsx`

### Personal Info Tab
- **Scene Name**: `data-testid="scene-name-input"`
- **First Name**: `data-testid="first-name-input"`
- **Last Name**: `data-testid="last-name-input"`
- **Email**: `data-testid="email-input"`
- **Pronouns**: `data-testid="pronouns-input"`
- **Discord Username**: `data-testid="discord-name-input"`
- **FetLife Username**: `data-testid="fetlife-name-input"`
- **Phone Number**: `data-testid="phone-number-input"`
- **Other Names**: `data-testid="other-names-input"`
- **Bio**: `data-testid="bio-input"`

### Tab Navigation
- **Personal Tab**: `button[role="tab"]` with text "Personal"
- **Security Tab**: `button[role="tab"]` with text "Change Password" or "Security"
- **Vetting Tab**: `button[role="tab"]` with text "Vetting"

---

## Form Validation Schema

**Location**: `/apps/web/src/features/vetting/schemas/simplifiedApplicationSchema.ts`

### Validation Rules Summary
```typescript
{
  firstName: min(1), max(50), trim, required
  lastName: min(1), max(50), trim, required
  pronouns: max(50), optional
  fetLifeHandle: max(50), optional
  otherNames: max(500), optional
  whyJoin: max(2000), trim, required (length > 0)
  experienceWithRope: max(2000), trim, required (length > 0)
  agreeToCommunityStandards: boolean, required (must be true)
}
```

---

## Test Writing Guidelines

### DO ✅
```typescript
// Use data-testid attributes from actual components
await page.getByTestId('first-name-input').fill('John');
await page.getByTestId('last-name-input').fill('Doe');
await page.getByTestId('why-join-textarea').fill('I want to learn...');
await page.getByTestId('experience-with-rope-textarea').fill('I have experience...');
await page.getByTestId('community-standards-checkbox').check();
await page.getByTestId('submit-application-button').click();
```

### DON'T ❌
```typescript
// Don't use old field names
await page.locator('input[name="realName"]').fill('John Doe'); // WRONG
await page.locator('textarea[name="experience"]').fill('...'); // WRONG
await page.locator('input[name="agreedToGuidelines"]').check(); // WRONG

// Don't guess at selectors
await page.locator('.form-input').fill('...'); // TOO GENERIC
```

---

## Modal Selectors (Mantine Components)

### Mantine Modal
```typescript
const modal = page.locator('[role="dialog"]')
  .or(page.locator('[class*="mantine-Modal"]'))
  .first();
```

### Mantine Notifications (Success/Error Messages)
```typescript
const notification = page.locator('[class*="mantine-Notification"]')
  .filter({ hasText: /success|error/i })
  .first();
```

### Mantine Form Validation Errors
```typescript
const errors = page.locator('[class*="mantine-Input-error"]')
  .or(page.locator('text=/required|must be/i'));
```

---

## Migration Checklist for Old Tests

When updating tests from old field structure:

- [ ] Replace `realName` with `firstName` + `lastName`
- [ ] Replace `experience` with `experienceWithRope`
- [ ] Replace `agreedToGuidelines` with `agreeToCommunityStandards`
- [ ] Update selectors to use `data-testid` attributes
- [ ] Update validation error messages to match new schema
- [ ] Test against Docker environment on port 5173
- [ ] Update TEST_CATALOG with changes

---

## Related Documentation

- **Vetting Application Component**: `/apps/web/src/features/vetting/components/VettingApplicationForm.tsx`
- **Vetting Schema**: `/apps/web/src/features/vetting/schemas/simplifiedApplicationSchema.ts`
- **Profile Settings Page**: `/apps/web/src/pages/dashboard/ProfileSettingsPage.tsx`
- **Testing Guide**: `/docs/standards-processes/testing/TESTING_GUIDE.md`
- **Playwright Guide**: `/docs/standards-processes/testing/browser-automation/playwright-guide.md`

---

## Change Log

### 2025-12-02
- **Created** initial field reference document
- **Documented** current vetting form structure (simplified form)
- **Documented** profile settings page field structure
- **Added** migration guide for old tests
- **Added** Mantine component selectors
