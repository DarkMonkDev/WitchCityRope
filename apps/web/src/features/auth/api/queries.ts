import { useQuery } from '@tanstack/react-query'
import { api } from '../../../api/client'
import type { UserDto } from '@witchcityrope/shared-types'

// Response interfaces - temporary until full API coverage
interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
}

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
      const response = await api.get<ApiResponse<UserDto>>('/api/auth/user')
      return response.data.data
    },
    staleTime: 5 * 60 * 1000, // Consider data stale after 5 minutes
    gcTime: 10 * 60 * 1000, // Keep in cache for 10 minutes (v5 uses gcTime instead of cacheTime)
    retry: (failureCount, error: any) => {
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
      const response = await api.get<ApiResponse<ProtectedWelcomeData>>('/api/protected/welcome')
      return response.data.data
    },
    staleTime: 30 * 1000, // Consider data stale after 30 seconds
    gcTime: 60 * 1000, // Keep in cache for 1 minute
    retry: (failureCount, error: any) => {
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