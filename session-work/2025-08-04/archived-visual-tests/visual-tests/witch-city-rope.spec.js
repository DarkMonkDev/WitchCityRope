const { test, expect } = require('@playwright/test');
const { testScenarios } = require('../tools/ui-test-scenarios');

// Test data for authenticated scenarios
const testUsers = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'AdminPass123!'
  },
  member: {
    email: 'member@example.com',
    password: 'MemberPass123!'
  },
  staff: {
    email: 'staff@witchcityrope.com',
    password: 'StaffPass123!'
  }
};

// Helper function to login
async function login(page, role = 'member') {
  const user = testUsers[role.toLowerCase()];
  await page.goto('/login');
  await page.fill('#email', user.email);
  await page.fill('#password', user.password);
  await page.click('button[type="submit"]');
  await page.waitForURL(/dashboard|admin/);
}

// Core page screenshots
test.describe('WitchCityRope Core Pages', () => {
  const corePages = [
    { name: 'landing', url: '/', waitFor: '.hero-section' },
    { name: 'events-list', url: '/events', waitFor: '.event-list' },
    { name: 'login', url: '/login', waitFor: 'form' },
    { name: 'register', url: '/register', waitFor: 'form' },
    { name: 'vetting-apply', url: '/vetting/apply', waitFor: 'form' }
  ];

  corePages.forEach(({ name, url, waitFor }) => {
    test(`${name} page screenshot`, async ({ page }) => {
      await page.goto(url);
      if (waitFor) {
        await page.waitForSelector(waitFor, { state: 'visible' });
      }
      await page.waitForLoadState('networkidle');
      await expect(page).toHaveScreenshot(`${name}.png`, {
        fullPage: true,
        animations: 'disabled'
      });
    });
  });
});

// Authenticated pages
test.describe('Authenticated Pages', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, 'member');
  });

  const memberPages = [
    { name: 'dashboard', url: '/dashboard' },
    { name: 'my-events', url: '/my-events' },
    { name: 'profile', url: '/profile' },
    { name: 'settings', url: '/settings' }
  ];

  memberPages.forEach(({ name, url }) => {
    test(`${name} page screenshot`, async ({ page }) => {
      await page.goto(url);
      await page.waitForLoadState('networkidle');
      await expect(page).toHaveScreenshot(`member-${name}.png`, {
        fullPage: true
      });
    });
  });
});

// Admin pages
test.describe('Admin Pages', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, 'admin');
  });

  const adminPages = [
    { name: 'dashboard', url: '/admin' },
    { name: 'events', url: '/admin/events' },
    { name: 'vetting', url: '/admin/vetting' },
    { name: 'users', url: '/admin/users' }
  ];

  adminPages.forEach(({ name, url }) => {
    test(`admin ${name} page screenshot`, async ({ page }) => {
      await page.goto(url);
      await page.waitForLoadState('networkidle');
      await expect(page).toHaveScreenshot(`admin-${name}.png`, {
        fullPage: true
      });
    });
  });
});

// Mobile responsive views
test.describe('Mobile Responsive Views', () => {
  const viewports = [
    { width: 375, height: 667, name: 'iphone-se' },
    { width: 390, height: 844, name: 'iphone-14' },
    { width: 768, height: 1024, name: 'ipad' },
    { width: 1366, height: 768, name: 'laptop' },
    { width: 1920, height: 1080, name: 'desktop' }
  ];

  const responsivePages = [
    { url: '/', name: 'landing' },
    { url: '/events', name: 'events' },
    { url: '/login', name: 'login' }
  ];

  viewports.forEach(({ width, height, name: deviceName }) => {
    test.describe(`${deviceName} viewport`, () => {
      test.use({ viewport: { width, height } });

      responsivePages.forEach(({ url, name }) => {
        test(`${name} page`, async ({ page }) => {
          await page.goto(url);
          await page.waitForLoadState('networkidle');
          await expect(page).toHaveScreenshot(`${name}-${deviceName}.png`);
        });
      });
    });
  });
});

