/**
 * CSRF Token Hook - Standard .NET 9 + React Pattern
 *
 * Backend generates token at /api/antiforgery/token
 * Token stored in XSRF-TOKEN cookie (non-httpOnly, JS can read)
 * This hook reads the cookie and provides it to API client
 *
 * IMPORTANT: Call initializeCSRFProtection() after login
 */

import { apiClient } from '../lib/api/client'

/**
 * Utility function to get CSRF token synchronously from cookie
 * Use this in API interceptors where hooks can't be used
 */
export function getCSRFToken(): string | null {
  // Read XSRF-TOKEN cookie (set by backend)
  const cookies = document.cookie.split(';')
  const csrfCookie = cookies.find(c => c.trim().startsWith('XSRF-TOKEN='))

  if (!csrfCookie) {
    console.warn('⚠️ XSRF-TOKEN cookie not found - call initializeCSRFProtection() after login')
    return null
  }

  const token = csrfCookie.split('=')[1]
  return token
}

/**
 * Hook to read CSRF token from cookie and provide it to components
 * The token is set by the backend in the XSRF-TOKEN cookie
 */
export function useCSRFToken(): string | null {
  return getCSRFToken()
}

/**
 * Initialize CSRF protection by fetching token from backend
 * Call this after successful login
 */
export async function initializeCSRFProtection(): Promise<void> {
  try {
    // Fetch token endpoint - backend will set XSRF-TOKEN cookie
    // Using axios instead of fetch to ensure baseURL is applied correctly
    await apiClient.get('/api/antiforgery/token')

    console.log('✅ CSRF protection initialized')
  } catch (error) {
    console.error('❌ Failed to initialize CSRF protection:', error)
    throw error // Let caller handle error
  }
}
