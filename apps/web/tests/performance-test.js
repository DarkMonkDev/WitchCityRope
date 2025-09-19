/**
 * Performance Validation Tests for Authentication Vertical Slice
 * Phase 4: Testing & Validation
 */

const API_BASE = 'http://localhost:5655'
const testResults = []

// Test credentials
const testUser = {
  email: `perf-test-${Date.now()}@example.com`,
  password: 'PerfTest123',
  sceneName: `PerfTester${Date.now()}`,
}

// Performance targets (in milliseconds)
const TARGETS = {
  registration: 2000, // < 2 seconds
  login: 1000, // < 1 second
  protectedapi: 200, // < 200ms
  logout: 500, // < 500ms
}

// Helper function to measure async operation time
async function measureTime(name, operation) {
  const start = Date.now()
  try {
    const result = await operation()
    const duration = Date.now() - start
    return { name, duration, success: true, result }
  } catch (error) {
    const duration = Date.now() - start
    return { name, duration, success: false, error: error.message }
  }
}

// Test registration performance
async function testRegistration() {
  return measureTime('Registration', async () => {
    const response = await fetch(`${API_BASE}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(testUser),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(`Registration failed: ${error}`)
    }

    return await response.json()
  })
}

// Test login performance
async function testLogin() {
  return measureTime('Login', async () => {
    const response = await fetch(`${API_BASE}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        email: testUser.email,
        password: testUser.password,
      }),
    })

    if (!response.ok) {
      throw new Error(`Login failed: ${response.status}`)
    }

    const data = await response.json()
    // BFF Pattern: No token returned - authentication via httpOnly cookies
    return data
  })
}

// Test protected API call performance
async function testProtectedApi() {
  return measureTime('Protected API', async () => {
    // BFF Pattern: Authentication via httpOnly cookies, no Authorization header needed
    const response = await fetch(`${API_BASE}/api/protected/welcome`, {
      credentials: 'include', // Include httpOnly cookies
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`Protected API failed: ${response.status}`)
    }

    return await response.json()
  })
}

// Test logout performance
async function testLogout() {
  return measureTime('Logout', async () => {
    // BFF Pattern: Authentication via httpOnly cookies, no Authorization header needed
    const response = await fetch(`${API_BASE}/api/auth/logout`, {
      method: 'POST',
      credentials: 'include', // Include httpOnly cookies
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`Logout failed: ${response.status}`)
    }

    return { success: true }
  })
}

// Run all performance tests
async function runPerformanceTests() {
  console.log('🚀 Starting Performance Validation Tests')
  console.log('=====================================\n')

  // Run tests in sequence
  const tests = [testRegistration, testLogin, testProtectedApi, testLogout]

  for (const test of tests) {
    const result = await test()
    testResults.push(result)

    const target = TARGETS[result.name.toLowerCase().replace(' ', '')]
    const passed = result.success && result.duration <= target
    const icon = passed ? '✅' : '❌'

    console.log(`${icon} ${result.name}`)
    console.log(`   Duration: ${result.duration}ms`)
    console.log(`   Target: < ${target}ms`)
    console.log(`   Status: ${passed ? 'PASS' : 'FAIL'}`)
    if (!result.success) {
      console.log(`   Error: ${result.error}`)
    }
    console.log('')
  }

  // Summary
  console.log('\n=====================================')
  console.log('📊 Performance Test Summary')
  console.log('=====================================')

  const totalPassed = testResults.filter((r) => {
    const target = TARGETS[r.name.toLowerCase().replace(' ', '')]
    return r.success && r.duration <= target
  }).length

  console.log(`Total Tests: ${testResults.length}`)
  console.log(`Passed: ${totalPassed}`)
  console.log(`Failed: ${testResults.length - totalPassed}`)

  // Performance metrics
  console.log('\n📈 Performance Metrics:')
  testResults.forEach((result) => {
    if (result.success) {
      console.log(`   ${result.name}: ${result.duration}ms`)
    }
  })

  const allPassed = totalPassed === testResults.length
  console.log(
    `\n${allPassed ? '✅ ALL PERFORMANCE TARGETS MET!' : '❌ Some performance targets not met'}`
  )

  // Exit with appropriate code
  process.exit(allPassed ? 0 : 1)
}

// Run tests
runPerformanceTests().catch((error) => {
  console.error('❌ Fatal error running performance tests:', error)
  process.exit(1)
})
