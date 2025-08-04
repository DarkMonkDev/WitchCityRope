import { test, expect, Page } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import * as fs from 'fs';
import * as path from 'path';

/**
 * Form Validation Diagnostic Tool
 * Converted from Puppeteer test: test-validation-diagnostics.js
 * 
 * This diagnostic tool analyzes the validation implementation status
 * of all forms in the WitchCityRope application. It checks for:
 * - Presence of WCR validation components
 * - Validation behavior (empty form, real-time validation)
 * - Form accessibility and authentication requirements
 */

// Diagnostic results interface
interface DiagnosticResult {
  name: string;
  url: string;
  timestamp: string;
  status: 'fully_migrated' | 'partially_migrated' | 'form_exists_no_validation' | 'no_form_found' | 'error' | 'http_error' | 'unknown';
  pageLoaded: boolean;
  hasForm: boolean;
  hasValidationComponents: boolean;
  validationBehavior: Array<{
    test: string;
    passed: boolean;
    note?: string;
    validationCount?: number;
  }>;
  errors: string[];
  screenshots: string[];
  elements: {
    wcrInputs: number;
    wcrValidationMessages: number;
    wcrValidationSummary: number;
    standardInputs: number;
    submitButtons: number;
  };
  httpStatus?: number;
  redirectedTo?: string;
}

interface DiagnosticResults {
  timestamp: string;
  baseUrl: string;
  forms: DiagnosticResult[];
}

// Screenshot directory
const SCREENSHOT_DIR = 'test-results/validation-diagnostics';

// Forms to diagnose
const formsToTest = [
  // Identity Forms
  { name: 'Login', url: `${testConfig.baseUrl}/Identity/Account/Login`, testRealtimeValidation: true },
  { name: 'Register', url: `${testConfig.baseUrl}/Identity/Account/Register`, testRealtimeValidation: true },
  { name: 'Forgot Password', url: `${testConfig.baseUrl}/Identity/Account/ForgotPassword`, testRealtimeValidation: true },
  { name: 'Reset Password', url: `${testConfig.baseUrl}/Identity/Account/ResetPassword?code=test`, testRealtimeValidation: true },
  { name: 'Change Password', url: `${testConfig.baseUrl}/Identity/Account/Manage/ChangePassword`, testRealtimeValidation: true },
  { name: 'Manage Email', url: `${testConfig.baseUrl}/Identity/Account/Manage/Email`, testRealtimeValidation: true },
  { name: 'Manage Profile', url: `${testConfig.baseUrl}/Identity/Account/Manage`, testRealtimeValidation: true },
  { name: 'Delete Personal Data', url: `${testConfig.baseUrl}/Identity/Account/Manage/DeletePersonalData`, testRealtimeValidation: false },
  { name: 'Login with 2FA', url: `${testConfig.baseUrl}/Identity/Account/LoginWith2fa`, testRealtimeValidation: true },
  
  // Application Forms
  { name: 'Vetting Application', url: `${testConfig.baseUrl}/vetting/application`, testRealtimeValidation: true },
  { name: 'Events List', url: `${testConfig.baseUrl}/events`, testRealtimeValidation: false },
  { name: 'Admin Events', url: `${testConfig.baseUrl}/admin/events`, testRealtimeValidation: false },
  { name: 'Admin Incidents', url: `${testConfig.baseUrl}/admin/incidents`, testRealtimeValidation: false },
  { name: 'Member Dashboard', url: `${testConfig.baseUrl}/dashboard`, testRealtimeValidation: false },
  { name: 'Member Events', url: `${testConfig.baseUrl}/member/events`, testRealtimeValidation: false },
  { name: 'Profile Edit', url: `${testConfig.baseUrl}/profile`, testRealtimeValidation: true }
];

const diagnosticResults: DiagnosticResults = {
  timestamp: new Date().toISOString(),
  baseUrl: testConfig.baseUrl,
  forms: []
};

