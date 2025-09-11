/**
 * API Configuration - Centralized API Base URL Management
 * 
 * This module provides a centralized way to access the API base URL
 * using environment variables, preventing hard-coded port issues.
 */

/**
 * Get the current API base URL from environment variables
 * Falls back to development default if not configured
 */
export const getApiBaseUrl = (): string => {
  return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655'
}

/**
 * Get the API URL for a specific endpoint
 * @param endpoint - The API endpoint path (e.g., '/api/events', '/api/auth/login')
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
      login: '/api/v1/auth/login',
      register: '/api/v1/auth/register',
      logout: '/api/v1/auth/logout',
      refresh: '/api/v1/auth/refresh',
      user: '/api/auth/user',
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