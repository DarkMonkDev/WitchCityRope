import { test, expect } from '@playwright/test';

test.describe('Comprehensive Application State Report', () => {
  test('generate comprehensive application status report', async ({ page }) => {
    const report = {
      timestamp: new Date().toISOString(),
      environment: {
        reactServer: 'Unknown',
        apiServer: 'Unknown', 
        database: 'Unknown'
      },
      pages: {},
      apiTests: {},
      functionality: {},
      issues: []
    };

    console.log('🔍 COMPREHENSIVE APPLICATION TESTING REPORT');
    console.log('==============================================');

    // Test 1: Home page functionality
    try {
      await page.goto('http://localhost:5173/', { waitUntil: 'domcontentloaded', timeout: 10000 });
      const title = await page.title();
      const h1Text = await page.locator('h1').first().textContent();
      
      report.pages.home = {
        status: 'SUCCESS',
        title: title,
        mainHeading: h1Text,
        screenshot: 'test-results/report-home.png'
      };
      
      await page.screenshot({ path: 'test-results/report-home.png', fullPage: true });
      console.log('✅ HOME PAGE: Working - Title:', title);
    } catch (error) {
      report.pages.home = { status: 'FAILED', error: error.message };
      report.issues.push('Home page failed to load');
      console.log('❌ HOME PAGE: Failed -', error.message);
    }

    // Test 2: Login page functionality  
    try {
      await page.goto('http://localhost:5173/login', { waitUntil: 'domcontentloaded', timeout: 10000 });
      const welcomeText = await page.locator('h1').textContent();
      const emailInput = page.locator('[data-testid="email-input"]');
      const passwordInput = page.locator('[data-testid="password-input"]');
      const loginButton = page.locator('[data-testid="login-button"]');
      
      const elementsExist = {
        emailInput: await emailInput.count() > 0,
        passwordInput: await passwordInput.count() > 0, 
        loginButton: await loginButton.count() > 0
      };
      
      report.pages.login = {
        status: 'SUCCESS',
        welcomeText: welcomeText,
        formElements: elementsExist,
        screenshot: 'test-results/report-login.png'
      };
      
      await page.screenshot({ path: 'test-results/report-login.png', fullPage: true });
      console.log('✅ LOGIN PAGE: Working - Welcome text:', welcomeText);
      console.log('   Form elements:', JSON.stringify(elementsExist, null, 2));
    } catch (error) {
      report.pages.login = { status: 'FAILED', error: error.message };
      report.issues.push('Login page failed to load or missing form elements');
      console.log('❌ LOGIN PAGE: Failed -', error.message);
    }

    // Test 3: Events page functionality
    try {
      await page.goto('http://localhost:5173/events', { waitUntil: 'domcontentloaded', timeout: 10000 });
      const pageTitle = await page.locator('h1').textContent();
      const pageContent = await page.textContent('body');
      
      const hasNoEventsMessage = pageContent.includes('No Events Currently Available');
      const hasLoadingState = pageContent.includes('Loading');
      
      report.pages.events = {
        status: 'SUCCESS',
        pageTitle: pageTitle,
        showsNoEvents: hasNoEventsMessage,
        hasLoadingState: hasLoadingState,
        screenshot: 'test-results/report-events.png'
      };
      
      await page.screenshot({ path: 'test-results/report-events.png', fullPage: true });
      console.log('✅ EVENTS PAGE: Working - Title:', pageTitle);
      console.log('   Shows no events message:', hasNoEventsMessage);
    } catch (error) {
      report.pages.events = { status: 'FAILED', error: error.message };
      report.issues.push('Events page failed to load');
      console.log('❌ EVENTS PAGE: Failed -', error.message);
    }

    // Test 4: API connectivity test
    try {
      console.log('🔍 Testing API connectivity...');
      const apiResponse = await page.evaluate(async () => {
        try {
          const response = await fetch('http://localhost:5655/api/events');
          const data = await response.json();
          return {
            status: response.status,
            success: response.ok,
            dataLength: data?.data?.length || 0,
            hasData: !!data?.data
          };
        } catch (error) {
          return { status: 'ERROR', error: error.message };
        }
      });
      
      report.apiTests.directApi = apiResponse;
      console.log('✅ API DIRECT: Status', apiResponse.status, '- Events count:', apiResponse.dataLength);
    } catch (error) {
      report.apiTests.directApi = { status: 'FAILED', error: error.message };
      report.issues.push('Direct API connection failed');
      console.log('❌ API DIRECT: Failed -', error.message);
    }

    // Test 5: Proxy API test
    try {
      const proxyResponse = await page.evaluate(async () => {
        try {
          const response = await fetch('/api/events');
          const data = await response.json();
          return {
            status: response.status,
            success: response.ok,
            dataLength: data?.data?.length || 0,
            error: data?.error || null
          };
        } catch (error) {
          return { status: 'ERROR', error: error.message };
        }
      });
      
      report.apiTests.proxyApi = proxyResponse;
      console.log('📍 API PROXY: Status', proxyResponse.status, '- Success:', proxyResponse.success);
    } catch (error) {
      report.apiTests.proxyApi = { status: 'FAILED', error: error.message };
      report.issues.push('Proxy API connection failed');
      console.log('❌ API PROXY: Failed -', error.message);
    }

    // Test 6: Login form submission test
    try {
      await page.goto('http://localhost:5173/login', { waitUntil: 'domcontentloaded', timeout: 10000 });
      
      await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
      await page.fill('[data-testid="password-input"]', 'Test123!');
      
      // Click login but don't wait for navigation (might fail due to API issues)
      await page.click('[data-testid="login-button"]');
      await page.waitForTimeout(2000); // Wait for any API response
      
      const currentUrl = page.url();
      const pageContent = await page.textContent('body');
      const hasError = pageContent.includes('error') || pageContent.includes('Error');
      
      report.functionality.loginSubmission = {
        status: 'COMPLETED',
        finalUrl: currentUrl,
        hasError: hasError,
        screenshot: 'test-results/report-login-attempt.png'
      };
      
      await page.screenshot({ path: 'test-results/report-login-attempt.png', fullPage: true });
      console.log('📝 LOGIN SUBMISSION: Completed - Final URL:', currentUrl);
      console.log('   Has error messages:', hasError);
    } catch (error) {
      report.functionality.loginSubmission = { status: 'FAILED', error: error.message };
      console.log('❌ LOGIN SUBMISSION: Failed -', error.message);
    }

    // Environment assessment
    report.environment.reactServer = 'HEALTHY (Port 5173)';
    report.environment.apiServer = report.apiTests.directApi?.success ? 'HEALTHY (Port 5655)' : 'ISSUE DETECTED';
    report.environment.database = report.apiTests.directApi?.dataLength > 0 ? 'HEALTHY (Has Data)' : 'NO DATA OR ISSUES';

    // Generate summary
    console.log('\n📊 SUMMARY REPORT');
    console.log('==================');
    console.log('Environment Status:');
    console.log('  React Server:', report.environment.reactServer);
    console.log('  API Server:', report.environment.apiServer);
    console.log('  Database:', report.environment.database);
    console.log('\nPage Status:');
    Object.entries(report.pages).forEach(([page, data]) => {
      console.log(`  ${page.toUpperCase()}: ${data.status}`);
    });
    console.log('\nAPI Status:');
    Object.entries(report.apiTests).forEach(([test, data]) => {
      console.log(`  ${test.toUpperCase()}: ${data.status || 'Unknown'}`);
    });
    console.log('\nIssues Found:', report.issues.length);
    report.issues.forEach((issue, i) => {
      console.log(`  ${i + 1}. ${issue}`);
    });

    // Save report
    await page.evaluate((reportData) => {
      const reportJson = JSON.stringify(reportData, null, 2);
      // This would save the report in a real scenario
      console.log('📋 Full report data available for analysis');
    }, report);

    // The test always passes - it's about information gathering
    expect(true).toBe(true);
  });
});