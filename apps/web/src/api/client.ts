import axios from 'axios'

// API Service handles both authentication and business logic
// For production/staging: VITE_API_BASE_URL should be empty (same-origin requests)
// For local dev: Falls back to http://localhost:5655
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL !== undefined
  ? import.meta.env.VITE_API_BASE_URL
  : 'http://localhost:5655'

export const api = axios.create({
  baseURL: apiBaseUrl,
  withCredentials: true, // Include httpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor for logging (auth handled by httpOnly cookies)
api.interceptors.request.use(
  (config) => {
    // No token needed - auth handled by httpOnly cookies
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor for auth errors
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Clear auth state - no tokens to clear with httpOnly cookies
      const { useAuthStore } = await import('../stores/authStore')
      const store = useAuthStore.getState()
      store.actions.logout()
      
      // Don't try to redirect to login - let components handle navigation
      console.log('401 Unauthorized - Authentication expired, state cleared')
    }
    return Promise.reject(error)
  }
)