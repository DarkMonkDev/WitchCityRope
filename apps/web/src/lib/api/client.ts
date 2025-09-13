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

// Request interceptor for logging (auth now handled by httpOnly cookies)
apiClient.interceptors.request.use(
  (config) => {
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
      // Check if this is a login page to avoid redirect loops
      const isOnLoginPage = window.location.pathname.includes('/login');
      
      console.warn(`🚫 401 Unauthorized: ${config?.method?.toUpperCase()} ${config?.url}`);
      
      if (!isOnLoginPage) {
        // Clear auth store if available
        const authStore = (window as any).__AUTH_STORE__;
        try {
          if (authStore?.getState) {
            authStore.getState().actions.logout();
          }
        } catch (error) {
          console.warn('Could not clear auth store:', error);
        }
        
        // Clear query cache and redirect to login
        queryClient.clear();
        window.location.href = '/login';
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