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
      try {
        const { data } = await apiClient.get<{ user: UserDto }>('/api/protected/welcome')
        return data.user
      } catch (error) {
        // Fallback to dedicated user endpoint
        const { data } = await apiClient.get<UserDto>('/api/auth/user')
        return data
      }
    },
    staleTime: 30 * 60 * 1000, // 30 minutes
    retry: false, // Don't retry auth failures
    enabled,
  })
}
