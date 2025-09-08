import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../../../api/client'
import { useAuthActions } from '../../../stores/authStore'
import type { 
  UserDto, 
  LoginRequest, 
  LoginResponse
} from '@witchcityrope/shared-types'

// Extended LoginResponse with JWT token fields (until NSwag is updated)
interface LoginResponseWithToken {
  token: string;
  expiresAt: string;
  user: UserDto;
}

// API Response wrapper
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
}

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
    mutationFn: async (credentials: LoginRequest): Promise<LoginResponseWithToken> => {
      const response = await api.post('/api/Auth/login', credentials)
      return response.data
    },
    onSuccess: (data, variables, context) => {
      // Handle flat response structure from API
      // The API returns { token: '...', expiresAt: '...', user: {...}, refreshToken: '...' }
      const userData = data.user
      const token = data.token
      const expiresAt = new Date(data.expiresAt)
      
      // Update Zustand store with user data and JWT token
      login(userData, token, expiresAt)
      
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
    mutationFn: async (credentials: RegisterCredentials): Promise<UserDto> => {
      const response = await api.post('/api/Auth/register', credentials)
      return response.data
    },
    onSuccess: (userData) => {
      // Handle flat response structure from API
      // Registration doesn't return JWT token - user needs to login
      // Just navigate to login page with success message
      
      // Navigate to login page after successful registration
      navigate('/login?message=Registration successful. Please log in.', { replace: true })
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
    mutationFn: async (_?: void): Promise<void> => {
      await api.post('/api/Auth/logout')
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