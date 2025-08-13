import { defineConfig, devices } from '@playwright/test';
import { testConfig } from './helpers/test.config';

/**
 * Playwright configuration for WitchCityRope E2E tests
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './',
  testMatch: ['**/*.spec.ts'],
  
  // Maximum time for entire test run (increased for Docker environment)
  timeout: 60 * 1000,
  
  // Maximum time for each test
  expect: {
    timeout: testConfig.timeouts.assertion
  },

  // Fail fast on CI
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,

  // Reporter configuration
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['json', { outputFile: 'test-results.json' }],
    ['list']
  ],

  // Global test configuration
  use: {
    // Base URL for all tests
    baseURL: testConfig.baseUrl,
    
    // Collect trace when retrying the failed test
    trace: 'on-first-retry',
    
    // Screenshot on failure
    screenshot: 'only-on-failure',
    
    // Video recording
    video: 'retain-on-failure',
    
    // Action timeout
    actionTimeout: testConfig.timeouts.action,
    
    // Navigation timeout
    navigationTimeout: testConfig.timeouts.navigation,
    
    // Test ID attribute
    testIdAttribute: 'data-testid',
    
    // Accept downloads
    acceptDownloads: true,
    
    // Ignore HTTPS errors
    ignoreHTTPSErrors: true,
    
    // Viewport size
    viewport: { width: 1280, height: 720 },
    
    // Locale and timezone
    locale: 'en-US',
    timezoneId: 'America/New_York',
  },

  // Configure projects for major browsers
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    // Mobile testing
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] },
    },
  ],

  // Run local dev server before starting tests
  webServer: process.env.CI ? undefined : {
    command: 'echo "Dev server should already be running"',
    port: 5651,
    reuseExistingServer: true,
    timeout: 120 * 1000,
  },

  // Global setup and teardown
  globalSetup: './helpers/global-setup.ts',
  globalTeardown: './helpers/global-teardown.ts',
});