import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/playwright',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'list',
  
  // Global timeout settings for improved reliability
  timeout: 60000, // Overall test timeout
  expect: {
    timeout: 10000, // Assertion timeout
  },
  
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    
    // Enhanced timeout settings for authentication flows
    actionTimeout: 15000, // Action timeout (clicks, fills, etc.)
    navigationTimeout: 30000, // Navigation timeout (goto, waitForURL)
    
    // Additional settings for reliability
    video: 'retain-on-failure',
    launchOptions: {
      slowMo: process.env.CI ? 0 : 100, // Slow down actions in development
    },
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: [
    {
      command: 'npm run dev',
      port: 5173,
      reuseExistingServer: true,
    },
  ],
});