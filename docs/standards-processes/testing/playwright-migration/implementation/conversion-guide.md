# Puppeteer to Playwright Conversion Guide

## Overview
This guide provides step-by-step instructions for converting Puppeteer tests to Playwright, based on the patterns established during the WitchCityRope migration.

## Pre-Conversion Checklist

Before converting a test:
- [ ] Identify test category (auth, events, admin, etc.)
- [ ] Check if Page Object exists for the pages being tested
- [ ] Review test for any special requirements
- [ ] Ensure test data doesn't conflict with other tests

## Conversion Steps

### Step 1: File Structure Setup

**Puppeteer Structure:**
```
/tests/e2e/tests/e2e/test-feature-name.js
```

**Playwright Structure:**
```
/tests/playwright/
  ├── category/
  │   └── feature-name.spec.ts
  └── pages/
      └── feature.page.ts (if needed)
```

### Step 2: Basic Syntax Conversion

#### Imports
**Puppeteer:**
```javascript
const puppeteer = require('puppeteer');
```

**Playwright:**
```typescript
import { test, expect } from '@playwright/test';
import { FeaturePage } from '../pages/feature.page';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';
```

#### Test Structure
**Puppeteer:**
```javascript
describe('Feature Test', () => {
  let browser;
  let page;

  beforeEach(async () => {
    browser = await puppeteer.launch();
    page = await browser.newPage();
  });

  afterEach(async () => {
    await browser.close();
  });

  it('should do something', async () => {
    // test code
  });
});
```

**Playwright:**
```typescript
test.describe('Feature Test', () => {
  let featurePage: FeaturePage;

  test.beforeEach(async ({ page }) => {
    featurePage = new FeaturePage(page);
    await featurePage.goto();
  });

  test('should do something', async ({ page }) => {
    // test code
  });
});
```

### Step 3: Common Pattern Conversions

#### Navigation
**Puppeteer:**
```javascript
await page.goto('http://localhost:5651/login', {
  waitUntil: 'networkidle2',
  timeout: 30000
});
```

**Playwright:**
```typescript
await page.goto('/login'); // Uses baseURL from config
await BlazorHelpers.waitForBlazorReady(page);
```

#### Element Selection
**Puppeteer:**
```javascript
await page.waitForSelector('#email');
const element = await page.$('#email');
```

**Playwright:**
```typescript
const element = page.locator('#email');
await element.waitFor({ state: 'visible' });
```

#### Form Filling
**Puppeteer:**
```javascript
await page.type('#email', 'test@example.com');
await page.type('#password', 'password');
await page.click('button[type="submit"]');
```

**Playwright:**
```typescript
await BlazorHelpers.fillAndWait(page, '#email', 'test@example.com');
await BlazorHelpers.fillAndWait(page, '#password', 'password');
await BlazorHelpers.clickAndWait(page, 'button[type="submit"]');
```

#### Assertions
**Puppeteer:**
```javascript
const text = await page.$eval('.message', el => el.textContent);
expect(text).toBe('Success');
```

**Playwright:**
```typescript
await expect(page.locator('.message')).toHaveText('Success');
```

#### Screenshots
**Puppeteer:**
```javascript
await page.screenshot({ path: 'screenshot.png' });
```

**Playwright:**
```typescript
await page.screenshot({ path: 'test-results/screenshots/test-name.png' });
// Or use visual regression:
await expect(page).toHaveScreenshot('feature-name.png');
```

### Step 4: Blazor Server Specific Patterns

#### Wait for Blazor Initialization
Always wait for Blazor after navigation:
```typescript
await page.goto('/page');
await BlazorHelpers.waitForBlazorReady(page);
```

#### Handle Form Validation
```typescript
// Submit form
await loginPage.submitEmptyForm();

// Wait for validation
await BlazorHelpers.waitForValidation(page);

// Check validation messages
const messages = await loginPage.getValidationMessages();
```

#### Handle Dynamic Content
```typescript
// Wait for specific text to appear
await BlazorHelpers.waitForText(page, 'Content loaded');

// Wait for component
await BlazorHelpers.waitForComponent(page, 'event-list');
```

