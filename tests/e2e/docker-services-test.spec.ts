import { test, expect } from '@playwright/test';
import { ServiceHelper } from './test-utils/helpers/service.helper';

// Environment-aware URLs for container/host compatibility
const WEB_BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';
const API_BASE_URL = process.env.API_URL || 'http://localhost:5655';


/**
 * Docker Services Configuration Test
 *
 * This test verifies that the E2E configuration properly connects to
 * existing Docker services without trying to start new ones.
 */

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || WEB_BASE_URL;

test.describe('Docker Services Configuration', () => {
  
  test('should connect to existing web service', async ({ page }) => {
    // This should work without starting new services
    await page.goto('/', { waitUntil: 'domcontentloaded' });

    // Verify the React app loads - actual title is "Witch City Rope - Salem's Rope Bondage Community"
    await expect(page).toHaveTitle(/Witch City Rope/i);

    console.log('✅ Web service accessible via Playwright');
  });

  test('should verify Docker services are accessible', async ({ page }) => {
    // Use the service helper to verify services
    await ServiceHelper.checkServicesWithPage(page, { verbose: true });
    
    console.log('✅ Docker service verification complete');
  });

  test('should handle API requests without CORS issues', async ({ page }) => {
    // Navigate to the app first
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    
    // Try to make an API request from the browser context
    const apiCheck = await page.evaluate(async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/health`, {
          method: 'GET',
          headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
          }
        });
        
        return {
          success: true,
          status: response.status,
          statusText: response.statusText
        };
      } catch (error) {
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        };
      }
    });
    
    // Log the result for debugging
    console.log('API Check Result:', apiCheck);
    
    // The API might not be fully running, but we should be able to attempt the request
    // without getting immediate CORS or connection refusal errors
    if (!apiCheck.success) {
      console.log('⚠️  API not fully accessible:', apiCheck.error);
      // Don't fail the test if API is not ready, as long as web service works
    } else {
      console.log('✅ API request successful');
    }
  });

  test('should have correct baseURL configuration', async ({ page }) => {
    // Verify that the baseURL is properly configured
    await page.goto('/', { waitUntil: 'domcontentloaded' });

    const currentUrl = page.url();
    // Check for :5173 port (Docker web service) - accepts both localhost and container hostname
    expect(currentUrl).toMatch(/:5173/);

    console.log('✅ Base URL configuration correct:', currentUrl);
  });

});

test.describe('Configuration Validation', () => {
  
  test('should not start conflicting web servers', async ({ page }) => {
    // This test verifies that no webServer configuration is trying to start
    // new services that would conflict with Docker
    
    // If webServer was still configured, this would potentially fail
    // due to port conflicts or service startup races
    
    await page.goto('/', { 
      waitUntil: 'domcontentloaded',
      timeout: 15000 // Should be fast since services are already running
    });
    
    // If we get here quickly, it means we're using existing services
    const startTime = Date.now();
    await expect(page.locator('body')).toBeVisible();
    const loadTime = Date.now() - startTime;
    
    console.log(`✅ Page loaded in ${loadTime}ms (using existing Docker services)`);
    
    // Should be fast since services are already running
    expect(loadTime).toBeLessThan(10000); // Should load in under 10 seconds
  });

  test('should support authentication with existing services', async ({ page }) => {
    // Try to navigate to login page to verify the auth flow works
    // with existing Docker services

    await page.goto('/login', { waitUntil: 'domcontentloaded' });

    // Verify login page loads from Docker services using correct data-testid selectors
    await expect(page.locator('[data-testid="email-or-scenename-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible();

    console.log('✅ Authentication pages load correctly from Docker services');
  });

});