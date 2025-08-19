import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../../../api/client'
import { useAuthActions } from '../../../stores/authStore'
import type { User } from '../../../stores/authStore'

// Login credentials interface
export interface LoginCredentials {
  email: string
  password: string
}

// Login response interface
export interface LoginResponse {
  user: User
  message?: string
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
    mutationFn: async (credentials: LoginCredentials): Promise<LoginResponse> => {
      const response = await api.post('/api/auth/login', credentials)
      return response.data
    },
    onSuccess: (data, variables, context) => {
      // Update Zustand store with user data
      login(data.user)
      
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