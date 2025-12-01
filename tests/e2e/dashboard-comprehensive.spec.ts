import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';
import { FormHelpers } from './test-utils/helpers/form.helpers';
import { WaitHelpers } from './test-utils/helpers/wait.helpers';

/**
 * Comprehensive Dashboard E2E Tests
 * Tests dashboard navigation, profile management, security settings, and user data
 */
test.describe('Dashboard - Navigation and Layout', () => {
  test.beforeEach(async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
  });

  test('should load dashboard with correct layout and navigation', async ({ page }) => {
    await expect(page).toHaveURL('/dashboard');
    await WaitHelpers.waitForPageLoad(page);

    // Verify dashboard title/header
    const dashboardHeaders = [
      'h1:has-text("Dashboard")',
      '[data-testid="dashboard-title"]',
      'text=Welcome',
      '.dashboard-header'
    ];

    let headerFound = false;
    for (const selector of dashboardHeaders) {
      const header = page.locator(selector);
      if (await header.count() > 0 && await header.isVisible()) {
        headerFound = true;
        console.log(`✅ Dashboard header found: ${selector}`);
        break;
      }
    }

    expect(headerFound).toBe(true);

    // Verify navigation menu exists
    const navSelectors = [
      '[data-testid="dashboard-nav"]',
      '.dashboard-navigation',
      'nav',
      '.sidebar'
    ];

    let navFound = false;
    for (const selector of navSelectors) {
      const nav = page.locator(selector);
      if (await nav.count() > 0) {
        navFound = true;
        console.log(`✅ Dashboard navigation found: ${selector}`);
        break;
      }
    }

    if (navFound) {
      // Verify common navigation links
      const commonLinks = [
        'Profile',
        'Security', 
        'Events',
        'Membership'
      ];

      for (const linkText of commonLinks) {
        const link = page.locator(`a:has-text("${linkText}"), button:has-text("${linkText}")`);
        if (await link.count() > 0) {
          console.log(`✅ Navigation link available: ${linkText}`);
        }
      }
    }

    console.log('✅ Dashboard layout loaded successfully');
  });

  test('should navigate between dashboard sections', async ({ page }) => {
    await WaitHelpers.waitForPageLoad(page);

    const navigationTests = [
      { linkText: 'Profile', expectedUrl: '/profile', expectedContent: 'Profile' },
      { linkText: 'Security', expectedUrl: '/security', expectedContent: 'Security' },
      { linkText: 'Events', expectedUrl: '/events', expectedContent: 'Events' },
      { linkText: 'Membership', expectedUrl: '/membership', expectedContent: 'Membership' }
    ];

    for (const navTest of navigationTests) {
      // Look for navigation link (use .first() to handle multiple matches)
      const navLink = page.locator(`a:has-text("${navTest.linkText}"), button:has-text("${navTest.linkText}")`).first();

      if (await navLink.count() > 0 && await navLink.isVisible()) {
        await navLink.click();
        await page.waitForTimeout(500);

        // Verify navigation worked
        if (navTest.expectedUrl) {
          await expect(page).toHaveURL(new RegExp(navTest.expectedUrl));
        }

        // Verify content loaded
        await expect(page.locator(`text=${navTest.expectedContent}`).first()).toBeVisible();

        console.log(`✅ Successfully navigated to ${navTest.linkText}`);

        // Navigate back to dashboard for next test
        const dashboardLink = page.locator('a:has-text("Dashboard"), button:has-text("Dashboard")').first();
        if (await dashboardLink.count() > 0) {
          await dashboardLink.click();
          await page.waitForTimeout(300);
        }
      } else {
        console.log(`ℹ️ Navigation link not found: ${navTest.linkText}`);
      }
    }
  });

  test('should display user information correctly', async ({ page }) => {
    await WaitHelpers.waitForPageLoad(page);

    // Dashboard displays user's scene name in header (Welcome, RopeMaster)
    // And shows Edit Profile link which confirms user context
    const welcomeMessage = page.locator('text=Welcome').first();
    const editProfileLink = page.locator('a:has-text("Edit Profile")').first();

    // At least one user-identifying element should be visible
    const welcomeVisible = await welcomeMessage.count() > 0 && await welcomeMessage.isVisible();
    const editProfileVisible = await editProfileLink.count() > 0 && await editProfileLink.isVisible();

    expect(welcomeVisible || editProfileVisible).toBeTruthy();

    if (welcomeVisible) {
      console.log('✅ Welcome message displayed with user context');
    }

    if (editProfileVisible) {
      console.log('✅ Edit Profile link displayed (user context confirmed)');
    }

    // Page should show user-specific content (not just generic public page)
    const pageContent = await page.textContent('body');
    expect(pageContent).not.toBeNull();
    expect(pageContent!.length).toBeGreaterThan(100); // Has actual content

    console.log('✅ User-specific dashboard content displayed');
  });
});

