import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/playwright',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  maxFailures: 2, // Stop after 2 failures to prevent runaway tests
  workers: process.env.CI ? 1 : 6, // Limit to 2 workers max to prevent system crashes
  globalTeardown: './tests/playwright/global-teardown.ts', // Clean up orphaned processes
  reporter: [['list'], ['html', { outputFolder: './playwright-report' }]], // Fixed path conflict
  
  // Global timeout settings for improved reliability - aligned with main configs
  timeout: 90 * 1000, // Overall test timeout (1.5 minutes)
  expect: {
    timeout: 10000, // Assertion timeout
  },
  
  use: {
    baseURL: process.env.VITE_BASE_URL || `http://localhost:${process.env.VITE_PORT || 5173}`,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    
    // Enhanced timeout settings for authentication flows
    actionTimeout: 15000, // Action timeout (clicks, fills, etc.)
    navigationTimeout: 30000, // Navigation timeout (goto, waitForURL)
    
    // Additional settings for reliability
    video: 'retain-on-failure',
    launchOptions: {
      slowMo: process.env.CI ? 0 : 100, // Slow down actions in development
      // Memory management arguments to prevent system crashes
      args: [
        '--max-old-space-size=1024', // Limit Node.js memory
        '--disable-dev-shm-usage', // Overcome limited resource problems
        '--disable-extensions-except', // Reduce memory footprint
        '--disable-gpu', // Disable GPU hardware acceleration
        '--no-sandbox', // Required for containerized environments
        '--disable-web-security', // Allow cross-origin requests in tests
        '--disable-features=VizDisplayCompositor', // Reduce memory usage
        '--memory-pressure-off', // Disable memory pressure warnings
        '--max_old_space_size=1024', // Additional Node memory limit
        '--disable-background-timer-throttling', // Prevent hanging tests
        '--disable-backgrounding-occluded-windows',
        '--disable-renderer-backgrounding',
      ],
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
      port: parseInt(process.env.VITE_PORT || '5173', 10),
      reuseExistingServer: true,
    },
  ],
});