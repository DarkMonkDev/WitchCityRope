import type { 
  UserDto, 
  LoginRequest, 
  LoginResponse 
} from '@witchcityrope/shared-types'
import { apiConfig, apiRequest, getApiUrl } from '../config/api'

// Local types for services not covered by generated types yet
export interface RegisterCredentials {
  email: string
  password: string
  sceneName: string
}

export interface AuthResponse {
  user: UserDto
}

export interface ProtectedWelcomeResponse {
  message: string
  user: UserDto
  serverTime: string
}

class AuthService {

  // No longer needed - auth is handled via httpOnly cookies

  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await fetch(getApiUrl('/api/auth/login'), {
      method: 'POST',
      credentials: 'include', // CRITICAL: Include cookies
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials)
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Login failed')
    }

    const data = await response.json()
    console.log('Login API response:', data)
    // API now sets httpOnly cookie, no token in response
    
    return { user: data.user }
  }

  async register(credentials: RegisterCredentials): Promise<AuthResponse> {
    const response = await fetch(getApiUrl('/api/auth/register'), {
      method: 'POST',
      credentials: 'include', // CRITICAL: Include cookies
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials)
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Registration failed')
    }

    const data = await response.json()
    // API now sets httpOnly cookie, no token in response
    
    return { user: data.user }
  }

  async logout(): Promise<void> {
    try {
      await fetch(getApiUrl('/api/auth/logout'), {
        method: 'POST',
        credentials: 'include' // Use cookie for auth
      })
    } catch (error) {
      console.error('Logout error:', error)
      // Don't fail logout due to API errors - cookie will still be cleared by API
    }
  }

  async getProtectedWelcome(): Promise<ProtectedWelcomeResponse> {
    const response = await fetch(getApiUrl('/api/protected/welcome'), {
      credentials: 'include' // Use cookie for auth
    })

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Unauthorized')
      }
      throw new Error('Failed to fetch protected data')
    }

    return response.json()
  }

  // Token methods no longer needed - auth is handled via httpOnly cookies

  /**
   * Get current user info to restore authentication state
   */
  async getCurrentUser(): Promise<UserDto | null> {
    try {
      const response = await fetch(getApiUrl('/api/auth/user'), {
        credentials: 'include' // Use cookie for auth
      })

      if (!response.ok) {
        if (response.status === 401) {
          // User is not authenticated
          return null
        }
        throw new Error('Failed to get current user')
      }

      const userData = await response.json()
      return userData
    } catch (error) {
      console.warn('Failed to get current user:', error)
      return null
    }
  }
}

export const authService = new AuthService()
