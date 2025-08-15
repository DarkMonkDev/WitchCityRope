# Playwright Quick Reference - Puppeteer to Playwright

## Core API Conversions

### Browser and Page Management

| Puppeteer | Playwright | Notes |
|-----------|------------|-------|
| `puppeteer.launch()` | Auto-managed in tests | Use `{ page }` fixture |
| `browser.newPage()` | `await context.newPage()` | Or use fixture |
| `page.goto(url)` | `await page.goto(url)` | Same API |
| `page.reload()` | `await page.reload()` | Same API |
| `page.goBack()` | `await page.goBack()` | Same API |
| `page.close()` | Auto-handled | Cleanup automatic |

### Element Selection

| Puppeteer | Playwright | Example |
|-----------|------------|---------|
| `page.$(selector)` | `page.locator(selector)` | `page.locator('.button')` |
| `page.$$(selector)` | `page.locator(selector).all()` | `await page.locator('.item').all()` |
| `page.$eval(selector, fn)` | `page.locator(selector).evaluate(fn)` | `await page.locator('.text').evaluate(el => el.textContent)` |
| `page.$$eval(selector, fn)` | `page.locator(selector).evaluateAll(fn)` | `await page.locator('.items').evaluateAll(els => els.length)` |
| `element.$(selector)` | `locator.locator(selector)` | `card.locator('.title')` |

### Waiting Strategies

| Puppeteer | Playwright | Better Alternative |
|-----------|------------|-------------------|
| `page.waitForSelector(selector)` | `page.locator(selector).waitFor()` | Usually not needed |
| `page.waitForSelector(selector, {visible: true})` | `await expect(locator).toBeVisible()` | Auto-retry assertion |
| `page.waitForTimeout(ms)` | `await page.waitForTimeout(ms)` | Avoid - use conditions |
| `page.waitForNavigation()` | `await page.waitForURL()` | Or `waitForLoadState()` |
| `page.waitForFunction()` | `await page.waitForFunction()` | Same API |
| `waitForSelector(..., {hidden: true})` | `await expect(locator).toBeHidden()` | Auto-retry assertion |

### Actions

| Puppeteer | Playwright | Notes |
|-----------|------------|-------|
| `element.click()` | `await locator.click()` | Auto-waits |
| `page.type(selector, text)` | `await page.fill(selector, text)` | Clears first |
| `page.type(selector, text, {delay})` | `await page.type(selector, text, {delay})` | For typing animation |
| `page.select(selector, value)` | `await page.selectOption(selector, value)` | Better API |
| `page.check(selector)` | `await page.check(selector)` | For checkboxes |
| `page.focus(selector)` | `await page.focus(selector)` | Same API |
| `page.hover(selector)` | `await page.hover(selector)` | Same API |

### Keyboard and Mouse

| Puppeteer | Playwright |
|-----------|------------|
| `page.keyboard.press('Enter')` | `await page.keyboard.press('Enter')` |
| `page.keyboard.type('text')` | `await page.keyboard.type('text')` |
| `page.mouse.click(x, y)` | `await page.mouse.click(x, y)` |
| `page.keyboard.down('Shift')` | `await page.keyboard.down('Shift')` |
| `page.keyboard.up('Shift')` | `await page.keyboard.up('Shift')` |

### Assertions (New in Playwright)

| Task | Playwright Assertion |
|------|---------------------|
| Element visible | `await expect(locator).toBeVisible()` |
| Element hidden | `await expect(locator).toBeHidden()` |
| Element enabled | `await expect(locator).toBeEnabled()` |
| Element disabled | `await expect(locator).toBeDisabled()` |
| Has text | `await expect(locator).toHaveText('text')` |
| Contains text | `await expect(locator).toContainText('text')` |
| Has value | `await expect(locator).toHaveValue('value')` |
| Has attribute | `await expect(locator).toHaveAttribute('name', 'value')` |
| Has class | `await expect(locator).toHaveClass(/active/)` |
| Count elements | `await expect(locator).toHaveCount(3)` |
| Page URL | `await expect(page).toHaveURL(/pattern/)` |
| Page title | `await expect(page).toHaveTitle('Title')` |

## Blazor-Specific Patterns

### Navigation

```typescript
// Puppeteer way
await page.goto(url);
await page.waitForSelector('.content', {visible: true});

// Playwright + Blazor way
await page.goto(url);
await BlazorHelpers.waitForBlazorReady(page);
```

### Form Interaction

```typescript
// Puppeteer way
await page.type('#email', 'user@example.com');
await page.click('#submit');
await page.waitForNavigation();

// Playwright + Blazor way
await BlazorHelpers.fillInput(page.locator('#email'), 'user@example.com');
await BlazorHelpers.clickAndWait(page, page.locator('#submit'));
```

