# E2E Persistence Testing Guide

**Last Updated:** 2025-10-09
**Version:** 1.0
**Status:** PRODUCTION
**Owner:** Test Team

## Overview

This guide explains how to use the E2E persistence test templates to catch bugs where UI shows success but database isn't updated. These templates were created in response to two critical production bugs:

1. **Profile Update Bug**: UI showed success, but database wasn't updated (backend silently ignored fields)
2. **Ticket Cancellation Bug**: UI showed success, but database wasn't updated (frontend called wrong endpoint)

Both bugs would have been caught by following the persistence testing pattern described here.

---

## The Persistence Testing Pattern

### Problem Category

**Symptom**: User performs action → UI shows success → Page refresh reveals failure

**Root Causes**:
- Backend ignores fields (validation failure, missing mapping)
- Frontend calls wrong endpoint (404 ignored)
- Database transaction rollback not detected
- API returns success but doesn't save

**Impact**: Data loss, user trust erosion, support burden

### Solution: Comprehensive Persistence Verification

**Standard Test Pattern**:
```
1. Setup: Login, navigate, prepare state
2. Action: Perform operation (update, delete, create)
3. Verify UI: Success message, button state changes
4. Verify API: HTTP 200/204, correct response data
5. Verify Database: Query database directly (CRITICAL)
6. Refresh: Reload page to clear client state
7. Verify Persistence: UI still shows changes (CRITICAL)
8. Verify Database Again: Confirm still correct
```

**Why This Works**:
- **Step 5** catches backend failures even when API returns 200
- **Step 7** catches client-side caching masking backend failures
- **Step 8** ensures database changes are durable

---

## Available Templates

### Location

All templates are in `/apps/web/tests/playwright/templates/`

### Templates

#### 1. Base Persistence Template

**File**: `persistence-test-template.ts`

**Purpose**: Reusable framework for all persistence tests

**Key Features**:
- Standardized test flow
- Screenshot capture at each step
- API response monitoring
- Database verification helpers
- Detailed logging

**Usage**:
```typescript
import { PersistenceTestTemplate, TestConfig } from './templates/persistence-test-template';

const config: TestConfig = {
  description: 'Your test description',
  setup: async (page) => { /* Setup code */ },
  action: async (page) => { /* Perform action */ },
  verifyUiSuccess: async (page) => { /* Check UI */ },
  verifyDatabaseState: async (page) => { /* Check DB */ },
  verifyPersistence: async (page) => { /* Check after refresh */ },
};

await PersistenceTestTemplate.runTest(page, config, options);
```

#### 2. Profile Update Persistence Template

**File**: `profile-update-persistence-template.ts`

**Purpose**: Test profile field updates persist to database

**Catches**: Backend ignoring new fields, validation failures

**Usage**:
```typescript
import { testProfileUpdatePersistence } from './templates/profile-update-persistence-template';

await testProfileUpdatePersistence(page, {
  userEmail: 'test@example.com',
  userPassword: 'password',
  updatedFields: {
    firstName: 'NewFirst',
    lastName: 'NewLast',
    bio: 'Updated bio',
  },
});
```

**What It Tests**:
- ✅ UI shows success message
- ✅ API returns 200 OK
- ✅ Database contains updated values
- ✅ Page refresh shows updated values
- ✅ Database still correct after refresh

#### 3. Ticket Cancellation Persistence Template

**File**: `ticket-cancellation-persistence-template.ts`

**Purpose**: Test ticket cancellations persist to database

**Catches**: Wrong endpoint calls, 404 errors ignored, status not updated

**Usage**:
```typescript
import { testTicketCancellationPersistence } from './templates/ticket-cancellation-persistence-template';

await testTicketCancellationPersistence(page, {
  userEmail: 'test@example.com',
  userPassword: 'password',
  eventId: 'event-id-here',
  cancellationReason: 'Test reason',
});
```