test.describe('Form Validation Diagnostics', () => {
  test.beforeAll(async () => {
    // Create screenshot directory
    if (!fs.existsSync(SCREENSHOT_DIR)) {
      fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });
    }
  });

  test.afterAll(async () => {
    // Generate summary
    console.log('\n\n' + '='.repeat(60));
    console.log('📊 DIAGNOSTIC SUMMARY');
    console.log('='.repeat(60));

    const statusCounts: Record<string, number> = {};
    diagnosticResults.forms.forEach(form => {
      statusCounts[form.status] = (statusCounts[form.status] || 0) + 1;
    });

    console.log('\nForm Status Distribution:');
    Object.entries(statusCounts).forEach(([status, count]) => {
      console.log(`  ${status}: ${count}`);
    });

    console.log('\nFully Migrated Forms:');
    diagnosticResults.forms
      .filter(f => f.status === 'fully_migrated')
      .forEach(f => console.log(`  ✅ ${f.name}`));

    console.log('\nPartially Migrated Forms:');
    diagnosticResults.forms
      .filter(f => f.status === 'partially_migrated')
      .forEach(f => console.log(`  ⚠️  ${f.name}`));

    console.log('\nForms with Issues:');
    diagnosticResults.forms
      .filter(f => f.status === 'error' || f.status === 'http_error')
      .forEach(f => console.log(`  ❌ ${f.name}: ${f.errors.join(', ')}`));

    // Save detailed results
    const resultsFile = `${SCREENSHOT_DIR}/diagnostic-results-${Date.now()}.json`;
    fs.writeFileSync(resultsFile, JSON.stringify(diagnosticResults, null, 2));
    console.log(`\n📄 Detailed results saved to: ${resultsFile}`);

    // Generate diagnostic report
    generateDiagnosticReport(diagnosticResults);
  });

  formsToTest.forEach(form => {
    test(`Diagnose ${form.name} Form`, async ({ page }) => {
      await diagnoseForm(page, form);
    });
  });
});

async function diagnoseForm(page: Page, formInfo: { name: string; url: string; testRealtimeValidation: boolean }) {
  console.log(`\n${'='.repeat(60)}`);
  console.log(`Diagnosing: ${formInfo.name}`);
  console.log(`URL: ${formInfo.url}`);
  console.log(`${'='.repeat(60)}`);

  const result: DiagnosticResult = {
    name: formInfo.name,
    url: formInfo.url,
    timestamp: new Date().toISOString(),
    status: 'unknown',
    pageLoaded: false,
    hasForm: false,
    hasValidationComponents: false,
    validationBehavior: [],
    errors: [],
    screenshots: [],
    elements: {
      wcrInputs: 0,
      wcrValidationMessages: 0,
      wcrValidationSummary: 0,
      standardInputs: 0,
      submitButtons: 0
    }
  };

  try {
    // Navigate to the page
    console.log('  📍 Navigating to page...');
    const response = await page.goto(formInfo.url, {
      waitUntil: 'networkidle',
      timeout: testConfig.timeouts.navigation
    });

    if (response) {
      result.httpStatus = response.status();
      result.pageLoaded = response.ok();
      console.log(`  HTTP Status: ${response.status()}`);

      if (!response.ok()) {
        result.status = 'http_error';
        result.errors.push(`HTTP ${response.status()} error`);
        const screenshot = await captureScreenshot(page, `${formInfo.name}-http-error`);
        result.screenshots.push(screenshot);
        diagnosticResults.forms.push(result);
        return;
      }
    }

    // Wait a bit for any redirects or client-side rendering
    await page.waitForTimeout(2000);

    // Check if we were redirected
    const currentUrl = page.url();
    if (currentUrl !== formInfo.url) {
      console.log(`  ⚠️  Redirected to: ${currentUrl}`);
      result.redirectedTo = currentUrl;
    }

    // Check for form element (exclude newsletter forms)
    console.log('  📍 Checking for form element...');
    const hasForm = await page.locator('form:not(.newsletter-form)').isVisible();
    result.hasForm = hasForm;
    console.log(`  Form found: ${hasForm ? 'Yes' : 'No'}`);

    // Check for validation components
    console.log('  📍 Checking for validation components...');
    
    // Count WCR components
    result.elements.wcrInputs = await page.locator('wcr-input-text, wcr-input-email, wcr-input-password, [class*="wcr-input"]').count();
    result.elements.wcrValidationMessages = await page.locator('.wcr-field-validation, wcr-validation-message').count();
    result.elements.wcrValidationSummary = await page.locator('.wcr-validation-summary, wcr-validation-summary').count();
    result.elements.standardInputs = await page.locator('input[type="text"], input[type="email"], input[type="password"]').count();
    result.elements.submitButtons = await page.locator('button[type="submit"], input[type="submit"], .sign-in-btn, .register-btn').count();

    result.hasValidationComponents = result.elements.wcrInputs > 0 || 
                                   result.elements.wcrValidationMessages > 0;

    console.log(`  WCR Input Components: ${result.elements.wcrInputs}`);
    console.log(`  Standard Inputs: ${result.elements.standardInputs}`);
    console.log(`  Submit Buttons: ${result.elements.submitButtons}`);

    // Take initial screenshot
    const initialScreenshot = await captureScreenshot(page, `${formInfo.name}-initial`);
    result.screenshots.push(initialScreenshot);

    // If form exists, test validation behavior
    if (hasForm && result.elements.submitButtons > 0) {
      console.log('  📍 Testing validation behavior...');

      // Test 1: Empty form submission
      console.log('    - Testing empty form submission...');
      const submitButton = page.locator('button[type="submit"], input[type="submit"], .sign-in-btn, .register-btn').first();
      await submitButton.click();
      await page.waitForTimeout(1500);

      // Check for validation messages
      const afterSubmitValidation = await page.locator('.wcr-field-validation:not(:empty), .wcr-validation-summary li, .validation-message, [class*="error"]:not(:empty)').count();
      console.log(`    - Validation messages after submit: ${afterSubmitValidation}`);

      if (afterSubmitValidation > 0) {
        result.validationBehavior.push({
          test: 'Empty form submission',
          passed: true,
          validationCount: afterSubmitValidation
        });

        // Capture validation state
        const validationScreenshot = await captureScreenshot(page, `${formInfo.name}-validation`);
        result.screenshots.push(validationScreenshot);
      } else {
        result.validationBehavior.push({
          test: 'Empty form submission',
          passed: false,
          note: 'No validation messages appeared'
        });
      }

      // Test 2: Check for real-time validation (if applicable)
      if (formInfo.testRealtimeValidation && result.elements.standardInputs > 0) {
        console.log('    - Testing real-time validation...');
        const firstInput = page.locator('input[type="text"], input[type="email"], input[type="password"]').first();

        // Type invalid data and blur
        await firstInput.fill('invalid');
        await page.keyboard.press('Tab');
        await page.waitForTimeout(500);

        const realtimeValidation = await page.locator('.wcr-field-validation:not(:empty)').count();
        if (realtimeValidation > 0) {
          result.validationBehavior.push({
            test: 'Real-time validation',
            passed: true,
            validationCount: realtimeValidation
          });
        } else {
          result.validationBehavior.push({
            test: 'Real-time validation',
            passed: false,
            note: 'No real-time validation detected'
          });
        }
      }
    }

    // Determine overall status
    if (result.hasForm && result.hasValidationComponents) {
      result.status = 'fully_migrated';
    } else if (result.hasForm && result.elements.standardInputs > 0) {
      result.status = 'partially_migrated';
    } else if (result.hasForm) {
      result.status = 'form_exists_no_validation';
    } else {
      result.status = 'no_form_found';
    }

    console.log(`  ✅ Status: ${result.status}`);

  } catch (error) {
    result.status = 'error';
    result.errors.push(error instanceof Error ? error.message : String(error));
    console.log(`  ❌ Error: ${error instanceof Error ? error.message : String(error)}`);

    // Capture error screenshot
    const errorScreenshot = await captureScreenshot(page, `${formInfo.name}-error`);
    result.screenshots.push(errorScreenshot);
  }

  diagnosticResults.forms.push(result);
}

