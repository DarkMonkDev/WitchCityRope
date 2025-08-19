import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../../../api/client'
import { useAuthActions } from '../../../stores/authStore'
import type { 
  UserDto, 
  LoginRequest, 
  LoginResponse
} from '@witchcityrope/shared-types'

// Registration credentials interface - not generated yet, create locally
export interface RegisterCredentials {
  email: string
  password: string
  sceneName: string
}

/**
 * Login mutation using TanStack Query v5 + Zustand integration
 * Follows pattern from: /docs/functional-areas/api-integration-validation/requirements/functional-specification-v2.md
 * Section: "Mutation Pattern"
 */
export function useLogin() {
  const queryClient = useQueryClient()
  const { login } = useAuthActions()
  const navigate = useNavigate()
  
  return useMutation({
    mutationFn: async (credentials: LoginRequest): Promise<LoginResponse> => {
      const response = await api.post('/api/auth/login', credentials)
      return response.data
    },
    onSuccess: (response, variables, context) => {
      // Handle nested response structure from API
      // The API returns { success: true, user: { ...user }, message: "..." }
      const userData = response.user
      
      // Update Zustand store with user data
      login(userData)
      
      // Invalidate any user-related queries (if they exist)
      queryClient.invalidateQueries({ queryKey: ['user'] })
      queryClient.invalidateQueries({ queryKey: ['auth'] })
      
      // Navigate to dashboard or returnTo URL
      const urlParams = new URLSearchParams(window.location.search)
      const returnTo = urlParams.get('returnTo') || '/dashboard'
      navigate(returnTo, { replace: true })
    },
    onError: (error) => {
      console.error('Login failed:', error)
      // Error handling is managed by the component
    },
    // Don't retry failed login attempts automatically
    retry: false,
  })
}

/**
 * Registration mutation using TanStack Query v5 + Zustand integration
 * Connects to the working API endpoint from vertical slice
 */
export function useRegister() {
  const queryClient = useQueryClient()
  const { login } = useAuthActions()
  const navigate = useNavigate()
  
  return useMutation({
    mutationFn: async (credentials: RegisterCredentials): Promise<LoginResponse> => {
      const response = await api.post('/api/auth/register', credentials)
      return response.data
    },
    onSuccess: (response) => {
      // Handle nested response structure from API
      const userData = response.user
      
      // Update Zustand store with user data
      login(userData)
      
      // Invalidate any user-related queries
      queryClient.invalidateQueries({ queryKey: ['user'] })
      queryClient.invalidateQueries({ queryKey: ['auth'] })
      
      // Navigate to dashboard after registration
      navigate('/dashboard', { replace: true })
    },
    onError: (error) => {
      console.error('Registration failed:', error)
      // Error handling is managed by the component
    },
    // Don't retry failed registration attempts automatically
    retry: false,
  })
}

/**
 * Logout mutation using TanStack Query v5 + Zustand integration
 */
export function useLogout() {
  const queryClient = useQueryClient()
  const { logout } = useAuthActions()
  const navigate = useNavigate()
  
  return useMutation({
    mutationFn: async (): Promise<void> => {
      await api.post('/api/auth/logout')
    },
    onSuccess: () => {
      // Update Zustand store (clear auth state)
      logout()
      
      // Clear all cached queries on logout
      queryClient.clear()
      
      // Navigate to login page
      navigate('/login', { replace: true })
    },
    onError: (error) => {
      console.error('Logout failed:', error)
      // Even if logout fails, clear local state and redirect
      logout()
      queryClient.clear()
      navigate('/login', { replace: true })
    },
    // Don't retry logout attempts
    retry: false,
  })
}