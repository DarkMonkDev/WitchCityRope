# Blazor Server Testing Guide with Playwright

## Understanding Blazor Server Challenges

Blazor Server applications present unique testing challenges due to:
- SignalR-based real-time communication
- Server-side rendering with client-side interactivity
- Dynamic DOM updates via WebSockets
- Component lifecycle and re-rendering

## Key Concepts

### 1. SignalR Connection Management

Blazor Server apps maintain a persistent SignalR connection. Your tests must account for this:

```typescript
// Always wait for Blazor to be ready
await BlazorHelpers.waitForBlazorReady(page);

// This helper checks:
// 1. Blazor object exists
// 2. SignalR connection is established
// 3. Navigation manager is available
```

### 2. Component Rendering Delays

Unlike traditional SPAs, Blazor Server components may have rendering delays:

```typescript
// Bad - immediate action might fail
await page.goto('/events');
await page.click('.event-card'); // Component might not be rendered

// Good - wait for Blazor
await page.goto('/events');
await BlazorHelpers.waitForBlazorReady(page);
await page.click('.event-card');
```

## Blazor-Specific Helpers

### Complete BlazorHelpers Implementation

```typescript
export class BlazorHelpers {
  /**
   * Wait for Blazor Server to be fully initialized
   */
  static async waitForBlazorReady(page: Page): Promise<void> {
    // Wait for Blazor object
    await page.waitForFunction(
      () => (window as any).Blazor !== undefined,
      { timeout: 10000 }
    );

    // Wait for SignalR connection
    await page.waitForFunction(
      () => {
        const blazor = (window as any).Blazor;
        return blazor && blazor._internal && blazor._internal.navigationManager;
      },
      { timeout: 10000 }
    );

    // Small wait for component rendering
    await page.waitForTimeout(100);
  }

  /**
   * Click element and wait for Blazor to process
   */
  static async clickAndWait(page: Page, locator: Locator): Promise<void> {
    await locator.click();
    await page.waitForTimeout(100); // Allow Blazor to process
    await this.waitForBlazorReady(page);
  }

  /**
   * Wait for Blazor navigation to complete
   */
  static async waitForNavigation(page: Page): Promise<void> {
    await page.waitForLoadState('networkidle');
    await this.waitForBlazorReady(page);
  }

  /**
   * Fill form field with Blazor binding awareness
   */
  static async fillInput(locator: Locator, value: string): Promise<void> {
    await locator.clear();
    await locator.fill(value);
    await locator.blur(); // Trigger Blazor binding
  }

  /**
   * Check if Blazor connection is active
   */
  static async isConnected(page: Page): Promise<boolean> {
    return await page.evaluate(() => {
      const blazor = (window as any).Blazor;
      return blazor && blazor._internal && blazor._internal.connected === true;
    });
  }
}
```

## Common Testing Patterns

### 1. Page Navigation

```typescript
test('navigate between Blazor pages', async ({ page }) => {
  // Initial navigation
  await page.goto('/');
  await BlazorHelpers.waitForBlazorReady(page);

  // Click navigation link
  await page.locator('nav a:has-text("Events")').click();
  
  // Wait for navigation to complete
  await BlazorHelpers.waitForNavigation(page);
  
  // Verify new page loaded
  await expect(page).toHaveURL('/events');
  await expect(page.locator('h1')).toHaveText('Events');
});
```

### 2. Form Submission

```typescript
test('submit Blazor form', async ({ page }) => {
  await page.goto('/events/create');
  await BlazorHelpers.waitForBlazorReady(page);

  // Fill form fields with Blazor-aware helper
  await BlazorHelpers.fillInput(
    page.locator('input[name="title"]'), 
    'Test Event'
  );

  // Select dropdown (Blazor Select component)
  await page.locator('select[name="venue"]').selectOption('venue-1');
  
  // Handle date inputs
  await page.locator('input[type="date"]').fill('2024-12-25');

  // Submit form
  await BlazorHelpers.clickAndWait(
    page, 
    page.locator('button[type="submit"]')
  );

  // Wait for server response and navigation
  await page.waitForURL(/\/events\/\d+/);
});
```

### 3. Dynamic Content Loading

```typescript
test('load dynamic content', async ({ page }) => {
  await page.goto('/events');
  await BlazorHelpers.waitForBlazorReady(page);

  // Wait for data to load
  await expect(page.locator('.event-card')).toHaveCount(
    count => count > 0
  );

  // Or wait for specific content
  await page.waitForSelector('.event-card', { 
    state: 'visible' 
  });

  // Verify content loaded
  const eventCount = await page.locator('.event-card').count();
  expect(eventCount).toBeGreaterThan(0);
});
```

### 4. Modal Dialogs

