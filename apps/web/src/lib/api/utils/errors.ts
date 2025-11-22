import type { AxiosError } from 'axios'
import type { ApiError, ProblemDetails } from '../../../types/api.types'

// Error handling utilities
export class ApiErrorHandler {
  /**
   * Checks if error response is RFC 9457 Problem Details
   */
  static isProblemDetails(data: any): data is ProblemDetails {
    return (
      data &&
      typeof data === 'object' &&
      ('title' in data || 'detail' in data || 'status' in data)
    )
  }

  static formatError(error: unknown): ApiError {
    if (error instanceof Error) {
      // Axios error with response
      if ('response' in error && error.response) {
        const axiosError = error as AxiosError<any>
        const responseData = axiosError.response?.data
        const status = axiosError.response?.status || 0

        // Handle RFC 9457 Problem Details format (Pattern B)
        if (this.isProblemDetails(responseData)) {
          const problem = responseData as ProblemDetails
          return {
            message: problem.detail || problem.title || 'An error occurred',
            status: problem.status || status,
            code: problem.status?.toString() || status?.toString()
          }
        }

        // Legacy format support
        return {
          message: responseData?.message ||
                  responseData?.error ||
                  axiosError.message ||
                  'An error occurred',
          status,
          code: status?.toString()
        }
      }

      // Network or other errors
      return {
        message: error.message || 'Network error occurred',
        status: 0,
        code: 'NETWORK_ERROR'
      }
    }

    return {
      message: 'An unexpected error occurred',
      status: 0,
      code: 'UNKNOWN_ERROR'
    }
  }
  
  static getUserFriendlyMessage(error: ApiError): string {
    switch (error.code) {
      case '401':
        return 'Please log in to continue'
      case '403':
        return 'You don\'t have permission for this action'
      case '404':
        return 'The requested resource was not found'
      case '409':
        return 'This action conflicts with existing data'
      case '422':
        return 'Please check your input and try again'
      case '429':
        return 'Too many requests. Please try again later'
      case '500':
        return 'Server error. Please try again later'
      case 'NETWORK_ERROR':
        return 'Connection failed. Please check your internet connection'
      default:
        return error.message || 'Something went wrong. Please try again'
    }
  }
  
  static shouldRetry(error: ApiError): boolean {
    const noRetryStatus = ['401', '403', '404', '422', '429']
    return !noRetryStatus.includes(error.code || '')
  }
}

// Error notification helper
export const handleApiError = (error: unknown, showToast: (message: string) => void) => {
  const apiError = ApiErrorHandler.formatError(error)
  const userMessage = ApiErrorHandler.getUserFriendlyMessage(apiError)
  showToast(userMessage)

  // Log detailed error for debugging
  console.error('API Error:', apiError)

  return apiError
}

// =============================================================================
// Pattern B Helper Functions (Direct DTO + RFC 9457 Problem Details)
// =============================================================================

/**
 * Extracts user-friendly error message from Pattern B error response
 * Handles RFC 9457 Problem Details format
 *
 * @param error - Axios error object or any error
 * @returns Human-readable error message
 */
export function extractErrorMessage(error: any): string {
  // Check for RFC 9457 Problem Details in response
  if (error.response?.data) {
    const data = error.response.data

    if (ApiErrorHandler.isProblemDetails(data)) {
      const problem = data as ProblemDetails
      // Priority: detail > title > generic message
      // detail contains the specific error message, title is the generic category
      return problem.detail || problem.title || 'An error occurred'
    }

    // Fallback for non-ProblemDetails error responses
    if (typeof data === 'string') {
      return data
    }
  }

  // Check for axios error message
  if (error.message) {
    return error.message
  }

  // Last resort fallback
  return 'An unexpected error occurred'
}

/**
 * Gets the HTTP status code from error
 * @param error - Error object
 * @returns HTTP status code or undefined
 */
export function getErrorStatus(error: any): number | undefined {
  return error.response?.status
}

/**
 * Checks if error is a specific HTTP status
 * @param error - Error object
 * @param status - HTTP status code to check
 */
export function isErrorStatus(error: any, status: number): boolean {
  return getErrorStatus(error) === status
}