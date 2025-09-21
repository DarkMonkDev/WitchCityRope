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
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : 6, // Reduced workers to prevent port conflicts
  // globalSetup: './tests/e2e/global-setup.ts', // Verify Docker services before tests
  reporter: [
    ['list'],
    ['json', { outputFile: './playwright-results/test-results.json' }],
    ['html', { outputFolder: './playwright-results/html-report' }]
  ],
  use: {
    // DOCKER-ONLY: Must use Docker web service on port 5173
    baseURL: 'http://localhost:5173', // NEVER change this - Docker containers only
    
    // API endpoint for tests that need direct API access
    extraHTTPHeaders: {
      'Accept': 'application/json',
    },
    
    trace: 'on-first-retry',
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
  timeout: 90 * 1000, // 1.5 minutes per test

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