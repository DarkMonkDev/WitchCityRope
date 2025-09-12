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
  async (config) => {
    // Get token from multiple sources in priority order
    try {
      let token: string | null = null;
      
      // 1. Try to get token from auth store first
      const authStore = (window as any).__AUTH_STORE__;
      if (authStore?.getState) {
        token = authStore.getState()?.actions?.getToken?.();
      }
      
      // 2. Fallback to authService
      if (!token) {
        const { authService } = await import('../../services/authService');
        token = authService.getToken();
      }
      
      // 3. Final fallback to localStorage
      if (!token) {
        token = localStorage.getItem('auth_token');
      }
      
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
        console.debug(`🔑 Adding Authorization header for ${config.method?.toUpperCase()} ${config.url}`);
      } else {
        console.debug(`⚠️ No auth token available for ${config.method?.toUpperCase()} ${config.url}`);
      }
    } catch (error) {
      console.warn('Error getting auth token:', error);
      // Last resort fallback to localStorage
      const token = localStorage.getItem('auth_token');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
        console.debug(`🔑 Emergency fallback token from localStorage for ${config.method?.toUpperCase()} ${config.url}`);
      }
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
      // Check if this is an admin or demo page - different handling
      const isOnDemoPage = window.location.pathname.includes('/demo') || 
                          window.location.pathname.includes('/admin');
      const isOnLoginPage = window.location.pathname.includes('/login');
      
      console.warn(`🚫 401 Unauthorized: ${config?.method?.toUpperCase()} ${config?.url}`);
      
      if (!isOnLoginPage) {
        // Clear auth state
        localStorage.removeItem('auth_token');
        
        // Clear auth store if available
        try {
          const authStore = (window as any).__AUTH_STORE__;
          if (authStore?.getState) {
            authStore.getState().actions.logout();
          }
        } catch (error) {
          console.warn('Could not clear auth store:', error);
        }
        
        // For admin/demo pages, show a more informative message
        if (isOnDemoPage) {
          console.error('🔐 Authentication required for admin access. Please log in first.');
          // Still redirect but with context
          window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.pathname);
        } else {
          // Clear query cache and redirect for normal pages
          queryClient.clear();
          window.location.href = '/login';
        }
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