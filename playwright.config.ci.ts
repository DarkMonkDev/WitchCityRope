import { defineConfig, devices } from '@playwright/test';
import { testConfig } from './tests/playwright/helpers/test.config';

/**
 * Enhanced Playwright configuration optimized for CI/CD environments
 * Based on the working authentication patterns and Docker setup
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests/playwright',
  
  /* Test organization */
  fullyParallel: false, // Disabled for CI stability
  workers: process.env.CI ? 1 : 2, // Single worker in CI to prevent conflicts
  
  /* Snapshots and visual regression */
  snapshotDir: './tests/playwright/visual-regression/__screenshots__',
  snapshotPathTemplate: '{snapshotDir}/{testFileDir}/{testFileName}-snapshots/{arg}{-projectName}{-snapshotSuffix}{ext}',
  
  /* CI-specific settings */
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 3 : 1, // More retries in CI for stability
  
  /* Enhanced timeouts for CI environments */
  timeout: parseInt(process.env.TEST_TIMEOUT || '30000'),
  globalTimeout: 15 * 60 * 1000, // 15 minutes global timeout
  
  /* Comprehensive reporting for CI */
  reporter: [
    ['html', { 
      outputFolder: 'playwright-report',
      open: 'never' // Don't open in CI
    }],
    ['json', { outputFile: 'test-results/results.json' }],
    ['junit', { outputFile: 'test-results/junit-results.xml' }],
    ['github'], // GitHub Actions integration
    ['list', { printSteps: process.env.CI ? false : true }],
    ...(process.env.CI ? [['blob']] : []) // Blob reporter for CI artifact upload
  ],
  
  /* Global test settings optimized for CI */
  use: {
    /* Base URL with environment support */
    baseURL: process.env.BASE_URL || testConfig.baseUrl,
    
    /* Enhanced tracing for debugging failures */
    trace: process.env.CI ? 'retain-on-failure' : 'on-first-retry',
    
    /* Screenshot strategy */
    screenshot: process.env.CI ? 'only-on-failure' : 'off',
    
    /* Video recording for failure analysis */
    video: process.env.CI ? 'retain-on-failure' : 'off',
    
    /* Timeout settings */
    actionTimeout: parseInt(process.env.ACTION_TIMEOUT || '10000'),
    navigationTimeout: parseInt(process.env.NAVIGATION_TIMEOUT || '30000'),
    
    /* Viewport optimized for consistent testing */
    viewport: { width: 1440, height: 900 },
    
    /* Security and performance settings */
    ignoreHTTPSErrors: true,
    acceptDownloads: false,
    
    /* Browser context settings for CI */
    contextOptions: {
      // Reduce memory usage in CI
      reducedMotion: 'reduce',
      // Consistent timezone
      timezoneId: 'UTC',
      // Consistent locale
      locale: 'en-US'
    }
  },

  /* CI-optimized browser projects */
  projects: [
    // Authentication setup project
    {
      name: 'setup',
      testMatch: /setup\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    
    // Primary test browser (Chromium) - always runs
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        // CI-specific Chrome flags
        launchOptions: {
          args: [
            '--no-sandbox',
            '--disable-dev-shm-usage',
            '--disable-web-security',
            '--disable-features=TranslateUI',
            '--disable-ipc-flooding-protection',
            '--disable-renderer-backgrounding',
            '--disable-backgrounding-occluded-windows',
            '--disable-background-timer-throttling',
            '--disable-background-networking',
            '--disable-sync',
            '--metrics-recording-only',
            '--no-first-run'
          ]
        }
      },
      dependencies: ['setup']
    },

    // Firefox - runs on main branch and manual triggers
    {
      name: 'firefox',
      use: { 
        ...devices['Desktop Firefox'],
        launchOptions: {
          firefoxUserPrefs: {
            'media.navigator.streams.fake': true,
            'media.navigator.permission.disabled': true,
            'permissions.default.microphone': 1,
            'permissions.default.camera': 1
          }
        }
      },
      dependencies: ['setup']
    },

    // WebKit - limited to essential tests in CI
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
      dependencies: ['setup']
    },

    // Mobile testing (optional, triggered manually)
    {
      name: 'Mobile Chrome',
      use: { 
        ...devices['Pixel 5'],
        // Reduce mobile test complexity in CI
        contextOptions: {
          reducedMotion: 'reduce'
        }
      },
      dependencies: ['setup']
    },

    {
      name: 'Mobile Safari',
      use: { 
        ...devices['iPhone 12'],
        contextOptions: {
          reducedMotion: 'reduce'
        }
      },
      dependencies: ['setup']
    }
  ],

  /* Docker-aware web server configuration */
  webServer: process.env.CI ? undefined : {
    // Don't start server in CI - Docker handles it
    command: 'echo "Using Docker services in CI"',
    port: parseInt(process.env.WEB_PORT || '5651'),
    reuseExistingServer: true,
    timeout: 2 * 60 * 1000, // 2 minutes timeout
    env: {
      BASE_URL: process.env.BASE_URL || 'http://localhost:5651'
    }
  },

  /* Global setup and teardown */
  globalSetup: process.env.CI ? './tests/playwright/helpers/global-setup.ts' : undefined,
  globalTeardown: process.env.CI ? './tests/playwright/helpers/global-teardown.ts' : undefined,

  /* Enhanced expect configuration for CI */
  expect: {
    timeout: 10000, // Longer assertion timeout for CI
    toMatchSnapshot: {
      // More lenient screenshot comparison for CI
      maxDiffPixels: process.env.CI ? 200 : 100,
      threshold: process.env.CI ? 0.3 : 0.2
    },
    toHaveScreenshot: {
      maxDiffPixels: process.env.CI ? 200 : 100,
      threshold: process.env.CI ? 0.3 : 0.2,
      animations: 'disabled',
      scale: 'css',
      // CI-specific settings
      mode: process.env.CI ? 'local' : undefined
    }
  },

  /* Test metadata for better CI integration */
  metadata: {
    ci: !!process.env.CI,
    environment: process.env.ASPNETCORE_ENVIRONMENT || 'Development',
    baseUrl: process.env.BASE_URL || testConfig.baseUrl,
    dockerized: true,
    authenticationPattern: 'blazor-identity-fixed',
    testFramework: 'playwright-typescript'
  }
});

/* Environment-specific configurations */
if (process.env.CI) {
  // CI-specific optimizations
  console.log('🔧 Running in CI mode with optimized settings');
  console.log(`📍 Base URL: ${process.env.BASE_URL || testConfig.baseUrl}`);
  console.log(`🏗️ Environment: ${process.env.ASPNETCORE_ENVIRONMENT || 'Testing'}`);
}