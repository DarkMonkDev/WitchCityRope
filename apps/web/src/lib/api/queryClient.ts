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

// Memory monitoring - helps detect memory leaks early
// Small webapps should stay well under 100MB
if (typeof window !== 'undefined' && 'performance' in window && 'memory' in (performance as any)) {
  let lastMemoryCheck = Date.now();
  let memoryWarningCount = 0;

  setInterval(() => {
    const now = Date.now();
    // Only check every 30 seconds to avoid performance impact
    if (now - lastMemoryCheck < 30000) return;

    const memoryInfo = (performance as any).memory;
    const usedMB = Math.round(memoryInfo.usedJSHeapSize / (1024 * 1024));
    const limitMB = Math.round(memoryInfo.jsHeapSizeLimit / (1024 * 1024));

    // Warn at 75MB, act at 100MB (small webapp shouldn't need more)
    if (usedMB > 75) {
      memoryWarningCount++;
      if (memoryWarningCount >= 2) {
        console.warn(`High memory usage: ${usedMB}MB / ${limitMB}MB - investigating potential memory leak`);

        // Clear cache if over 100MB (indicates a real problem)
        if (usedMB > 100) {
          console.warn('Excessive memory usage detected (>100MB), clearing query cache');
          queryClient.clear();
          memoryWarningCount = 0;
        }
      }
    } else {
      memoryWarningCount = 0;
    }

    lastMemoryCheck = now;
  }, 30000); // Check every 30 seconds
}