import axios from 'axios'
import { queryClient } from './queryClient'

// Use environment variable for API base URL, fallback to development default
// Note: Vite replaces import.meta.env.VITE_* with literal values at build time
// Empty string means same-origin requests (production/staging), not localhost
const envApiUrl = import.meta.env.VITE_API_BASE_URL
const API_BASE_URL = envApiUrl === ''
  ? '' // Empty string = same-origin requests (staging/production)
  : (envApiUrl || 'http://localhost:5655') // Any other string or undefined = use value or fallback

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Include httpOnly cookies for auth
})

// Request interceptor for logging (BFF pattern - auth via httpOnly cookies only)
apiClient.interceptors.request.use(
  (config) => {
    // BFF Pattern: Authentication handled via httpOnly cookies automatically
    // No need to add Authorization header - JWT token is in secure cookie
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
        : undefined,
    })
    return response
  },
  async (error) => {
    const { response, config } = error

    // Check if this is an expected 404 for vetting application check
    // When a user visits /join without an existing application, the API returns 404
    // This is EXPECTED behavior - it means "no application found, show the form"
    const is404 = response?.status === 404
    const isMyApplicationEndpoint = config?.url?.includes('/my-application')
    const shouldSuppressLog = is404 && isMyApplicationEndpoint

    // Only log actual errors, not expected 404s for application checks
    if (!shouldSuppressLog) {
      console.error(`API Error: ${config?.method?.toUpperCase()} ${config?.url}`, {
        status: response?.status,
        statusText: response?.statusText,
        data: response?.data,
      })
    }

    if (response?.status === 401) {
      // Check if this is a login page or demo/test page to avoid redirect loops
      const currentPath = window.location.pathname
      const isOnLoginPage = currentPath.includes('/login')
      const isDemoPage = currentPath.includes('-demo') || currentPath.includes('/test')
      const isPublicRoute =
        currentPath === '/' || currentPath.startsWith('/events') || currentPath.startsWith('/join')

      console.warn(`🚫 401 Unauthorized: ${config?.method?.toUpperCase()} ${config?.url}`)

      // Only redirect if NOT on login, demo, test, or public pages
      if (!isOnLoginPage && !isDemoPage && !isPublicRoute) {
        // Clear auth store if available
        const authStore = (
          window as unknown as {
            __AUTH_STORE__?: { getState: () => { actions: { logout: () => void } } }
          }
        ).__AUTH_STORE__
        try {
          if (authStore?.getState) {
            authStore.getState().actions.logout()
          }
        } catch (error) {
          console.warn('Could not clear auth store:', error)
        }

        // Clear query cache and redirect to login
        queryClient.clear()
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