async function captureScreenshot(page: Page, name: string): Promise<string> {
  const timestamp = Date.now();
  const filename = `${SCREENSHOT_DIR}/${name}-${timestamp}.png`;
  await page.screenshot({
    path: filename,
    fullPage: true
  });
  return filename;
}

function generateDiagnosticReport(results: DiagnosticResults) {
  const html = `
<!DOCTYPE html>
<html>
<head>
    <title>Form Validation Diagnostic Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; }
        h2 { color: #555; margin-top: 30px; }
        .summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }
        .summary-card { padding: 15px; border-radius: 8px; text-align: center; }
        .summary-card.fully_migrated { background: #d4edda; color: #155724; }
        .summary-card.partially_migrated { background: #fff3cd; color: #856404; }
        .summary-card.error { background: #f8d7da; color: #721c24; }
        .summary-card.http_error { background: #f8d7da; color: #721c24; }
        .summary-card.no_form_found { background: #e2e3e5; color: #383d41; }
        .form-card { margin: 20px 0; padding: 20px; border: 1px solid #ddd; border-radius: 8px; }
        .form-card.fully_migrated { border-left: 4px solid #28a745; }
        .form-card.partially_migrated { border-left: 4px solid #ffc107; }
        .form-card.error { border-left: 4px solid #dc3545; }
        .form-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; }
        .form-title { font-size: 18px; font-weight: bold; }
        .status-badge { padding: 4px 12px; border-radius: 4px; font-size: 12px; font-weight: bold; }
        .status-badge.fully_migrated { background: #28a745; color: white; }
        .status-badge.partially_migrated { background: #ffc107; color: #333; }
        .status-badge.error { background: #dc3545; color: white; }
        .details-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 15px; margin: 15px 0; }
        .detail-item { padding: 10px; background: #f8f9fa; border-radius: 4px; }
        .detail-label { font-weight: bold; color: #666; font-size: 12px; text-transform: uppercase; }
        .detail-value { margin-top: 5px; font-size: 14px; }
        .screenshots { margin-top: 15px; }
        .screenshot-link { display: inline-block; margin-right: 10px; color: #007bff; text-decoration: none; }
        .screenshot-link:hover { text-decoration: underline; }
        .validation-test { margin: 5px 0; padding: 5px 10px; background: #f8f9fa; border-radius: 4px; }
        .validation-test.passed { border-left: 3px solid #28a745; }
        .validation-test.failed { border-left: 3px solid #dc3545; }
        .error-message { color: #dc3545; margin: 10px 0; padding: 10px; background: #f8d7da; border-radius: 4px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>Form Validation Diagnostic Report</h1>
        <p>Generated: ${new Date().toLocaleString()}</p>
        <p>Environment: ${results.baseUrl}</p>
        
        <h2>Summary</h2>
        <div class="summary-grid">
            ${Object.entries(getStatusCounts(results)).map(([status, count]) => `
                <div class="summary-card ${status}">
                    <div style="font-size: 24px; font-weight: bold;">${count}</div>
                    <div>${formatStatus(status)}</div>
                </div>
            `).join('')}
        </div>
        
        <h2>Form Details</h2>
        ${results.forms.map(form => generateFormCard(form)).join('')}
    </div>
</body>
</html>`;

  const reportFile = `${SCREENSHOT_DIR}/diagnostic-report.html`;
  fs.writeFileSync(reportFile, html);
  console.log(`📊 Diagnostic report generated: ${reportFile}`);
}