**What It Tests**:
- ✅ UI shows success message
- ✅ Cancel button disappears/disabled
- ✅ API returns 204 No Content or 200 OK
- ✅ Database shows status = 'Cancelled'
- ✅ Page refresh doesn't show Cancel button again
- ✅ Database still shows 'Cancelled'

#### 4. RSVP Persistence Template

**File**: `rsvp-persistence-template.ts`

**Purpose**: Test RSVP and RSVP cancellation persistence

**Usage**:
```typescript
import { testRsvpPersistence, testCancelRsvpPersistence } from './templates/rsvp-persistence-template';

// Test RSVP
await testRsvpPersistence(page, {
  userEmail: 'test@example.com',
  userPassword: 'password',
  eventId: 'event-id-here',
});

// Test RSVP cancellation
await testCancelRsvpPersistence(page, {
  userEmail: 'test@example.com',
  userPassword: 'password',
  eventId: 'event-id-here',
});
```

#### 5. Event Creation Persistence Template

**File**: `event-creation-persistence-template.ts`

**Purpose**: Test admin event creation persists all fields

**Usage**:
```typescript
import { testEventCreationPersistence } from './templates/event-creation-persistence-template';

const eventId = await testEventCreationPersistence(page, {
  adminEmail: 'admin@example.com',
  adminPassword: 'password',
  eventData: {
    title: 'Test Event',
    description: 'Test description',
    eventType: 'Workshop',
    location: 'Test Location',
    startDate: new Date('2025-12-01'),
    endDate: new Date('2025-12-02'),
    capacity: 20,
  },
});
```

---

## Database Helpers

### Location

`/apps/web/tests/playwright/utils/database-helpers.ts`

### Key Functions

#### Profile Verification

```typescript
import { DatabaseHelpers } from './utils/database-helpers';

// Verify profile fields match expected values
const profile = await DatabaseHelpers.verifyProfileFields(userId, {
  firstName: 'Expected',
  lastName: 'Values',
  bio: 'Expected bio',
});

// Get user ID from email
const userId = await DatabaseHelpers.getUserIdFromEmail('test@example.com');
```

#### Event Participation Verification

```typescript
// Verify participation exists with correct status
const participation = await DatabaseHelpers.verifyEventParticipation(
  userId,
  eventId,
  'Registered' // or 'Cancelled', 'Confirmed', 'NoShow'
);

// Verify NO participation exists
await DatabaseHelpers.verifyNoEventParticipation(userId, eventId);
```

#### Event Verification

```typescript
// Verify event exists
const event = await DatabaseHelpers.verifyEventExists(eventId);

// Get events by creator
const events = await DatabaseHelpers.getEventsByCreator(userId);
```

#### Audit Log Verification

```typescript
// Verify audit log entry exists
const exists = await DatabaseHelpers.verifyAuditLogExists(
  'ParticipationHistory',
  participationId,
  'Cancelled'
);
```

---

## When to Add Persistence Tests

### Required For

✅ **Any CRUD operation** (Create, Read, Update, Delete)
✅ **User profile updates**
✅ **Event registration/cancellation**
✅ **Payment processing**
✅ **Status changes** (vetting, approval, etc.)
✅ **Settings updates**
✅ **Data imports/exports**

### Not Required For

❌ **Read-only operations** (viewing data)
❌ **UI-only state** (modal open/close)
❌ **Client-side sorting/filtering**
❌ **Navigation** (unless it changes state)

---

## Best Practices

### 1. Test Both Success and Failure Paths

```typescript
// Success path
await testProfileUpdatePersistence(page, { ... });

// Failure path
await testProfileUpdateExpectingError(page, {
  updatedFields: { firstName: '' }, // Invalid - required field
  expectedError: 'First name is required',
});
```

### 2. Use Unique Test Data

```typescript
const timestamp = Date.now();
const testData = {
  firstName: `Test${timestamp}`,
  email: `test-${timestamp}@example.com`,
};
```

### 3. Clean Up Test Data

