/**
 * AUTHENTICATION ARCHITECTURE DOCUMENTATION
 *
 * ⚠️ ⚠️ ⚠️ CRITICAL - DO NOT CHANGE WITHOUT EXPLICIT DIRECTION ⚠️ ⚠️ ⚠️
 *
 * THIS AUTHENTICATION PATTERN HAS BEEN THOROUGHLY TESTED AND WORKS CORRECTLY.
 * THE LOGOUT PERSISTENCE BUG HAS BEEN FIXED. DO NOT MODIFY THIS CODE
 * UNLESS SPECIFICALLY DIRECTED TO DO SO.
 *
 * TESTED AND VERIFIED ON: 2025-09-19
 *
 * ========================================================================
 *
 * This app uses a dual-store authentication pattern:
 *
 * 1. AuthContext (this file):
 *    - Provides authentication functions (login, logout, register)
 *    - Manages authentication flow and API calls
 *    - Ensures proper cleanup of ALL auth state on logout
 *    - Wrapped around entire app in main.tsx
 *
 * 2. Zustand Store (authStore.ts):
 *    - Provides global auth state (user, isAuthenticated)
 *    - Persists to sessionStorage for page refresh handling
 *    - Used by most components for reading auth state
 *
 * WHY TWO STORES?
 * - Zustand provides better performance for frequent state reads
 * - AuthContext provides centralized auth logic and cleanup
 * - This pattern ensures logout properly clears all state
 *
 * CRITICAL LOGOUT FLOW (DO NOT CHANGE):
 * 1. Component calls AuthContext logout (via useAuth hook)
 * 2. AuthContext clears both its own state AND Zustand store
 * 3. SessionStorage is cleared to prevent rehydration
 * 4. API is called to clear httpOnly cookie
 * 5. User is redirected to home page (/)
 *
 * This ensures logout persists across page refreshes.
 *
 * USAGE:
 * - For reading auth state: use Zustand hooks (useUser, useIsAuthenticated)
 * - For auth actions: use AuthContext (useAuth().login/logout/register)
 */

import React, { createContext, useContext, useState, useCallback, ReactNode, useEffect } from 'react'
import type { UserDto, LoginRequest } from '@witchcityrope/shared-types'
import {
  authService,
  RegisterCredentials,
  ProtectedWelcomeResponse,
} from '../services/authService'

export interface AuthContextType {
  user: UserDto | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
  login: (credentials: LoginRequest) => Promise<void>
  register: (credentials: RegisterCredentials) => Promise<void>
  logout: () => Promise<void>
  clearError: () => void
  getProtectedWelcome: () => Promise<ProtectedWelcomeResponse>
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined)

interface AuthProviderProps {
  children: ReactNode
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<UserDto | null>(null)
  const [isLoading, setIsLoading] = useState(true) // Start as loading to check initial auth state
  const [error, setError] = useState<string | null>(null)
  const [isInitialized, setIsInitialized] = useState(false)

  const isAuthenticated = !!user

  const clearError = useCallback(() => {
    setError(null)
  }, [])

  const login = useCallback(async (credentials: LoginRequest) => {
    try {
      setIsLoading(true)
      setError(null)
      const response = await authService.login(credentials)
      setUser(response.user)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Login failed'
      setError(errorMessage)
      throw err // Re-throw for component error handling
    } finally {
      setIsLoading(false)
    }
  }, [])

  const register = useCallback(async (credentials: RegisterCredentials) => {
    try {
      setIsLoading(true)
      setError(null)
      const response = await authService.register(credentials)
      setUser(response.user)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Registration failed'
      setError(errorMessage)
      throw err // Re-throw for component error handling
    } finally {
      setIsLoading(false)
    }
  }, [])

  /**
   * Logout function that properly clears all authentication state
   *
   * ⚠️ DO NOT CHANGE THIS FUNCTION WITHOUT EXPLICIT DIRECTION ⚠️
   * This implementation has been tested and verified to fix the logout persistence bug.
   *
   * This function performs a complete logout by:
   * 1. Clearing the local React state (Context)
   * 2. Clearing the Zustand store state
   * 3. Removing the auth-store from sessionStorage
   * 4. Calling the API to clear the httpOnly cookie
   * 5. Redirecting to the home page (/)
   *
   * This ensures that the logout persists even after page refresh
   */
  const logout = useCallback(async () => {
    try {
      setIsLoading(true)

      // Step 1: Clear React Context state immediately for UI update
      setUser(null)
      setError(null)

      // Step 2: Clear Zustand store state
      // We need to import and clear the Zustand store to ensure
      // all components using the store directly are updated
      const { useAuthStore } = await import('../stores/authStore')
      useAuthStore.getState().actions.logout()

      // Step 3: Clear sessionStorage to prevent rehydration on refresh
      // The Zustand store persists to sessionStorage, so we must clear it
      sessionStorage.removeItem('auth-store')

      // Step 4: Call logout API to clear the httpOnly cookie on the server
      await authService.logout()

      // Step 5: Force redirect to home page (not login page)
      // Using window.location.href ensures a full page reload
      window.location.href = '/'
    } catch (err) {
      console.error('Logout error:', err)

      // Even if the API call fails, ensure local state is cleared
      setUser(null)
      setError(null)

      // Try to clear Zustand store even on error
      try {
        const { useAuthStore } = await import('../stores/authStore')
        useAuthStore.getState().actions.logout()
      } catch {
        // If dynamic import fails, continue with cleanup
      }

      // Always clear sessionStorage on error
      sessionStorage.removeItem('auth-store')

      // Always redirect to home page on error (not login page)
      window.location.href = '/'
    } finally {
      setIsLoading(false)
    }
  }, [])

  const getProtectedWelcome = useCallback(async (): Promise<ProtectedWelcomeResponse> => {
    try {
      setError(null)
      return await authService.getProtectedWelcome()
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch protected data'
      setError(errorMessage)
      if (errorMessage === 'Unauthorized') {
        // Clear user state if unauthorized
        setUser(null)
      }
      throw err
    }
  }, [])

  // Initialize authentication state on app startup
  useEffect(() => {
    const initializeAuth = async () => {
      if (isInitialized) return

      try {
        setIsLoading(true)
        // Try to restore user from httpOnly cookie
        const user = await authService.getCurrentUser()
        if (user) {
          setUser(user)
          console.log('Restored authentication from httpOnly cookie')
        } else {
          console.log('No valid authentication found')
          // CRITICAL FIX: Clear auth store if API says no auth
          const { useAuthStore } = await import('../stores/authStore')
          const authStore = useAuthStore.getState()
          if (authStore.isAuthenticated) {
            console.log('Clearing stale auth store - API returned no user')
            authStore.actions.logout()
            sessionStorage.removeItem('auth-store')
          }
        }
      } catch (error) {
        // User is not authenticated, which is fine
        console.log('Failed to restore authentication:', error)
        // Clear auth store on error too
        try {
          const { useAuthStore } = await import('../stores/authStore')
          useAuthStore.getState().actions.logout()
          sessionStorage.removeItem('auth-store')
        } catch {
          // Ignore import errors
        }
      } finally {
        setIsLoading(false)
        setIsInitialized(true)
      }
    }

    initializeAuth()
  }, [isInitialized])

  // Clear error on user state changes
  useEffect(() => {
    if (user) {
      setError(null)
    }
  }, [user])

  const value: AuthContextType = {
    user,
    isAuthenticated,
    isLoading,
    error,
    login,
    register,
    logout,
    clearError,
    getProtectedWelcome,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// Custom hook to use the auth context
export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
