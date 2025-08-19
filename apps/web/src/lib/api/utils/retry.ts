// Retry logic configuration and utilities
export const retryConfig = {
  // Default retry configuration
  default: {
    attempts: 3,
    delay: (attemptIndex: number) => Math.min(1000 * 2 ** attemptIndex, 30000),
  },
  
  // Critical operations (auth, payments) - fewer retries
  critical: {
    attempts: 1,
    delay: () => 1000,
  },
  
  // Background operations - more retries
  background: {
    attempts: 5,
    delay: (attemptIndex: number) => Math.min(500 * 2 ** attemptIndex, 10000),
  }
}

// Exponential backoff utility
export const exponentialBackoff = (
  attemptIndex: number, 
  baseDelay: number = 1000, 
  maxDelay: number = 30000
) => {
  const delay = baseDelay * 2 ** attemptIndex
  return Math.min(delay, maxDelay)
}

// Jittered backoff to prevent thundering herd
export const jitteredBackoff = (
  attemptIndex: number, 
  baseDelay: number = 1000, 
  maxDelay: number = 30000
) => {
  const delay = exponentialBackoff(attemptIndex, baseDelay, maxDelay)
  const jitter = Math.random() * 0.1 * delay // ±10% jitter
  return delay + jitter
}

// Retry condition helpers
export const shouldRetryCondition = {
  // Standard retry for network errors and 5xx
  standard: (failureCount: number, error: any) => {
    if (failureCount >= retryConfig.default.attempts) return false
    
    const status = error?.response?.status
    
    // Don't retry client errors (4xx) except timeout
    if (status >= 400 && status < 500 && status !== 408) return false
    
    // Retry server errors (5xx) and network errors
    return !status || status >= 500 || error.code === 'NETWORK_ERROR'
  },
  
  // Conservative retry for critical operations
  conservative: (failureCount: number, error: any) => {
    if (failureCount >= retryConfig.critical.attempts) return false
    
    // Only retry network errors and 503 (service unavailable)
    const status = error?.response?.status
    return !status || status === 503 || error.code === 'NETWORK_ERROR'
  },
  
  // Aggressive retry for background operations
  aggressive: (failureCount: number, error: any) => {
    if (failureCount >= retryConfig.background.attempts) return false
    
    const status = error?.response?.status
    
    // Don't retry auth errors, not found, or validation errors
    const noRetryStatus = [401, 403, 404, 422]
    if (noRetryStatus.includes(status)) return false
    
    return true
  }
}