import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

/**
 * Quick Mobile FAB Test
 * Isolated test to verify mobile FAB button fix
 */
test('Quick Test: Mobile FAB button click opens editor', async ({ page }) => {
  // 1. Set viewport to mobile FIRST
  await page.setViewportSize({ width: 375, height: 667 });
  console.log('✅ Viewport set to mobile (375×667)');

  // 2. Login as admin
  await AuthHelpers.loginAs(page, 'admin');
  console.log('✅ Logged in as admin');

  // 3. Navigate to CMS page
  await page.goto('http://localhost:5173/resources');
  await page.waitForLoadState('networkidle');
  console.log('✅ Navigated to /resources');

  // 4. Wait a moment for media queries to apply
  await page.waitForTimeout(1000);

  // 5. Verify FAB button exists using test ID
  const fabButton = page.locator('[data-testid="cms-edit-fab"]');
  await expect(fabButton).toBeVisible({ timeout: 5000 });
  console.log('✅ FAB button visible');

  // 6. Verify desktop button is hidden
  const desktopButton = page.locator('[data-testid="cms-edit-button"]');
  await expect(desktopButton).not.toBeVisible();
  console.log('✅ Desktop button hidden');

  // 7. Click FAB button
  await fabButton.click();
  console.log('✅ Clicked FAB button');
  await page.waitForTimeout(500);

  // 8. Verify editor appears
  const editor = page.locator('[contenteditable="true"]').first();
  await expect(editor).toBeVisible({ timeout: 5000 });
  console.log('✅ Editor opened successfully');
});