### Dynamic Content

```typescript
// Puppeteer way
await page.waitForSelector('.item');
const items = await page.$$('.item');

// Playwright way
await expect(page.locator('.item')).toHaveCount(count => count > 0);
const items = await page.locator('.item').all();
```

## Network Interception

| Puppeteer | Playwright |
|-----------|------------|
| `page.setRequestInterception(true)` | Not needed |
| `page.on('request', handler)` | `await page.route(url, handler)` |
| `request.continue()` | `await route.continue()` |
| `request.respond()` | `await route.fulfill()` |
| `request.abort()` | `await route.abort()` |

### Example

```typescript
// Puppeteer
await page.setRequestInterception(true);
page.on('request', request => {
  if (request.url().includes('/api/')) {
    request.respond({
      status: 200,
      body: JSON.stringify({data: []})
    });
  } else {
    request.continue();
  }
});

// Playwright
await page.route('**/api/**', route => {
  route.fulfill({
    status: 200,
    body: JSON.stringify({data: []})
  });
});
```

## Debugging

| Task | Playwright Command |
|------|-------------------|
| Debug mode | `npx playwright test --debug` |
| Headed mode | `npx playwright test --headed` |
| Slow motion | `npx playwright test --slow-mo=100` |
| Single test | `npx playwright test -g "test name"` |
| Trace viewer | `npx playwright test --trace on` |
| UI mode | `npx playwright test --ui` |

## Advanced Selectors

### Text Selectors
```typescript
// Exact text
page.locator('text=Login')

// Partial text
page.locator('text=/Log.*/i')

// Has text
page.locator('button:has-text("Submit")')
```

### CSS Extensions
```typescript
// Pseudo-selectors
page.locator(':visible')
page.locator('button:enabled')

// Chaining
page.locator('.card').filter({ hasText: 'Special' })

// Nth element
page.locator('.item').nth(2)
```

### ARIA Selectors
```typescript
page.getByRole('button', { name: 'Submit' })
page.getByRole('textbox', { name: 'Email' })
page.getByRole('link', { name: /learn more/i })
page.getByLabel('Password')
page.getByPlaceholder('Enter email')
page.getByTestId('submit-button')
```

## Common Patterns

### Login Flow
```typescript
// Page Object Pattern
class LoginPage {
  constructor(private page: Page) {}
  
  async login(email: string, password: string) {
    await this.page.fill('[name="email"]', email);
    await this.page.fill('[name="password"]', password);
    await this.page.click('button[type="submit"]');
    await this.page.waitForURL('/dashboard');
  }
}
```

### Wait for Multiple Elements
```typescript
// Wait for any of multiple conditions
await Promise.race([
  page.waitForURL('/success'),
  page.locator('.error').waitFor()
]);

// Wait for all conditions
await Promise.all([
  expect(page.locator('.header')).toBeVisible(),
  expect(page.locator('.content')).toHaveText(/loaded/i)
]);
```

### Retry Logic
```typescript
// Built-in retry with assertions
await expect(async () => {
  const count = await page.locator('.item').count();
  expect(count).toBeGreaterThan(5);
}).toPass({ timeout: 10000 });
```

## File Upload/Download

```typescript
// Upload
const [fileChooser] = await Promise.all([
  page.waitForEvent('filechooser'),
  page.click('input[type="file"]')
]);
await fileChooser.setFiles('path/to/file.pdf');

// Download
const [download] = await Promise.all([
  page.waitForEvent('download'),
  page.click('a.download-link')
]);
await download.saveAs('path/to/save.pdf');
```

## Screenshot Comparison

```typescript
// Visual regression testing
await expect(page).toHaveScreenshot('homepage.png');
await expect(page.locator('.card')).toHaveScreenshot('card.png');
```

## Multiple Contexts

```typescript
// Test with multiple users
const context1 = await browser.newContext();
const context2 = await browser.newContext();

const page1 = await context1.newPage();
const page2 = await context2.newPage();

// Different sessions, cookies, storage
```

## Tips for Migration

1. **Start with simple tests** - Login, navigation
2. **Use Playwright's auto-waiting** - Remove explicit waits
3. **Adopt web-first assertions** - More reliable than manual checks
4. **Leverage better selectors** - getByRole, getByText
5. **Use Page Object Pattern** - Better organization
6. **Enable traces for debugging** - Great for CI failures
7. **Run in parallel** - Playwright handles it well

## Need Help?

- Run existing tests: `npm run test:e2e`
- Debug a test: `npx playwright test --debug test-name`
- View trace: `npx playwright show-trace trace.zip`
- Check examples: `/tests/playwright/`