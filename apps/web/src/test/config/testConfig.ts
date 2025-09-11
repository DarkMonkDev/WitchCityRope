/**
 * Test Configuration - Centralized Test Environment Settings
 * 
 * This module provides environment-aware configuration for tests,
 * preventing hard-coded port issues in test files.
 */

/**
 * Test environment configuration object
 */
export const testConfig = {
  // API Configuration
  api: {
    // Use environment variable or fallback to development default
    baseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655',
    timeout: 30000,
  },
  
  // Web Application Configuration
  web: {
    // Use environment variable or fallback to development default
    baseUrl: `http://localhost:${import.meta.env.VITE_PORT || '5173'}`,
  },
  
  // Test Server Configuration (for wireframe comparisons, etc.)
  testServer: {
    baseUrl: `http://localhost:${import.meta.env.VITE_TEST_SERVER_PORT || '8080'}`,
  },
  
  // Database Configuration (for direct connection tests)
  database: {
    port: import.meta.env.VITE_DB_PORT || '5433',
  },
  
  // Common test URLs (computed from base URLs)
  get urls() {
    return {
      // Web Application URLs
      web: {
        home: this.web.baseUrl,
        login: `${this.web.baseUrl}/login`,
        register: `${this.web.baseUrl}/register`,
        dashboard: `${this.web.baseUrl}/dashboard`,
        events: `${this.web.baseUrl}/events`,
        admin: `${this.web.baseUrl}/admin`,
        formTest: `${this.web.baseUrl}/form-test`,
      },
      
      // API URLs
      api: {
        base: this.api.baseUrl,
        health: `${this.api.baseUrl}/api/health`,
        events: `${this.api.baseUrl}/api/events`,
        auth: {
          login: `${this.api.baseUrl}/api/auth/login`,
          logout: `${this.api.baseUrl}/api/auth/logout`,
          user: `${this.api.baseUrl}/api/auth/user`,
          profile: `${this.api.baseUrl}/api/Protected/profile`,
        },
        protected: {
          welcome: `${this.api.baseUrl}/api/protected/welcome`,
        },
      },
      
      // Test Server URLs
      testServer: {
        wireframes: `${this.testServer.baseUrl}/docs/functional-areas/events/admin-events-management/event-creation.html`,
      },
    }
  },
} as const

/**
 * Get test credentials for different user types
 */
export const testCredentials = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'Test123!',
  },
  teacher: {
    email: 'teacher@witchcityrope.com', 
    password: 'Test123!',
  },
  vettedMember: {
    email: 'vetted@witchcityrope.com',
    password: 'Test123!',
  },
  generalMember: {
    email: 'member@witchcityrope.com',
    password: 'Test123!',
  },
  guest: {
    email: 'guest@witchcityrope.com',
    password: 'Test123!',
  },
} as const

/**
 * Helper function to get URL for specific event by ID
 */
export const getEventUrl = (eventId: string): string => {
  return `${testConfig.urls.api.events}/${eventId}`
}

/**
 * Helper function to get wireframe URL for specific feature
 */
export const getWireframeUrl = (path: string): string => {
  return `${testConfig.testServer.baseUrl}/${path}`
}

/**
 * Check if we're running in CI environment
 */
export const isCI = (): boolean => {
  return process.env.CI === 'true' || process.env.NODE_ENV === 'test'
}

/**
 * Get appropriate timeouts based on environment
 */
export const getTimeouts = () => {
  return {
    // Shorter timeouts for CI, longer for local development
    short: isCI() ? 5000 : 10000,
    medium: isCI() ? 15000 : 30000,
    long: isCI() ? 30000 : 60000,
  }
}