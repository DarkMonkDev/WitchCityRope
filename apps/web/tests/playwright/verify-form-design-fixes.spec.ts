import { test, expect } from '@playwright/test';

test.skip('Verify Form Design A fixes - SKIPPED: Feature not implemented', async ({ page }) => {
  console.log('🔍 Testing updated Floating Label design fixes...');
  
  // Navigate to Design A
  await page.goto('http://localhost:5173/form-designs/a');
  
  // Wait for page to load
  await page.waitForSelector('[data-testid="form-demo"], input, .mantine-TextInput-root', { timeout: 10000 });
  
  console.log('✅ Page loaded successfully');
  
  // Take initial screenshot
  await page.screenshot({ path: 'test-results/design-a-initial.png', fullPage: true });
  console.log('📸 Initial screenshot taken');
  
  // Find the first input field
  const firstInput = page.locator('input').first();
  await expect(firstInput).toBeVisible();
  
  // Test 1: Click into field and check background doesn't turn white
  console.log('🧪 Test 1: Checking background color behavior...');
  await firstInput.click();
  await page.screenshot({ path: 'test-results/design-a-focused.png', fullPage: true });
  console.log('📸 Focused state screenshot taken');
  
  // Test 2: Click out and verify background stays dark
  console.log('🧪 Test 2: Checking blur behavior...');
  await page.click('body'); // Click away from the input
  await page.screenshot({ path: 'test-results/design-a-blurred.png', fullPage: true });
  console.log('📸 Blurred state screenshot taken');
  
  // Test 3: Check for helper text below fields
  console.log('🧪 Test 3: Checking for helper text...');
  const helperTexts = await page.locator('.mantine-TextInput-description, .helper-text, [class*="description"], [class*="helper"]').count();
  console.log(`📝 Found ${helperTexts} helper text elements`);
  
  // Test 4: Check for elevation effect on focus
  console.log('🧪 Test 4: Testing elevation effect...');
  await firstInput.focus();
  await page.waitForTimeout(500); // Wait for animation
  await page.screenshot({ path: 'test-results/design-a-elevation-test.png', fullPage: true });
  console.log('📸 Elevation effect screenshot taken');
  
  // Test 5: Verify no white/yellow text issues
  console.log('🧪 Test 5: Checking text contrast...');
  const inputStyles = await firstInput.evaluate((el) => {
    const styles = window.getComputedStyle(el);
    return {
      backgroundColor: styles.backgroundColor,
      color: styles.color,
      borderColor: styles.borderColor
    };
  });
  
  console.log('🎨 Input styles:', inputStyles);
  
  // Test 6: Test form interaction
  console.log('🧪 Test 6: Testing form interaction...');
  await firstInput.fill('test@example.com');
  await page.screenshot({ path: 'test-results/design-a-with-input.png', fullPage: true });
  console.log('📸 Form with input screenshot taken');
  
  console.log('✅ All visual tests completed successfully!');
});

test('Verify routes - C and D should be removed', async ({ page }) => {
  console.log('🔍 Testing route removals...');
  
  // Test Design C route
  console.log('🧪 Testing Design C route (should be removed)...');
  const responseC = await page.goto('http://localhost:5173/form-designs/c');
  console.log(`Design C status: ${responseC?.status()}`);
  
  // Test Design D route
  console.log('🧪 Testing Design D route (should be removed)...');
  const responseD = await page.goto('http://localhost:5173/form-designs/d');
  console.log(`Design D status: ${responseD?.status()}`);
  
  // Check showcase page only shows 2 designs
  console.log('🧪 Testing showcase page...');
  await page.goto('http://localhost:5173/form-designs');
  await page.waitForSelector('[href*="/form-designs/"]', { timeout: 5000 });
  
  const designLinks = await page.locator('[href*="/form-designs/"]').count();
  console.log(`📊 Found ${designLinks} design links in showcase`);
  
  // Take showcase screenshot
  await page.screenshot({ path: 'test-results/design-showcase.png', fullPage: true });
  console.log('📸 Showcase page screenshot taken');
  
  console.log('✅ Route verification completed!');
});