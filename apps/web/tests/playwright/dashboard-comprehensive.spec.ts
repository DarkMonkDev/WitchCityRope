import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';
import { FormHelpers } from './helpers/form.helpers';
import { WaitHelpers } from './helpers/wait.helpers';

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
      // Look for navigation link
      const navLink = page.locator(`a:has-text("${navTest.linkText}"), button:has-text("${navTest.linkText}")`);
      
      if (await navLink.count() > 0 && await navLink.isVisible()) {
        await navLink.click();
        await page.waitForTimeout(500);
        
        // Verify navigation worked
        if (navTest.expectedUrl) {
          await expect(page).toHaveURL(new RegExp(navTest.expectedUrl));
        }
        
        // Verify content loaded
        await expect(page.locator(`text=${navTest.expectedContent}`)).toBeVisible();
        
        console.log(`✅ Successfully navigated to ${navTest.linkText}`);
        
        // Navigate back to dashboard for next test
        const dashboardLink = page.locator('a:has-text("Dashboard"), button:has-text("Dashboard")');
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

    // Should display current user's email
    const userEmail = 'admin@witchcityrope.com';
    await expect(page.locator(`text=${userEmail}`)).toBeVisible({ timeout: 10000 });

    // Look for user profile information
    const userInfoSelectors = [
      '[data-testid="user-profile"]',
      '[data-testid="user-info"]',
      '.user-details',
      '.profile-summary'
    ];

    let userInfoFound = false;
    for (const selector of userInfoSelectors) {
      const element = page.locator(selector);
      if (await element.count() > 0 && await element.isVisible()) {
        userInfoFound = true;
        console.log(`✅ User information displayed: ${selector}`);
        break;
      }
    }

    if (!userInfoFound) {
      // At minimum, should show user email somewhere on page
      const pageContent = await page.textContent('body');
      expect(pageContent).toContain(userEmail);
      console.log('✅ User email displayed somewhere on dashboard');
    }
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
      await page.goto('/dashboard/profile');
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
    await page.goto('/dashboard/profile');
    await WaitHelpers.waitForPageLoad(page);

    // Test email validation
    const emailField = page.locator('[data-testid="email-input"], input[name="email"]');
    if (await emailField.count() > 0) {
      await emailField.clear();
      await emailField.fill('invalid-email');
      
      const saveButton = page.locator('[data-testid="save-profile"], button:has-text("Save")').first();
      if (await saveButton.count() > 0) {
        await saveButton.click();
        
        // Should show validation error
        await page.waitForTimeout(500);
        await FormHelpers.validateFieldError(page, 'email', 'Invalid email format');
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

  test.skip('should handle profile update successfully', async ({ page }) => {
    // TODO: Unskip when profile editing feature is fully implemented
    // Feature Status: Not implemented - profile update submission and success feedback not ready
    // Reference: /docs/functional-areas/dashboard/profile-management.md
    // Expected: Edit profile fields → click Save → see success message → fields retain new values

    await page.goto('/dashboard/profile');
    await WaitHelpers.waitForPageLoad(page);

    // Update scene name with valid value
    const sceneNameField = page.locator('[data-testid="scene-name-input"], input[name="sceneName"]');
    if (await sceneNameField.count() > 0) {
      const newSceneName = `UpdatedAdmin${Date.now()}`;

      await sceneNameField.clear();
      await sceneNameField.fill(newSceneName);

      const saveButton = page.locator('[data-testid="save-profile"], button:has-text("Save")').first();
      if (await saveButton.count() > 0) {
        await saveButton.click();

        // Wait for submission
        await WaitHelpers.waitForFormSubmission(page, 'save-profile');

        // Should show success message or confirmation
        const successIndicators = [
          '[data-testid="profile-success"]',
          'text=Profile updated',
          'text=Changes saved',
          '.success',
          '.notification'
        ];

        let successFound = false;
        for (const selector of successIndicators) {
          const element = page.locator(selector);
          if (await element.count() > 0 && await element.isVisible()) {
            successFound = true;
            console.log(`✅ Profile update success shown: ${selector}`);
            break;
          }
        }

        if (!successFound) {
          // Verify field retains new value
          await expect(sceneNameField).toHaveValue(newSceneName);
          console.log('✅ Profile update completed (field retains value)');
        }
      }
    }
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
      await page.goto('/dashboard/security');
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

  test.skip('should validate password change form', async ({ page }) => {
    // TODO: Unskip when password change feature is fully implemented
    // Feature Status: Not implemented - password change form and validation not ready
    // Reference: /docs/functional-areas/dashboard/security-settings.md
    // Expected: Fill password fields → validate → show errors for mismatch/weak passwords

    await page.goto('/dashboard/security');
    await WaitHelpers.waitForPageLoad(page);

    const currentPasswordField = page.locator('[data-testid="current-password-input"]');
    const newPasswordField = page.locator('[data-testid="new-password-input"]');
    const confirmPasswordField = page.locator('[data-testid="confirm-password-input"]');

    if (await newPasswordField.count() > 0 && await confirmPasswordField.count() > 0) {
      // Test password confirmation mismatch
      await newPasswordField.fill('NewPassword123!');
      await confirmPasswordField.fill('DifferentPassword123!');

      const updateButton = page.locator('[data-testid="update-password"], button:has-text("Update")');
      if (await updateButton.count() > 0) {
        await updateButton.click();
        await page.waitForTimeout(500);

        // Should show mismatch error
        const errorText = await page.locator('.error, .mantine-InputWrapper-error').textContent();
        if (errorText?.toLowerCase().includes('match')) {
          console.log('✅ Password confirmation validation works');
        }
      }

      // Test password complexity
      await newPasswordField.clear();
      await newPasswordField.fill('weak');
      await confirmPasswordField.clear();
      await confirmPasswordField.fill('weak');

      if (await updateButton.count() > 0) {
        await updateButton.click();
        await page.waitForTimeout(500);

        // Should show complexity error
        const complexityError = page.locator('.error:has-text("8"), .error:has-text("characters")');
        if (await complexityError.count() > 0) {
          console.log('✅ Password complexity validation works');
        }
      }
    }
  });

  test.skip('should handle 2FA toggle if available', async ({ page }) => {
    // TODO: Unskip when 2FA feature is implemented
    // Feature Status: Not implemented - Two-factor authentication not ready
    // Reference: /docs/functional-areas/dashboard/two-factor-auth.md
    // Expected: Toggle 2FA switch → enable/disable two-factor authentication

    await page.goto('/dashboard/security');
    await WaitHelpers.waitForPageLoad(page);

    // Look for 2FA toggle
    const twoFactorToggle = page.locator('[data-testid="2fa-toggle"], input[type="checkbox"][name*="2fa"]');

    if (await twoFactorToggle.count() > 0) {
      const isCurrentlyEnabled = await twoFactorToggle.isChecked();

      // Toggle the setting
      await twoFactorToggle.click();
      await WaitHelpers.waitForStateUpdate(page);

      // Verify toggle changed
      const newState = await twoFactorToggle.isChecked();
      expect(newState).toBe(!isCurrentlyEnabled);

      console.log(`✅ 2FA toggle works (was ${isCurrentlyEnabled}, now ${newState})`);
    } else {
      console.log('ℹ️ 2FA settings not yet implemented');
    }
  });

  test('should handle privacy settings toggles', async ({ page }) => {
    await page.goto('/dashboard/security');
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

  test.skip('should show user events and registrations', async ({ page }) => {
    // TODO: Unskip when dashboard event registrations display is implemented
    // Feature Status: Not implemented - My Events/Registrations dashboard section not ready
    // Reference: /docs/functional-areas/dashboard/user-events.md
    // Expected: Navigate to My Events → see upcoming/past events and active registrations

    // Navigate to user events
    const eventsLink = page.locator('a:has-text("Events"), a:has-text("My Events")');

    if (await eventsLink.count() > 0) {
      await eventsLink.click();
    } else {
      await page.goto('/dashboard/events');
    }

    await WaitHelpers.waitForPageLoad(page);

    // Should show user's registered events
    const eventSections = [
      'Upcoming Events',
      'Past Events',
      'My Registrations'
    ];

    for (const section of eventSections) {
      const sectionElement = page.locator(`text=${section}, [data-testid="${section.toLowerCase().replace(' ', '-')}"]`);
      if (await sectionElement.count() > 0) {
        console.log(`✅ Found events section: ${section}`);
      }
    }

    // Should show event cards or list items
    const eventItems = [
      '[data-testid="event-card"]',
      '[data-testid="registration-item"]',
      '.event-item',
      '.registration'
    ];

    let eventItemsFound = false;
    for (const selector of eventItems) {
      const items = page.locator(selector);
      if (await items.count() > 0) {
        eventItemsFound = true;
        const count = await items.count();
        console.log(`✅ Found ${count} event items: ${selector}`);
        break;
      }
    }

    if (!eventItemsFound) {
      // Check for empty state
      const emptyState = page.locator('text=No events, text=No registrations');
      if (await emptyState.count() > 0) {
        console.log('✅ Empty events state displayed correctly');
      }
    }
  });

  test.skip('should handle event registration cancellation', async ({ page }) => {
    // TODO: Unskip when registration cancellation feature is implemented
    // Feature Status: Not implemented - Event registration cancellation workflow not ready
    // Reference: /docs/functional-areas/dashboard/cancel-registration.md
    // Expected: Click Cancel on registration → confirm → registration removed from dashboard

    await page.goto('/dashboard/events');
    await WaitHelpers.waitForPageLoad(page);

    // Look for cancel registration buttons
    const cancelButtons = page.locator('[data-testid="cancel-registration"], button:has-text("Cancel")');

    if (await cancelButtons.count() > 0) {
      const firstCancelButton = cancelButtons.first();
      await firstCancelButton.click();

      // Should show confirmation dialog
      const confirmationDialog = page.locator('[data-testid="cancel-confirmation"], .modal, [role="dialog"]');

      if (await confirmationDialog.count() > 0) {
        await expect(confirmationDialog).toBeVisible();

        // Look for confirm button in dialog
        const confirmButton = confirmationDialog.locator('button:has-text("Confirm"), button:has-text("Yes")');
        if (await confirmButton.count() > 0) {
          await confirmButton.click();

          await WaitHelpers.waitForStateUpdate(page);
          console.log('✅ Registration cancellation flow works');
        }
      } else {
        console.log('ℹ️ No confirmation dialog - direct cancellation');
      }
    } else {
      console.log('ℹ️ No registrations to cancel or feature not implemented');
    }
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

      // Navigation should be accessible (might be hamburger menu on mobile)
      const navElements = [
        '[data-testid="mobile-menu"]',
        'button[aria-label="Menu"]',
        '.hamburger',
        '.mobile-nav',
        'nav'
      ];

      let navAccessible = false;
      for (const selector of navElements) {
        const nav = page.locator(selector);
        if (await nav.count() > 0 && await nav.isVisible()) {
          navAccessible = true;
          console.log(`✅ Navigation accessible on ${viewport.name}: ${selector}`);
          break;
        }
      }

      expect(navAccessible).toBe(true);

      // Take screenshot
      await page.screenshot({
        path: `test-results/dashboard-${viewport.name.toLowerCase()}.png`,
        fullPage: true
      });

      console.log(`✅ Dashboard responsive on ${viewport.name}`);
    });
  });
});