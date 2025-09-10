import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Design System v7 testing
 */
export default defineConfig({
  testDir: '.', // Look for tests in current directory
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['list'],
    ['json', { outputFile: './test-results/playwright-results.json' }],
    ['html', { outputFolder: './test-results/playwright-report' }]
  ],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    }
  ],

  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: true, // Use existing dev server
    timeout: 30 * 1000,
  },
});