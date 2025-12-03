import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for E2E testing against existing Docker services
 *
 * IMPORTANT: This configuration is designed to work with existing Docker containers:
 * - Web Service: http://localhost:5173 (React + Vite)
 * - API Service: http://localhost:5655 (Minimal API)
 * - Database: PostgreSQL on localhost:5433
 *
 * Start services first with: ./dev.sh
 */
export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 0, // No retries - we want to see ALL failures immediately
  workers: process.env.PW_WORKERS ? parseInt(process.env.PW_WORKERS, 10) : 6, // Always use 6 parallel workers for faster E2E testing (CI or local)
  // globalSetup: './tests/e2e/global-setup.ts', // Verify Docker services before tests
  reporter: [
    ['list'],
    // JSON reporter with detailed error output
    ['json', { outputFile: './test-results/test-results.json' }],
    // HTML report with all failure details
    ['html', { outputFolder: './test-results/html-report', open: 'never' }]
  ],
  // Output directory for test artifacts (screenshots, videos, traces)
  outputDir: './test-results',
  use: {
    // DOCKER-ONLY: Must use Docker web service on port 5173
    // In test containers, use internal service name (http://web:5173) via WEB_BASE_URL
    // On host machine, use localhost (http://localhost:5173)
    baseURL: process.env.PLAYWRIGHT_BASE_URL || process.env.WEB_BASE_URL || 'http://localhost:5173',

    // API endpoint for tests that need direct API access
    extraHTTPHeaders: {
      'Accept': 'application/json',
    },

    // CRITICAL: Capture traces on failure (not just retry - we have retries: 0)
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 30000,
    navigationTimeout: 30000,

    // Additional reliability settings for Docker environment
    launchOptions: {
      args: [
        '--disable-web-security', // Allow cross-origin requests for API testing
        '--disable-dev-shm-usage', // Overcome limited resource problems
        '--no-sandbox', // Required for containerized environments
      ],
    },
  },
  timeout: 120 * 1000, // 2 minutes per test (increased to accommodate slower selectors)

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    }
  ],

  // REMOVED webServer configuration - use existing Docker services
  // Tests expect services to be running at:
  // - Web: http://localhost:5173
  // - API: http://localhost:5655
  //
  // Start with: ./dev.sh before running tests
});
