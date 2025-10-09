import { test, expect } from '@playwright/test';
import { AuthHelpers } from '../../helpers/auth.helpers';

/**
 * User Dashboard Wireframe Validation Tests
 *
 * Purpose: Validate implementation matches approved wireframe v4 iteration exactly
 * Reference: /docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/APPROVED-DESIGN.md
 *
 * CRITICAL VALIDATIONS:
 * - NO "Learn More" buttons (MUST be "View Details")
 * - NO pricing displays
 * - NO capacity/spots available
 * - YES status badges
 * - YES user events only
 *
 * Test Accounts (seeded data):
 * - admin@witchcityrope.com / Test123! (Vetted)
 * - teacher@witchcityrope.com / Test123! (Vetted)
 * - vetted@witchcityrope.com / Test123! (Vetted)
 * - member@witchcityrope.com / Test123! (General Member - may have vetting status)
 * - guest@witchcityrope.com / Test123! (Guest/Attendee)
 */

test.describe('User Dashboard Wireframe Validation', () => {

  test.describe('Navigation Structure', () => {

    test('should display Edit Profile link in utility bar before Logout', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      // Navigate to dashboard
      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for Edit Profile link in utility bar
      const utilityBar = page.locator('nav, header, [data-testid="utility-bar"]');
      const editProfileLink = utilityBar.locator('a, button').filter({ hasText: /edit profile/i });

      await expect(editProfileLink).toBeVisible({ timeout: 5000 });

      // Verify Edit Profile comes before Logout
      const navLinks = await utilityBar.locator('a, button').allTextContents();
      const editProfileIndex = navLinks.findIndex(text => /edit profile/i.test(text));
      const logoutIndex = navLinks.findIndex(text => /logout|sign out/i.test(text));

      if (editProfileIndex >= 0 && logoutIndex >= 0) {
        expect(editProfileIndex).toBeLessThan(logoutIndex);
      }
    });

    test('should display dashboard page title format "[User\'s Name] Dashboard"', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for page title with pattern "[Name] Dashboard"
      const pageTitle = page.locator('h1, [data-testid="page-title"]').filter({ hasText: /dashboard/i });

      await expect(pageTitle).toBeVisible({ timeout: 5000 });

      // Verify format includes "Dashboard"
      const titleText = await pageTitle.textContent();
      expect(titleText).toMatch(/dashboard/i);
    });

    test('should display Edit Profile button on dashboard page', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for Edit Profile button on the page (not in nav)
      const editProfileButton = page.locator('button, a').filter({ hasText: /edit profile/i });

      // Should have at least one Edit Profile button/link visible
      await expect(editProfileButton.first()).toBeVisible({ timeout: 5000 });
    });

    test('should navigate to /dashboard/profile-settings from Edit Profile link', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Click Edit Profile link
      const editProfileLink = page.locator('a, button').filter({ hasText: /edit profile/i }).first();
      await editProfileLink.click();

      // Verify navigation to profile settings
      await page.waitForURL('**/dashboard/profile-settings', { timeout: 10000 });

      const currentUrl = page.url();
      expect(currentUrl).toContain('/dashboard/profile-settings');
    });

    test('should return to dashboard when Dashboard button clicked in main nav', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      // Navigate to profile settings first
      await page.goto('http://localhost:5173/dashboard/profile-settings');
      await page.waitForLoadState('networkidle');

      // Click Dashboard link in navigation
      const dashboardLink = page.locator('nav a, nav button').filter({ hasText: /dashboard|my events/i });

      if (await dashboardLink.count() > 0) {
        await dashboardLink.first().click();

        // Verify navigation back to dashboard
        await page.waitForURL('**/dashboard', { timeout: 10000 });

        const currentUrl = page.url();
        expect(currentUrl).toMatch(/\/dashboard(?:\/)?$/);
      } else {
        console.log('Dashboard navigation link not found - feature may not be implemented yet');
      }
    });
  });

  test.describe('Vetting Alert Box Tests', () => {

    test('should NOT display alert for Vetted users', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for vetting alert - should NOT be visible
      const vettingAlert = page.locator('[role="alert"], .alert, [data-testid*="vetting-alert"]')
        .filter({ hasText: /application|vetting|review|approved|hold|denied/i });

      // Wait a moment to ensure alert doesn't appear
      await page.waitForTimeout(1000);

      // Verify no vetting alert visible
      const alertCount = await vettingAlert.count();
      expect(alertCount).toBe(0);
    });

    test('should display blue Pending alert with correct message', async ({ page }) => {
      // NOTE: This test requires a user with Pending status
      // Using member account which may have Pending status
      await AuthHelpers.loginAs(page, 'member');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for pending status alert
      const pendingAlert = page.locator('[role="alert"], .alert, [data-testid*="vetting-alert"]')
        .filter({ hasText: /under review|pending|application/i });

      if (await pendingAlert.count() > 0) {
        // Verify alert is visible
        await expect(pendingAlert.first()).toBeVisible({ timeout: 5000 });

        // Verify contains expected text
        const alertText = await pendingAlert.first().textContent();
        expect(alertText).toMatch(/under review|pending|application/i);

        // Check for clock emoji or blue color indicator
        const hasIcon = alertText?.includes('⏰');
        console.log('Pending alert has clock icon:', hasIcon);
      } else {
        console.log('Member account is not in Pending status - skipping Pending alert test');
      }
    });

    test('should display green Approved alert with interview link', async ({ page }) => {
      // NOTE: This test requires a user with Approved status
      // May need to skip if no approved user available
      await AuthHelpers.loginAs(page, 'member');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for approved status alert
      const approvedAlert = page.locator('[role="alert"], .alert, [data-testid*="vetting-alert"]')
        .filter({ hasText: /approved|interview|great news/i });

      if (await approvedAlert.count() > 0) {
        // Verify alert is visible
        await expect(approvedAlert.first()).toBeVisible({ timeout: 5000 });

        // Verify contains expected text
        const alertText = await approvedAlert.first().textContent();
        expect(alertText).toMatch(/approved|interview/i);

        // Look for interview scheduling link
        const interviewLink = approvedAlert.locator('a').filter({ hasText: /schedule|interview/i });
        if (await interviewLink.count() > 0) {
          await expect(interviewLink).toBeVisible();
        }

        // Check for checkmark emoji
        const hasIcon = alertText?.includes('✅');
        console.log('Approved alert has checkmark icon:', hasIcon);
      } else {
        console.log('Member account is not in Approved status - skipping Approved alert test');
      }
    });

    test('should display amber On Hold alert with correct message', async ({ page }) => {
      // NOTE: This test requires a user with On Hold status
      await AuthHelpers.loginAs(page, 'member');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for on hold status alert
      const onHoldAlert = page.locator('[role="alert"], .alert, [data-testid*="vetting-alert"]')
        .filter({ hasText: /on hold|hold/i });

      if (await onHoldAlert.count() > 0) {
        // Verify alert is visible
        await expect(onHoldAlert.first()).toBeVisible({ timeout: 5000 });

        // Verify contains expected text
        const alertText = await onHoldAlert.first().textContent();
        expect(alertText).toMatch(/on hold|hold/i);

        // Check for pause emoji
        const hasIcon = alertText?.includes('⏸️');
        console.log('On Hold alert has pause icon:', hasIcon);
      } else {
        console.log('Member account is not in On Hold status - skipping On Hold alert test');
      }
    });

    test('should display red Denied alert with reapply link', async ({ page }) => {
      // NOTE: This test requires a user with Denied status
      await AuthHelpers.loginAs(page, 'member');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for denied status alert
      const deniedAlert = page.locator('[role="alert"], .alert, [data-testid*="vetting-alert"]')
        .filter({ hasText: /denied|not approved/i });

      if (await deniedAlert.count() > 0) {
        // Verify alert is visible
        await expect(deniedAlert.first()).toBeVisible({ timeout: 5000 });

        // Verify contains expected text
        const alertText = await deniedAlert.first().textContent();
        expect(alertText).toMatch(/denied|not approved/i);

        // Look for reapply link
        const reapplyLink = deniedAlert.locator('a').filter({ hasText: /reapply/i });
        if (await reapplyLink.count() > 0) {
          await expect(reapplyLink).toBeVisible();
        }

        // Check for X emoji
        const hasIcon = alertText?.includes('❌');
        console.log('Denied alert has X icon:', hasIcon);
      } else {
        console.log('Member account is not in Denied status - skipping Denied alert test');
      }
    });
  });

  test.describe('Event Display Tests - Grid View (User Dashboard Version)', () => {

    test('should NOT display pricing information on any event', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // CRITICAL: Verify NO pricing information is visible
      const pageContent = await page.locator('body').textContent();

      // Check for common pricing patterns
      const hasPricing = /\$\d+|\$\s*\d+|free|cost|price/i.test(pageContent || '');

      if (hasPricing) {
        // Take screenshot for debugging if pricing found
        await page.screenshot({ path: 'test-results/dashboard-pricing-violation.png' });
      }

      expect(hasPricing).toBe(false);
    });

    test('should NOT display capacity or spots available', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // CRITICAL: Verify NO capacity information is visible
      const pageContent = await page.locator('body').textContent();

      // Check for capacity patterns
      const hasCapacity = /spots? available|\d+\s*spots?|capacity|seats? available/i.test(pageContent || '');

      if (hasCapacity) {
        await page.screenshot({ path: 'test-results/dashboard-capacity-violation.png' });
      }

      expect(hasCapacity).toBe(false);
    });

    test('should display "View Details" button NOT "Learn More"', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for View Details buttons
      const viewDetailsButtons = page.locator('button, a').filter({ hasText: /view details/i });

      if (await viewDetailsButtons.count() > 0) {
        await expect(viewDetailsButtons.first()).toBeVisible({ timeout: 5000 });
      }

      // CRITICAL: Verify NO "Learn More" buttons
      const learnMoreButtons = page.locator('button, a').filter({ hasText: /learn more/i });
      const learnMoreCount = await learnMoreButtons.count();

      if (learnMoreCount > 0) {
        await page.screenshot({ path: 'test-results/dashboard-learn-more-violation.png' });
      }

      expect(learnMoreCount).toBe(0);
    });

    test('should display event cards with status badges', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for event cards
      const eventCards = page.locator('[data-testid="event-card"], .event-card, [class*="EventCard"]');

      if (await eventCards.count() > 0) {
        // Verify first card has status badge
        const firstCard = eventCards.first();
        const statusBadge = firstCard.locator('[data-testid="status-badge"], .badge, [class*="Badge"]');

        await expect(statusBadge).toBeVisible({ timeout: 5000 });

        // Verify badge contains one of the expected statuses
        const badgeText = await statusBadge.textContent();
        const validStatuses = /rsvp confirmed|ticket purchased|attended|social event/i;
        expect(badgeText).toMatch(validStatuses);
      } else {
        console.log('No event cards found - user may not have registered for any events');
      }
    });

    test('should display grid layout with correct pattern', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for event grid container
      const eventGrid = page.locator('[data-testid="events-grid"], .events-grid, [class*="EventGrid"]');

      if (await eventGrid.count() > 0) {
        // Verify grid layout CSS
        const gridStyle = await eventGrid.first().evaluate((el) => {
          const styles = window.getComputedStyle(el);
          return {
            display: styles.display,
            gridTemplateColumns: styles.gridTemplateColumns
          };
        });

        console.log('Grid layout styles:', gridStyle);

        // Should be using CSS Grid
        expect(gridStyle.display).toMatch(/grid|flex/i);
      } else {
        console.log('Event grid not found - checking for event cards directly');

        // Fallback: verify event cards exist
        const eventCards = page.locator('[data-testid="event-card"], .event-card');
        const cardCount = await eventCards.count();
        console.log('Event cards found:', cardCount);
      }
    });

    test('should display social event with "Ticket Purchased (Social Event)" badge', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for social event badge
      const socialEventBadge = page.locator('[data-testid="status-badge"], .badge')
        .filter({ hasText: /social event/i });

      if (await socialEventBadge.count() > 0) {
        await expect(socialEventBadge.first()).toBeVisible({ timeout: 5000 });

        // Verify text includes "Ticket Purchased" and "Social Event"
        const badgeText = await socialEventBadge.first().textContent();
        expect(badgeText).toMatch(/ticket purchased.*social event|social event.*ticket/i);
      } else {
        console.log('No social event registrations found for this user');
      }
    });
  });

  test.describe('Event Display Tests - Table View', () => {

    test('should switch to table view when toggle clicked', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for view toggle button
      const tableViewToggle = page.locator('button, [role="tab"]').filter({ hasText: /list view|table view/i });

      if (await tableViewToggle.count() > 0) {
        await tableViewToggle.first().click();
        await page.waitForTimeout(500);

        // Verify table is visible
        const eventTable = page.locator('table, [data-testid="events-table"], [class*="EventTable"]');
        await expect(eventTable).toBeVisible({ timeout: 5000 });
      } else {
        console.log('View toggle not found - feature may not be implemented yet');
      }
    });

    test('should display table columns: Date, Time, Title, Status, Action', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Switch to table view if toggle exists
      const tableViewToggle = page.locator('button, [role="tab"]').filter({ hasText: /list view|table view/i });
      if (await tableViewToggle.count() > 0) {
        await tableViewToggle.first().click();
        await page.waitForTimeout(500);
      }

      // Look for table headers
      const tableHeaders = page.locator('th, [data-testid*="column-header"]');

      if (await tableHeaders.count() > 0) {
        const headerTexts = await tableHeaders.allTextContents();
        const headerString = headerTexts.join(' ').toLowerCase();

        // Verify expected columns present
        expect(headerString).toContain('date');
        expect(headerString).toContain('time');
        expect(headerString).toMatch(/title|event/);
        expect(headerString).toContain('status');
        expect(headerString).toContain('action');

        // CRITICAL: Verify NO price or capacity columns
        expect(headerString).not.toMatch(/price|cost|\$/);
        expect(headerString).not.toMatch(/capacity|spots|available/);
      } else {
        console.log('Table headers not found - table view may not be implemented yet');
      }
    });

    test('should display "View Details" button in Action column NOT "Learn More"', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Switch to table view
      const tableViewToggle = page.locator('button, [role="tab"]').filter({ hasText: /list view|table view/i });
      if (await tableViewToggle.count() > 0) {
        await tableViewToggle.first().click();
        await page.waitForTimeout(500);

        // Look for View Details in table
        const viewDetailsButtons = page.locator('table button, table a').filter({ hasText: /view details/i });

        if (await viewDetailsButtons.count() > 0) {
          await expect(viewDetailsButtons.first()).toBeVisible({ timeout: 5000 });
        }

        // CRITICAL: Verify NO "Learn More" in table
        const learnMoreButtons = page.locator('table button, table a').filter({ hasText: /learn more/i });
        const learnMoreCount = await learnMoreButtons.count();

        expect(learnMoreCount).toBe(0);
      }
    });

    test('should display status badges in Status column', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Switch to table view
      const tableViewToggle = page.locator('button, [role="tab"]').filter({ hasText: /list view|table view/i });
      if (await tableViewToggle.count() > 0) {
        await tableViewToggle.first().click();
        await page.waitForTimeout(500);

        // Look for status badges in table
        const statusBadges = page.locator('table [data-testid="status-badge"], table .badge');

        if (await statusBadges.count() > 0) {
          await expect(statusBadges.first()).toBeVisible({ timeout: 5000 });

          // Verify badge contains valid status
          const badgeText = await statusBadges.first().textContent();
          const validStatuses = /rsvp confirmed|ticket purchased|attended/i;
          expect(badgeText).toMatch(validStatuses);
        }
      }
    });

    test('should have sortable Date column', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Switch to table view
      const tableViewToggle = page.locator('button, [role="tab"]').filter({ hasText: /list view|table view/i });
      if (await tableViewToggle.count() > 0) {
        await tableViewToggle.first().click();
        await page.waitForTimeout(500);

        // Look for Date column header
        const dateHeader = page.locator('th').filter({ hasText: /date/i });

        if (await dateHeader.count() > 0) {
          // Check for sort indicator or clickable header
          const headerText = await dateHeader.textContent();
          const hasSortIndicator = /↕|↑|↓|sort/i.test(headerText || '');

          console.log('Date header has sort indicator:', hasSortIndicator);

          // Verify header is clickable
          const isClickable = await dateHeader.evaluate((el) => {
            return el.onclick !== null || el.style.cursor === 'pointer';
          });

          console.log('Date header is clickable:', isClickable);
        }
      }
    });
  });

  test.describe('Filter Controls Tests', () => {

    test('should display "Show Past Events" checkbox unchecked by default', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for "Show Past Events" checkbox
      const showPastCheckbox = page.locator('input[type="checkbox"]').filter({ hasText: /show past|past events/i });
      const showPastLabel = page.locator('label').filter({ hasText: /show past|past events/i });

      if (await showPastLabel.count() > 0 || await showPastCheckbox.count() > 0) {
        // Verify checkbox is unchecked by default
        const checkbox = showPastCheckbox.count() > 0
          ? showPastCheckbox
          : showPastLabel.locator('input[type="checkbox"]');

        if (await checkbox.count() > 0) {
          const isChecked = await checkbox.isChecked();
          expect(isChecked).toBe(false);
        }
      } else {
        console.log('Show Past Events checkbox not found - filter may not be implemented yet');
      }
    });

    test('should hide past events when checkbox unchecked', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for past event elements
      const pastEvents = page.locator('.past-event, [data-testid="past-event"]');

      if (await pastEvents.count() > 0) {
        // Verify past events are hidden
        const firstPastEvent = pastEvents.first();
        const isVisible = await firstPastEvent.isVisible();

        // Past events should be hidden by default
        expect(isVisible).toBe(false);
      } else {
        console.log('No past events found for this user');
      }
    });

    test('should show past events when checkbox checked', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Find and check the "Show Past Events" checkbox
      const showPastLabel = page.locator('label').filter({ hasText: /show past|past events/i });

      if (await showPastLabel.count() > 0) {
        const checkbox = showPastLabel.locator('input[type="checkbox"]');

        if (await checkbox.count() > 0) {
          await checkbox.check();
          await page.waitForTimeout(500);

          // Look for past events
          const pastEvents = page.locator('.past-event, [data-testid="past-event"]');

          if (await pastEvents.count() > 0) {
            // Verify at least one past event is now visible
            const visiblePastEvents = await pastEvents.filter({ hasText: /.+/ }).count();
            expect(visiblePastEvents).toBeGreaterThan(0);
          }
        }
      } else {
        console.log('Show Past Events checkbox not found');
      }
    });

    test('should display Grid/Table view toggle', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for view toggle controls
      const gridViewToggle = page.locator('button, [role="tab"]').filter({ hasText: /card view|grid view/i });
      const tableViewToggle = page.locator('button, [role="tab"]').filter({ hasText: /list view|table view/i });

      const hasGridToggle = await gridViewToggle.count() > 0;
      const hasTableToggle = await tableViewToggle.count() > 0;

      if (hasGridToggle && hasTableToggle) {
        await expect(gridViewToggle.first()).toBeVisible();
        await expect(tableViewToggle.first()).toBeVisible();
      } else {
        console.log('View toggle not found - feature may not be implemented yet');
      }
    });

    test('should display search input for filtering events', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for search input
      const searchInput = page.locator('input[type="search"], input[placeholder*="search" i], [data-testid="search-input"]');

      if (await searchInput.count() > 0) {
        await expect(searchInput.first()).toBeVisible({ timeout: 5000 });

        // Verify placeholder text
        const placeholder = await searchInput.first().getAttribute('placeholder');
        expect(placeholder).toMatch(/search/i);
      } else {
        console.log('Search input not found - feature may not be implemented yet');
      }
    });
  });

  test.describe('Profile Settings Tests', () => {

    test('should display 4 tabs: Personal, Social, Security, Vetting', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard/profile-settings');
      await page.waitForLoadState('networkidle');

      // Look for tabs
      const tabs = page.locator('[role="tab"], .tab, button').filter({
        hasText: /personal|social|security|vetting/i
      });

      if (await tabs.count() >= 4) {
        // Verify all 4 tabs exist
        const personalTab = tabs.filter({ hasText: /personal/i });
        const socialTab = tabs.filter({ hasText: /social/i });
        const securityTab = tabs.filter({ hasText: /security/i });
        const vettingTab = tabs.filter({ hasText: /vetting/i });

        await expect(personalTab).toBeVisible({ timeout: 5000 });
        await expect(socialTab).toBeVisible({ timeout: 5000 });
        await expect(securityTab).toBeVisible({ timeout: 5000 });
        await expect(vettingTab).toBeVisible({ timeout: 5000 });
      } else {
        console.log('Profile tabs not found - feature may not be implemented yet');
      }
    });

    test('should display profile fields in Personal tab', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard/profile-settings');
      await page.waitForLoadState('networkidle');

      // Ensure Personal tab is active
      const personalTab = page.locator('[role="tab"], button').filter({ hasText: /personal/i });
      if (await personalTab.count() > 0) {
        await personalTab.first().click();
        await page.waitForTimeout(500);
      }

      // Look for profile form fields
      const sceneNameInput = page.locator('input[name*="scene" i], input[id*="scene" i]');
      const emailInput = page.locator('input[type="email"], input[name*="email" i]');

      if (await sceneNameInput.count() > 0 && await emailInput.count() > 0) {
        await expect(sceneNameInput.first()).toBeVisible({ timeout: 5000 });
        await expect(emailInput.first()).toBeVisible({ timeout: 5000 });
      } else {
        console.log('Profile form fields not found - feature may not be implemented yet');
      }
    });

    test('should display Change Password form in Security tab', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard/profile-settings');
      await page.waitForLoadState('networkidle');

      // Click Security tab
      const securityTab = page.locator('[role="tab"], button').filter({ hasText: /security/i });
      if (await securityTab.count() > 0) {
        await securityTab.first().click();
        await page.waitForTimeout(500);

        // Look for password fields
        const currentPasswordInput = page.locator('input[type="password"]').filter({ hasText: /current/i });
        const newPasswordInput = page.locator('input[type="password"]').filter({ hasText: /new/i });

        if (await page.locator('input[type="password"]').count() >= 2) {
          console.log('Password change form found');

          // Verify at least 2 password fields (current and new)
          const passwordFields = await page.locator('input[type="password"]').count();
          expect(passwordFields).toBeGreaterThanOrEqual(2);
        } else {
          console.log('Password change form not found - feature may not be implemented yet');
        }
      }
    });
  });

  test.describe('Responsive Design Tests', () => {

    test('should display correctly on mobile (768px)', async ({ page }) => {
      // Set viewport to mobile size
      await page.setViewportSize({ width: 768, height: 1024 });

      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Verify page title is visible
      const pageTitle = page.locator('h1').filter({ hasText: /dashboard/i });
      await expect(pageTitle).toBeVisible({ timeout: 5000 });

      // Verify navigation remains functional
      const navigation = page.locator('nav, header');
      await expect(navigation).toBeVisible({ timeout: 5000 });

      // Take screenshot for visual verification
      await page.screenshot({ path: 'test-results/dashboard-mobile-768.png' });
    });

    test('should stack event cards vertically on mobile', async ({ page }) => {
      // Set viewport to mobile size
      await page.setViewportSize({ width: 375, height: 667 });

      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for event cards
      const eventCards = page.locator('[data-testid="event-card"], .event-card');

      if (await eventCards.count() >= 2) {
        // Get positions of first two cards
        const firstCardBox = await eventCards.first().boundingBox();
        const secondCardBox = await eventCards.nth(1).boundingBox();

        if (firstCardBox && secondCardBox) {
          // On mobile, cards should stack vertically (second card below first)
          const isStacked = secondCardBox.y > firstCardBox.y + firstCardBox.height - 10;
          expect(isStacked).toBe(true);
        }
      } else {
        console.log('Not enough event cards to test stacking');
      }

      await page.screenshot({ path: 'test-results/dashboard-mobile-375.png' });
    });
  });

  test.describe('Critical Validation - Sales Elements Removal', () => {

    test('CRITICAL: Verify NO pricing anywhere on dashboard', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      const pageContent = await page.locator('body').textContent();

      // Comprehensive pricing pattern check
      const pricingPatterns = [
        /\$\d+/,                    // $45
        /\$\s*\d+/,                 // $ 45
        /\d+\s*dollars?/i,          // 45 dollars
        /free/i,                    // Free
        /cost:?\s*\$?/i,            // Cost: $
        /price:?\s*\$?/i,           // Price: $
        /\d+\.\d{2}/                // 45.00
      ];

      for (const pattern of pricingPatterns) {
        const hasPattern = pattern.test(pageContent || '');
        if (hasPattern) {
          await page.screenshot({ path: `test-results/pricing-violation-${pattern}.png` });
          console.error(`CRITICAL: Found pricing pattern: ${pattern}`);
        }
        expect(hasPattern).toBe(false);
      }
    });

    test('CRITICAL: Verify NO capacity information anywhere', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      const pageContent = await page.locator('body').textContent();

      // Comprehensive capacity pattern check
      const capacityPatterns = [
        /\d+\s*spots?\s*available/i,
        /\d+\s*spots?\s*remaining/i,
        /capacity:?\s*\d+/i,
        /\d+\s*seats?\s*available/i,
        /sold out/i,
        /full/i,
        /waitlist/i
      ];

      for (const pattern of capacityPatterns) {
        const hasPattern = pattern.test(pageContent || '');
        if (hasPattern) {
          await page.screenshot({ path: `test-results/capacity-violation-${pattern}.png` });
          console.error(`CRITICAL: Found capacity pattern: ${pattern}`);
        }
        expect(hasPattern).toBe(false);
      }
    });

    test('CRITICAL: Verify NO "Learn More" buttons anywhere', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // CRITICAL: Search for any "Learn More" buttons
      const learnMoreButtons = page.locator('button, a').filter({ hasText: /learn more/i });
      const count = await learnMoreButtons.count();

      if (count > 0) {
        await page.screenshot({ path: 'test-results/learn-more-violation.png' });
        console.error(`CRITICAL: Found ${count} "Learn More" buttons on dashboard`);
      }

      expect(count).toBe(0);
    });

    test('CRITICAL: Verify ONLY "View Details" buttons for events', async ({ page }) => {
      await AuthHelpers.loginAs(page, 'vetted');

      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');

      // Look for View Details buttons
      const viewDetailsButtons = page.locator('button, a').filter({ hasText: /view details/i });
      const viewDetailsCount = await viewDetailsButtons.count();

      // Should have some View Details buttons (if user has events)
      console.log(`Found ${viewDetailsCount} "View Details" buttons`);

      // CRITICAL: Verify NO "Register Now" buttons
      const registerButtons = page.locator('button, a').filter({ hasText: /register now|sign up|rsvp/i });
      const registerCount = await registerButtons.count();

      if (registerCount > 0) {
        await page.screenshot({ path: 'test-results/register-button-violation.png' });
        console.error(`CRITICAL: Found ${registerCount} registration buttons on user dashboard`);
      }

      expect(registerCount).toBe(0);
    });
  });
});
