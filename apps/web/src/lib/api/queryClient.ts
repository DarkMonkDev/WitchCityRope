import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 15 * 60 * 1000, // 15 minutes (gcTime replaces cacheTime in v5)
      retry: (failureCount, error: any) => {
        // Don't retry authentication errors
        if (error?.response?.status === 401) return false
        // Don't retry validation errors (4xx)
        if (error?.response?.status >= 400 && error?.response?.status < 500) return false
        // Retry network errors up to 3 times
        return failureCount < 3
      },
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
      refetchOnWindowFocus: false, // Disable for development/validation
    },
    mutations: {
      retry: 1, // Retry mutations once on failure
    },
  },
})

// Memory management for validation environment
if (typeof window !== 'undefined' && 'memory' in performance) {
  setInterval(() => {
    const memoryInfo = (performance as any).memory
    if (memoryInfo?.usedJSHeapSize > 50 * 1024 * 1024) { // 50MB threshold
      console.warn('High memory usage detected, clearing query cache')
      queryClient.clear()
    }
  }, 60000) // Check every minute
}