// Interactive elements
test.describe('Interactive Elements', () => {
  test('navigation menu states', async ({ page }) => {
    await page.goto('/');
    
    // Desktop navigation hover
    const navItem = page.locator('nav a').first();
    await navItem.hover();
    await expect(page.locator('nav')).toHaveScreenshot('nav-hover.png');
    
    // Mobile menu
    await page.setViewportSize({ width: 375, height: 667 });
    await page.click('.mobile-menu-toggle');
    await page.waitForSelector('.mobile-menu', { state: 'visible' });
    await expect(page).toHaveScreenshot('mobile-menu-open.png');
  });

  test('form validation states', async ({ page }) => {
    await page.goto('/login');
    
    // Empty form submission
    await page.click('button[type="submit"]');
    await page.waitForSelector('.validation-error', { state: 'visible' });
    await expect(page.locator('form')).toHaveScreenshot('login-validation-errors.png');
    
    // Focus state
    await page.focus('#email');
    await expect(page.locator('form')).toHaveScreenshot('login-email-focused.png');
  });

  test('event registration modal', async ({ page }) => {
    await login(page);
    await page.goto('/events/1');
    
    // Open registration modal
    await page.click('.register-button');
    await page.waitForSelector('.registration-modal', { state: 'visible' });
    await expect(page.locator('.registration-modal')).toHaveScreenshot('registration-modal.png');
  });
});

// Dark theme support
test.describe('Dark Theme', () => {
  test.use({ colorScheme: 'dark' });

  const darkThemePages = [
    { name: 'landing-dark', url: '/' },
    { name: 'events-dark', url: '/events' },
    { name: 'login-dark', url: '/login' }
  ];

  darkThemePages.forEach(({ name, url }) => {
    test(`${name} screenshot`, async ({ page }) => {
      await page.goto(url);
      await page.waitForLoadState('networkidle');
      await expect(page).toHaveScreenshot(`${name}.png`, {
        fullPage: true
      });
    });
  });
});

// Accessibility checks
test.describe('Accessibility', () => {
  test('landing page accessibility', async ({ page }) => {
    await page.goto('/');
    
    // Tab through interactive elements
    for (let i = 0; i < 5; i++) {
      await page.keyboard.press('Tab');
      await page.waitForTimeout(100);
    }
    
    await expect(page).toHaveScreenshot('landing-keyboard-navigation.png');
  });

  test('high contrast mode', async ({ page }) => {
    await page.goto('/');
    
    // Emulate high contrast
    await page.emulateMedia({ forcedColors: 'active' });
    await expect(page).toHaveScreenshot('landing-high-contrast.png', {
      fullPage: true
    });
  });
});

// Error pages
test.describe('Error Pages', () => {
  const errorPages = [
    { code: 404, url: '/non-existent-page' },
    { code: 403, url: '/admin' }, // Without auth
  ];

  errorPages.forEach(({ code, url }) => {
    test(`${code} error page`, async ({ page }) => {
      const response = await page.goto(url, { waitUntil: 'networkidle' });
      
      // For 403, we might be redirected to login
      if (code === 403 && response?.status() !== 403) {
        // Try to access admin page without proper role
        await login(page, 'member');
        await page.goto('/admin');
      }
      
      await expect(page).toHaveScreenshot(`error-${code}.png`, {
        fullPage: true
      });
    });
  });
});

// Component-specific screenshots
test.describe('UI Components', () => {
  test('event cards gallery', async ({ page }) => {
    await page.goto('/events');
    await page.waitForSelector('.event-card', { state: 'visible' });
    
    const eventCards = page.locator('.event-list');
    await expect(eventCards).toHaveScreenshot('event-cards-gallery.png');
  });

  test('user dashboard widgets', async ({ page }) => {
    await login(page);
    await page.goto('/dashboard');
    
    // Capture individual dashboard sections
    const sections = [
      { selector: '.upcoming-events-widget', name: 'upcoming-events-widget' },
      { selector: '.vetting-status-widget', name: 'vetting-status-widget' },
      { selector: '.quick-actions-widget', name: 'quick-actions-widget' }
    ];

    for (const { selector, name } of sections) {
      const element = page.locator(selector);
      if (await element.isVisible()) {
        await expect(element).toHaveScreenshot(`${name}.png`);
      }
    }
  });
});

// Loading states
test.describe('Loading States', () => {
  test('event list loading', async ({ page }) => {
    // Slow down network to capture loading state
    await page.route('**/api/events**', route => {
      setTimeout(() => route.continue(), 2000);
    });
    
    await page.goto('/events');
    await page.waitForTimeout(500); // Capture mid-load
    await expect(page).toHaveScreenshot('events-loading.png');
  });
});

// Print styles
test.describe('Print Styles', () => {
  test('event detail print view', async ({ page }) => {
    await page.goto('/events/1');
    await page.waitForLoadState('networkidle');
    
    await page.emulateMedia({ media: 'print' });
    await expect(page).toHaveScreenshot('event-detail-print.png', {
      fullPage: true
    });
  });
});