import { defineConfig, devices } from '@playwright/test';

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: '.',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Reduced workers to prevent port conflicts with Docker services */
  workers: process.env.CI ? 1 : 6,
  /* Verify Docker services before running tests */
  globalSetup: './global-setup.ts',
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: 'http://localhost:5173',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',

    /* Screenshot on failure */
    screenshot: 'only-on-failure',
    
    /* Additional settings for Docker environment reliability */
    video: 'retain-on-failure',
    
    /* Timeouts for actions and navigation - increased for Docker */
    actionTimeout: 30000,
    navigationTimeout: 30000,
    
    /* Additional browser settings for Docker environment */
    launchOptions: {
      args: [
        '--disable-web-security', // Allow cross-origin requests for API testing
        '--disable-dev-shm-usage', // Overcome limited resource problems
        '--no-sandbox', // Required for containerized environments
        '--disable-gpu', // Prevent GPU issues in containers
      ],
    },
    
    /* Extra HTTP headers for API requests */
    extraHTTPHeaders: {
      'Accept': 'application/json,text/html,*/*',
    },
  },
  
  /* Global test timeout - 1.5 minutes per test */
  timeout: 90 * 1000,

  /* Configure projects for major browsers */
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

    /* Test against mobile viewports. */
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] },
    },
  ],

  /* Folder for test artifacts such as screenshots, videos, traces, etc. */
  outputDir: 'test-results/',

  /* Use existing Docker services - DO NOT start new servers */
  // REMOVED webServer configuration to prevent port conflicts
  // 
  // Tests expect services to be running via Docker at:
  // - Web: http://localhost:5173 (React + Vite)  
  // - API: http://localhost:5655 (Minimal API)
  // - Database: PostgreSQL on localhost:5433
  // 
  // Start services first with: ./dev.sh from project root
});