function getStatusCounts(results: DiagnosticResults): Record<string, number> {
  const counts: Record<string, number> = {};
  results.forms.forEach(form => {
    counts[form.status] = (counts[form.status] || 0) + 1;
  });
  return counts;
}

function formatStatus(status: string): string {
  const statusMap: Record<string, string> = {
    'fully_migrated': 'Fully Migrated',
    'partially_migrated': 'Partially Migrated',
    'no_form_found': 'No Form Found',
    'form_exists_no_validation': 'Form Without Validation',
    'error': 'Error',
    'http_error': 'HTTP Error'
  };
  return statusMap[status] || status;
}

function generateFormCard(form: DiagnosticResult): string {
  return `
    <div class="form-card ${form.status}">
        <div class="form-header">
            <div class="form-title">${form.name}</div>
            <div class="status-badge ${form.status}">${formatStatus(form.status)}</div>
        </div>
        
        ${form.redirectedTo ? `<p>Redirected to: <code>${form.redirectedTo}</code></p>` : ''}
        
        ${form.errors.length > 0 ? `
            <div class="error-message">
                ${form.errors.join('<br>')}
            </div>
        ` : ''}
        
        <div class="details-grid">
            <div class="detail-item">
                <div class="detail-label">Page Loaded</div>
                <div class="detail-value">${form.pageLoaded ? '✅ Yes' : '❌ No'}</div>
            </div>
            <div class="detail-item">
                <div class="detail-label">Form Found</div>
                <div class="detail-value">${form.hasForm ? '✅ Yes' : '❌ No'}</div>
            </div>
            <div class="detail-item">
                <div class="detail-label">WCR Components</div>
                <div class="detail-value">${form.elements.wcrInputs || 0}</div>
            </div>
            <div class="detail-item">
                <div class="detail-label">Standard Inputs</div>
                <div class="detail-value">${form.elements.standardInputs || 0}</div>
            </div>
        </div>
        
        ${form.validationBehavior.length > 0 ? `
            <h4>Validation Tests</h4>
            ${form.validationBehavior.map(test => `
                <div class="validation-test ${test.passed ? 'passed' : 'failed'}">
                    ${test.test}: ${test.passed ? '✅ Passed' : '❌ Failed'}
                    ${test.note ? `<br><small>${test.note}</small>` : ''}
                    ${test.validationCount ? `<br><small>${test.validationCount} validation messages</small>` : ''}
                </div>
            `).join('')}
        ` : ''}
        
        ${form.screenshots.length > 0 ? `
            <div class="screenshots">
                Screenshots: 
                ${form.screenshots.map((s, i) => `
                    <a href="${path.basename(s)}" class="screenshot-link" target="_blank">View ${i + 1}</a>
                `).join('')}
            </div>
        ` : ''}
    </div>
  `;
}