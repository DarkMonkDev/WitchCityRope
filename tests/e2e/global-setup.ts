import { ServiceHelper, checkDockerServices } from './helpers/service.helper'

// Environment-aware URLs for container/host compatibility
const WEB_BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';
const API_BASE_URL = process.env.API_URL || 'http://localhost:5655';


/**
 * Global setup for Playwright E2E tests
 * 
 * This file runs once before all tests to ensure Docker services are accessible.
 * If services are not running, tests will fail with clear error messages.
 */
async function globalSetup() {
  console.log('🚀 Setting up E2E test environment...')

  // DOCKER-ONLY ENFORCEMENT: Check for local dev servers and warn
  console.log('🔍 Verifying Docker-only environment...')

  try {
    // Check for local npm/node processes that might conflict
    const { exec } = require('child_process')
    const { promisify } = require('util')
    const execAsync = promisify(exec)

    const localProcesses = await execAsync('ps aux | grep -E "(npm run dev|vite.*--port.*517)" | grep -v grep | grep -v docker | grep -v "/app/" || true')
    if (localProcesses.stdout.trim()) {
      console.error(`
❌ CRITICAL: Local dev servers detected!

Local Node/npm processes found that may conflict with Docker:
${localProcesses.stdout}

🔧 FIX: Run the cleanup script first:
   ./scripts/kill-local-dev-servers.sh

🐳 THEN start Docker containers:
   ./dev.sh
`)
      process.exit(1)
    }
  } catch (error) {
    console.log('⚠️  Could not check for local processes (probably okay)')
  }

  // First, quickly check if services are accessible
  console.log('🔍 Checking Docker service status...')
  const status = await checkDockerServices()
  
  console.log('📊 Service Status:')
  console.log(`  - Web Service (http://localhost:5173): ${status.web ? '✅' : '❌'}`)
  console.log(`  - API Service (http://localhost:5655): ${status.api ? '✅' : '❌'}`)
  console.log(`  - API Health (http://localhost:5655/health): ${status.apiHealth ? '✅' : '❌'}`)
  
  // If critical services are not running, provide helpful error message
  if (!status.web) {
    console.error(`
❌ SETUP FAILED: Web service is not accessible

The React web application is not running at http://localhost:5173

🔧 TO FIX:
1. Start Docker services: ./dev.sh
2. Wait for services to be ready (check for "ready" message)
3. Re-run tests

📊 Current Status:
- Web: ${status.web ? 'OK' : 'FAILED'}
- API: ${status.api ? 'OK' : 'FAILED'}  
- API Health: ${status.apiHealth ? 'OK' : 'FAILED'}
`)
    process.exit(1)
  }

  // API service can be less strict since it might be starting up
  if (!status.api && !status.apiHealth) {
    console.warn(`
⚠️  WARNING: API service is not fully accessible

This might be okay if:
- API is still starting up
- Tests only use frontend features
- API health endpoint is not implemented

If tests fail due to API issues:
1. Check API logs: docker-compose logs api
2. Restart services: ./dev.sh
3. Verify API health: curl http://localhost:5655/health

Continuing with test execution...
`)
  } else {
    console.log('✅ API service is accessible')
  }

  // Wait for services to be stable with verbose output
  try {
    console.log('⏳ Waiting for services to be stable...')
    await ServiceHelper.waitForServices({ 
      verbose: true,
      timeout: 15000,
      retries: 3,
      retryDelay: 3000
    })
    console.log('✅ All services are ready for testing')
  } catch (error) {
    console.warn(`⚠️  Service stability check failed: ${error}`)
    console.log('Continuing with test execution (services may be partially ready)...')
  }

  console.log('🎯 E2E test environment setup complete\n')
}

export default globalSetup