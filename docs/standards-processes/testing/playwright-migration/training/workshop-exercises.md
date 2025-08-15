# Playwright Workshop Exercises

## Introduction

These hands-on exercises will help you practice converting Puppeteer tests to Playwright and writing new Playwright tests for our Blazor Server application. Each exercise builds on the previous one.

## Setup

Before starting, ensure you have:
1. Playwright installed: `npm install @playwright/test`
2. Access to the test environment
3. Reviewed the basic documentation

## Exercise 1: Basic Navigation and Assertions

### Task
Convert this Puppeteer test to Playwright:

```javascript
// Puppeteer version
describe('Homepage Navigation', () => {
  let browser;
  let page;

  beforeEach(async () => {
    browser = await puppeteer.launch();
    page = await browser.newPage();
    await page.goto('http://localhost:5000');
  });

  afterEach(async () => {
    await browser.close();
  });

  it('should display the homepage', async () => {
    await page.waitForSelector('h1');
    const title = await page.$eval('h1', el => el.textContent);
    expect(title).toBe('Welcome to WitchCityRope');
    
    const loginButton = await page.$('a[href="/login"]');
    expect(loginButton).toBeTruthy();
  });
});
```

### Your Solution
```typescript
// Write your Playwright version here:
import { test, expect } from '@playwright/test';

test('should display the homepage', async ({ page }) => {
  // Your code here
});
```

<details>
<summary>Solution</summary>

```typescript
import { test, expect } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';

test('should display the homepage', async ({ page }) => {
  // Navigate to homepage
  await page.goto('http://localhost:5000');
  await BlazorHelpers.waitForBlazorReady(page);
  
  // Verify title is visible and has correct text
  await expect(page.locator('h1')).toBeVisible();
  await expect(page.locator('h1')).toHaveText('Welcome to WitchCityRope');
  
  // Verify login button exists
  await expect(page.locator('a[href="/login"]')).toBeVisible();
});
```

</details>

## Exercise 2: Form Interaction with Blazor

### Task
Write a Playwright test for a user registration form that:
1. Navigates to the registration page
2. Fills in the form fields
3. Handles validation errors
4. Successfully submits the form

### Starter Code
```typescript
test('should handle user registration', async ({ page }) => {
  // Navigate to registration page
  
  // Fill in form fields
  
  // Submit with invalid data first
  
  // Fix validation errors
  
  // Submit successfully
});
```

<details>
<summary>Solution</summary>

```typescript
test('should handle user registration', async ({ page }) => {
  // Navigate to registration page
  await page.goto('/register');
  await BlazorHelpers.waitForBlazorReady(page);
  
  // Fill in form fields with invalid data
  await page.fill('input[name="email"]', 'invalid-email');
  await page.fill('input[name="password"]', '123'); // Too short
  
  // Submit form
  await BlazorHelpers.clickAndWait(page, page.locator('button[type="submit"]'));
  
  // Check validation errors
  await expect(page.locator('.validation-message')).toContainText('valid email');
  await expect(page.locator('.field-validation-error')).toContainText('at least 6 characters');
  
  // Fix validation errors
  await BlazorHelpers.fillInput(page.locator('input[name="email"]'), 'user@example.com');
  await BlazorHelpers.fillInput(page.locator('input[name="password"]'), 'SecurePassword123!');
  await BlazorHelpers.fillInput(page.locator('input[name="confirmPassword"]'), 'SecurePassword123!');
  
  // Submit successfully
  await BlazorHelpers.clickAndWait(page, page.locator('button[type="submit"]'));
  
  // Verify redirect to dashboard
  await page.waitForURL('/dashboard');
  await expect(page).toHaveURL('/dashboard');
});
```

</details>

## Exercise 3: Testing Dynamic Content

### Task
Create a test that:
1. Logs in as an admin
2. Navigates to the events page
3. Searches for events
4. Verifies search results update dynamically
5. Tests pagination

### Requirements
- Use Page Object Pattern
- Handle Blazor's dynamic updates
- Implement proper waiting strategies

### Starter Code
```typescript
// Create an EventsPage class
class EventsPage {
  constructor(private page: Page) {}
  
  // Add methods here
}

test('should search and paginate events', async ({ page }) => {
  // Your test here
});
```

<details>
<summary>Solution</summary>

