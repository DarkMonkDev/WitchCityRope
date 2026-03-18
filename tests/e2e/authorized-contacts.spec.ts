/**
 * E2E Tests: Authorized Contacts Management
 *
 * Test Plan Reference: Section 8, Flow 1 - Authorized Contacts Management
 *
 * Tests the Authorized Contacts tab on the Profile Settings page.
 * Uses DataFactory fixture for test data creation with automatic cleanup.
 *
 * UI Components Tested:
 * - ProfileSettingsPage.tsx (Tab: "Authorized Contacts")
 * - AuthorizedContactsTab.tsx (data-testid="add-contact-button", "cancel-add-contact")
 * - DelegateList.tsx (data-testid="delegate-row-{id}", "remove-delegate-{id}")
 * - PrincipalList.tsx (data-testid="principal-row-{id}")
 * - ContactSearchInput.tsx (data-testid="contact-search-input")
 *
 * API Endpoints Exercised:
 * - GET /api/authorized-contacts
 * - POST /api/authorized-contacts
 * - DELETE /api/authorized-contacts/{id}
 * - GET /api/members/search?sceneName={query}
 */

import { test, expect } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Authorized Contacts Management', () => {

  test('should display empty state when user has no authorized contacts', async ({ page, df }) => {
    // Create a fresh user with no contacts
    const userA = await df.users.createVerified({
      email: `e2e-contacts-empty-${Date.now()}@test.local`,
      roles: ['VettedMember'],
    });

    // Login as the new user
    await AuthHelpers.loginWith(page, {
      email: userA.email,
      password: 'Test123!',
    });

    // Navigate to Profile Settings page
    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    // Click the "Authorized Contacts" tab
    const contactsTab = page.getByRole('tab', { name: 'Authorized Contacts' });
    await contactsTab.click();

    // Verify empty state text for delegates section
    await expect(
      page.getByText("You haven't authorized anyone yet.")
    ).toBeVisible({ timeout: 10000 });

    // Verify empty state text for principals section
    await expect(
      page.getByText('No one has authorized you yet.')
    ).toBeVisible();

    // Verify "Add Contact" button is visible
    await expect(page.getByTestId('add-contact-button')).toBeVisible();
  });

  test('should add an authorized contact via search and see it in the list', async ({ page, df }) => {
    const timestamp = Date.now();

    // Create two users
    const userA = await df.users.createVerified({
      email: `e2e-contacts-add-a-${timestamp}@test.local`,
      roles: ['VettedMember'],
    });
    const userB = await df.users.createVerified({
      email: `e2e-contacts-add-b-${timestamp}@test.local`,
      roles: ['VettedMember'],
    });

    // Login as userA
    await AuthHelpers.loginWith(page, {
      email: userA.email,
      password: 'Test123!',
    });

    // Navigate to Profile Settings > Authorized Contacts
    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    const contactsTab = page.getByRole('tab', { name: 'Authorized Contacts' });
    await contactsTab.click();

    // Wait for the tab content to load
    await expect(page.getByTestId('add-contact-button')).toBeVisible({ timeout: 10000 });

    // Click "Add Contact" to reveal the search input (progressive disclosure)
    await page.getByTestId('add-contact-button').click();

    // Verify search input appears
    await expect(page.getByTestId('contact-search-input')).toBeVisible();

    // Search for userB by their scene name (type at least 2 chars to trigger search)
    const searchInput = page.getByTestId('contact-search-input').locator('input');
    const searchText = userB.sceneName.substring(0, Math.max(4, userB.sceneName.length));
    await searchInput.fill(searchText);

    // Wait for search results to appear in the autocomplete dropdown
    const option = page.getByRole('option', { name: userB.sceneName });
    await expect(option).toBeVisible({ timeout: 10000 });

    // Select userB from the dropdown
    await option.click();

    // Wait for the contact to be added and notification to appear
    await expect(
      page.getByText(`${userB.sceneName} added as authorized contact`)
    ).toBeVisible({ timeout: 10000 });

    // Verify userB now appears in the delegates list
    await expect(page.getByText(userB.sceneName).first()).toBeVisible();
  });

  test('should remove an authorized contact with confirmation modal', async ({ page, df }) => {
    const timestamp = Date.now();

    const userA = await df.users.createVerified({
      email: `e2e-contacts-remove-a-${timestamp}@test.local`,
      roles: ['VettedMember'],
    });
    const userB = await df.users.createVerified({
      email: `e2e-contacts-remove-b-${timestamp}@test.local`,
      roles: ['VettedMember'],
    });

    // Login as userA
    await AuthHelpers.loginWith(page, {
      email: userA.email,
      password: 'Test123!',
    });

    // Navigate to Authorized Contacts tab
    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    const contactsTab = page.getByRole('tab', { name: 'Authorized Contacts' });
    await contactsTab.click();
    await expect(page.getByTestId('add-contact-button')).toBeVisible({ timeout: 10000 });

    // First, add the contact
    await page.getByTestId('add-contact-button').click();
    const searchInput = page.getByTestId('contact-search-input').locator('input');
    await searchInput.fill(userB.sceneName.substring(0, 4));
    const addOption = page.getByRole('option', { name: userB.sceneName });
    await expect(addOption).toBeVisible({ timeout: 10000 });
    await addOption.click();
    await expect(
      page.getByText(`${userB.sceneName} added as authorized contact`)
    ).toBeVisible({ timeout: 10000 });

    // Now find the Remove button for the delegate
    const delegateRow = page.locator('[data-testid^="delegate-row-"]').filter({ hasText: userB.sceneName });
    await expect(delegateRow).toBeVisible();

    const removeButton = delegateRow.locator('[data-testid^="remove-delegate-"]');
    await removeButton.click();

    // Confirmation modal should appear
    await expect(page.getByText(`Remove ${userB.sceneName}?`)).toBeVisible({ timeout: 5000 });
    await expect(
      page.getByText('They will no longer be able to purchase tickets or RSVP on your behalf.')
    ).toBeVisible();

    // Click "Remove" in the confirmation modal (the red button in the modal)
    const confirmRemoveButton = page.locator('.mantine-Modal-body button').filter({ hasText: 'Remove' });
    await confirmRemoveButton.last().click();

    // Verify success notification
    await expect(page.getByText(`${userB.sceneName} removed`)).toBeVisible({ timeout: 10000 });

    // Verify the contact is no longer in the delegates list
    await expect(
      page.getByText("You haven't authorized anyone yet.")
    ).toBeVisible({ timeout: 10000 });
  });

  test('should show cancel button and toggle search visibility', async ({ page, df }) => {
    const userA = await df.users.createVerified({
      email: `e2e-contacts-cancel-${Date.now()}@test.local`,
      roles: ['VettedMember'],
    });

    await AuthHelpers.loginWith(page, {
      email: userA.email,
      password: 'Test123!',
    });

    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    const contactsTab = page.getByRole('tab', { name: 'Authorized Contacts' });
    await contactsTab.click();

    await expect(page.getByTestId('add-contact-button')).toBeVisible({ timeout: 10000 });

    // Open search
    await page.getByTestId('add-contact-button').click();
    await expect(page.getByTestId('contact-search-input')).toBeVisible();

    // Verify cancel button appears
    await expect(page.getByTestId('cancel-add-contact')).toBeVisible();

    // Click cancel to close search and return to "Add Contact" button
    await page.getByTestId('cancel-add-contact').click();
    await expect(page.getByTestId('add-contact-button')).toBeVisible();
    await expect(page.getByTestId('contact-search-input')).not.toBeVisible();
  });

  test('should show userA in userB principal list when userA authorizes userB', async ({ page, df }) => {
    const timestamp = Date.now();

    const userA = await df.users.createVerified({
      email: `e2e-contacts-principal-a-${timestamp}@test.local`,
      roles: ['VettedMember'],
    });
    const userB = await df.users.createVerified({
      email: `e2e-contacts-principal-b-${timestamp}@test.local`,
      roles: ['VettedMember'],
    });

    // Login as userA and add userB as delegate
    await AuthHelpers.loginWith(page, {
      email: userA.email,
      password: 'Test123!',
    });

    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    const contactsTabA = page.getByRole('tab', { name: 'Authorized Contacts' });
    await contactsTabA.click();
    await expect(page.getByTestId('add-contact-button')).toBeVisible({ timeout: 10000 });

    // Add userB as delegate
    await page.getByTestId('add-contact-button').click();
    const searchInput = page.getByTestId('contact-search-input').locator('input');
    await searchInput.fill(userB.sceneName.substring(0, 4));
    const option = page.getByRole('option', { name: userB.sceneName });
    await expect(option).toBeVisible({ timeout: 10000 });
    await option.click();
    await expect(
      page.getByText(`${userB.sceneName} added as authorized contact`)
    ).toBeVisible({ timeout: 10000 });

    // Now login as userB and check the "People you can act for" section
    await AuthHelpers.loginWith(page, {
      email: userB.email,
      password: 'Test123!',
    });

    await page.goto('/dashboard/profile-settings');
    await page.waitForLoadState('domcontentloaded');

    const contactsTabB = page.getByRole('tab', { name: 'Authorized Contacts' });
    await contactsTabB.click();

    // Verify "People you can act for" section shows userA
    await expect(page.getByText('People you can act for')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(userA.sceneName)).toBeVisible({ timeout: 10000 });

    // Verify the read-only notice
    await expect(
      page.getByText('This list is managed by others. You cannot remove yourself.')
    ).toBeVisible();
  });
});
