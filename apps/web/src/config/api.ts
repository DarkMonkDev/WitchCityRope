/**
 * API Configuration - Centralized API Base URL Management
 *
 * This module provides a centralized way to access the API base URL
 * using environment variables, preventing hard-coded port issues.
 *
 * CRITICAL: In development, uses relative paths to go through Vite proxy.
 * This ensures cookies work correctly (same-origin) for BFF authentication pattern.
 */

/**
 * Get the current API base URL from environment variables
 * Falls back to development default if not configured
 *
 * IMPORTANT: Returns empty string in development to use Vite proxy (relative URLs)
 * This solves the cookie persistence issue where SameSite=Lax cookies weren't
 * being sent from localhost:5173 to localhost:5655 (different origins)
 */
export const getApiBaseUrl = (): string => {
  // In development, use empty string to get relative URLs (go through Vite proxy)
  // This ensures cookies are set for localhost:5173 (web server) not localhost:5655 (API)
  if (import.meta.env.DEV) {
    return ''
  }

  // In production/staging, use absolute URL from environment
  return import.meta.env.VITE_API_BASE_URL || ''
}

/**
 * Get the API URL for a specific endpoint
 * @param endpoint - The API endpoint path (e.g., '/api/events', '/api/auth/login')
 *
 * In development: Returns '/api/auth/login' (goes through Vite proxy to API)
 * In production: Returns 'https://api.example.com/api/auth/login' (direct to API)
 */
export const getApiUrl = (endpoint: string): string => {
  const baseUrl = getApiBaseUrl()
  // Ensure endpoint starts with /
  const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`
  return `${baseUrl}${cleanEndpoint}`
}

/**
 * Configuration object for API settings
 */
export const apiConfig = {
  baseUrl: getApiBaseUrl(),
  timeout: 30000, // 30 second timeout
  
  // Common endpoints
  endpoints: {
    auth: {
      login: '/api/auth/login',
      register: '/api/auth/register',
      logout: '/api/auth/logout',
      refresh: '/api/auth/refresh',
      currentUser: '/api/auth/current-user',
    },
    events: {
      list: '/api/events',
      byId: (id: string) => `/api/events/${id}`,
      bySlug: (slug: string) => `/api/events/${slug}`,
    },
    protected: {
      welcome: '/api/protected/welcome',
      profile: '/api/Protected/profile',
    },
    health: '/api/health',
  },
} as const

/**
 * Default request configuration for fetch calls
 */
export const defaultRequestConfig: RequestInit = {
  credentials: 'include', // Include httpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
}

/**
 * Helper function to make API requests with consistent configuration
 * @param endpoint - The API endpoint
 * @param options - Additional fetch options
 */
export const apiRequest = async (
  endpoint: string,
  options: RequestInit = {}
): Promise<Response> => {
  const url = getApiUrl(endpoint)
  const config = {
    ...defaultRequestConfig,
    ...options,
    headers: {
      ...defaultRequestConfig.headers,
      ...options.headers,
    },
  }
  
  return fetch(url, config)
}

// Export commonly used URLs for convenience
export const API_URLS = {
  BASE: getApiBaseUrl(),
  EVENTS: getApiUrl(apiConfig.endpoints.events.list),
  AUTH_LOGIN: getApiUrl(apiConfig.endpoints.auth.login),
  AUTH_LOGOUT: getApiUrl(apiConfig.endpoints.auth.logout),
  HEALTH: getApiUrl(apiConfig.endpoints.health),
} as const