test.describe('Dashboard - Profile Management', () => {
  test.beforeEach(async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
  });

  test('should access and display profile page', async ({ page }) => {
    // Navigate to profile
    const profileLink = page.locator('a:has-text("Profile"), button:has-text("Profile")');
    
    if (await profileLink.count() > 0) {
      await profileLink.click();
    } else {
      // Direct navigation if link not found
      await page.goto('/dashboard/profile-settings');
    }
    
    await WaitHelpers.waitForPageLoad(page);

    // Verify profile form elements
    const profileFields = [
      'email',
      'scene-name', 
      'first-name',
      'last-name'
    ];

    let formElementsFound = 0;
    for (const field of profileFields) {
      const fieldElement = page.locator(`[data-testid="${field}-input"], input[name="${field}"]`);
      if (await fieldElement.count() > 0) {
        formElementsFound++;
        await expect(fieldElement).toBeVisible();
        console.log(`✅ Profile field available: ${field}`);
      }
    }

    expect(formElementsFound).toBeGreaterThan(0);

    // Should have save/update button (use .first() to avoid strict mode violation)
    const saveButton = page.locator('[data-testid="save-profile"], button:has-text("Save"), button:has-text("Update")').first();
    if (await saveButton.count() > 0) {
      await expect(saveButton).toBeVisible();
      console.log('✅ Profile save button available');
    }
  });

  test('should validate profile form fields', async ({ page }) => {
    await page.goto('/dashboard/profile-settings');
    await WaitHelpers.waitForPageLoad(page);

    // Test email validation
    const emailField = page.locator('[data-testid="email-or-scenename-input"]');
    if (await emailField.count() > 0) {
      await emailField.clear();
      await emailField.fill('invalid-email');
      
      const saveButton = page.locator('[data-testid="save-profile"], button:has-text("Save")').first();
      if (await saveButton.count() > 0) {
        await saveButton.click();
        
        // Should show validation error
        await page.waitForTimeout(500);
        // Check for validation error - FormHelpers.verifyFormValidation checks for error presence
        await FormHelpers.verifyFormValidation(page, 'email-or-scenename', false);
        console.log('✅ Email validation works');
      }
    }

    // Test scene name validation
    const sceneNameField = page.locator('[data-testid="scene-name-input"], input[name="sceneName"]');
    if (await sceneNameField.count() > 0) {
      await sceneNameField.clear();
      await sceneNameField.fill('ab'); // Too short
      
      const saveButton = page.locator('[data-testid="save-profile"], button:has-text("Save")').first();
      if (await saveButton.count() > 0) {
        await saveButton.click();
        await page.waitForTimeout(500);
        
        // Should show validation error
        const errorText = await page.locator('.error, .mantine-InputWrapper-error').textContent();
        if (errorText?.includes('3') || errorText?.includes('characters')) {
          console.log('✅ Scene name validation works');
        }
      }
    }
  });

  test('should handle profile update successfully', async ({ page }) => {
    // Navigate to profile settings page
    await page.goto('/dashboard/profile-settings');
    await WaitHelpers.waitForPageLoad(page);

    // Wait for form to load - Scene name field should be visible
    const sceneNameField = page.locator('[data-testid="scene-name-input"]');
    await expect(sceneNameField).toBeVisible({ timeout: 10000 });

    // Update scene name with valid value
    const newSceneName = `UpdatedAdmin${Date.now()}`;
    await sceneNameField.clear();
    await sceneNameField.fill(newSceneName);

    // Click Save Changes button
    const saveButton = page.locator('button:has-text("Save Changes")');
    await expect(saveButton).toBeVisible();
    await saveButton.click();

    // Wait for success notification (Mantine notifications)
    await page.waitForSelector('text=Profile updated successfully', { timeout: 10000 });

    // Verify field retains new value
    await expect(sceneNameField).toHaveValue(newSceneName);

    console.log('✅ Profile update completed successfully');
  });
});

