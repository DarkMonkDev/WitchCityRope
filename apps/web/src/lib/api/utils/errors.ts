import type { AxiosError } from 'axios'
import type { ApiError } from '../types/api.types'

// Error handling utilities
export class ApiErrorHandler {
  static formatError(error: unknown): ApiError {
    if (error instanceof Error) {
      // Axios error with response
      if ('response' in error && error.response) {
        const axiosError = error as AxiosError<any>
        return {
          message: axiosError.response?.data?.message || 
                  axiosError.response?.data?.error || 
                  axiosError.message || 
                  'An error occurred',
          code: axiosError.response?.status?.toString(),
          details: axiosError.response?.data
        }
      }
      
      // Network or other errors
      return {
        message: error.message || 'Network error occurred',
        code: 'NETWORK_ERROR'
      }
    }
    
    return {
      message: 'An unexpected error occurred',
      code: 'UNKNOWN_ERROR',
      details: error
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