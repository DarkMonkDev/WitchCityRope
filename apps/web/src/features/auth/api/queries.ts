import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../../lib/api/client'
import type { UserDto } from '@witchcityrope/shared-types'

interface ProtectedWelcomeData {
  message: string
  user: UserDto
  serverTime: string
}

/**
 * Query to fetch current authenticated user
 * Connects to /api/auth/user endpoint (requires JWT token)
 */
export function useCurrentUser() {
  return useQuery<UserDto>({
    queryKey: ['auth', 'user'],
    queryFn: async (): Promise<UserDto> => {
      // skipAutoRedirect: true prevents the API interceptor from redirecting to /login
      // on 401. This is a "check if logged in" query — 401 simply means "not authenticated"
      // and should be handled by TanStack Query's error state, not a page redirect.
      // Without this, unauthenticated users on non-public routes (like the 404 page)
      // get incorrectly bounced to the login page.
      const response = await apiClient.get<UserDto>('/api/auth/user', {
        skipAutoRedirect: true,
      })
      return response.data  // API returns UserDto directly, not wrapped in ApiResponse
    },
    staleTime: 5 * 60 * 1000, // Consider data stale after 5 minutes
    gcTime: 10 * 60 * 1000, // Keep in cache for 10 minutes (v5 uses gcTime instead of cacheTime)
    retry: (failureCount: number, error: any) => {
      // Don't retry on 401 (unauthorized) errors
      if (error?.response?.status === 401) return false
      return failureCount < 3
    },
  })
}

/**
 * Query to fetch protected welcome message
 * This endpoint requires JWT authentication
 */
export function useProtectedWelcome() {
  return useQuery<ProtectedWelcomeData>({
    queryKey: ['protected', 'welcome'],
    queryFn: async (): Promise<ProtectedWelcomeData> => {
      // skipAutoRedirect: true — same pattern as useCurrentUser above.
      // Auth check queries should never trigger login redirects.
      const response = await apiClient.get<ProtectedWelcomeData>('/api/protected/welcome', {
        skipAutoRedirect: true,
      })
      return response.data
    },
    staleTime: 30 * 1000, // Consider data stale after 30 seconds
    gcTime: 60 * 1000, // Keep in cache for 1 minute
    retry: (failureCount: number, error: any) => {
      // Don't retry on auth errors
      if (error?.response?.status === 401 || error?.response?.status === 403) {
        return false
      }
      return failureCount < 2
    },
  })
}

/**
 * Query key factory for auth-related queries
 * Following the pattern from functional specification
 */
export const authKeys = {
  all: ['auth'] as const,
  user: () => [...authKeys.all, 'user'] as const,
  protected: () => ['protected'] as const,
  welcome: () => [...authKeys.protected(), 'welcome'] as const,
}