```typescript
class EventsPage {
  constructor(private page: Page) {}
  
  async goto() {
    await this.page.goto('/events');
    await BlazorHelpers.waitForBlazorReady(this.page);
  }
  
  async searchEvents(query: string) {
    await BlazorHelpers.fillInput(
      this.page.locator('input[placeholder="Search events..."]'),
      query
    );
    // Wait for search results to update
    await this.page.waitForTimeout(500); // Debounce delay
    await BlazorHelpers.waitForBlazorReady(this.page);
  }
  
  async getEventCards() {
    return this.page.locator('.event-card').all();
  }
  
  async goToNextPage() {
    await BlazorHelpers.clickAndWait(
      this.page,
      this.page.locator('button:has-text("Next")')
    );
  }
  
  async getCurrentPageNumber() {
    return this.page.locator('.pagination .active').textContent();
  }
}

test('should search and paginate events', async ({ page }) => {
  // Login as admin
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.loginAsAdmin();
  
  // Navigate to events
  const eventsPage = new EventsPage(page);
  await eventsPage.goto();
  
  // Verify initial events load
  const initialEvents = await eventsPage.getEventCards();
  expect(initialEvents.length).toBeGreaterThan(0);
  
  // Search for specific events
  await eventsPage.searchEvents('workshop');
  
  // Verify search results
  const searchResults = await eventsPage.getEventCards();
  expect(searchResults.length).toBeGreaterThan(0);
  
  // Verify all results contain search term
  for (const card of searchResults) {
    const text = await card.textContent();
    expect(text?.toLowerCase()).toContain('workshop');
  }
  
  // Test pagination if available
  const nextButton = page.locator('button:has-text("Next")');
  if (await nextButton.isVisible()) {
    await eventsPage.goToNextPage();
    const page2Events = await eventsPage.getEventCards();
    expect(page2Events.length).toBeGreaterThan(0);
    
    const currentPage = await eventsPage.getCurrentPageNumber();
    expect(currentPage).toBe('2');
  }
});
```

</details>

## Exercise 4: Complex Interactions

### Task
Test a drag-and-drop event scheduling interface:
1. Login as admin
2. Navigate to calendar view
3. Drag an event to a new time slot
4. Verify the change is saved
5. Test conflict detection

### Challenge Points
- Handle Blazor's real-time updates
- Implement custom drag-and-drop helpers
- Verify SignalR updates

### Starter Code
```typescript
test('should reschedule event via drag and drop', async ({ page }) => {
  // Your implementation
});
```

<details>
<summary>Solution</summary>

```typescript
test('should reschedule event via drag and drop', async ({ page }) => {
  // Login as admin
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.loginAsAdmin();
  
  // Navigate to calendar
  await page.goto('/admin/calendar');
  await BlazorHelpers.waitForBlazorReady(page);
  
  // Find an event to drag
  const eventElement = page.locator('.calendar-event').first();
  await expect(eventElement).toBeVisible();
  
  // Get initial position/time
  const initialSlot = await eventElement.getAttribute('data-time-slot');
  
  // Find target time slot
  const targetSlot = page.locator('.time-slot[data-time="14:00"]').first();
  
  // Perform drag and drop
  await eventElement.dragTo(targetSlot);
  
  // Wait for Blazor to process the change
  await BlazorHelpers.waitForBlazorReady(page);
  
  // Verify confirmation dialog (if any)
  const confirmDialog = page.locator('.modal:has-text("Reschedule")');
  if (await confirmDialog.isVisible()) {
    await BlazorHelpers.clickAndWait(
      page,
      confirmDialog.locator('button:has-text("Confirm")')
    );
  }
  
  // Verify event moved
  const movedEvent = page.locator('.calendar-event').first();
  const newSlot = await movedEvent.getAttribute('data-time-slot');
  expect(newSlot).not.toBe(initialSlot);
  expect(newSlot).toContain('14:00');
  
  // Test conflict detection
  const conflictingEvent = page.locator('.calendar-event').nth(1);
  
  // Try to drag to same slot
  await conflictingEvent.dragTo(targetSlot);
  
  // Should show conflict warning
  await expect(page.locator('.alert-warning')).toBeVisible();
  await expect(page.locator('.alert-warning')).toContainText('conflict');
});
```

</details>

## Exercise 5: Network Mocking

### Task
Test error handling by mocking network failures:
1. Mock API endpoints to return errors
2. Test retry mechanisms
3. Verify error messages
4. Test offline scenarios

### Starter Code
```typescript
test('should handle API errors gracefully', async ({ page }) => {
  // Setup network mocks
  
  // Test various error scenarios
});
```

<details>
<summary>Solution</summary>

