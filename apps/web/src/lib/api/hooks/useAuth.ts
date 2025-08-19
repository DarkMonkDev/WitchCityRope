import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../client'
import { authKeys, cacheUtils } from '../utils/cache'
import type { 
  LoginCredentials, 
  RegisterCredentials, 
  LoginResponse, 
  UserDto 
} from '../types/auth.types'
import type { ApiResponse } from '../types/api.types'

// Current user query - integrates with existing auth context
export function useCurrentUser(enabled: boolean = true) {
  return useQuery({
    queryKey: authKeys.me(),
    queryFn: async (): Promise<UserDto> => {
      // Try the protected welcome endpoint first (existing pattern)
      try {
        const { data } = await apiClient.get<ApiResponse<UserDto>>('/api/protected/welcome')
        return data.data || (data as any).user // Handle both response formats
      } catch (error) {
        // Fallback to dedicated user endpoint
        const { data } = await apiClient.get<ApiResponse<UserDto>>('/api/auth/me')
        if (!data.data) throw new Error('User not found')
        return data.data
      }
    },
    staleTime: 30 * 60 * 1000, // 30 minutes
    retry: false, // Don't retry auth failures
    enabled,
  })
}

// Login mutation - integrates with existing auth service
export function useLogin() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (credentials: LoginCredentials): Promise<LoginResponse> => {
      const { data } = await apiClient.post<ApiResponse<LoginResponse>>('/api/auth/login', credentials)
      if (!data.data) throw new Error('Login failed')
      return data.data
    },
    onSuccess: (loginResponse) => {
      // Store token for validation (in production, use httpOnly cookies)
      localStorage.setItem('auth_token', loginResponse.token)
      
      // Cache user data
      queryClient.setQueryData(authKeys.me(), loginResponse.user)
      
      console.log('Login successful:', loginResponse.user.sceneName)
    },
    onError: (error) => {
      console.error('Login failed:', error)
      localStorage.removeItem('auth_token')
    },
  })
}

// Register mutation
export function useRegister() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (credentials: RegisterCredentials): Promise<LoginResponse> => {
      const { data } = await apiClient.post<ApiResponse<LoginResponse>>('/api/auth/register', credentials)
      if (!data.data) throw new Error('Registration failed')
      return data.data
    },
    onSuccess: (registerResponse) => {
      // Store token for validation
      localStorage.setItem('auth_token', registerResponse.token)
      
      // Cache user data
      queryClient.setQueryData(authKeys.me(), registerResponse.user)
      
      console.log('Registration successful:', registerResponse.user.sceneName)
    },
    onError: (error) => {
      console.error('Registration failed:', error)
    },
  })
}

// Logout mutation
export function useLogout() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (): Promise<void> => {
      try {
        await apiClient.post('/api/auth/logout')
      } catch (error) {
        // Don't fail logout for API errors
        console.error('Logout API call failed:', error)
      }
    },
    onSuccess: () => {
      // Always clear local state regardless of API response
      localStorage.removeItem('auth_token')
      cacheUtils.clearAuth(queryClient)
      console.log('Logout successful')
    },
    onError: (error) => {
      // Still clear local state on error
      localStorage.removeItem('auth_token')
      cacheUtils.clearAuth(queryClient)
      console.error('Logout error, but clearing local state:', error)
    },
  })
}

// Refresh token mutation (for future use)
export function useRefreshToken() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (): Promise<LoginResponse> => {
      const { data } = await apiClient.post<ApiResponse<LoginResponse>>('/api/auth/refresh')
      if (!data.data) throw new Error('Token refresh failed')
      return data.data
    },
    onSuccess: (refreshResponse) => {
      localStorage.setItem('auth_token', refreshResponse.token)
      queryClient.setQueryData(authKeys.me(), refreshResponse.user)
      console.log('Token refreshed successfully')
    },
    onError: (error) => {
      console.error('Token refresh failed:', error)
      // Redirect to login on refresh failure
      localStorage.removeItem('auth_token')
      cacheUtils.clearAuth(queryClient)
      window.location.href = '/login'
    },
  })
}

// Update profile mutation
export function useUpdateProfile() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (profileData: Partial<UserDto>): Promise<UserDto> => {
      const { data } = await apiClient.put<ApiResponse<UserDto>>('/api/auth/profile', profileData)
      if (!data.data) throw new Error('Profile update failed')
      return data.data
    },
    onMutate: async (updatedProfile) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: authKeys.me() })
      
      // Snapshot previous value
      const previousUser = queryClient.getQueryData<UserDto>(authKeys.me())
      
      // Optimistically update profile
      queryClient.setQueryData(authKeys.me(), (old: UserDto | undefined) => {
        if (!old) return old
        return { ...old, ...updatedProfile }
      })
      
      return { previousUser }
    },
    onError: (err, _updatedProfile, context) => {
      // Rollback on error
      if (context?.previousUser) {
        queryClient.setQueryData(authKeys.me(), context.previousUser)
      }
      console.error('Profile update failed, rolling back:', err)
    },
    onSettled: () => {
      // Always refetch after error or success
      queryClient.invalidateQueries({ queryKey: authKeys.me() })
    },
  })
}

// Check authentication status
export function useAuthStatus() {
  return useQuery({
    queryKey: [...authKeys.all, 'status'],
    queryFn: async (): Promise<boolean> => {
      try {
        await apiClient.get('/api/auth/status')
        return true
      } catch {
        return false
      }
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: false,
  })
}