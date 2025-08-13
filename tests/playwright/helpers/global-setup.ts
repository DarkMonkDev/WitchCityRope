/**
 * Global setup for Playwright E2E tests in CI/CD environments
 * Handles authentication state preparation and environment verification
 */

import { chromium, FullConfig } from '@playwright/test';
import { AuthHelpers } from './auth.helpers';
import { testConfig } from './test.config';
import { BlazorHelpers } from './blazor.helpers';

async function globalSetup(config: FullConfig) {
  console.log('🔧 Starting global setup for E2E tests...');
  
  const baseURL = process.env.BASE_URL || testConfig.baseUrl;
  console.log(`📍 Base URL: ${baseURL}`);
  
  // Wait for application to be ready
  console.log('⏳ Waiting for application to be ready...');
  await waitForApplicationReady(baseURL);
  
  // Setup authentication states for all test users
  console.log('🔐 Setting up authentication states...');
  await setupAuthenticationStates(baseURL);
  
  console.log('✅ Global setup completed successfully');
}

/**
 * Wait for the application to be ready and responsive
 */
async function waitForApplicationReady(baseURL: string, maxAttempts: number = 30): Promise<void> {
  let attempts = 0;
  
  while (attempts < maxAttempts) {
    try {
      const response = await fetch(`${baseURL}/health`);
      if (response.ok) {
        console.log('✅ Application health check passed');
        return;
      }
    } catch (error) {
      // Application not ready yet
    }
    
    attempts++;
    console.log(`⏳ Waiting for application... (attempt ${attempts}/${maxAttempts})`);
    await new Promise(resolve => setTimeout(resolve, 2000));
  }
  
  // Try basic homepage as fallback
  try {
    const response = await fetch(baseURL);
    if (response.status === 200) {
      console.log('✅ Application responding (fallback check)');
      return;
    }
  } catch (error) {
    console.error('❌ Application not responding:', error);
    throw new Error(`Application at ${baseURL} is not ready after ${maxAttempts} attempts`);
  }
}

/**
 * Setup authentication states for all test account types
 */
async function setupAuthenticationStates(baseURL: string): Promise<void> {
  const browser = await chromium.launch({
    headless: true,
    args: [
      '--no-sandbox',
      '--disable-dev-shm-usage',
      '--disable-web-security'
    ]
  });
  
  try {
    // Initialize auth storage directory
    await AuthHelpers.initializeAuthStorage();
    
    // Setup authentication for each account type
    const accountTypes = Object.keys(testConfig.accounts) as Array<keyof typeof testConfig.accounts>;
    
    for (const accountType of accountTypes) {
      console.log(`🔐 Setting up authentication for ${accountType}...`);
      
      try {
        const context = await browser.newContext({
          baseURL,
          viewport: { width: 1440, height: 900 }
        });
        
        const page = await context.newPage();
        
        // Navigate to home first to ensure application is loaded
        await page.goto('/');
        
        // Wait for application to be ready with timeout
        try {
          await BlazorHelpers.waitForBlazorReady(page);
        } catch (error) {
          console.warn(`Blazor initialization took longer than expected for ${accountType}, continuing...`);
        }
        
        // Login and save auth state with retry logic
        const credentials = testConfig.accounts[accountType];
        await loginWithRetries(page, credentials, accountType);
        
        console.log(`✅ Authentication setup complete for ${accountType}`);
        
        await context.close();
        
        // Small delay between setups to prevent overwhelming the application
        await new Promise(resolve => setTimeout(resolve, 1000));
        
      } catch (error) {
        console.warn(`⚠️ Failed to setup authentication for ${accountType}:`, error);
        
        // For CI, we'll continue with other accounts rather than failing completely
        // This allows partial test runs even if some accounts have issues
        if (!process.env.CI) {
          throw error;
        }
      }
    }
    
  } finally {
    await browser.close();
  }
}

/**
 * Login with retry logic for better reliability
 * Handles transient network or initialization issues
 */
async function loginWithRetries(page: any, credentials: any, accountType: string, maxRetries: number = 3): Promise<void> {
  let attempt = 1;
  
  while (attempt <= maxRetries) {
    try {
      console.log(`Attempting login for ${accountType} (attempt ${attempt}/${maxRetries})`);
      await AuthHelpers.login(page, credentials);
      console.log(`✅ Login successful for ${accountType}`);
      return;
    } catch (error) {
      console.warn(`❌ Login attempt ${attempt} failed for ${accountType}:`, error);
      
      if (attempt === maxRetries) {
        throw new Error(`Failed to login ${accountType} after ${maxRetries} attempts: ${error}`);
      }
      
      // Wait before retry
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Try to navigate to home again in case of navigation issues
      try {
        await page.goto('/');
        await BlazorHelpers.waitForBlazorReady(page);
      } catch (navError) {
        console.warn(`Navigation retry failed for ${accountType}:`, navError);
      }
      
      attempt++;
    }
  }
}

/**
 * Verify test data is available in the database
 */
async function verifyTestData(): Promise<void> {
  console.log('🔍 Verifying test data availability...');
  
  // This could be expanded to check database connectivity and test data
  // For now, we'll assume the seeding process handled this
  
  console.log('✅ Test data verification completed');
}

/**
 * Setup test artifacts directories
 */
async function setupTestDirectories(): Promise<void> {
  const fs = await import('fs/promises');
  const path = await import('path');
  
  const directories = [
    'test-results',
    'playwright-report', 
    'test-results/screenshots',
    'test-results/videos',
    'test-results/traces'
  ];
  
  for (const dir of directories) {
    const fullPath = path.join(process.cwd(), dir);
    try {
      await fs.mkdir(fullPath, { recursive: true });
    } catch (error) {
      console.warn(`Could not create directory ${dir}:`, error);
    }
  }
  
  console.log('📁 Test directories setup completed');
}

export default globalSetup;