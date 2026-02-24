import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import { authKeys } from '../utils/cache'
import type { UserDto } from '../types/auth.types'

/**
 * Current user query - integrates with existing auth context
 *
 * NOTE: This is the ONLY hook in this file that's actually used.
 * All other auth operations (login, logout, register, etc.) use either:
 * - TanStack Query hooks from features/auth/api/mutations.ts
 * - AuthContext + authService pattern
 */
export function useCurrentUser(enabled: boolean = true) {
  return useQuery({
    queryKey: authKeys.me(),
    queryFn: async (): Promise<UserDto> => {
      // Try the protected welcome endpoint first (existing pattern)
      // skipAutoRedirect: true prevents the API interceptor from redirecting to /login
      // on 401 responses. This is a "check if logged in" call — 401 means "not logged in"
      // and should be handled locally, not trigger a redirect (which would break pages
      // like the 404 page that are visible to unauthenticated users).
      try {
        const { data } = await apiClient.get<{ user: UserDto }>('/api/protected/welcome', {
          skipAutoRedirect: true,
        })
        return data.user
      } catch {
        // Fallback to dedicated user endpoint
        const { data } = await apiClient.get<UserDto>('/api/auth/user', {
          skipAutoRedirect: true,
        })
        return data
      }
    },
    staleTime: 30 * 60 * 1000, // 30 minutes
    retry: false, // Don't retry auth failures
    enabled,
  })
}
