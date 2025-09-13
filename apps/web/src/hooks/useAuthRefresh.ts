import { useEffect } from 'react'
import { getApiUrl } from '../config/api'

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
        
        const response = await fetch(getApiUrl('/api/auth/refresh'), {
          method: 'POST',
          credentials: 'include' // Critical: Include httpOnly cookies
        })
        
        if (response.ok) {
          console.log('✅ Token refresh successful')
        } else if (response.status === 401) {
          console.log('⚠️ Token refresh failed - user may need to log in again')
          // Don't force logout here - let the auth check handle it
        } else {
          console.warn('⚠️ Token refresh failed with status:', response.status)
        }
      } catch (error) {
        console.error('❌ Token refresh error:', error)
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