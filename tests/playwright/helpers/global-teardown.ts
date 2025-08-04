/**
 * Global teardown for Playwright E2E tests in CI/CD environments
 * Handles cleanup and reporting of test artifacts
 */

import { FullConfig } from '@playwright/test';
import { AuthHelpers } from './auth.helpers';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting global teardown...');
  
  try {
    // Generate test execution summary
    await generateTestSummary();
    
    // Clean up authentication states if not in CI (preserve for debugging in CI)
    if (!process.env.CI) {
      console.log('🔐 Cleaning up authentication states...');
      await AuthHelpers.clearAllAuthStates();
    } else {
      console.log('🔐 Preserving authentication states for CI debugging');
    }
    
    // Clean up temporary test data
    await cleanupTempData();
    
    // Report final statistics
    await reportTestStatistics();
    
    console.log('✅ Global teardown completed successfully');
    
  } catch (error) {
    console.error('❌ Error during global teardown:', error);
    // Don't fail the entire test run due to teardown issues
  }
}

/**
 * Generate a summary of test execution
 */
async function generateTestSummary(): Promise<void> {
  const fs = await import('fs/promises');
  const path = await import('path');
  
  try {
    const testResultsPath = path.join(process.cwd(), 'test-results');
    const summaryPath = path.join(testResultsPath, 'execution-summary.json');
    
    const summary = {
      timestamp: new Date().toISOString(),
      environment: {
        ci: !!process.env.CI,
        baseUrl: process.env.BASE_URL,
        node_version: process.version,
        platform: process.platform
      },
      execution: {
        startTime: process.env.TEST_START_TIME || new Date().toISOString(),
        endTime: new Date().toISOString(),
        duration: Date.now() - (parseInt(process.env.TEST_START_TIMESTAMP || '0') || Date.now())
      },
      configuration: {
        workers: process.env.CI ? 1 : 'auto',
        headless: !!process.env.HEADLESS,
        retries: process.env.CI ? 3 : 1
      }
    };
    
    await fs.writeFile(summaryPath, JSON.stringify(summary, null, 2));
    console.log('📊 Test execution summary generated');
    
  } catch (error) {
    console.warn('⚠️ Could not generate test summary:', error);
  }
}

/**
 * Clean up temporary test data and artifacts
 */
async function cleanupTempData(): Promise<void> {
  const fs = await import('fs/promises');
  const path = await import('path');
  
  try {
    // Clean up temporary files (not test results)
    const tempPatterns = [
      'tmp-*',
      '*.tmp',
      '.temp-*'
    ];
    
    const testResultsPath = path.join(process.cwd(), 'test-results');
    
    try {
      const files = await fs.readdir(testResultsPath);
      
      for (const file of files) {
        if (tempPatterns.some(pattern => 
          file.match(pattern.replace('*', '.*'))
        )) {
          const filePath = path.join(testResultsPath, file);
          await fs.unlink(filePath);
          console.log(`🗑️ Cleaned up temporary file: ${file}`);
        }
      }
    } catch (error) {
      // Directory might not exist or be accessible
      console.log('📁 No temporary files to clean up');
    }
    
    console.log('🧹 Temporary data cleanup completed');
    
  } catch (error) {
    console.warn('⚠️ Could not complete temporary data cleanup:', error);
  }
}

/**
 * Report test execution statistics
 */
async function reportTestStatistics(): Promise<void> {
  const fs = await import('fs/promises');
  const path = await import('path');
  
  try {
    const testResultsPath = path.join(process.cwd(), 'test-results');
    
    // Count different types of artifacts
    let screenshotCount = 0;
    let videoCount = 0;
    let traceCount = 0;
    
    try {
      const files = await fs.readdir(testResultsPath, { recursive: true });
      
      for (const file of files) {
        if (typeof file === 'string') {
          if (file.endsWith('.png')) screenshotCount++;
          if (file.endsWith('.webm')) videoCount++;
          if (file.endsWith('.zip') && file.includes('trace')) traceCount++;
        }
      }
    } catch (error) {
      console.log('📊 Could not analyze test artifacts');
    }
    
    console.log('📈 Test Execution Statistics:');
    console.log(`   📸 Screenshots: ${screenshotCount}`);
    console.log(`   🎥 Videos: ${videoCount}`);
    console.log(`   🔍 Traces: ${traceCount}`);
    
    if (process.env.CI) {
      console.log('☁️ Artifacts will be uploaded by CI system');
    }
    
  } catch (error) {
    console.warn('⚠️ Could not generate statistics:', error);
  }
}

/**
 * Cleanup browser processes (safety measure)
 */
async function cleanupBrowserProcesses(): Promise<void> {
  if (process.platform !== 'win32') {
    try {
      const { exec } = await import('child_process');
      const { promisify } = await import('util');
      const execAsync = promisify(exec);
      
      // Kill any remaining browser processes
      await execAsync('pkill -f chrome || true');
      await execAsync('pkill -f firefox || true');
      await execAsync('pkill -f webkit || true');
      
      console.log('🔄 Browser process cleanup completed');
    } catch (error) {
      console.log('🔄 Browser process cleanup not needed or failed silently');
    }
  }
}

/**
 * Report CI-specific information
 */
async function reportCIInformation(): Promise<void> {
  if (!process.env.CI) return;
  
  console.log('🏗️ CI Environment Information:');
  console.log(`   Build: ${process.env.GITHUB_RUN_ID || 'Unknown'}`);
  console.log(`   Branch: ${process.env.GITHUB_REF_NAME || 'Unknown'}`);
  console.log(`   Commit: ${process.env.GITHUB_SHA?.substring(0, 8) || 'Unknown'}`);
  console.log(`   Workflow: ${process.env.GITHUB_WORKFLOW || 'Unknown'}`);
  
  if (process.env.GITHUB_ACTIONS) {
    console.log('📋 Test results will be integrated with GitHub Actions');
  }
}

export default globalTeardown;