/**
 * useApiMutation - Standardized TanStack Query mutation wrapper
 *
 * Purpose:
 * Provides consistent error handling and notification patterns for API mutations.
 * Uses apiClient which now has RFC 9457 error extraction built-in via interceptor.
 *
 * Pattern:
 * - Error messages are automatically extracted by apiClient interceptor
 * - Notifications use Mantine notifications for consistent UX
 * - onSuccess/onError callbacks allow component-specific behavior
 *
 * @see /docs/functional-areas/api-architecture-modernization/new-work/2025-12-09-error-handling-standardization/implementation-plan.md
 * @see /docs/standards-processes/frontend/api-error-handling-standard.md
 */

import { useMutation } from '@tanstack/react-query'
import { notifications } from '@mantine/notifications'
import type { AxiosError } from 'axios'

/**
 * Extended mutation options with notification support
 */
export interface UseApiMutationOptions<TData, TVariables, TError = AxiosError | Error> {
  /**
   * Success notification message (optional)
   * If provided, shows a success notification on successful mutation
   */
  successMessage?: string | ((data: TData, variables: TVariables) => string)

  /**
   * Error notification title (optional, default: 'Error')
   * Title shown in error notification
   */
  errorTitle?: string

  /**
   * Whether to show error notification (default: true)
   * Set to false to handle errors manually in the component
   */
  showErrorNotification?: boolean

  /**
   * Whether to show success notification (default: true if successMessage provided)
   * Set to false to show success manually
   */
  showSuccessNotification?: boolean

  /**
   * Called on successful mutation (after notification shown)
   */
  onSuccess?: (data: TData, variables: TVariables) => void | Promise<void>

  /**
   * Called on error (after notification shown)
   */
  onError?: (error: TError, variables: TVariables) => void | Promise<void>

  /**
   * Called when mutation settles (success or error)
   */
  onSettled?: (data: TData | undefined, error: TError | null, variables: TVariables) => void | Promise<void>
}

/**
 * Standardized mutation hook with automatic error handling
 *
 * @example
 * // Basic usage with success message
 * const createEvent = useApiMutation(
 *   async (data: CreateEventRequest) => {
 *     const response = await apiClient.post('/api/events', data)
 *     return response.data
 *   },
 *   { successMessage: 'Event created successfully' }
 * )
 *
 * @example
 * // Dynamic success message
 * const updateEvent = useApiMutation(
 *   async (data: UpdateEventRequest) => {
 *     const response = await apiClient.put(`/api/events/${data.id}`, data)
 *     return response.data
 *   },
 *   { successMessage: (result) => `Event "${result.title}" updated` }
 * )
 *
 * @example
 * // Manual error handling
 * const deleteEvent = useApiMutation(
 *   async (id: string) => {
 *     await apiClient.delete(`/api/events/${id}`)
 *   },
 *   {
 *     showErrorNotification: false,
 *     onError: (error) => {
 *       // Custom error handling
 *       console.log('Error message:', error.message) // Extracted by interceptor
 *     },
 *   }
 * )
 */
export function useApiMutation<TData = unknown, TVariables = void, TError = AxiosError | Error>(
  mutationFn: (variables: TVariables) => Promise<TData>,
  options?: UseApiMutationOptions<TData, TVariables, TError>
) {
  const {
    successMessage,
    errorTitle = 'Error',
    showErrorNotification = true,
    showSuccessNotification = true,
    onSuccess,
    onError,
    onSettled,
  } = options || {}

  return useMutation({
    mutationFn,
    onSuccess: async (data, variables) => {
      // Show success notification if message provided and enabled
      if (successMessage && showSuccessNotification) {
        const message = typeof successMessage === 'function'
          ? successMessage(data, variables)
          : successMessage

        notifications.show({
          title: 'Success',
          message,
          color: 'green',
        })
      }

      // Call original onSuccess if provided
      await onSuccess?.(data, variables)
    },
    onError: async (error, variables) => {
      // Error message is already extracted by apiClient interceptor
      // error.message now contains the RFC 9457 detail/title instead of generic axios message
      if (showErrorNotification) {
        const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred'
        notifications.show({
          title: errorTitle,
          message: errorMessage,
          color: 'red',
        })
      }

      // Log error for debugging (always)
      console.error('Mutation error:', error instanceof Error ? error.message : error, error)

      // Call original onError if provided
      await onError?.(error as TError, variables)
    },
    onSettled: async (data, error, variables) => {
      await onSettled?.(data, error as TError | null, variables)
    },
  })
}

/**
 * Type-safe wrapper for mutations that return Axios response data
 * Automatically extracts .data from Axios response
 *
 * @example
 * const { mutate } = useApiMutationWithExtract(
 *   async (eventData: CreateEventRequest) => {
 *     return apiClient.post<EventDto>('/api/events', eventData)
 *   },
 *   { successMessage: 'Event created!' }
 * )
 */
export function useApiMutationWithExtract<TData = unknown, TVariables = void, TError = AxiosError | Error>(
  mutationFn: (variables: TVariables) => Promise<{ data: TData }>,
  options?: UseApiMutationOptions<TData, TVariables, TError>
) {
  return useApiMutation<TData, TVariables, TError>(
    async (variables) => {
      const response = await mutationFn(variables)
      return response.data
    },
    options
  )
}
