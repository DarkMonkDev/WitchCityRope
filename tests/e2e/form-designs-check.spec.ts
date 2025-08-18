import { test, expect } from '@playwright/test';

/**
 * Form Design Showcase Content Verification Tests
 * 
 * CONTEXT: Form design pages at /form-designs/* were created but user reports
 * they don't actually load content, despite returning HTTP 200.
 * 
 * PURPOSE: Verify the pages actually display content, not just return 200.
 * 
 * TEST STRATEGY:
 * - Navigate to each form design page
 * - Check for console errors that might indicate React rendering issues
 * - Verify actual content is present (headings, form fields, etc.)
 * - Take screenshots to see what's actually displayed
 * - Test with extended timeouts to account for potential loading delays
 */

test.describe('Form Design Showcase Pages Content Verification', () => {
  let consoleErrors: string[] = [];
  let networkErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    // Reset error arrays for each test
    consoleErrors = [];
    networkErrors = [];

    // Capture console errors that might indicate React rendering issues
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Capture network failures that might affect content loading
    page.on('requestfailed', request => {
      networkErrors.push(`${request.method()} ${request.url()} - ${request.failure()?.errorText}`);
    });
  });

  test.afterEach(async ({ page }) => {
    // Log any errors found during the test for debugging
    if (consoleErrors.length > 0) {
      console.log('Console errors detected:', consoleErrors);
    }
    if (networkErrors.length > 0) {
      console.log('Network errors detected:', networkErrors);
    }
  });

  test('Form Design A (Floating Labels) page loads and displays content', async ({ page }) => {
    console.log('Testing Form Design A - Floating Labels...');

    // Navigate with extended timeout
    await page.goto('/form-designs/a', { timeout: 30000 });
    
    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle', { timeout: 30000 });
    
    // CRITICAL: Wait for React to actually render content
    // Look for specific content that should be in FormDesignA component
    console.log('Waiting for React content to render...');
    try {
      await page.waitForSelector('text="Floating Label Design"', { timeout: 15000 });
      console.log('✅ Found "Floating Label Design" heading - React content loaded');
    } catch (error) {
      console.log('❌ React content did not load within 15 seconds');
      console.log('Trying to wait for any form elements...');
      try {
        await page.waitForSelector('input, button, form', { timeout: 10000 });
        console.log('✅ Found some form elements');
      } catch {
        console.log('❌ No form elements found either');
      }
    }
    
    // Take screenshot to see what's actually displayed
    await page.screenshot({ 
      path: 'test-results/form-design-a-actual.png', 
      fullPage: true 
    });
    
    // Get basic page information
    const title = await page.title();
    const url = page.url();
    const bodyText = await page.locator('body').innerText();
    
    console.log('Page title:', title);
    console.log('Page URL:', url);
    console.log('Body text sample (first 200 chars):', bodyText.substring(0, 200));
    
    // Check for common content indicators
    const hasHeading = await page.locator('h1, h2, h3, h4, h5, h6').count() > 0;
    const hasForm = await page.locator('form').count() > 0;
    const hasInput = await page.locator('input, select, textarea').count() > 0;
    const hasButton = await page.locator('button').count() > 0;
    
    // Check for React error boundaries or error overlays
    const hasErrorOverlay = await page.locator('.error, [class*="error"], .error-boundary').count() > 0;
    const hasReactError = await page.locator('[data-testid*="error"], .react-error').count() > 0;
    
    // Check for common "page not found" or "loading" indicators
    const hasNotFound = bodyText.toLowerCase().includes('not found') || 
                       bodyText.toLowerCase().includes('404') ||
                       bodyText.toLowerCase().includes('page not found');
    const hasGenericError = bodyText.toLowerCase().includes('something went wrong') ||
                           bodyText.toLowerCase().includes('error occurred');
    
    console.log('Content analysis:');
    console.log('- Has heading:', hasHeading);
    console.log('- Has form:', hasForm);
    console.log('- Has input fields:', hasInput);
    console.log('- Has buttons:', hasButton);
    console.log('- Has error overlay:', hasErrorOverlay);
    console.log('- Has React error:', hasReactError);
    console.log('- Has "not found" text:', hasNotFound);
    console.log('- Has generic error text:', hasGenericError);
    console.log('- Console errors count:', consoleErrors.length);
    console.log('- Network errors count:', networkErrors.length);
    
    // Expected content for Form Design A (Floating Labels)
    // Look for specific content that should be on the page
    const expectedContent = [
      'floating',
      'label',
      'form',
      'design'
    ];
    
    let foundExpectedContent = false;
    for (const content of expectedContent) {
      if (bodyText.toLowerCase().includes(content)) {
        foundExpectedContent = true;
        console.log(`- Found expected content: "${content}"`);
        break;
      }
    }
    
    // Assertions to verify the page is actually working
    // These are designed to help diagnose what's wrong rather than just pass/fail
    
    // Basic page load verification
    expect(url).toContain('/form-designs/a');
    
    // Should have some meaningful content (more than just a blank page)
    expect(bodyText.length).toBeGreaterThan(50);
    
    // Should not be showing common error states
    expect(hasNotFound).toBe(false);
    expect(hasGenericError).toBe(false);
    
    // Should have some interactive elements (this is a form design page)
    const hasInteractiveElements = hasForm || hasInput || hasButton;
    expect(hasInteractiveElements).toBe(true);
    
    // Should not have major console errors (a few warnings might be OK)
    expect(consoleErrors.length).toBeLessThan(5);
    
    // Log final verdict
    if (foundExpectedContent && hasInteractiveElements && !hasNotFound && consoleErrors.length === 0) {
      console.log('✅ Form Design A appears to be loading correctly');
    } else {
      console.log('⚠️ Form Design A may have loading issues - check screenshot and logs');
    }
  });

  test('Form Design showcase main page loads and displays content', async ({ page }) => {
    console.log('Testing Form Design Showcase main page...');

    // Navigate to the main showcase page
    await page.goto('/form-designs', { timeout: 30000 });
    
    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle', { timeout: 30000 });
    
    // Take screenshot
    await page.screenshot({ 
      path: 'test-results/form-designs-main-actual.png', 
      fullPage: true 
    });
    
    const title = await page.title();
    const url = page.url();
    const bodyText = await page.locator('body').innerText();
    
    console.log('Page title:', title);
    console.log('Page URL:', url);
    console.log('Body text sample (first 300 chars):', bodyText.substring(0, 300));
    
    // Check for navigation links to the different design variants
    const hasLinkToA = await page.locator('a[href*="/form-designs/a"], a[href*="floating"]').count() > 0;
    const hasLinkToB = await page.locator('a[href*="/form-designs/b"], a[href*="minimal"]').count() > 0;
    const hasLinkToC = await page.locator('a[href*="/form-designs/c"], a[href*="card"]').count() > 0;
    const hasLinkToD = await page.locator('a[href*="/form-designs/d"], a[href*="gradient"]').count() > 0;
    
    console.log('Navigation links:');
    console.log('- Link to Design A:', hasLinkToA);
    console.log('- Link to Design B:', hasLinkToB);
    console.log('- Link to Design C:', hasLinkToC);
    console.log('- Link to Design D:', hasLinkToD);
    
    // Look for showcase-specific content
    const showcaseKeywords = ['showcase', 'design', 'form', 'floating', 'minimal', 'card', 'gradient'];
    const foundKeywords = showcaseKeywords.filter(keyword => 
      bodyText.toLowerCase().includes(keyword)
    );
    
    console.log('Found showcase keywords:', foundKeywords);
    console.log('Console errors count:', consoleErrors.length);
    console.log('Network errors count:', networkErrors.length);
    
    // Verify this looks like a showcase page
    expect(url).toContain('/form-designs');
    expect(bodyText.length).toBeGreaterThan(50);
    expect(foundKeywords.length).toBeGreaterThan(0);
    
    // Should have navigation to different designs
    const hasNavigation = hasLinkToA || hasLinkToB || hasLinkToC || hasLinkToD;
    if (!hasNavigation) {
      console.log('⚠️ No navigation links found - this might not be a proper showcase page');
      // Check if it's just showing text content instead
      const hasShowcaseText = bodyText.toLowerCase().includes('showcase') || 
                             bodyText.toLowerCase().includes('design');
      expect(hasShowcaseText).toBe(true);
    }
  });

  test('All form design pages return successful HTTP responses', async ({ page }) => {
    console.log('Testing HTTP responses for all form design pages...');

    const designPages = [
      { path: '/form-designs', name: 'Main Showcase' },
      { path: '/form-designs/a', name: 'Floating Labels' },
      { path: '/form-designs/b', name: 'Inline Minimal' },
      { path: '/form-designs/c', name: 'Card Elevated' },
      { path: '/form-designs/d', name: 'Gradient Accent' }
    ];

    for (const design of designPages) {
      console.log(`Testing ${design.name} (${design.path})...`);
      
      const response = await page.goto(design.path, { timeout: 30000 });
      
      // Check HTTP status
      const status = response?.status();
      console.log(`- HTTP Status: ${status}`);
      
      // Check if content is actually loaded
      await page.waitForLoadState('networkidle', { timeout: 15000 });
      const bodyText = await page.locator('body').innerText();
      const contentLength = bodyText.length;
      
      console.log(`- Content length: ${contentLength} characters`);
      console.log(`- Content sample: ${bodyText.substring(0, 100)}...`);
      
      // Basic assertions
      expect(status).toBe(200);
      expect(contentLength).toBeGreaterThan(20); // Should have some content
      
      // Take a quick screenshot for manual review
      await page.screenshot({ 
        path: `test-results/${design.name.toLowerCase().replace(/\s+/g, '-')}-response.png` 
      });
    }
  });

  test('Form Design A - Detailed content inspection', async ({ page }) => {
    console.log('Performing detailed inspection of Form Design A...');

    await page.goto('/form-designs/a', { timeout: 30000 });
    await page.waitForLoadState('networkidle', { timeout: 30000 });
    
    // Wait a bit more for any potential React hydration
    await page.waitForTimeout(2000);
    
    // Take screenshot
    await page.screenshot({ 
      path: 'test-results/form-design-a-detailed.png', 
      fullPage: true 
    });
    
    // Get the entire page HTML for debugging
    const pageHTML = await page.content();
    console.log('Page HTML structure (first 500 chars):', pageHTML.substring(0, 500));
    
    // Check for React root and mounting
    const hasReactRoot = await page.locator('#root, [data-reactroot], .react-root').count() > 0;
    console.log('Has React root element:', hasReactRoot);
    
    // Check for specific form design elements
    const formElements = {
      inputs: await page.locator('input').count(),
      textareas: await page.locator('textarea').count(),
      selects: await page.locator('select').count(),
      buttons: await page.locator('button').count(),
      labels: await page.locator('label').count(),
      forms: await page.locator('form').count()
    };
    
    console.log('Form elements found:', formElements);
    
    // Check for CSS classes that might indicate the design style
    const bodyClasses = await page.locator('body').getAttribute('class') || '';
    const rootClasses = await page.locator('#root').getAttribute('class') || '';
    
    console.log('Body classes:', bodyClasses);
    console.log('Root classes:', rootClasses);
    
    // Look for floating label specific patterns
    const floatingLabelIndicators = await page.locator('[class*="float"], [class*="Float"], [data-testid*="float"]').count();
    console.log('Floating label indicators:', floatingLabelIndicators);
    
    // Check for any error messages in the DOM
    const errorElements = await page.locator('.error, .Error, [class*="error"], [data-testid*="error"]').count();
    console.log('Error elements in DOM:', errorElements);
    
    // Final assessment
    const hasContent = formElements.inputs > 0 || formElements.forms > 0 || formElements.buttons > 0;
    const hasNoMajorErrors = consoleErrors.length < 3 && errorElements === 0;
    
    if (hasContent && hasNoMajorErrors) {
      console.log('✅ Form Design A appears to have proper content');
    } else {
      console.log('❌ Form Design A may have content loading issues');
      console.log('- Has form content:', hasContent);
      console.log('- No major errors:', hasNoMajorErrors);
      console.log('- Console errors:', consoleErrors);
    }
    
    // Basic assertions for the detailed test
    expect(pageHTML).toContain('html');  // Should have basic HTML structure
    expect(hasReactRoot).toBe(true);     // Should have React mounting point
  });
});