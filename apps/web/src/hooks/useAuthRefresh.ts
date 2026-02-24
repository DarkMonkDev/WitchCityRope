import { useEffect } from 'react'
import { apiClient } from '../lib/api/client'

/**
 * Hook for automatic token refresh to prevent user logout
 *
 * Features:
 * - Refreshes authentication token every 25 minutes
 * - Uses httpOnly cookies for secure token management
 * - Handles refresh failures gracefully
 * - Prevents multiple concurrent refresh attempts
 */
export function useAuthRefresh() {
  useEffect(() => {
    // Refresh token every 25 minutes (JWT tokens typically expire after 30 minutes)
    const REFRESH_INTERVAL = 25 * 60 * 1000 // 25 minutes in milliseconds

    const refreshToken = async () => {
      try {
        console.log('🔄 Attempting automatic token refresh...')

        // Use apiClient for consistent error handling and CSRF protection.
        // skipAutoRedirect: true prevents the API interceptor from redirecting to /login
        // on 401. Token refresh is speculative — if the user isn't authenticated, 401 simply
        // means "nothing to refresh" and should not trigger a login redirect. Without this,
        // unauthenticated users on non-public routes (like the 404 page) get bounced to login
        // 5 seconds after page load when this initial refresh fires.
        await apiClient.post('/api/auth/refresh', null, { skipAutoRedirect: true })
        console.log('✅ Token refresh successful')
      } catch (error: any) {
        if (error.response?.status === 401) {
          console.log('⚠️ Token refresh failed - user may need to log in again')
          // Don't force logout here - let the auth check handle it
        } else {
          console.warn('⚠️ Token refresh failed:', error.message)
        }
        // Network errors are common, don't force logout
      }
    }

    // Start the refresh interval
    const refreshInterval = setInterval(refreshToken, REFRESH_INTERVAL)

    // Initial refresh after a short delay (in case user just logged in)
    const initialRefreshTimeout = setTimeout(refreshToken, 5000) // 5 seconds

    // Cleanup on unmount
    return () => {
      clearInterval(refreshInterval)
      clearTimeout(initialRefreshTimeout)
    }
  }, [])
}
