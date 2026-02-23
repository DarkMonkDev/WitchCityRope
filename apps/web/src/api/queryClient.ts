import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,      // 5 minutes
      gcTime: 10 * 60 * 1000,        // 10 minutes (formerly cacheTime)
      retry: (failureCount: number, error: Error) => {
        // Don't retry on 4xx client errors
        if (error instanceof Error && 'status' in error) {
          const status = (error as Record<string, unknown>).status as number
          if (status >= 400 && status < 500) return false
        }
        return failureCount < 3
      },
      retryDelay: (attemptIndex: number) => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
    mutations: {
      retry: false, // Don't retry mutations by default
      onError: (error: Error) => {
        console.error('Mutation failed:', error)
        // Add toast notification here
      },
    },
  },
})