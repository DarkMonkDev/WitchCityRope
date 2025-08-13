import { defineConfig, devices } from '@playwright/test';

/**
 * Simple Playwright configuration without global setup
 * Use this to bypass the Blazor E2E helper timeout issues
 */
export default defineConfig({
  testDir: './',
  testMatch: ['**/admin-direct-test.spec.ts'],
  
  // Maximum time for entire test run
  timeout: 60 * 1000,
  
  // Maximum time for each test
  expect: {
    timeout: 5000
  },

  // Minimal parallel execution
  fullyParallel: false,
  retries: 0,
  workers: 1,

  // Reporter configuration
  reporter: [
    ['list']
  ],

  // Global test configuration
  use: {
    // Base URL for all tests
    baseURL: 'http://localhost:5651',
    
    // Screenshot on failure
    screenshot: 'only-on-failure',
    
    // Action timeout
    actionTimeout: 10000,
    
    // Navigation timeout
    navigationTimeout: 30000,
    
    // Test ID attribute
    testIdAttribute: 'data-testid',
    
    // Ignore HTTPS errors
    ignoreHTTPSErrors: true,
    
    // Viewport size
    viewport: { width: 1280, height: 720 },
  },

  // Configure projects for major browsers
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // NO GLOBAL SETUP - that's what's causing the timeout
  // globalSetup: undefined,
  // globalTeardown: undefined,
});