```typescript
test('handle Blazor modal', async ({ page }) => {
  await page.goto('/admin/events');
  await BlazorHelpers.waitForBlazorReady(page);

  // Open modal
  await page.locator('button:has-text("Add Event")').click();
  
  // Wait for modal to render
  await expect(page.locator('.modal')).toBeVisible();
  
  // Interact with modal content
  await BlazorHelpers.fillInput(
    page.locator('.modal input[name="title"]'),
    'Modal Test Event'
  );

  // Close modal
  await BlazorHelpers.clickAndWait(
    page,
    page.locator('.modal button:has-text("Save")')
  );

  // Verify modal closed
  await expect(page.locator('.modal')).not.toBeVisible();
});
```

### 5. Real-time Updates

```typescript
test('handle SignalR updates', async ({ page }) => {
  await page.goto('/live-dashboard');
  await BlazorHelpers.waitForBlazorReady(page);

  // Get initial count
  const initialCount = await page.locator('.notification-count').textContent();

  // Trigger action that causes SignalR update
  // (In another browser context or via API)

  // Wait for SignalR update
  await expect(page.locator('.notification-count')).not.toHaveText(initialCount);
});
```

## Error Handling

### 1. Connection Loss

```typescript
test('handle SignalR disconnection', async ({ page }) => {
  await page.goto('/dashboard');
  await BlazorHelpers.waitForBlazorReady(page);

  // Simulate connection loss
  await page.evaluate(() => {
    (window as any).Blazor.disconnect();
  });

  // Verify reconnection UI
  await expect(page.locator('.reconnect-modal')).toBeVisible();

  // Wait for auto-reconnect
  await page.waitForFunction(
    () => (window as any).Blazor._internal.connected === true,
    { timeout: 30000 }
  );
});
```

### 2. Validation Errors

```typescript
test('display validation errors', async ({ page }) => {
  await page.goto('/register');
  await BlazorHelpers.waitForBlazorReady(page);

  // Submit empty form
  await BlazorHelpers.clickAndWait(
    page,
    page.locator('button[type="submit"]')
  );

  // Check for Blazor validation messages
  await expect(page.locator('.validation-message')).toBeVisible();
  await expect(page.locator('.validation-message')).toHaveText(
    'Email is required'
  );
});
```

## Performance Considerations

### 1. Optimize Waits

```typescript
// Avoid fixed timeouts
await page.waitForTimeout(5000); // Bad

// Use conditional waits
await page.waitForLoadState('networkidle'); // Better
await expect(page.locator('.content')).toBeVisible(); // Best
```

### 2. Batch Operations

```typescript
test('efficient data entry', async ({ page }) => {
  await page.goto('/bulk-entry');
  await BlazorHelpers.waitForBlazorReady(page);

  // Fill multiple fields efficiently
  await Promise.all([
    page.locator('#field1').fill('value1'),
    page.locator('#field2').fill('value2'),
    page.locator('#field3').fill('value3')
  ]);

  // Single blur to trigger all bindings
  await page.locator('#field3').blur();
});
```

## Debugging Blazor Tests

### 1. Check SignalR Connection

```typescript
// Add to failing tests
const isConnected = await BlazorHelpers.isConnected(page);
console.log('Blazor connected:', isConnected);
```

### 2. Log Blazor Errors

```typescript
// Capture Blazor errors
page.on('pageerror', error => {
  console.error('Blazor error:', error);
});

// Check console for Blazor logs
page.on('console', msg => {
  if (msg.text().includes('Blazor')) {
    console.log('Blazor log:', msg.text());
  }
});
```

### 3. Visual Debugging

```typescript
// Use debug mode for Blazor apps
test('debug Blazor interaction', async ({ page }) => {
  await page.goto('/complex-form');
  await BlazorHelpers.waitForBlazorReady(page);
  
  // Pause to inspect Blazor state
  await page.pause();
  
  // Take screenshot before action
  await page.screenshot({ path: 'before-action.png' });
  
  // Perform action
  await page.locator('.trigger').click();
  
  // Screenshot after
  await page.screenshot({ path: 'after-action.png' });
});
```

## Best Practices Summary

1. **Always wait for Blazor to be ready** after navigation
2. **Use BlazorHelpers** for consistent behavior
3. **Handle SignalR disconnections** gracefully
4. **Account for server-side rendering delays**
5. **Use proper selectors** that work with Blazor's dynamic DOM
6. **Test both connected and disconnected states**
7. **Verify component lifecycles** in complex scenarios

## Common Issues and Solutions

| Issue | Solution |
|-------|----------|
| "Element not found" after navigation | Add `BlazorHelpers.waitForBlazorReady()` |
| Form values not updating | Use `blur()` after filling inputs |
| Intermittent test failures | Check SignalR connection status |
| Modal/dropdown not appearing | Wait for component render with `expect().toBeVisible()` |
| Navigation not completing | Use `BlazorHelpers.waitForNavigation()` |

## Next Steps

- Practice with the [Workshop Exercises](./workshop-exercises.md)
- Review the [Quick Reference](./quick-reference.md) for more patterns
- Explore our actual test implementations in `/tests/playwright/`