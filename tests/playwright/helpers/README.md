# Playwright Test Helpers

This directory contains utility modules to help write efficient and maintainable E2E tests for WitchCityRope.

## Available Helpers

### 1. Data Generators (`data-generators.ts`)

Generates unique test data for various entities in the application.

**Key Features:**
- Generates unique users, events, RSVPs, and vetting applications
- Uses Faker.js for realistic test data
- Ensures uniqueness with timestamps and counters
- Provides pre-configured test scenarios

**Usage Example:**
```typescript
import { TestDataGenerator, TestDataPatterns } from './helpers/data-generators';

// Generate a single user
const user = TestDataGenerator.generateUser({
  firstName: 'Test',
  lastName: 'User'
});

// Generate an event
const event = TestDataGenerator.generateEvent({
  eventType: 'Workshop',
  requiresVetting: true
});

// Generate a complete test scenario
const scenario = TestDataGenerator.generateTestScenario();
// Returns users (admin, teacher, vetted member, new member) and events

// Use test data patterns
const validEmail = user.email;
const invalidEmail = TestDataPatterns.invalidEmail();
const strongPassword = TestDataPatterns.strongPassword();
```

### 2. Authentication Helpers (`auth.helpers.ts`)

Manages authentication state and provides utilities for auth-related operations.

**Key Features:**
- Login/logout functionality
- Authentication state persistence across tests
- Role-based access verification
- Support for impersonation and 2FA

**Usage Example:**
```typescript
import { AuthHelpers } from './helpers/auth.helpers';
import { testConfig } from './helpers/test.config';

// Login and save auth state
const authState = await AuthHelpers.login(page, {
  email: testConfig.accounts.admin.email,
  password: testConfig.accounts.admin.password
});

// Get authenticated page for a user type
const { page, authState } = await AuthHelpers.getAuthenticatedPage(browser, 'admin');

// Reuse authentication across tests
const { context, authState } = await AuthHelpers.getAuthenticatedContext(browser, {
  email: 'test@example.com',
  password: 'TestPass123!'
});

// Verify route access
const hasAccess = await AuthHelpers.verifyRouteAccess(page, '/admin', true);

// Logout
await AuthHelpers.logout(page);
```

### 3. Database Helpers (`database.helpers.ts`)

PostgreSQL test data setup and cleanup utilities.

**Key Features:**
- Direct database access for test data setup
- Automatic cleanup tracking
- Support for complex test scenarios
- Transaction-safe operations

**Usage Example:**
```typescript
import { DatabaseHelpers } from './helpers/database.helpers';

// Connect to test database
await DatabaseHelpers.connect();

// Create test users
const userId = await DatabaseHelpers.createTestUser({
  email: 'testuser@example.com',
  firstName: 'Test',
  lastName: 'User'
});

// Assign roles
await DatabaseHelpers.assignUserRole(userId, 'Admin');
await DatabaseHelpers.markUserAsVetted(userId);

// Create test events
const eventId = await DatabaseHelpers.createTestEvent({
  name: 'Test Workshop',
  eventType: 'Workshop'
}, userId);

// Create RSVP
await DatabaseHelpers.createRSVP(userId, eventId, 'Attending');

// Create complete scenario
const scenario = await DatabaseHelpers.createTestScenario();
// Returns userIds and eventIds for the created data

// Cleanup after tests
await DatabaseHelpers.cleanupAllTestData();
await DatabaseHelpers.disconnect();
```

### 4. Blazor Helpers (`blazor.helpers.ts`)

Utilities for working with Blazor Server applications.

**Key Features:**
- Wait for Blazor components and SignalR connections
- Handle Blazor-specific timing issues
- Form interaction utilities
- Circuit connection management

**Usage Example:**
```typescript
import { BlazorHelpers } from './helpers/blazor.helpers';

// Wait for Blazor to be ready
await BlazorHelpers.waitForBlazorReady(page);

// Wait for a component
const component = await BlazorHelpers.waitForComponent(page, 'event-list');

// Fill form with Blazor binding support
await BlazorHelpers.fillAndWait(page, '#email-input', 'test@example.com');

// Click and wait for Blazor processing
await BlazorHelpers.clickAndWait(page, '#submit-button');

// Handle navigation
await BlazorHelpers.waitForNavigation(page, '/events');
```

### 5. Test Configuration (`test.config.ts`)

Central configuration for all E2E tests.

**Key Features:**
- Test account credentials
- Common URLs
- Timeout configurations
- Test data patterns

**Usage Example:**
```typescript
import { testConfig, generateTestData } from './helpers/test.config';

// Use predefined accounts
const adminAccount = testConfig.accounts.admin;

// Navigate to common URLs
await page.goto(testConfig.urls.adminDashboard);

// Generate unique test data
const testData = generateTestData();
console.log(testData.eventName); // "Test Event 1234567890"
```

## Best Practices

1. **Always clean up test data**: Use `DatabaseHelpers.cleanupAllTestData()` in afterEach/afterAll hooks
2. **Reuse authentication**: Use `AuthHelpers.getAuthenticatedContext()` to avoid repeated logins
3. **Use unique data**: Always use `TestDataGenerator` to avoid conflicts between parallel tests
4. **Wait for Blazor**: Always use `BlazorHelpers.waitForBlazorReady()` after navigation
5. **Track created data**: The database helpers automatically track created records for cleanup

## Environment Variables

Set these environment variables for database access:

```bash
TEST_DB_HOST=localhost
TEST_DB_PORT=5432
TEST_DB_NAME=witchcityrope_test
TEST_DB_USER=postgres
TEST_DB_PASSWORD=postgres
TEST_DB_SSL=false
```

## Example Test Structure

```typescript
import { test, expect } from '@playwright/test';
import { DatabaseHelpers } from './helpers/database.helpers';
import { AuthHelpers } from './helpers/auth.helpers';
import { TestDataGenerator } from './helpers/data-generators';
import { BlazorHelpers } from './helpers/blazor.helpers';

test.describe('Event Management', () => {
  test.beforeAll(async () => {
    await DatabaseHelpers.connect();
  });

  test.afterAll(async () => {
    await DatabaseHelpers.cleanupAllTestData();
    await DatabaseHelpers.disconnect();
  });

  test('Admin can create and edit events', async ({ browser }) => {
    // Get authenticated admin page
    const { page, authState } = await AuthHelpers.getAuthenticatedPage(browser, 'admin');
    
    // Generate test event data
    const eventData = TestDataGenerator.generateEvent({
      eventType: 'Workshop',
      name: 'Test Rope Workshop'
    });

    // Navigate to event creation
    await page.goto('/admin/events/create');
    await BlazorHelpers.waitForBlazorReady(page);

    // Fill form
    await BlazorHelpers.fillAndWait(page, '#event-name', eventData.name);
    await BlazorHelpers.fillAndWait(page, '#event-description', eventData.description);
    
    // Submit
    await BlazorHelpers.clickAndWait(page, '#create-event-button');

    // Verify creation
    await expect(page).toHaveURL(/\/admin\/events\/\d+/);
    
    // Cleanup is handled automatically by afterAll hook
  });
});
```