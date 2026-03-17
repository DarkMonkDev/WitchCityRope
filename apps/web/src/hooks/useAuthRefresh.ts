import { useEffect } from 'react'
import { apiClient } from '../lib/api/client'
import { useIsAuthenticated } from '../stores/authStore'

/**
 * Hook for automatic token refresh to maintain persistent login sessions.
 *
 * Two refresh strategies work together:
 * 1. PROACTIVE: 13-minute interval refreshes the JWT before it expires (15-min lifetime)
 *    - Only fires when tab is visible (no wasted refreshes for background tabs)
 * 2. REACTIVE: visibilitychange listener refreshes when user returns from sleep/tab switch
 *    - Catches the case where the JWT expired while computer was asleep
 *    - The 401 interceptor in client.ts also handles this, but this is a proactive measure
 *
 * The actual auth persistence is handled by the refresh-token httpOnly cookie.
 * This hook just keeps the short-lived JWT access token fresh.
 */
export function useAuthRefresh() {
  const isAuthenticated = useIsAuthenticated()

  useEffect(() => {
    if (!isAuthenticated) return

    // PROACTIVE: Refresh JWT every 13 minutes (before 15-min expiry)
    const REFRESH_INTERVAL_MS = 13 * 60 * 1000

    const silentRefresh = async () => {
      try {
        await apiClient.post('/api/auth/refresh', null, {
          skipAutoRedirect: true,
          _isRetryAfterRefresh: true, // Prevent the 401 interceptor from retrying this
        } as any)
        console.log('Proactive token refresh successful')
      } catch (error: any) {
        if (error.response?.status === 401) {
          console.log('Token refresh failed - session may have expired')
        } else if (error.response?.status === 400) {
          // CSRF validation failed - this is expected after sleep when CSRF token is stale
          // The 401 interceptor in client.ts will handle it on the next actual API call
          console.log(
            'Token refresh CSRF failed (expected after sleep) - will retry on next API call'
          )
        } else {
          console.warn('Token refresh failed:', error.message)
        }
      }
    }

    // REACTIVE: Refresh when user returns to the tab (wake from sleep, tab switch)
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        silentRefresh()
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)

    // Start proactive refresh interval (only refreshes when tab is visible)
    const intervalId = setInterval(() => {
      if (document.visibilityState === 'visible') {
        silentRefresh()
      }
    }, REFRESH_INTERVAL_MS)

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      clearInterval(intervalId)
    }
  }, [isAuthenticated])
}
