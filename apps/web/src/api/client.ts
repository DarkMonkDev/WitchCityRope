import axios from 'axios'
import { getCSRFToken } from '../hooks/useCSRFToken'

// API Service handles both authentication and business logic
// For production/staging: VITE_API_BASE_URL should be empty string (same-origin requests)
// For local dev: undefined falls back to http://localhost:5655
// Note: Vite replaces import.meta.env.VITE_* with literal values at build time
const envApiUrl = import.meta.env.VITE_API_BASE_URL
const apiBaseUrl = envApiUrl === ''
  ? '' // Empty string = same-origin requests (staging/production)
  : (envApiUrl || 'http://localhost:5655') // Any other string or undefined = use value or fallback

export const api = axios.create({
  baseURL: apiBaseUrl,
  withCredentials: true, // Include httpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor for CSRF token and logging
api.interceptors.request.use(
  (config) => {
    // Add CSRF token to all state-changing requests
    const method = config.method?.toLowerCase()
    if (method && ['post', 'put', 'delete', 'patch'].includes(method)) {
      const csrfToken = getCSRFToken()

      if (csrfToken) {
        config.headers['X-CSRF-TOKEN'] = csrfToken
        console.debug('✅ CSRF token added to request:', config.url)
      } else {
        console.error('❌ No CSRF token available for state-changing request:', config.url)
        // Let request proceed - backend will reject if token is required
      }
    }

    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor for auth errors and API error message extraction
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    // Handle 401 authentication errors
    if (error.response?.status === 401) {
      // Clear auth state - no tokens to clear with httpOnly cookies
      const { useAuthStore } = await import('../stores/authStore')
      const store = useAuthStore.getState()
      store.actions.logout()

      // Don't try to redirect to login - let components handle navigation
      console.log('401 Unauthorized - Authentication expired, state cleared')
    }

    // Extract API error message from RFC 7807 Problem Details format
    // This replaces the generic "Request failed with status code 400" with the actual API message
    const apiData = error.response?.data
    if (apiData) {
      const apiMessage = apiData.detail || apiData.title || apiData.message
      if (apiMessage && typeof apiMessage === 'string') {
        error.message = apiMessage
      }
    }

    return Promise.reject(error)
  }
)