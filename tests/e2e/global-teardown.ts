import { chromium, FullConfig } from '@playwright/test';
import { execSync } from 'child_process';

/**
 * Global teardown to prevent orphaned processes that can crash the system
 */
async function globalTeardown(config: FullConfig) {
  console.log('🧹 Running global teardown to prevent system crashes...');
  
  try {
    // Kill any orphaned Chromium processes
    try {
      execSync('pkill -f "chromium" || true', { stdio: 'ignore' });
      execSync('pkill -f "chrome" || true', { stdio: 'ignore' });
      console.log('✅ Cleaned up any orphaned browser processes');
    } catch (error) {
      // Ignore errors, these are cleanup commands
      console.log('⚠️  No orphaned browser processes found (this is good)');
    }
    
    // Kill any orphaned Node.js test processes
    try {
      execSync('pkill -f "vitest" || true', { stdio: 'ignore' });
      execSync('pkill -f "playwright" || true', { stdio: 'ignore' });
      console.log('✅ Cleaned up any orphaned test processes');
    } catch (error) {
      // Ignore errors, these are cleanup commands
      console.log('⚠️  No orphaned test processes found (this is good)');
    }
    
    // Force garbage collection if available
    if (global.gc) {
      global.gc();
      console.log('✅ Forced garbage collection');
    }
    
    console.log('🎉 Global teardown completed successfully');
  } catch (error) {
    console.error('❌ Error during global teardown:', error);
    // Don't throw - we don't want teardown failures to fail the tests
  }
}

export default globalTeardown;