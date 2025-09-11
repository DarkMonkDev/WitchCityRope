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
    this.token = data.token // Store JWT token in memory
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
    this.token = data.token // Store JWT token in memory
    return data
  }

  async logout(): Promise<void> {
    try {
      await apiRequest(apiConfig.endpoints.auth.logout, {
        method: 'POST',
        headers: this.token ? { Authorization: `Bearer ${this.token}` } : {},
      })
    } catch (error) {
      console.error('Logout error:', error)
    } finally {
      this.token = null
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
    this.token = token
  }
}

export const authService = new AuthService()
