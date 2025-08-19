import { useQuery } from '@tanstack/react-query'
import { api } from '../../../api/client'
import type { User } from '../../../stores/authStore'

// Response interfaces matching API structure from vertical slice
interface UserResponse {
  success: boolean
  data: User
}

interface ProtectedWelcomeResponse {
  success: boolean
  data: {
    message: string
    user: User
    serverTime: string
  }
}

/**
 * Query to fetch current authenticated user
 * Connects to /api/auth/user endpoint from vertical slice
 */
export function useCurrentUser() {
  return useQuery({
    queryKey: ['auth', 'user'],
    queryFn: async (): Promise<User> => {
      const response = await api.get<UserResponse>('/api/auth/user')
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
 * This endpoint requires JWT authentication via the Web Service proxy
 */
export function useProtectedWelcome() {
  return useQuery({
    queryKey: ['protected', 'welcome'],
    queryFn: async (): Promise<ProtectedWelcomeResponse['data']> => {
      const response = await api.get<ProtectedWelcomeResponse>('/api/protected/welcome')
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