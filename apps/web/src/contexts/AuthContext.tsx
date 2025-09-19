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

  const logout = useCallback(async () => {
    try {
      setIsLoading(true)
      // Clear user state first to update UI immediately
      setUser(null)
      setError(null)

      // CRITICAL FIX: Clear Zustand store persistence
      sessionStorage.removeItem('auth-store')

      // Then call logout API to clear the cookie
      await authService.logout()

      // Force a page reload to ensure all state is cleared
      // This also ensures the cookie deletion takes effect
      window.location.href = '/login'
    } catch (err) {
      console.error('Logout error:', err)
      // Even if logout fails, ensure we're logged out locally
      setUser(null)
      setError(null)

      // CRITICAL FIX: Clear Zustand store persistence even on error
      sessionStorage.removeItem('auth-store')

      // Still redirect to login page
      window.location.href = '/login'
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
        }
      } catch (error) {
        // User is not authenticated, which is fine
        console.log('Failed to restore authentication:', error)
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
