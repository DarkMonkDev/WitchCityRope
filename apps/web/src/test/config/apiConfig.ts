// test/config/apiConfig.ts
// Centralized API URL configuration for tests - NO MORE HARD-CODED PORTS

/**
 * Gets the API base URL for tests, using environment variables
 * Falls back to the same default as production code
 */
export const getTestApiBaseUrl = (): string => {
  return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655'
}

/**
 * Default API base URL for tests
 * Use this constant in test files instead of hard-coding ports
 */
export const TEST_API_BASE_URL = getTestApiBaseUrl()

/**
 * Common test configuration
 */
export const testConfig = {
  apiBaseUrl: TEST_API_BASE_URL,
  defaultTimeout: 10000,
  retryCount: 0, // No retries in tests
} as const