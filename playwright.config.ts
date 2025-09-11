import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Admin Events Management testing
 */
export default defineConfig({
  testDir: './tests/playwright', // Look for tests in tests/playwright directory
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['list'],
    ['json', { outputFile: './test-results/test-results.json' }],
    ['html', { outputFolder: './test-results/html-report' }]
  ],
  use: {
    baseURL: process.env.VITE_BASE_URL || `http://localhost:${process.env.VITE_PORT || 5173}`,
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
    url: process.env.VITE_BASE_URL || `http://localhost:${process.env.VITE_PORT || 5173}`,
    reuseExistingServer: true, // Use existing dev server
    timeout: 30 * 1000,
  },
});