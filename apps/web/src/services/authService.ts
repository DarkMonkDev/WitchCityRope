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
  token: string
}

export interface ProtectedWelcomeResponse {
  message: string
  user: UserDto
  serverTime: string
}

class AuthService {
  private token: string | null = null
  private readonly TOKEN_KEY = 'witchcityrope_jwt_token'
  private readonly TOKEN_EXPIRY_KEY = 'witchcityrope_jwt_expiry'

  constructor() {
    // Restore token from localStorage on initialization
    this.initializeFromStorage()
  }

  private initializeFromStorage(): void {
    try {
      const storedToken = localStorage.getItem(this.TOKEN_KEY)
      const storedExpiry = localStorage.getItem(this.TOKEN_EXPIRY_KEY)
      
      if (storedToken && storedExpiry) {
        const expiryTime = new Date(storedExpiry)
        if (new Date() < expiryTime) {
          // Token is still valid
          this.token = storedToken
        } else {
          // Token expired, clean up
          this.clearStoredAuth()
        }
      }
    } catch (error) {
      console.warn('Failed to restore auth from localStorage:', error)
      this.clearStoredAuth()
    }
  }

  private storeAuth(token: string, expiresAt: string): void {
    try {
      localStorage.setItem(this.TOKEN_KEY, token)
      localStorage.setItem(this.TOKEN_EXPIRY_KEY, expiresAt)
      this.token = token
    } catch (error) {
      console.warn('Failed to store auth in localStorage:', error)
      // Fall back to memory-only storage
      this.token = token
    }
  }

  private clearStoredAuth(): void {
    try {
      localStorage.removeItem(this.TOKEN_KEY)
      localStorage.removeItem(this.TOKEN_EXPIRY_KEY)
    } catch (error) {
      console.warn('Failed to clear auth from localStorage:', error)
    }
    this.token = null
  }

  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await apiRequest(apiConfig.endpoints.auth.login, {
      method: 'POST',
      body: JSON.stringify(credentials),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Login failed')
    }

    const data = await response.json()
    console.log('Login API response:', data)
    // API returns flat structure directly: { token, user, refreshToken, expiresAt }
    
    // Store JWT token persistently with expiration
    if (data.token && data.expiresAt) {
      this.storeAuth(data.token, data.expiresAt)
    } else {
      // Fallback for missing expiry - assume 8 hours
      const expiresAt = new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString()
      this.storeAuth(data.token, expiresAt)
    }
    
    return data
  }

  async register(credentials: RegisterCredentials): Promise<AuthResponse> {
    const response = await apiRequest(apiConfig.endpoints.auth.register, {
      method: 'POST',
      body: JSON.stringify(credentials),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Registration failed')
    }

    const data = await response.json()
    // API returns flat structure directly: { token, user, refreshToken, expiresAt }
    
    // Store JWT token persistently with expiration
    if (data.token && data.expiresAt) {
      this.storeAuth(data.token, data.expiresAt)
    } else {
      // Fallback for missing expiry - assume 8 hours
      const expiresAt = new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString()
      this.storeAuth(data.token, expiresAt)
    }
    
    return data
  }

  async logout(): Promise<void> {
    try {
      // Call API logout even though it's currently a placeholder
      // This will be important when proper token blacklisting is implemented
      await apiRequest(apiConfig.endpoints.auth.logout, {
        method: 'POST',
        headers: this.token ? { Authorization: `Bearer ${this.token}` } : {},
      })
    } catch (error) {
      console.error('Logout error:', error)
      // Don't fail logout due to API errors
    } finally {
      // Always clear stored authentication
      this.clearStoredAuth()
    }
  }

  async getProtectedWelcome(): Promise<ProtectedWelcomeResponse> {
    const response = await apiRequest(apiConfig.endpoints.protected.welcome, {
      headers: this.token ? { Authorization: `Bearer ${this.token}` } : {},
    })

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Unauthorized')
      }
      throw new Error('Failed to fetch protected data')
    }

    return response.json()
  }

  getToken(): string | null {
    return this.token
  }

  setToken(token: string | null): void {
    if (token) {
      // When manually setting token, assume 8-hour expiry
      const expiresAt = new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString()
      this.storeAuth(token, expiresAt)
    } else {
      this.clearStoredAuth()
    }
  }

  /**
   * Check if current token is expired
   */
  isTokenExpired(): boolean {
    if (!this.token) return true
    
    try {
      const storedExpiry = localStorage.getItem(this.TOKEN_EXPIRY_KEY)
      if (!storedExpiry) return true
      
      const expiryTime = new Date(storedExpiry)
      return new Date() >= expiryTime
    } catch (error) {
      console.warn('Failed to check token expiry:', error)
      return true
    }
  }

  /**
   * Get current user info to restore authentication state
   */
  async getCurrentUser(): Promise<UserDto | null> {
    if (!this.token || this.isTokenExpired()) {
      this.clearStoredAuth()
      return null
    }

    try {
      const response = await apiRequest(apiConfig.endpoints.auth.currentUser, {
        headers: { Authorization: `Bearer ${this.token}` },
      })

      if (!response.ok) {
        if (response.status === 401) {
          // Token is invalid, clear it
          this.clearStoredAuth()
          return null
        }
        throw new Error('Failed to get current user')
      }

      const userData = await response.json()
      return userData
    } catch (error) {
      console.warn('Failed to get current user:', error)
      this.clearStoredAuth()
      return null
    }
  }
}

export const authService = new AuthService()
