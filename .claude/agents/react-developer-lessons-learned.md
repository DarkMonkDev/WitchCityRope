# React Developer - Lessons Learned

## 🚨 VITEST CONFIGURATION - PERFORMANCE CRITICAL 🚨

### Parallel Test Execution (2025-10-03)
**Problem**: Tests were timing out (180+ seconds) with 84 test files, preventing TDD workflow.

**Root Cause**: Vitest was configured for serial execution:
- `singleThread: true` - Only 1 test file at a time
- `maxConcurrency: 1` - Only 1 test within a file at a time
- Result: ~42 minutes to run all tests serially

**Solution**: Optimized for parallel execution with safety limits:
```typescript
test: {
  pool: 'forks',  // Better isolation than threads for React tests
  poolOptions: {
    forks: {
      singleFork: false,  // Allow parallel test files
      maxForks: 4,        // Run 4 test files concurrently
      minForks: 1,
    }
  },
  isolate: true,        // Better cleanup between tests
  maxConcurrency: 5,    // Allow 5 concurrent tests per file
}
```

**Results**:
- Tests complete in ~54-58 seconds (was timing out at 180+)
- All 84 test files execute successfully
- 3-4x performance improvement
- 42% test pass rate (76/181 passing)

**Key Lessons**:
1. **Use `forks` not `threads`** for React component tests - better process isolation
2. **Balance parallelism with memory** - 4 concurrent files is sweet spot
3. **Always test single file first** - if one file runs fast but all files hang, it's a config issue
4. **Serial execution doesn't scale** - 84 files × 30s timeout = 42 minutes minimum

## 🚨 CRITICAL TESTING RULES - NEVER VIOLATE 🚨

### BROWSER TESTING - PLAYWRIGHT ONLY
- **ALWAYS USE PLAYWRIGHT** for browser-based testing and debugging
- **NEVER INSTALL PUPPETEER** - This is a HARD VIOLATION
- **NEVER INSTALL ANY TESTING PACKAGES** without explicit user permission
- Playwright is already installed and configured - USE IT
- Location: `/tests/playwright/`
- Run tests: `npm run test:e2e:playwright`

### PACKAGE INSTALLATION RULES
- **DO NOT INSTALL NEW PACKAGES** without explicit permission
- **ESPECIALLY NO TESTING PACKAGES** - We have everything we need
- If you think you need a package, ASK FIRST
- The following are BANNED:
  - puppeteer
  - cypress  
  - selenium
  - Any alternative testing frameworks

### DEBUGGING REACT COMPONENTS
When debugging React components:
1. Use Playwright for browser automation: `npx playwright test`
2. Use the existing test infrastructure in `/tests/playwright/`
3. Create temporary test files if needed but use Playwright
4. Check browser console with Playwright's page.evaluate()
5. Take screenshots with Playwright for visual debugging

## File Organization Rules
- React components go in `apps/web/src/components/`
- Pages go in `apps/web/src/pages/`
- Types go in `apps/web/src/types/`
- Tests go in `tests/playwright/`

## Technology Stack
- React 18 with TypeScript
- Vite for build tooling
- Mantine v7 UI components (NOT v8)
- TanStack Query for data fetching
- React Router v6 for routing
- Playwright for E2E testing (NO OTHER TESTING TOOLS)

## Common Issues and Solutions

### Missing Package Imports
- Check if package exists in package.json FIRST
- DO NOT auto-install missing packages
- Create local type definitions if shared types are missing
- Ask for permission before ANY npm install

### Component Not Rendering
1. Check browser console with Playwright
2. Verify all imports are correct
3. Check for missing dependencies in package.json
4. Use Playwright to debug rendering issues
5. NEVER install puppeteer to debug

### Testing Approach
```typescript
// CORRECT - Using Playwright
import { test, expect } from '@playwright/test';

test('component renders', async ({ page }) => {
  await page.goto('http://localhost:5174/demo');
  await expect(page.locator('.component')).toBeVisible();
});

// WRONG - NEVER DO THIS
// import puppeteer from 'puppeteer'; // BANNED
```

## Architecture Patterns
- Functional components with hooks (no class components)
- Proper TypeScript typing for all props
- Use existing Mantine components (v7)
- Follow vertical slice architecture

## Remember
- **PLAYWRIGHT ONLY** for browser testing
- **NO NEW TESTING PACKAGES** without permission
- **ASK BEFORE INSTALLING** any packages