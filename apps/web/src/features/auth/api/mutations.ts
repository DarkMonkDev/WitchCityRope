import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../../../lib/api/client'
import { useAuthActions } from '../../../stores/authStore'
import { initializeCSRFProtection, getCSRFToken } from '../../../hooks/useCSRFToken'
import type { UserDto, LoginRequest, RegisterRequest } from '@witchcityrope/shared-types'

// API Response for httpOnly cookie authentication (no tokens)
interface LoginResponseData {
  success: boolean;
  user: UserDto;
  message?: string;
  returnUrl?: string | null; // Backend-validated return URL (OWASP compliant)
}

// Type alias for backwards compatibility
export type RegisterCredentials = RegisterRequest

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
    // apiClient interceptor extracts RFC 9457 error message to error.message automatically
    mutationFn: async (credentials: LoginRequest): Promise<LoginResponseData> => {
      const response = await apiClient.post('/api/auth/login', credentials)
      return response.data
    },
    onSuccess: async (data: LoginResponseData, _variables: LoginRequest, _context: unknown) => {
      // Handle httpOnly cookie authentication - no tokens in response
      // The API returns { success: true, user: {...}, returnUrl: '...' }
      const userData = data.user

      // Update Zustand store with user data (httpOnly cookies handle auth)
      login(userData)

      // Initialize CSRF protection after successful login
      try {
        await initializeCSRFProtection()
      } catch (error) {
        console.error('CSRF initialization failed:', error)
        // Don't block login flow, but log for monitoring
      }

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
    onError: (error: Error) => {
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
  return useMutation({
    // apiClient interceptor extracts RFC 9457 error message to error.message automatically
    mutationFn: async (credentials: RegisterCredentials): Promise<UserDto> => {
      const response = await apiClient.post('/api/auth/register', credentials)
      return response.data
    },
    onSuccess: () => {
      // Don't navigate away - let RegisterPage show its built-in success screen
      // which tells the user to check their email for a verification link.
      // The RegisterPage checks registerMutation.isSuccess && registeredEmail
      // to conditionally render the "Registration Successful" view.
    },
    onError: (error: Error) => {
      console.error('Registration failed:', error)
      // Error handling is managed by the component
    },
    // Don't retry failed registration attempts automatically
    retry: false,
  })
}

/**
 * Logout mutation using TanStack Query v5 + Zustand integration
 *
 * STANDARD PATTERN: This is the official authentication pattern for WitchCityRope.
 * ALL authentication operations use TanStack Query mutations + Zustand for global state.
 *
 * Implementation:
 * - Uses TanStack Query mutation for server operation (logout API call)
 * - Handles CSRF token validation with automatic retry
 * - Clears Zustand auth store on success
 * - Clears React Query cache with queryClient.clear() (NOT invalidateQueries)
 * - Navigates to home page
 *
 * CRITICAL: Uses queryClient.clear() to prevent refetch after logout.
 * Using invalidateQueries() would trigger queries while user is logged out (bug).
 *
 * See: /docs/standards-processes/frontend/authentication-pattern-guide.md
 */
export function useLogout() {
  const queryClient = useQueryClient()
  const { logout } = useAuthActions()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: async (_?: void): Promise<void> => {
      // Check if CSRF token exists before attempting logout
      let csrfToken = getCSRFToken()

      if (!csrfToken) {
        console.warn('⚠️ No CSRF token found, fetching fresh token...')
        try {
          await initializeCSRFProtection()
          csrfToken = getCSRFToken()
        } catch (error) {
          console.error('❌ Failed to fetch CSRF token for logout:', error)
          throw new Error('Unable to log out. Please refresh the page and try again.')
        }
      }

      // Attempt logout with CSRF token
      try {
        await apiClient.post('/api/auth/logout')
      } catch (error: any) {
        // If CSRF validation failed, try once more with fresh token
        if (error.response?.status === 400 &&
            error.response?.data?.title === 'CSRF Validation Failed') {
          console.warn('⚠️ CSRF validation failed, retrying with fresh token...')
          await initializeCSRFProtection()
          await apiClient.post('/api/auth/logout') // Retry once
        } else {
          throw error // Re-throw other errors
        }
      }
    },
    onSuccess: () => {
      // 1. Clear Zustand auth store
      logout()

      // 2. Clear sessionStorage (Zustand persistence)
      sessionStorage.removeItem('auth-store')

      // 3. Clear React Query cache (CRITICAL: use clear() not invalidateQueries())
      // This prevents queries from refetching after user is logged out
      queryClient.clear()

      // 4. Navigate to home page
      navigate('/', { replace: true })

      console.log('✅ Logout successful')
    },
    onError: (error: Error) => {
      console.error('❌ Logout failed after retry:', error)
      // CRITICAL: Still clear local state even on error
      // Backend logout may have succeeded even if we got an error response
      logout()
      sessionStorage.removeItem('auth-store')
      queryClient.clear()
      navigate('/', { replace: true })
    },
    // Don't retry logout attempts (we handle retry manually in mutationFn)
    retry: false,
  })
}

/**
 * Email verification mutation
 * Verifies user's email address with userId and token from verification link
 */
export function useVerifyEmail() {
  return useMutation({
    // apiClient interceptor extracts RFC 9457 error message to error.message automatically
    mutationFn: async ({ userId, token }: { userId: string; token: string }): Promise<void> => {
      const response = await apiClient.post('/api/auth/verify-email', { userId, token })
      return response.data
    },
    retry: false,
  })
}

/**
 * Resend verification email mutation
 * Sends a new verification email to the user
 */
export function useResendVerification() {
  return useMutation({
    // apiClient interceptor extracts RFC 9457 error message to error.message automatically
    mutationFn: async ({ email }: { email: string }): Promise<void> => {
      const response = await apiClient.post('/api/auth/resend-verification', { email })
      return response.data
    },
    retry: false,
  })
}

/**
 * Forgot password mutation - Phase 3: Password Reset
 * Sends password reset email to the user
 * Always returns success to prevent email enumeration
 */
export function useForgotPassword() {
  return useMutation({
    // apiClient interceptor extracts RFC 9457 error message to error.message automatically
    mutationFn: async ({ email }: { email: string }): Promise<void> => {
      const response = await apiClient.post('/api/auth/forgot-password', { email })
      return response.data
    },
    retry: false,
  })
}

/**
 * Reset password mutation - Phase 3: Password Reset
 * Resets user password using token from email link
 */
export function useResetPassword() {
  return useMutation({
    // apiClient interceptor extracts RFC 9457 error message to error.message automatically
    mutationFn: async ({ userId, token, newPassword }: { userId: string; token: string; newPassword: string }): Promise<void> => {
      const response = await apiClient.post('/api/auth/reset-password', { userId, token, newPassword })
      return response.data
    },
    retry: false,
  })
}