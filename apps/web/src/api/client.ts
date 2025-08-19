import axios from 'axios'

// Web Service handles authentication with HttpOnly cookies
// API Service (5655) handles business logic with JWT tokens
// The Web Service acts as a proxy for authenticated API calls
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5651',
  withCredentials: true, // Include httpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
})

// Response interceptor for auth errors
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Try to refresh token
      try {
        await api.post('/auth/refresh')
        // Retry original request
        return api.request(error.config)
      } catch (refreshError) {
        // Redirect to login
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)