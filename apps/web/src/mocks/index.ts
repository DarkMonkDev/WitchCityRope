// MSW Setup for UI Testing
// Only enables when VITE_MSW_ENABLED=true for UI testing purposes

export async function enableMocking() {
  // Only enable MSW in development when explicitly requested
  if (import.meta.env.MODE !== 'development' || import.meta.env.VITE_MSW_ENABLED !== 'true') {
    console.log('🔶 MSW disabled - Not in development mode or VITE_MSW_ENABLED not true')
    return
  }

  try {
    const { worker } = await import('../test/mocks/browser')
    
    // Start the worker and wait for it to be ready
    await worker.start({
      onUnhandledRequest: 'warn', // Don't error on unhandled requests
      quiet: false, // Show MSW logs in console
    })
    
    console.log('🔶 MSW enabled for UI testing - Intercepting API calls')
    console.log('🔶 Available test endpoints:')
    console.log('  - POST /api/Auth/login (admin@witchcityrope.com / Test123!)')
    console.log('  - GET /api/Protected/profile')
    console.log('  - POST /api/Auth/logout')
    console.log('  - GET /api/events')
  } catch (error) {
    console.error('❌ Failed to start MSW:', error)
  }
}