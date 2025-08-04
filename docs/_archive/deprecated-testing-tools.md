# Deprecated Testing Tools

> ⚠️ **WARNING: These tools are no longer used for E2E testing in WitchCityRope** ⚠️
> 
> **Use Playwright exclusively for all E2E testing. See `/docs/standards-processes/testing/browser-automation/playwright-guide.md`**

## Overview

This document archives information about testing tools that were previously used but have been deprecated in favor of Playwright. This information is kept for historical reference only.

## Puppeteer (Deprecated January 2025)

### Why It Was Deprecated
- 180 Puppeteer tests were successfully migrated to Playwright
- Playwright provides:
  - 40% faster test execution
  - 86% less test flakiness
  - Cross-browser support (Chrome, Firefox, Safari)
  - Better TypeScript support
  - Built-in test runner

### Migration Details
- **Migration Date**: January 2025
- **Tests Migrated**: 180 tests
- **Old Location**: `/tests/e2e/` and `/ToBeDeleted/`
- **New Location**: `/tests/playwright/`

### Key Differences from Playwright
- Required manual `waitForSelector` calls
- No built-in test runner (used Jest/Mocha)
- Chrome/Chromium only
- Less robust auto-waiting
- More verbose API

### Common Puppeteer Patterns (DO NOT USE)
```javascript
// OLD Puppeteer pattern - DO NOT USE
const puppeteer = require('puppeteer');
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000');
await page.waitForSelector('.element');
await page.click('.element');
```

## Stagehand MCP Server (Deprecated)

### What It Was
- AI-powered browser automation tool
- Used natural language commands
- Required OpenAI API key
- Located at: `/home/chad/mcp-servers/mcp-server-browserbase/stagehand/`

### Why It Was Deprecated
- Playwright's capabilities made it unnecessary
- Added complexity with AI dependency
- Slower than direct automation
- Less deterministic results

### Stagehand Commands (DO NOT USE)
```bash
# OLD commands - DO NOT USE
export OPENAI_API_KEY='your-key'
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh
```

## Browser-Tools MCP Server (Deprecated)

### What It Was
- Direct Puppeteer wrapper for MCP
- Used for quick browser automation tasks
- Required Chrome DevTools on port 9222

### Why It Was Deprecated
- Playwright's superior API and features
- Built-in cross-browser support
- Better integration with testing framework

## Migration Guide

If you find old Puppeteer tests, DO NOT update them. Instead:

1. **Check if already migrated**: Look in `/tests/playwright/specs/`
2. **Use Playwright**: All new tests must use Playwright
3. **Reference the guide**: See `/docs/standards-processes/testing/browser-automation/playwright-guide.md`

## DO NOT USE These Patterns

### ❌ Puppeteer Test Pattern
```javascript
// DO NOT USE
const puppeteer = require('puppeteer');
describe('Login', () => {
  it('should login', async () => {
    const browser = await puppeteer.launch();
    // ... test code
  });
});
```

### ❌ Stagehand Natural Language
```javascript
// DO NOT USE
stagehand.navigate("Go to login page");
stagehand.action("Click the login button");
```

### ❌ Chrome DevTools Connection
```bash
# DO NOT USE
google-chrome --remote-debugging-port=9222
curl http://localhost:9222/json/version
```

## Use Playwright Instead

### ✅ Correct Pattern
```typescript
// USE THIS
import { test, expect } from '@playwright/test';

test('should login', async ({ page }) => {
  await page.goto('/login');
  await page.fill('input[name="email"]', 'user@example.com');
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL(/dashboard/);
});
```

## Historical Context

### Timeline
- **2024**: Project started with Puppeteer
- **Early 2025**: Evaluation of Playwright benefits
- **January 2025**: Full migration to Playwright completed
- **January 21, 2025**: All Puppeteer references removed from documentation

### Lessons Learned
1. **Tool Selection**: Playwright's built-in features reduced maintenance
2. **Migration Effort**: 180 tests migrated successfully with improved stability
3. **Performance**: Significant speed improvements with Playwright
4. **Cross-browser**: Firefox and Safari testing now possible

## References

For current E2E testing practices:
- Primary Guide: `/docs/standards-processes/testing/browser-automation/playwright-guide.md`
- Migration Details: `/docs/enhancements/playwright-migration/`
- Test Examples: `/tests/playwright/specs/`

---

**Remember**: This document is for historical reference only. Always use Playwright for E2E testing.