```typescript
test.afterEach(async () => {
  await DatabaseHelpers.cleanupTestData('Users', testUserIds);
  await DatabaseHelpers.closeDatabaseConnections();
});
```

### 4. Take Screenshots for Debugging

```typescript
await testProfileUpdatePersistence(page, {
  userEmail: 'test@example.com',
  userPassword: 'password',
  updatedFields: { ... },
  screenshotPath: '/tmp/profile-test', // Custom path
});
```

### 5. Use Descriptive Test Names

```typescript
// ✅ Good
test('should persist profile bio update with special characters to database');

// ❌ Bad
test('test profile update');
```

### 6. Verify Database Directly

```typescript
// ✅ Good - Direct database verification
const dbProfile = await DatabaseHelpers.verifyProfileFields(userId, expectedFields);

// ❌ Bad - Only checking UI
await expect(page.locator('#firstName')).toHaveValue('Expected');
```

---

## Example: Creating a New Persistence Test

### Scenario: Test notification settings persist

**Step 1**: Create test data

```typescript
const notificationSettings = {
  emailNotifications: true,
  smsNotifications: false,
  pushNotifications: true,
};
```

**Step 2**: Define test config

```typescript
import { PersistenceTestTemplate, TestConfig } from './templates/persistence-test-template';
import { DatabaseHelpers } from './utils/database-helpers';

const config: TestConfig = {
  description: 'Notification settings persistence',

  setup: async (page) => {
    // Login
    await page.goto('http://localhost:5173/login');
    // ... login code ...

    // Navigate to settings
    await page.goto('http://localhost:5173/settings/notifications');
  },

  action: async (page) => {
    // Update settings
    await page.locator('#emailNotifications').check();
    await page.locator('#smsNotifications').uncheck();
    await page.locator('#pushNotifications').check();

    // Save
    await page.locator('button[type="submit"]').click();
  },

  verifyUiSuccess: async (page) => {
    // Check success message
    await expect(page.locator('.alert-success')).toContainText('Settings saved');
  },

  verifyDatabaseState: async (page) => {
    // Query database
    const settings = await DatabaseHelpers.query(`
      SELECT email_notifications, sms_notifications, push_notifications
      FROM user_settings
      WHERE user_id = $1
    `, [userId]);

    // Verify values
    expect(settings[0].email_notifications).toBe(true);
    expect(settings[0].sms_notifications).toBe(false);
    expect(settings[0].push_notifications).toBe(true);
  },

  verifyPersistence: async (page) => {
    // Check checkboxes still correct after refresh
    await expect(page.locator('#emailNotifications')).toBeChecked();
    await expect(page.locator('#smsNotifications')).not.toBeChecked();
    await expect(page.locator('#pushNotifications')).toBeChecked();
  },
};
```

**Step 3**: Run the test

```typescript
await PersistenceTestTemplate.runTest(page, config, {
  screenshotPath: '/tmp/notification-settings-test',
  apiEndpoint: '/api/settings/notifications',
  apiMethod: 'PUT',
});
```

---

## Debugging Failed Persistence Tests

### Common Failure Modes

#### 1. Database Not Updated

**Symptom**: Test fails at "Verify database was updated"

**Possible Causes**:
- Backend validation failure (silently ignored)
- Database transaction rollback
- Wrong table/column name
- Field not included in backend model

**Debug Steps**:
1. Check API response status and body
2. Check backend logs for errors
3. Query database manually to confirm state
4. Add console.log to see actual vs expected values

#### 2. Refresh Shows Old Values

**Symptom**: Test fails at "Verify persistence after refresh"

**Possible Causes**:
- Client-side caching
- Backend not saving
- Wrong selector (showing different field)

**Debug Steps**:
1. Check screenshot at "after-refresh.png"
2. Inspect browser network tab (API called on refresh?)
3. Clear browser cache and retry
4. Verify database state directly

#### 3. API Returns Error But UI Shows Success

**Symptom**: API verification fails, UI verification passes

