import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/playwright',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  maxFailures: process.env.CI ? 2 : undefined, // No limit in dev - let all tests run
  workers: process.env.CI ? 1 : 6,
  globalTeardown: './tests/playwright/global-teardown.ts',
  reporter: [
    ['list'],
    ['json', { outputFile: './playwright-report/test-results.json' }],
    ['html', { outputFolder: './playwright-report/html-report' }]
  ],

  // Timeout settings - MAXIMUM 90 seconds enforced across all tests
  // WHY 90 SECONDS: No test should need more than 60 seconds realistically,
  // but we set max to 90 seconds (1.5 minutes) as a safety buffer.
  // Tests consistently taking >30 seconds indicate problems that should be fixed.
  timeout: 90 * 1000, // 90 seconds ABSOLUTE MAXIMUM per test
  expect: {
    timeout: 5000, // 5 second assertion timeout (reasonable for UI)
  },
  
  use: {
    baseURL: process.env.VITE_BASE_URL || `http://localhost:${process.env.VITE_PORT || 5173}`,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',

    // Fast timeout settings - optimized for local development
    actionTimeout: 5000, // 5s for clicks, fills, etc.
    navigationTimeout: 10000, // 10s for page navigation
    
    // Additional settings for reliability
    video: 'retain-on-failure',
    launchOptions: {
      slowMo: 0, // No artificial delays - run at full speed
      // Memory management arguments
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

  // REMOVED webServer configuration - use existing Docker services
  // 
  // Tests expect services to be running via Docker at:
  // - Web: http://localhost:5173 (React + Vite)  
  // - API: http://localhost:5655 (Minimal API) 
  // - Database: PostgreSQL on localhost:5433
  // 
  // Start services first with: ./dev.sh from project root
});