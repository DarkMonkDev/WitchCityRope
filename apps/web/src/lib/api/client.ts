import axios from 'axios'
import { queryClient } from './queryClient'

const API_BASE_URL = 'http://localhost:5655'

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
      console.log('🔍 401 Response intercepted. Current path:', window.location.pathname);
      
      // Only redirect if not already on login page or a demo page
      const isOnDemoPage = window.location.pathname.includes('/demo') || 
                          window.location.pathname.includes('/admin/events-management-api-demo') ||
                          window.location.pathname.includes('/admin/event-session-matrix-demo')
      
      console.log('🔍 Is on demo page?', isOnDemoPage);
      console.log('🔍 Is on login page?', window.location.pathname.includes('/login'));
      
      if (!window.location.pathname.includes('/login') && !isOnDemoPage) {
        console.log('🔍 Redirecting to login...');
        // Clear auth state only when redirecting
        localStorage.removeItem('auth_token')
        queryClient.clear()
        window.location.href = '/login'
      } else {
        console.log('🔍 Skipping redirect and query clearing (on login or demo page)');
        // Don't clear queryClient on demo pages to prevent reload loops
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