### Step 5: Page Object Model Creation

Create a page object for reusable page interactions:

```typescript
import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';

export class FeaturePage {
  readonly page: Page;
  
  // Define locators
  readonly submitButton: Locator;
  readonly inputField: Locator;
  
  constructor(page: Page) {
    this.page = page;
    this.submitButton = page.locator('button[type="submit"]');
    this.inputField = page.locator('#input-field');
  }
  
  async goto() {
    await this.page.goto('/feature');
    await BlazorHelpers.waitForBlazorReady(this.page);
  }
  
  async submitForm(data: string) {
    await BlazorHelpers.fillAndWait(this.page, this.inputField, data);
    await BlazorHelpers.clickAndWait(this.page, this.submitButton);
  }
}
```

### Step 6: Test Data Management

Use the data generators for unique test data:

```typescript
import { TestDataGenerator } from '../helpers/data-generators';

test('should create event', async ({ page }) => {
  const eventData = TestDataGenerator.generateEvent();
  // Use eventData.name, eventData.description, etc.
});
```

### Step 7: Authentication State

For tests requiring authentication:

```typescript
import { AuthHelpers } from '../helpers/auth.helpers';

test.describe('Authenticated tests', () => {
  test.use({ storageState: 'auth-state.json' });
  
  test.beforeAll(async ({ browser }) => {
    await AuthHelpers.createAuthenticatedState(browser, 'admin');
  });
  
  test('should access admin area', async ({ page }) => {
    await page.goto('/admin');
    // Already authenticated
  });
});
```

## Common Gotchas and Solutions

### 1. Timing Issues
**Problem:** Test fails intermittently due to Blazor rendering delays
**Solution:** Always use `BlazorHelpers.waitForBlazorReady()` after navigation

### 2. Element Not Found
**Problem:** Playwright can't find element that Puppeteer could
**Solution:** Use more specific locators or wait for visibility:
```typescript
await page.locator('.element').waitFor({ state: 'visible' });
```

### 3. Form Submission Not Working
**Problem:** Form doesn't submit in Blazor
**Solution:** Use Tab key to trigger Blazor binding:
```typescript
await page.fill('#input', 'value');
await page.press('#input', 'Tab');
```

### 4. Authentication State Not Persisting
**Problem:** Need to login for every test
**Solution:** Use storage state:
```typescript
test.use({ storageState: 'auth-state.json' });
```

## Conversion Checklist

For each converted test:
- [ ] Uses TypeScript (.spec.ts extension)
- [ ] Imports necessary helpers and page objects
- [ ] Uses Blazor helpers for timing
- [ ] Implements proper assertions with expect()
- [ ] Handles errors gracefully
- [ ] Uses unique test data
- [ ] Follows naming conventions
- [ ] Includes test annotations where helpful
- [ ] Cleans up test data if needed
- [ ] Passes in all three browsers

## Quick Reference

### Essential Imports
```typescript
import { test, expect } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';
import { TestDataGenerator } from '../helpers/data-generators';
import { AuthHelpers } from '../helpers/auth.helpers';
import { DatabaseHelpers } from '../helpers/database.helpers';
```

### Common Patterns
```typescript
// Navigation with Blazor
await page.goto('/path');
await BlazorHelpers.waitForBlazorReady(page);

// Form interaction
await BlazorHelpers.fillAndWait(page, selector, value);
await BlazorHelpers.clickAndWait(page, selector);

// Assertions
await expect(element).toBeVisible();
await expect(element).toHaveText('text');
await expect(page).toHaveURL(/pattern/);

// Wait for content
await BlazorHelpers.waitForComponent(page, 'test-id');
await BlazorHelpers.waitForText(page, 'text');
```

## Next Steps

1. Choose a test to convert
2. Create/update page objects as needed
3. Follow the conversion patterns
4. Run the test to verify it works
5. Remove the old Puppeteer test
6. Update the test registry

Remember: The goal is not just to convert tests, but to improve them with Playwright's capabilities!