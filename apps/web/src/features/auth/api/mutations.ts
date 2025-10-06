import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../../../api/client'
import { useAuthActions } from '../../../stores/authStore'
import type { 
  UserDto, 
  LoginRequest, 
  LoginResponse
} from '@witchcityrope/shared-types'

// API Response for httpOnly cookie authentication (no tokens)
interface LoginResponseData {
  success: boolean;
  user: UserDto;
  message?: string;
}

// Registration credentials interface - not generated yet, create locally
export interface RegisterCredentials {
  email: string
  password: string
  sceneName: string
}

/**
 * Helper function to extract user-friendly error messages from API errors
 */
function getErrorMessage(error: any): string {
  // Handle Axios errors
  if (error.response) {
    const status = error.response.status
    const message = error.response.data?.message || error.response.data?.error

    // Map status codes to user-friendly messages
    switch (status) {
      case 401:
        return 'The email or password is incorrect. Please try again.'
      case 429:
        return 'Too many login attempts. Please wait a few minutes and try again.'
      case 500:
      case 502:
      case 503:
        return 'An error occurred while processing your request. Please try again later.'
      default:
        // Use server message if available, otherwise generic error
        return message || 'An error occurred. Please try again.'
    }
  }

  // Handle network errors
  if (error.code === 'ECONNABORTED' || error.code === 'ERR_NETWORK' || !error.response) {
    return 'Unable to connect to the server. Please check your internet connection and try again.'
  }

  // Generic fallback
  return error.message || 'An error occurred. Please try again.'
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
    mutationFn: async (credentials: LoginRequest): Promise<LoginResponseData> => {
      try {
        const response = await api.post('/api/auth/login', credentials)
        return response.data
      } catch (error: any) {
        // Enhance error with user-friendly message
        const userFriendlyMessage = getErrorMessage(error)
        const enhancedError = new Error(userFriendlyMessage)
        // Preserve original error for debugging
        console.error('Login error:', error)
        throw enhancedError
      }
    },
    onSuccess: (data, variables, context) => {
      // Handle httpOnly cookie authentication - no tokens in response
      // The API returns { success: true, user: {...}, message: '...' }
      const userData = data.user
      
      // Update Zustand store with user data (httpOnly cookies handle auth)
      login(userData)
      
      // Invalidate any user-related queries (if they exist)
      queryClient.invalidateQueries({ queryKey: ['user'] })
      queryClient.invalidateQueries({ queryKey: ['auth'] })
      
      // Navigate to returnTo URL from query params or dashboard
      const urlParams = new URLSearchParams(window.location.search)
      const returnTo = urlParams.get('returnTo')

      if (returnTo) {
        // Decode the return URL and navigate there
        navigate(decodeURIComponent(returnTo), { replace: true })
      } else {
        // Default to dashboard if no returnTo specified
        navigate('/dashboard', { replace: true })
      }
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
      try {
        const response = await api.post('/api/auth/register', credentials)
        return response.data
      } catch (error: any) {
        // Enhance error with user-friendly message
        const userFriendlyMessage = getErrorMessage(error)
        const enhancedError = new Error(userFriendlyMessage)
        // Preserve original error for debugging
        console.error('Registration error:', error)
        throw enhancedError
      }
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