```typescript
test('should handle API errors gracefully', async ({ page }) => {
  // Login first
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.loginAsAdmin();
  
  // Mock API error for events endpoint
  await page.route('**/api/events', route => {
    route.fulfill({
      status: 500,
      contentType: 'application/json',
      body: JSON.stringify({ error: 'Internal Server Error' })
    });
  });
  
  // Navigate to events page
  await page.goto('/events');
  await BlazorHelpers.waitForBlazorReady(page);
  
  // Should show error message
  await expect(page.locator('.alert-danger')).toBeVisible();
  await expect(page.locator('.alert-danger')).toContainText('Unable to load events');
  
  // Test retry button
  const retryButton = page.locator('button:has-text("Retry")');
  await expect(retryButton).toBeVisible();
  
  // Fix the API response
  await page.route('**/api/events', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ events: [{ id: 1, title: 'Test Event' }] })
    });
  });
  
  // Click retry
  await BlazorHelpers.clickAndWait(page, retryButton);
  
  // Should now show events
  await expect(page.locator('.event-card')).toBeVisible();
  await expect(page.locator('.alert-danger')).not.toBeVisible();
  
  // Test offline scenario
  await page.context().setOffline(true);
  
  // Try to create new event
  await page.locator('button:has-text("Create Event")').click();
  
  // Should show offline message
  await expect(page.locator('.offline-banner')).toBeVisible();
  
  // Go back online
  await page.context().setOffline(false);
  
  // Offline banner should disappear
  await expect(page.locator('.offline-banner')).not.toBeVisible();
});
```

</details>

## Exercise 6: Performance Testing

### Task
Create a performance test that:
1. Measures page load times
2. Checks for memory leaks
3. Tests with large datasets
4. Monitors SignalR connection stability

### Starter Code
```typescript
test('should maintain performance with large datasets', async ({ page }) => {
  // Your implementation
});
```

<details>
<summary>Solution</summary>

```typescript
test('should maintain performance with large datasets', async ({ page }) => {
  // Start performance measurement
  await page.goto('/');
  
  // Login
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.loginAsAdmin();
  
  // Mock large dataset
  await page.route('**/api/events', route => {
    const largeDataset = Array.from({ length: 1000 }, (_, i) => ({
      id: i + 1,
      title: `Event ${i + 1}`,
      date: new Date().toISOString(),
      venue: `Venue ${i % 10}`,
      description: 'Lorem ipsum dolor sit amet'
    }));
    
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ events: largeDataset })
    });
  });
  
  // Measure load time
  const startTime = Date.now();
  await page.goto('/events');
  await BlazorHelpers.waitForBlazorReady(page);
  await expect(page.locator('.event-card').first()).toBeVisible();
  const loadTime = Date.now() - startTime;
  
  console.log(`Page load time with 1000 events: ${loadTime}ms`);
  expect(loadTime).toBeLessThan(5000); // Should load within 5 seconds
  
  // Check rendering performance
  const eventCount = await page.locator('.event-card').count();
  console.log(`Rendered ${eventCount} events`);
  
  // Test scrolling performance
  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  await page.waitForTimeout(1000);
  
  // Check memory usage (if supported)
  const metrics = await page.evaluate(() => {
    if ((performance as any).memory) {
      return {
        usedJSHeapSize: (performance as any).memory.usedJSHeapSize,
        totalJSHeapSize: (performance as any).memory.totalJSHeapSize
      };
    }
    return null;
  });
  
  if (metrics) {
    console.log('Memory usage:', metrics);
  }
  
  // Test SignalR stability
  const isConnected = await BlazorHelpers.isConnected(page);
  expect(isConnected).toBe(true);
  
  // Interact with multiple items
  for (let i = 0; i < 10; i++) {
    await page.locator('.event-card').nth(i).click();
    await page.waitForTimeout(100);
  }
  
  // Verify connection still stable
  const stillConnected = await BlazorHelpers.isConnected(page);
  expect(stillConnected).toBe(true);
});
```

</details>

## Exercise 7: Advanced Scenarios

### Task
Create a comprehensive test suite for the admin dashboard that tests:
1. Real-time notifications
2. Multi-user interactions
3. Data synchronization
4. Role-based access control

### Requirements
- Use multiple browser contexts
- Test SignalR broadcasts
- Verify permission-based UI changes

<details>
<summary>Solution</summary>

