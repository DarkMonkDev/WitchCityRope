import { test, expect } from '@playwright/test';

/**
 * E2E Test: Anonymous Incident Report Submission
 * Created: 2025-10-18
 * Updated: 2025-10-19 - Fixed selectors based on actual UI inspection
 *
 * User Journey:
 * 1. Public user navigates to incident reporting page
 * 2. User fills out anonymous incident report form
 * 3. User submits report
 * 4. User receives reference number and confirmation
 *
 * Authorization: Public (no authentication required)
 * Environment: Docker containers (port 5173)
 *
 * ACTUAL UI STRUCTURE:
 * - Route: /safety/report (NOT /report-incident)
 * - Anonymous toggle: RADIO BUTTONS (not checkbox)
 * - Submit button: "SUBMIT SAFETY REPORT"
 * - Description: 50 character minimum
 */

test.describe('Anonymous Incident Report Submission', () => {
  test.setTimeout(90000); // 90 second maximum

  // Capture console errors during tests
  test.beforeEach(async ({ page }) => {
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    // Store for test access
    (page as any).consoleErrors = consoleErrors;
  });

  test('should submit anonymous incident report and receive reference number', async ({ page }) => {
    // Navigate to incident reporting page (CORRECT URL)
    await page.goto('http://localhost:5173/safety/report');
    await page.waitForLoadState('networkidle');

    // Verify page title
    await expect(page).toHaveTitle(/Witch City Rope/i);

    // Select anonymous report radio button (not checkbox!) - HARD ASSERTION
    const anonymousRadio = page.locator('input[type="radio"]').filter({ hasText: /Anonymous/i }).first();
    const anonymousLabel = page.locator('text=/Anonymous Report/i').first();

    // Try radio first, then label - at least one MUST exist
    const radioVisible = await anonymousRadio.isVisible();
    const labelVisible = await anonymousLabel.isVisible();
    expect(radioVisible || labelVisible).toBeTruthy(); // HARD ASSERTION - element must exist

    if (radioVisible) {
      await anonymousRadio.check();
    } else {
      await anonymousLabel.click();
    }

    await page.waitForTimeout(500);

    // Fill incident type (required field) - HARD ASSERTION
    const incidentTypeInput = page.getByLabel(/Type of Incident/i);
    await expect(incidentTypeInput).toBeVisible({ timeout: 5000 }); // HARD ASSERTION
    await incidentTypeInput.click();
    await page.waitForTimeout(300);

    // Select "Safety Concern" or first available option - HARD ASSERTION
    const typeOption = page.locator('text=/Safety Concern|Consent|Harassment/i').first();
    await expect(typeOption).toBeVisible({ timeout: 5000 }); // HARD ASSERTION
    await typeOption.click();

    // Fill location type (required field) - HARD ASSERTION
    const locationTypeInput = page.getByLabel(/Where did this occur/i);
    await expect(locationTypeInput).toBeVisible({ timeout: 5000 }); // HARD ASSERTION
    await locationTypeInput.click();
    await page.waitForTimeout(300);

    // Select first available location - HARD ASSERTION
    const locationOption = page.locator('text=/At a Witch City Rope event|Other location/i').first();
    await expect(locationOption).toBeVisible({ timeout: 5000 }); // HARD ASSERTION
    await locationOption.click();

    // Date picker should have a default value (today), so skip manual date entry

    // Scroll down to ensure the description field is visible
    await page.locator('text=/What happened/i').scrollIntoViewIfNeeded();

    // Fill description (50 char minimum!) - HARD ASSERTION
    const descriptionTextarea = page.locator('textarea[placeholder*="describe the incident" i]').first();
    await expect(descriptionTextarea).toBeVisible({ timeout: 5000 }); // HARD ASSERTION
    await descriptionTextarea.fill(
      'This is a detailed anonymous safety incident report with sufficient characters to meet the 50 character minimum requirement for form submission validation.'
    );

    // Accept acknowledgment checkbox (required) - HARD ASSERTION
    const acknowledgmentCheckbox = page.getByRole('checkbox', { name: /understand this report may trigger/i });
    await expect(acknowledgmentCheckbox).toBeVisible({ timeout: 5000 }); // HARD ASSERTION
    await acknowledgmentCheckbox.check();

    // Submit report and wait for API response - HARD ASSERTION
    const submitButton = page.locator('button:has-text("SUBMIT SAFETY REPORT"), button[type="submit"]').first();

    // Wait for API response to validate submission
    const [response] = await Promise.all([
      page.waitForResponse(
        resp => resp.url().includes('/api/safety/incidents') && resp.request().method() === 'POST',
        { timeout: 10000 }
      ),
      submitButton.click()
    ]);

    // HARD ASSERTION - API must return 200/201
    expect(response.status()).toBeLessThanOrEqual(201);
    expect(response.status()).toBeGreaterThanOrEqual(200);

    // Validate API response structure
    const responseBody = await response.json();
    expect(responseBody).toHaveProperty('success');
    expect(responseBody.success).toBe(true);
    expect(responseBody).toHaveProperty('data');
    expect(responseBody.data).toHaveProperty('referenceNumber');

    // HARD ASSERTION - Reference number must match format
    const apiReferenceNumber = responseBody.data.referenceNumber;
    expect(apiReferenceNumber).toMatch(/^SAF-\d{8}-\d{4}$/);
    console.log(`✅ API returned reference number: ${apiReferenceNumber}`);

    // HARD ASSERTION - Reference number MUST be visible in UI
    const referenceNumberElement = page.locator(`text=${apiReferenceNumber}`);
    await expect(referenceNumberElement).toBeVisible({ timeout: 5000 }); // HARD ASSERTION
    console.log(`✅ Reference number visible in UI: ${apiReferenceNumber}`);

    // HARD ASSERTION - No console errors allowed
    const consoleErrors = (page as any).consoleErrors || [];
    expect(consoleErrors).toHaveLength(0); // HARD ASSERTION
  });

  test('should validate required fields before submission', async ({ page }) => {
    // Navigate to incident reporting page (CORRECT URL)
    await page.goto('http://localhost:5173/safety/report');
    await page.waitForLoadState('networkidle');

    // Try to submit without filling required fields (Mantine form validation)
    const submitButton = page.locator('button:has-text("SUBMIT SAFETY REPORT"), button[type="submit"]').first();
    await expect(submitButton).toBeVisible({ timeout: 5000 }); // HARD ASSERTION

    // Try clicking submit with empty form (should be prevented)
    await submitButton.click();
    await page.waitForTimeout(1000);

    // HARD ASSERTION - Error summary MUST appear for validation failures
    const errorSummary = page.locator('[role="alert"]').filter({ hasText: /fix the following errors|error|required/i }).first();
    await expect(errorSummary).toBeVisible({ timeout: 5000 }); // HARD ASSERTION

    // HARD ASSERTION - Specific validation errors must be shown
    const errorList = errorSummary.locator('li');
    const errorCount = await errorList.count();
    expect(errorCount).toBeGreaterThan(0); // HARD ASSERTION - at least one error

    console.log(`✅ Form validation working - ${errorCount} errors shown`);

    // Verify description field has error state (50 char minimum not met)
    const descriptionError = page.locator('text=/description.*required|50 character/i').first();
    await expect(descriptionError).toBeVisible({ timeout: 5000 }); // HARD ASSERTION

    // Fill minimum required fields to clear validation errors
    const descriptionTextarea = page.locator('textarea').first();
    await descriptionTextarea.fill(
      'Minimum viable description for testing validation with at least 50 characters as required by the form validation rules.'
    );

    // Accept acknowledgment checkbox - HARD ASSERTION it exists
    const acknowledgmentCheckbox = page.locator('input[type="checkbox"]').filter({ hasText: /understand/i }).first();
    await expect(acknowledgmentCheckbox).toBeVisible({ timeout: 5000 }); // HARD ASSERTION
    await acknowledgmentCheckbox.check();
    await page.waitForTimeout(500);

    // HARD ASSERTION - Submit button should now be enabled (not disabled)
    await expect(submitButton).toBeEnabled({ timeout: 5000 }); // HARD ASSERTION
    console.log('✅ Submit button enabled after filling required fields');

    // HARD ASSERTION - No console errors during validation
    const consoleErrors = (page as any).consoleErrors || [];
    expect(consoleErrors).toHaveLength(0); // HARD ASSERTION
  });
});
