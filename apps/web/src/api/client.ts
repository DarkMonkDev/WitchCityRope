import axios from 'axios'

// API Service handles both authentication and business logic
// Updated to connect directly to the .NET API at port 5655
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655',
  withCredentials: true, // Include httpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor to add JWT token to Authorization header
api.interceptors.request.use(
  async (config) => {
    // Dynamically import to avoid circular dependency
    const { useAuthStore } = await import('../stores/authStore')
    const store = useAuthStore.getState()
    
    // Get valid token if available
    const token = store.actions.getToken()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    
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
      // Clear invalid token from auth store
      const { useAuthStore } = await import('../stores/authStore')
      const store = useAuthStore.getState()
      store.actions.logout()
      
      // Don't try to redirect to login - let components handle navigation
      console.log('401 Unauthorized - Authentication expired, token cleared')
    }
    return Promise.reject(error)
  }
)