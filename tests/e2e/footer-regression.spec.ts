/**
 * Footer Regression Tests
 *
 * Tests footer functionality across desktop and mobile viewports.
 * Focus: Functionality and user interactions, NOT design/CSS specifics.
 *
 * Created: 2025-11-30
 * Part of: E2E Test Modernization (Phase 3.1)
 */

import { test, expect } from '@playwright/test';

test.describe('Footer Regression Tests', () => {
  test('footer is visible on public pages', async ({ page }) => {
    // Test on home page
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');
    await expect(footer).toBeVisible();

    // Verify footer contains expected sections by checking for key text
    await expect(footer).toContainText('About');
    await expect(footer).toContainText('Legal');
    await expect(footer).toContainText('Connect');
  });

  test('footer appears on events page', async ({ page }) => {
    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');
    await expect(footer).toBeVisible();
  });

  test('footer links work correctly - Privacy Policy navigation', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');

    // Find and click Privacy Policy link
    const privacyLink = footer.locator('a[href="/privacy-policy"]');
    await expect(privacyLink).toBeVisible();

    await privacyLink.click();
    await page.waitForLoadState('domcontentloaded');

    // Verify navigation occurred
    expect(page.url()).toContain('/privacy-policy');
  });

  test('footer links work correctly - About Us navigation', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');

    // Find and click About Us link
    const aboutLink = footer.locator('a[href="/about-us"]');
    await expect(aboutLink).toBeVisible();

    await aboutLink.click();
    await page.waitForLoadState('domcontentloaded');

    // Verify navigation occurred
    expect(page.url()).toContain('/about-us');
  });

  test('footer displays 3 columns on desktop', async ({ page }) => {
    // Set desktop viewport
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');

    // Verify all three section headings are visible (indicates columns are displayed)
    await expect(footer.locator('text=About').first()).toBeVisible();
    await expect(footer.locator('text=Legal').first()).toBeVisible();
    await expect(footer.locator('text=Connect').first()).toBeVisible();

    // Verify all sections have visible links (not in accordion mode)
    await expect(footer.locator('a:has-text("About Us")')).toBeVisible();
    await expect(footer.locator('a:has-text("Privacy Policy")')).toBeVisible();
    await expect(footer.locator('a:has-text("Contact Us")')).toBeVisible();
  });

  test('footer accordion works on mobile', async ({ page }) => {
    // Set mobile viewport (iPhone 13)
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');

    // Find accordion control button for About section
    const aboutControl = footer.locator('button').filter({ hasText: /about/i }).first();
    await expect(aboutControl).toBeVisible();

    // Verify initially collapsed
    let isExpanded = await aboutControl.getAttribute('aria-expanded');
    expect(isExpanded).toBe('false');

    // Click to expand
    await aboutControl.click();
    await page.waitForTimeout(400); // Wait for accordion animation

    // Verify now expanded
    isExpanded = await aboutControl.getAttribute('aria-expanded');
    expect(isExpanded).toBe('true');

    // Verify links are now visible
    await expect(footer.locator('a:has-text("About Us")')).toBeVisible();
    await expect(footer.locator('a:has-text("Code of Conduct")')).toBeVisible();

    // Click again to collapse
    await aboutControl.click();
    await page.waitForTimeout(400); // Wait for accordion animation

    // Verify collapsed again
    isExpanded = await aboutControl.getAttribute('aria-expanded');
    expect(isExpanded).toBe('false');
  });

  test('footer social media links open in new tab', async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');

    // FetLife link - test if it exists
    const fetLifeLink = footer.locator('a[href="https://fetlife.com/witchcityrope"]');
    const fetLifeExists = await fetLifeLink.count() > 0;

    if (fetLifeExists) {
      await expect(fetLifeLink).toBeVisible();
      await expect(fetLifeLink).toHaveAttribute('target', '_blank');
    }

    // Instagram link - test if it exists
    const instagramLink = footer.locator('a[href="https://instagram.com/witchcityrope"]');
    const instagramExists = await instagramLink.count() > 0;

    if (instagramExists) {
      await expect(instagramLink).toBeVisible();
      await expect(instagramLink).toHaveAttribute('target', '_blank');
    }

    // Verify at least one social link exists
    expect(fetLifeExists || instagramExists).toBe(true);
  });

  test('footer email link has correct mailto href', async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');
    const emailLink = footer.locator('a[href="mailto:info@witchcityrope.com"]');

    await expect(emailLink).toBeVisible();
    const href = await emailLink.getAttribute('href');
    expect(href).toBe('mailto:info@witchcityrope.com');
  });

  test('footer displays copyright text', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const footer = page.locator('footer');
    await expect(footer).toContainText('© 2025 WitchCityRope');
  });
});
