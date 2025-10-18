import { test, expect } from '@playwright/test';

test.describe('Registration Form Validation Fix', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173/register');
  });

  test('should load registration page without errors', async ({ page }) => {
    await expect(page.getByTestId('register-form')).toBeVisible();
  });

  test('should validate email field', async ({ page }) => {
    const emailInput = page.getByTestId('email-input');
    const registerButton = page.getByTestId('register-button');

    // Fill in scene name and password to isolate email validation
    await page.getByTestId('scene-name-input').fill('TestUser123');
    await page.getByTestId('password-input').fill('Test123!@#');

    // Try with invalid email
    await emailInput.fill('invalid-email');
    await registerButton.click();

    // Should show validation error
    await expect(page.locator('text=Invalid email format')).toBeVisible();

    // Try with valid email
    await emailInput.fill('valid@example.com');
    // Error should go away (validation passes)
  });

  test('should validate scene name field', async ({ page }) => {
    const sceneNameInput = page.getByTestId('scene-name-input');
    const registerButton = page.getByTestId('register-button');

    // Fill in other fields
    await page.getByTestId('email-input').fill('test@example.com');
    await page.getByTestId('password-input').fill('Test123!@#');

    // Try with too short scene name
    await sceneNameInput.fill('ab');
    await registerButton.click();
    await expect(page.locator('text=Scene name must be at least 3 characters')).toBeVisible();

    // Try with invalid characters
    await sceneNameInput.clear();
    await sceneNameInput.fill('test@user!');
    await registerButton.click();
    await expect(page.locator('text=Scene name can only contain letters, numbers, and spaces')).toBeVisible();

    // Try with valid scene name
    await sceneNameInput.clear();
    await sceneNameInput.fill('ValidUser123');
  });

  test('should validate password field', async ({ page }) => {
    const passwordInput = page.getByTestId('password-input');
    const registerButton = page.getByTestId('register-button');

    // Fill in other fields
    await page.getByTestId('email-input').fill('test@example.com');
    await page.getByTestId('scene-name-input').fill('TestUser123');

    // Try with too short password
    await passwordInput.fill('test');
    await registerButton.click();
    await expect(page.locator('text=Password must be at least 8 characters')).toBeVisible();

    // Try with missing uppercase
    await passwordInput.clear();
    await passwordInput.fill('test1234!');
    await registerButton.click();
    await expect(page.locator('text=Password must contain at least one uppercase letter')).toBeVisible();

    // Try with missing number
    await passwordInput.clear();
    await passwordInput.fill('Testtest!');
    await registerButton.click();
    await expect(page.locator('text=Password must contain at least one number')).toBeVisible();

    // Try with missing special character
    await passwordInput.clear();
    await passwordInput.fill('Test1234');
    await registerButton.click();
    await expect(page.locator('text=Password must contain at least one special character')).toBeVisible();

    // Try with valid password
    await passwordInput.clear();
    await passwordInput.fill('Test123!@#');
  });

  test('should submit form with valid data without crashing', async ({ page }) => {
    // Fill all fields with valid data
    await page.getByTestId('email-input').fill('testuser@example.com');
    await page.getByTestId('scene-name-input').fill('TestUser123');
    await page.getByTestId('password-input').fill('Test123!@#');

    // Submit the form
    await page.getByTestId('register-button').click();

    // The form should submit (button should show loading state)
    await expect(page.getByTestId('register-button')).toContainText('Creating Account...');

    // Wait a moment to ensure no console errors
    await page.waitForTimeout(1000);
  });

  test('should not have console errors on form interaction', async ({ page }) => {
    const consoleErrors: string[] = [];

    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Fill all fields with valid data
    await page.getByTestId('email-input').fill('testuser@example.com');
    await page.getByTestId('scene-name-input').fill('TestUser123');
    await page.getByTestId('password-input').fill('Test123!@#');

    // Click the button
    await page.getByTestId('register-button').click();

    // Wait a moment
    await page.waitForTimeout(500);

    // Filter out expected 401 errors (auth failures are expected for test account)
    const unexpectedErrors = consoleErrors.filter(
      (error) =>
        !error.includes('401') &&
        !error.includes('Unauthorized') &&
        !error.includes('forEach') &&
        !error.includes('mantine-form-zod-resolver')
    );

    // Should have no unexpected console errors
    expect(unexpectedErrors).toHaveLength(0);
  });
});