```typescript
test.describe('Admin Dashboard Advanced Tests', () => {
  test('should handle real-time multi-user updates', async ({ browser }) => {
    // Create two contexts - admin and regular user
    const adminContext = await browser.newContext();
    const userContext = await browser.newContext();
    
    const adminPage = await adminContext.newPage();
    const userPage = await userContext.newPage();
    
    // Login as admin
    const adminLogin = new LoginPage(adminPage);
    await adminLogin.goto();
    await adminLogin.loginAsAdmin();
    
    // Login as regular user
    const userLogin = new LoginPage(userPage);
    await userLogin.goto();
    await userLogin.loginAsUser();
    
    // Both navigate to dashboard
    await adminPage.goto('/dashboard');
    await userPage.goto('/dashboard');
    await BlazorHelpers.waitForBlazorReady(adminPage);
    await BlazorHelpers.waitForBlazorReady(userPage);
    
    // Admin creates announcement
    await adminPage.locator('button:has-text("New Announcement")').click();
    await BlazorHelpers.fillInput(
      adminPage.locator('textarea[name="message"]'),
      'Important: Site maintenance tonight'
    );
    await BlazorHelpers.clickAndWait(
      adminPage,
      adminPage.locator('button:has-text("Send")')
    );
    
    // User should receive notification in real-time
    await expect(userPage.locator('.notification-toast')).toBeVisible({ timeout: 5000 });
    await expect(userPage.locator('.notification-toast')).toContainText('Site maintenance');
    
    // Test role-based UI
    await expect(adminPage.locator('.admin-controls')).toBeVisible();
    await expect(userPage.locator('.admin-controls')).not.toBeVisible();
    
    // Test data sync - admin updates event
    await adminPage.goto('/events/1/edit');
    await BlazorHelpers.fillInput(
      adminPage.locator('input[name="title"]'),
      'Updated Event Title'
    );
    await adminPage.locator('button[type="submit"]').click();
    
    // User should see updated title
    await userPage.goto('/events/1');
    await expect(userPage.locator('h1')).toHaveText('Updated Event Title', { timeout: 5000 });
    
    // Test concurrent edits
    await adminPage.goto('/events/2/edit');
    await userPage.goto('/events/2'); // User viewing same event
    
    // Admin makes change
    await BlazorHelpers.fillInput(
      adminPage.locator('input[name="venue"]'),
      'New Venue Location'
    );
    await adminPage.locator('button[type="submit"]').click();
    
    // User should see update notification
    await expect(userPage.locator('.update-notification')).toBeVisible();
    await expect(userPage.locator('.update-notification')).toContainText('Event has been updated');
    
    // Cleanup
    await adminContext.close();
    await userContext.close();
  });
  
  test('should enforce role-based permissions', async ({ page }) => {
    // Test as regular user trying to access admin features
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.loginAsUser();
    
    // Try direct navigation to admin pages
    await page.goto('/admin/users');
    await expect(page).toHaveURL('/unauthorized');
    
    // Try API manipulation
    const response = await page.request.delete('/api/events/1');
    expect(response.status()).toBe(403);
    
    // Verify limited UI
    await page.goto('/events');
    await expect(page.locator('button:has-text("Delete")')).not.toBeVisible();
    await expect(page.locator('button:has-text("Edit")')).not.toBeVisible();
  });
});
```

</details>

## Final Challenge: Full E2E Scenario

### Task
Create a complete end-to-end test that simulates a real user journey:

1. New user registration
2. Browse events
3. Register for an event
4. Receive confirmation
5. Add to calendar
6. Logout

Include error handling, performance checks, and accessibility testing.

<details>
<summary>Solution Outline</summary>

```typescript
test('complete user journey', async ({ page }) => {
  // 1. Registration
  // - Test form validation
  // - Verify email confirmation
  
  // 2. Browse events
  // - Test search and filters
  // - Check performance with pagination
  
  // 3. Event registration
  // - Handle payment flow
  // - Test error scenarios
  
  // 4. Confirmation
  // - Verify email sent
  // - Check dashboard update
  
  // 5. Calendar integration
  // - Download .ics file
  // - Verify content
  
  // 6. Logout
  // - Ensure clean session end
  // - Verify data persistence
});
```

</details>

## Best Practices Checklist

After completing these exercises, ensure you:

- [ ] Always wait for Blazor to be ready after navigation
- [ ] Use Page Object Pattern for reusable components
- [ ] Implement proper error handling
- [ ] Add meaningful test descriptions
- [ ] Use data-testid attributes for reliable selection
- [ ] Avoid hardcoded waits - use conditions
- [ ] Clean up test data after tests
- [ ] Run tests in parallel when possible
- [ ] Add screenshots for debugging failures
- [ ] Test both happy path and error scenarios

## Resources

- [Playwright Documentation](https://playwright.dev)
- [Our Blazor Helpers](../helpers/blazor.helpers.ts)
- [Example Tests](../tests/playwright/)
- [Quick Reference Guide](./quick-reference.md)

## Next Steps

1. Review your solutions with the team
2. Run the full test suite locally
3. Add new test cases for uncovered scenarios
4. Contribute improvements to the helper functions