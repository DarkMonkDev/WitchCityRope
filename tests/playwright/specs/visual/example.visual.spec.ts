import { test, expect } from '@playwright/test';

test.describe('Visual Regression Example', () => {
  // Disable animations for all tests in this describe block
  test.beforeEach(async ({ page }) => {
    // Inject CSS to disable animations
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
  });

  test('homepage visual test', async ({ page }) => {
    // Navigate to homepage
    await page.goto('/');
    
    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle');
    
    // Take full page screenshot
    await expect(page).toHaveScreenshot('homepage-full.png', {
      fullPage: true,
    });
  });

  test('homepage header visual test', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Take screenshot of header only
    const header = page.locator('header').first();
    await expect(header).toHaveScreenshot('homepage-header.png');
  });

  test('homepage with masked dynamic content', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Mask dynamic content like timestamps or user-specific data
    await expect(page).toHaveScreenshot('homepage-masked.png', {
      mask: [
        page.locator('.timestamp'),
        page.locator('.user-avatar'),
        page.locator('.dynamic-content'),
      ],
      fullPage: true,
    });
  });

  test('responsive design - mobile view', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    await expect(page).toHaveScreenshot('homepage-mobile.png', {
      fullPage: true,
    });
  });

  test('responsive design - tablet view', async ({ page }) => {
    // Set tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    await expect(page).toHaveScreenshot('homepage-tablet.png', {
      fullPage: true,
    });
  });

  test('dark mode visual test', async ({ page }) => {
    await page.goto('/');
    
    // Enable dark mode (adjust selector based on your app)
    // Example: clicking a dark mode toggle
    // await page.click('[data-testid="dark-mode-toggle"]');
    
    // Or set dark mode via localStorage/cookies
    await page.evaluate(() => {
      localStorage.setItem('theme', 'dark');
      document.documentElement.classList.add('dark');
    });
    
    await page.waitForLoadState('networkidle');
    
    await expect(page).toHaveScreenshot('homepage-dark-mode.png', {
      fullPage: true,
    });
  });

  test('form states visual test', async ({ page }) => {
    await page.goto('/contact'); // Adjust URL as needed
    await page.waitForLoadState('networkidle');
    
    // Normal state
    await expect(page).toHaveScreenshot('form-normal.png');
    
    // Focus state
    await page.focus('input[type="text"]');
    await expect(page).toHaveScreenshot('form-focused.png');
    
    // Error state (trigger validation)
    await page.click('button[type="submit"]');
    await expect(page).toHaveScreenshot('form-error.png');
  });

  test('component isolation visual test', async ({ page }) => {
    // Navigate to a page with the component
    await page.goto('/components'); // Adjust URL
    await page.waitForLoadState('networkidle');
    
    // Isolate and screenshot specific component
    const card = page.locator('.card-component').first();
    await expect(card).toHaveScreenshot('card-component.png', {
      // Add padding around the component
      clip: {
        x: -10,
        y: -10,
        width: 320,
        height: 220,
      },
    });
  });

  test('hover and interaction states', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Capture button in different states
    const button = page.locator('button').first();
    
    // Normal state
    await expect(button).toHaveScreenshot('button-normal.png');
    
    // Hover state
    await button.hover();
    await expect(button).toHaveScreenshot('button-hover.png');
    
    // Active/pressed state
    await button.hover();
    await page.mouse.down();
    await expect(button).toHaveScreenshot('button-active.png');
    await page.mouse.up();
  });

  test('cross-browser visual consistency', async ({ page, browserName }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Include browser name in screenshot for comparison
    await expect(page).toHaveScreenshot(`homepage-${browserName}.png`, {
      fullPage: true,
    });
  });
});

// Example of testing with different color schemes
test.describe('Color Scheme Tests', () => {
  [
    { scheme: 'light', className: 'light-theme' },
    { scheme: 'dark', className: 'dark-theme' },
    { scheme: 'high-contrast', className: 'high-contrast-theme' },
  ].forEach(({ scheme, className }) => {
    test(`visual test - ${scheme} color scheme`, async ({ page }) => {
      await page.goto('/');
      
      // Apply color scheme
      await page.evaluate((cls) => {
        document.documentElement.className = cls;
      }, className);
      
      await page.waitForLoadState('networkidle');
      
      await expect(page).toHaveScreenshot(`color-scheme-${scheme}.png`, {
        fullPage: true,
      });
    });
  });
});