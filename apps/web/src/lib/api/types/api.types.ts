// Base API response wrapper
export interface ApiResponse<T> {
  success: boolean
  data?: T
  error?: string
  details?: string
  message?: string
  timestamp: string
}

// Pagination types
export interface PaginationParams {
  page?: number
  limit?: number
  offset?: number
}

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  limit: number
  total: number
  totalPages: number
  hasNext: boolean
  hasPrev: boolean
}

// Error types
export interface ApiError {
  message: string
  code?: string
  field?: string
  details?: unknown
}

// Request/Response timing metadata
export interface RequestMetadata {
  requestStartTime: number
}

// Extend axios config to include metadata
declare module 'axios' {
  interface AxiosRequestConfig {
    metadata?: RequestMetadata
  }
}