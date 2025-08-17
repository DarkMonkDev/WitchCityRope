export interface LoginCredentials {
  email: string
  password: string
}

export interface RegisterCredentials {
  email: string
  password: string
  sceneName: string
}

export interface User {
  id: string
  email: string
  sceneName: string
  createdAt: string
  lastLoginAt: string
}

export interface AuthResponse {
  user: User
  token: string
}

export interface ProtectedWelcomeResponse {
  message: string
  user: User
  serverTime: string
}

const API_BASE_URL = 'http://localhost:5655'

class AuthService {
  private token: string | null = null

  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include', // Include httpOnly cookies
      body: JSON.stringify(credentials),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Login failed')
    }

    const data = await response.json()
    // API returns nested structure: { success: true, data: { token, user } }
    const authData = data.data || data
    this.token = authData.token // Store JWT token in memory
    return authData
  }

  async register(credentials: RegisterCredentials): Promise<AuthResponse> {
    const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include', // Include httpOnly cookies
      body: JSON.stringify(credentials),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Registration failed')
    }

    const data = await response.json()
    // API returns nested structure: { success: true, data: { token, user } }
    const authData = data.data || data
    this.token = authData.token // Store JWT token in memory
    return authData
  }

  async logout(): Promise<void> {
    try {
      await fetch(`${API_BASE_URL}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include', // Include httpOnly cookies
        headers: this.token ? { Authorization: `Bearer ${this.token}` } : {},
      })
    } catch (error) {
      console.error('Logout error:', error)
    } finally {
      this.token = null
    }
  }

  async getProtectedWelcome(): Promise<ProtectedWelcomeResponse> {
    const response = await fetch(`${API_BASE_URL}/api/protected/welcome`, {
      credentials: 'include', // Include httpOnly cookies
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
