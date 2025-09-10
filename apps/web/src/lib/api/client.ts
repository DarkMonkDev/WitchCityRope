import axios from 'axios'
import { queryClient } from './queryClient'

// Use relative URL to leverage Vite proxy in development
// In production, this would be set to the actual API URL via environment variable
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ''

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Include httpOnly cookies for auth
})

// Request interceptor for auth tokens and logging
apiClient.interceptors.request.use(
  (config) => {
    // Token could be retrieved from authService or context
    const token = localStorage.getItem('auth_token') // Fallback for validation
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    console.debug(`API Request: ${config.method?.toUpperCase()} ${config.url}`)
    return config
  },
  (error) => {
    console.error('Request interceptor error:', error)
    return Promise.reject(error)
  }
)

// Response interceptor for error handling and logging
apiClient.interceptors.response.use(
  (response) => {
    console.debug(`API Response: ${response.status} ${response.config.url}`, {
      duration: response.config.metadata?.requestStartTime 
        ? Date.now() - response.config.metadata.requestStartTime 
        : undefined
    })
    return response
  },
  async (error) => {
    const { response, config } = error
    
    console.error(`API Error: ${config?.method?.toUpperCase()} ${config?.url}`, {
      status: response?.status,
      statusText: response?.statusText,
      data: response?.data
    })
    
    if (response?.status === 401) {
      // Clear auth state and redirect to login
      localStorage.removeItem('auth_token')
      queryClient.clear()
      
      // Only redirect if not already on login page
      if (!window.location.pathname.includes('/login')) {
        window.location.href = '/login'
      }
    }
    
    return Promise.reject(error)
  }
)

// Add request timing metadata
apiClient.interceptors.request.use((config) => {
  config.metadata = { requestStartTime: Date.now() }
  return config
})

export default apiClient