**Possible Causes**:
- Frontend ignoring API errors
- Wrong success condition in frontend
- API endpoint doesn't exist (404)

**Debug Steps**:
1. Check actual API endpoint called (network tab)
2. Verify endpoint exists in backend
3. Check frontend error handling code
4. Look for console errors

---

## Integration with CI/CD

### Running Persistence Tests in CI

```yaml
# .github/workflows/e2e-tests.yml
name: E2E Persistence Tests

on: [push, pull_request]

jobs:
  persistence-tests:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Start Docker services
        run: ./dev.sh

      - name: Wait for services
        run: |
          timeout 60 bash -c 'until curl -f http://localhost:5173; do sleep 1; done'
          timeout 60 bash -c 'until curl -f http://localhost:5655/health; do sleep 1; done'

      - name: Run persistence tests
        run: |
          npm run test:e2e:playwright -- profile-update-full-persistence.spec.ts
          npm run test:e2e:playwright -- ticket-lifecycle-persistence.spec.ts
          npm run test:e2e:playwright -- rsvp-lifecycle-persistence.spec.ts

      - name: Upload screenshots on failure
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: persistence-test-screenshots
          path: /tmp/**/*.png
```

---

## Real-World Examples

### Example 1: Profile Update Bug (Caught)

**Before Template**:
- Manual testing: "Profile updates work fine"
- Bug shipped to production
- Users reported data loss

**After Template**:
```typescript
// This test would have caught the bug
test('should persist firstName to database', async ({ page }) => {
  await testProfileUpdatePersistence(page, {
    userEmail: 'test@example.com',
    userPassword: 'password',
    updatedFields: { firstName: 'NewName' },
  });
});

// Test Output:
// ❌ Database fields mismatch:
//    firstName: expected "NewName" but got null
//
// BUG DETECTED: Profile update did NOT persist to database!
```

### Example 2: Ticket Cancellation Bug (Caught)

**Before Template**:
- Manual testing: "Cancellation works"
- Bug: Frontend called `/ticket` endpoint (doesn't exist)
- Backend returned 404, frontend ignored error
- Database not updated

**After Template**:
```typescript
// This test would have caught the bug
test('should persist ticket cancellation', async ({ page }) => {
  await testTicketCancellationPersistence(page, {
    userEmail: 'test@example.com',
    userPassword: 'password',
    eventId: 'event-id',
  });
});

// Test Output:
// ❌ BUG DETECTED: Cancel Ticket button reappeared after refresh!
// ❌ Database shows status: 'Registered' (expected 'Cancelled')
//
// Network logs:
// DELETE /api/events/event-id/ticket → 404 Not Found
//
// Expected endpoint:
// DELETE /api/events/event-id/participation
```

---

## Summary

### Key Takeaways

1. **Always verify database state** - Don't trust UI success messages
2. **Always test after page refresh** - Catches client-side caching issues
3. **Use templates for consistency** - Reduces boilerplate and errors
4. **Take screenshots for debugging** - Makes failures easier to diagnose
5. **Clean up test data** - Prevents test pollution

### Template Usage Checklist

- [ ] Import correct template for operation type
- [ ] Provide unique test data (timestamps, GUIDs)
- [ ] Configure screenshot path for debugging
- [ ] Specify expected success message
- [ ] Add cleanup in afterEach/afterAll
- [ ] Document what the test verifies
- [ ] Run test locally before committing

---

## Additional Resources

- **Base Template**: `/apps/web/tests/playwright/templates/persistence-test-template.ts`
- **Database Helpers**: `/apps/web/tests/playwright/utils/database-helpers.ts`
- **Example Tests**: `/apps/web/tests/playwright/*-persistence.spec.ts`
- **Playwright Guide**: `/docs/standards-processes/testing/browser-automation/playwright-guide.md`
- **Test Catalog**: `/docs/standards-processes/testing/TEST_CATALOG.md`

---

**Questions or Issues?**

See Test Team or update this guide with solutions you discover.
