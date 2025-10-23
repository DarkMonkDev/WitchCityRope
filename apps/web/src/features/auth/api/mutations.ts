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
  returnUrl?: string | null; // Backend-validated return URL (OWASP compliant)
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
 * Get contextual success message based on return URL path
 */
function getSuccessMessage(returnUrl: string | null | undefined): string {
  if (!returnUrl) {
    return 'Welcome back!';
  }

  // Contextual messages based on return URL
  if (returnUrl.includes('/apply/vetting') || returnUrl.includes('/join')) {
    return 'Welcome back! Please complete your application.';
  }
  if (returnUrl.includes('/events/')) {
    return 'Welcome back! You can now register for this event.';
  }
  if (returnUrl.includes('/demo/')) {
    return 'Welcome! Explore all demo features.';
  }

  return 'Welcome back!';
}

/**
 * Login mutation using TanStack Query v5 + Zustand integration
 * Implements post-login return URL feature with backend validation
 * Requirements: /docs/functional-areas/authentication/new-work/2025-10-10-post-login-return/requirements/business-requirements.md
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
      // The API returns { success: true, user: {...}, returnUrl: '...' }
      const userData = data.user

      // Update Zustand store with user data (httpOnly cookies handle auth)
      login(userData)

      // Invalidate any user-related queries (if they exist)
      queryClient.invalidateQueries({ queryKey: ['user'] })
      queryClient.invalidateQueries({ queryKey: ['auth'] })

      // CRITICAL: Prioritize backend-validated returnUrl over query param for security
      // Backend applies OWASP-compliant validation (open redirect prevention)
      const returnUrl = data.returnUrl;

      // Get contextual success message
      const successMessage = getSuccessMessage(returnUrl);
      console.log('✅ Login successful:', successMessage);

      if (returnUrl) {
        // Backend-validated URL is guaranteed to be safe - redirect immediately
        console.log('📍 Redirecting to validated return URL:', returnUrl);
        navigate(returnUrl, {
          replace: true,
          state: { message: successMessage }
        });
      } else {
        // No return URL provided or validation failed - default to dashboard
        console.log('📍 Redirecting to dashboard (default)');
        navigate('/dashboard', {
          replace: true,
          state: { message: successMessage }
        });
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