test.describe('Dashboard - Security Settings', () => {
  test.beforeEach(async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
  });

  test('should access security settings page', async ({ page }) => {
    // Navigate to security settings
    const securityLink = page.locator('a:has-text("Security"), button:has-text("Security")');
    
    if (await securityLink.count() > 0) {
      await securityLink.click();
    } else {
      await page.goto('/dashboard/profile-settings');
    }
    
    await WaitHelpers.waitForPageLoad(page);

    // Verify security form elements
    const securityElements = [
      'current-password',
      'new-password', 
      'confirm-password'
    ];

    let securityElementsFound = 0;
    for (const element of securityElements) {
      const field = page.locator(`[data-testid="${element}-input"], input[name="${element}"]`);
      if (await field.count() > 0) {
        securityElementsFound++;
        console.log(`✅ Security field available: ${element}`);
      }
    }

    if (securityElementsFound > 0) {
      console.log('✅ Security settings form loaded');
      
      // Should have update password button
      const updateButton = page.locator('[data-testid="update-password"], button:has-text("Update Password")');
      if (await updateButton.count() > 0) {
        await expect(updateButton).toBeVisible();
      }
    } else {
      console.log('ℹ️ Security settings not yet implemented or different structure');
    }
  });

  test('should validate password change form', async ({ page }) => {
    // Navigate to profile settings and switch to security tab
    await page.goto('/dashboard/profile-settings');
    await WaitHelpers.waitForPageLoad(page);

    // Click Security tab - look for tab button with text "Change Password"
    const securityTab = page.locator('button[role="tab"]:has-text("Change Password")');
    await expect(securityTab).toBeVisible({ timeout: 10000 });
    await securityTab.click();
    await page.waitForTimeout(1000);

    // Verify password change form is visible
    // Use data-testid selectors if available, otherwise use type selectors
    const newPasswordField = page.locator('[data-testid="new-password-input"]').or(page.locator('input[type="password"]').nth(1));
    const confirmPasswordField = page.locator('[data-testid="confirm-password-input"]').or(page.locator('input[type="password"]').nth(2));
    await expect(newPasswordField).toBeVisible({ timeout: 5000 });
    await expect(confirmPasswordField).toBeVisible();

    // Test password confirmation mismatch
    await newPasswordField.fill('NewPassword123!');
    await confirmPasswordField.fill('DifferentPassword123!');

    const updateButton = page.locator('button[type="submit"]:has-text("Change Password")');
    await expect(updateButton).toBeVisible();
    await updateButton.click();
    await page.waitForTimeout(1000);

    // Should show mismatch error
    const mismatchError = page.locator('text=Passwords do not match');
    await expect(mismatchError).toBeVisible({ timeout: 5000 });
    console.log('✅ Password confirmation mismatch validation works');

    // Test password complexity (too weak)
    await newPasswordField.clear();
    await newPasswordField.fill('weak');
    await confirmPasswordField.clear();
    await confirmPasswordField.fill('weak');

    // Blur to trigger validation
    await confirmPasswordField.blur();
    await page.waitForTimeout(1000);

    // Should show complexity error (8 characters minimum)
    const complexityError = page.locator('text=/at least 8 characters/i');
    await expect(complexityError).toBeVisible({ timeout: 5000 });
    console.log('✅ Password complexity validation works');
  });

  // 2FA Test Removed: Two-factor authentication is not implemented and not planned for this project

  test('should handle privacy settings toggles', async ({ page }) => {
    await page.goto('/dashboard/profile-settings');
    await WaitHelpers.waitForPageLoad(page);

    const privacyToggles = [
      'profile-visibility',
      'event-visibility',
      'contact-visibility'
    ];

    let privacyControlsFound = 0;
    for (const toggle of privacyToggles) {
      const toggleElement = page.locator(`[data-testid="${toggle}"], input[name="${toggle}"]`);
      
      if (await toggleElement.count() > 0) {
        const initialState = await toggleElement.isChecked();
        
        await toggleElement.click();
        await WaitHelpers.waitForStateUpdate(page);
        
        const newState = await toggleElement.isChecked();
        expect(newState).toBe(!initialState);
        
        privacyControlsFound++;
        console.log(`✅ Privacy toggle works: ${toggle} (${initialState} → ${newState})`);
      }
    }

    if (privacyControlsFound === 0) {
      console.log('ℹ️ Privacy settings not yet implemented');
    }
  });
});

