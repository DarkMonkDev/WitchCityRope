# Visual Regression Testing Setup Guide

This guide explains how to set up and manage visual regression tests using Playwright.

## Overview

Visual regression testing captures screenshots of your application and compares them against baseline images to detect unintended visual changes.

## Generating Baseline Images

### Initial Setup

1. **First Time Setup**: Run tests with the `--update-snapshots` flag to generate initial baselines:
   ```bash
   npx playwright test --update-snapshots
   ```

2. **Selective Update**: Update baselines for specific tests:
   ```bash
   npx playwright test path/to/test.spec.ts --update-snapshots
   ```

3. **Using the Update Script**: Use our convenience script:
   ```bash
   ./scripts/update-visual-baselines.sh
   ```

## Updating Baselines When UI Changes

### When to Update Baselines

Update baselines when:
- Intentional UI changes are made
- Design updates are implemented
- Component styling is modified
- Layout changes are introduced

### How to Update

1. **Review Changes**: First run tests to see what's different:
   ```bash
   npx playwright test
   ```

2. **Verify Changes**: Open the HTML report to review visual differences:
   ```bash
   npx playwright show-report
   ```

3. **Update Baselines**: If changes are intentional:
   ```bash
   npx playwright test --update-snapshots
   ```

## Configuring Visual Comparison Settings

### Configuration Options

Visual comparison can be configured in your test files or globally in `playwright.config.ts`:

```typescript
// In test file
await expect(page).toHaveScreenshot({
  maxDiffPixels: 100,
  threshold: 0.2,
  animations: 'disabled',
  mask: [page.locator('.dynamic-content')],
  fullPage: true,
  clip: { x: 0, y: 0, width: 800, height: 600 }
});
```

### Key Configuration Options

- **maxDiffPixels**: Maximum number of pixels that can differ
- **threshold**: Threshold between 0-1 for pixel difference tolerance
- **animations**: Disable animations for consistent screenshots
- **mask**: Hide dynamic content that changes between runs
- **fullPage**: Capture entire page vs viewport only
- **clip**: Capture specific region of the page

### Global Configuration

Set default options in `playwright.config.ts`:

```typescript
use: {
  // Global screenshot options
  screenshot: {
    mode: 'only-on-failure',
    fullPage: true
  },
  
  // Visual regression options
  ignoreHTTPSErrors: true,
  video: 'retain-on-failure',
}
```

## Handling Cross-Platform Differences

### Platform-Specific Baselines

Playwright automatically creates platform-specific baseline folders:
- `__screenshots__/darwin/` - macOS
- `__screenshots__/linux/` - Linux
- `__screenshots__/win32/` - Windows

### Best Practices for Cross-Platform Testing

1. **Font Rendering**: Use web fonts to ensure consistency:
   ```css
   @import url('https://fonts.googleapis.com/css2?family=Roboto:wght@400;700&display=swap');
   body {
     font-family: 'Roboto', sans-serif;
   }
   ```

2. **Disable Animations**: Always disable animations in tests:
   ```typescript
   page.addInitScript(() => {
     window.matchMedia = () => ({
       matches: false,
       addListener: () => {},
       removeListener: () => {},
     });
   });
   ```

3. **Fixed Viewport**: Use consistent viewport sizes:
   ```typescript
   await page.setViewportSize({ width: 1280, height: 720 });
   ```

4. **Wait for Stability**: Ensure page is stable before screenshots:
   ```typescript
   await page.waitForLoadState('networkidle');
   await page.waitForTimeout(100); // Brief pause for rendering
   ```

### Docker for Consistency

For ultimate consistency, use Docker:

```dockerfile
FROM mcr.microsoft.com/playwright:v1.40.0-focal
WORKDIR /app
COPY . .
RUN npm ci
CMD ["npx", "playwright", "test"]
```

## Directory Structure

```
tests/playwright/
├── visual-regression/
│   ├── __screenshots__/        # Baseline images (auto-generated)
│   │   ├── darwin/
│   │   ├── linux/
│   │   └── win32/
│   └── visual-regression-setup.md
├── specs/
│   └── visual/
│       └── homepage.visual.spec.ts
└── playwright.config.ts
```

## Troubleshooting

### Common Issues

1. **Flaky Screenshots**
   - Ensure animations are disabled
   - Wait for network idle state
   - Use fixed viewport sizes
   - Mask dynamic content

2. **Platform Differences**
   - Use Docker for CI/CD
   - Maintain separate baselines per platform
   - Use web fonts instead of system fonts

3. **Large Diff Reports**
   - Increase threshold for minor differences
   - Use maxDiffPixels for acceptable variations
   - Review and update baselines regularly

### CI/CD Considerations

1. **Store Baselines in Git**:
   ```bash
   git add tests/playwright/visual-regression/__screenshots__/
   git commit -m "Update visual regression baselines"
   ```

2. **CI Configuration**:
   - Use consistent OS/browser versions
   - Consider using containers
   - Store artifacts on failure

3. **Review Process**:
   - Include visual diffs in PR reviews
   - Require approval for baseline updates
   - Document reasons for updates

## Example Test

```typescript
import { test, expect } from '@playwright/test';

test.describe('Visual Regression Tests', () => {
  test('homepage visual test', async ({ page }) => {
    // Navigate to page
    await page.goto('/');
    
    // Wait for stability
    await page.waitForLoadState('networkidle');
    
    // Disable animations
    await page.addInitScript(() => {
      const style = document.createElement('style');
      style.innerHTML = `
        *, *::before, *::after {
          animation-duration: 0s !important;
          animation-delay: 0s !important;
          transition-duration: 0s !important;
          transition-delay: 0s !important;
        }
      `;
      document.head.appendChild(style);
    });
    
    // Take screenshot
    await expect(page).toHaveScreenshot('homepage.png', {
      fullPage: true,
      animations: 'disabled',
      maxDiffPixels: 100
    });
  });
});
```