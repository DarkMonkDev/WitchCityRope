import React, { createContext, useContext, useState, useCallback, ReactNode, useEffect } from 'react'
import {
  authService,
  User,
  LoginCredentials,
  RegisterCredentials,
  ProtectedWelcomeResponse,
} from '../services/authService'

export interface AuthContextType {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
  login: (credentials: LoginCredentials) => Promise<void>
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
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const isAuthenticated = !!user

  const clearError = useCallback(() => {
    setError(null)
  }, [])

  const login = useCallback(async (credentials: LoginCredentials) => {
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
      await authService.logout()
      setUser(null)
      setError(null)
    } catch (err) {
      console.error('Logout error:', err)
      // Even if logout fails, clear local state
      setUser(null)
      setError(null)
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