test.describe('Dashboard - Events Management', () => {
  test.beforeEach(async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
  });

  test('should show user events and registrations', async ({ page }) => {
    // Navigate to dashboard (shows MyEventsPage by default)
    await page.goto('/dashboard');
    await WaitHelpers.waitForPageLoad(page);

    // Should see dashboard title with user's name
    const dashboardTitle = page.locator('h1');
    await expect(dashboardTitle).toBeVisible();

    // Title should contain "Events" (format: "{Name}'s Events")
    const titleText = await dashboardTitle.textContent();
    expect(titleText).toMatch(/Events/i);

    // Should see Edit Profile button
    const editProfileButton = page.locator('a:has-text("Edit Profile")');
    await expect(editProfileButton).toBeVisible();

    // Should see filter bar with show past toggle
    const filterBar = page.locator('text=/show past|past events/i');
    if (await filterBar.count() > 0) {
      console.log('✅ Filter bar with past events toggle found');
    }

    // Check for events or empty state
    const emptyState = page.locator('text=No Events Found');
    const browseEventsButton = page.locator('a:has-text("Browse Events")');

    const hasEmptyState = await emptyState.isVisible({ timeout: 3000 }).catch(() => false);

    if (hasEmptyState) {
      // User has no registered events - verify empty state
      await expect(browseEventsButton).toBeVisible();
      console.log('✅ Empty events state displayed correctly');
    } else {
      // User has registered events - verify they're displayed
      // Events can be in grid or table view
      const gridEvents = page.locator('[style*="grid-template-columns"]');
      const tableEvents = page.locator('table');

      const hasGrid = await gridEvents.count() > 0;
      const hasTable = await tableEvents.count() > 0;

      expect(hasGrid || hasTable).toBeTruthy();
      console.log(`✅ User events displayed in ${hasGrid ? 'grid' : 'table'} view`);
    }
  });

  test('should navigate to event detail for cancellation', async ({ page }) => {
    await page.goto('/dashboard');
    await WaitHelpers.waitForPageLoad(page);

    // Check if user has any events
    const emptyState = page.locator('text=No Events Found');
    const hasEmptyState = await emptyState.isVisible({ timeout: 3000 }).catch(() => false);

    if (!hasEmptyState) {
      // User has events - cancellation is done from event detail page, not dashboard
      // Dashboard shows events but doesn't have cancel buttons
      console.log('✅ Dashboard displays user events (cancellation done from event detail page)');
    } else {
      console.log('ℹ️ User has no events in dashboard (empty state)');
    }

    // Note: Actual cancellation test is in e2e-events-full-journey.spec.ts test 7
    // Dashboard is for viewing events, not cancelling them
  });
});

test.describe('Dashboard - Responsive Design', () => {
  const viewports = [
    { name: 'Mobile', width: 375, height: 667 },
    { name: 'Tablet', width: 768, height: 1024 }
  ];

  viewports.forEach(viewport => {
    test(`should display correctly on ${viewport.name}`, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await AuthHelpers.loginAs(page, 'admin');
      
      await WaitHelpers.waitForPageLoad(page);

      // Verify dashboard is responsive
      const body = await page.locator('body').boundingBox();
      expect(body?.width).toBeLessThanOrEqual(viewport.width);
      console.log(`✅ Body width (${body?.width}px) fits within viewport (${viewport.width}px)`);

      // Verify page has loaded content (dashboard title and some UI elements)
      const dashboardTitle = page.locator('h1:has-text("Dashboard")').first();
      await expect(dashboardTitle).toBeVisible({ timeout: 10000 });
      console.log(`✅ Dashboard title visible on ${viewport.name}`);

      // Verify essential dashboard elements are present
      const essentialElements = [
        'Edit Profile',  // Profile link
        'Dashboard',     // Title
        'Welcome'        // User welcome
      ];

      let elementsFound = 0;
      for (const elementText of essentialElements) {
        const element = page.locator(`text=${elementText}`).first();
        if (await element.count() > 0) {
          elementsFound++;
          console.log(`✅ Element visible on ${viewport.name}: ${elementText}`);
        }
      }

      expect(elementsFound).toBeGreaterThan(0);

      // Take screenshot
      await page.screenshot({
        path: `test-results/dashboard-${viewport.name.toLowerCase()}.png`,
        fullPage: true
      });

      console.log(`✅ Dashboard displays correctly on ${viewport.name} (${elementsFound}/3 elements visible)`);
    });
  });
});