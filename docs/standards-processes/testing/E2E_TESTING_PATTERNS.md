# E2E Testing Patterns

> 🚨 **REDIRECT**: This file has been consolidated into the Playwright guide 🚨

**ALL E2E TESTING PATTERNS ARE NOW LOCATED IN:**
- `/docs/standards-processes/testing/browser-automation/playwright-guide.md`

## Why This Change?

The previous content focused on deprecated Puppeteer patterns. Since ALL E2E tests have been migrated to Playwright (January 2025), maintaining separate pattern files creates confusion and duplication.

## What You Need Instead:

### For E2E Test Patterns:
- **Primary Guide**: [Playwright Guide](browser-automation/playwright-guide.md)
- **Test Inventory**: [Test Catalog](TEST_CATALOG.md)
- **Problem Solutions**: [Test Writers Lessons](/docs/lessons-learned/test-writers.md)

### For Writing New E2E Tests:
```bash
# Use Playwright exclusively
npm run test:e2e:playwright

# Follow Page Object Models in /tests/playwright/pages/
# Use helpers in /tests/playwright/helpers/
```

---

*This consolidation eliminates duplicate patterns and ensures single source of truth for E2E testing.*