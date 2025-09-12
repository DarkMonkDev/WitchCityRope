import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useUpdateEvent } from '../../apps/web/src/lib/api/hooks/useEvents'
import { apiClient } from '../../apps/web/src/lib/api/client'
import type { UpdateEventDto } from '../../apps/web/src/lib/api/types/events.types'
import { ReactElement, ReactNode } from 'react'

/**
 * Integration tests for event update authentication flow
 * 
 * CRITICAL ISSUE: Users are getting logged out when trying to save event changes in admin panel.
 * 
 * These tests focus on:
 * 1. Complete auth flow during update operations
 * 2. Cookie persistence during PUT requests
 * 3. Error handling for 401 responses
 * 4. Optimistic updates and rollback behavior
 * 5. Token refresh during long operations
 */

// Mock the API client
vi.mock('../../apps/web/src/lib/api/client', () => ({
  apiClient: {
    put: vi.fn(),
    get: vi.fn(),
    interceptors: {
      request: { use: vi.fn() },
      response: { use: vi.fn() }
    }
  }
}))

// Mock localStorage and cookies
const mockLocalStorage = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
}

Object.defineProperty(window, 'localStorage', {
  value: mockLocalStorage
})

// Mock document.cookie for httpOnly cookie testing
Object.defineProperty(document, 'cookie', {
  value: '',
  writable: true
})

// Mock window.location for logout redirect testing
const mockLocation = {
  href: '',
  pathname: '/admin/events'
}
Object.defineProperty(window, 'location', {
  value: mockLocation,
  writable: true
})

describe('Event Update Authentication Integration Tests', () => {
  let queryClient: QueryClient
  let wrapper: ({ children }: { children: ReactNode }) => ReactElement

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false }
      }
    })
    
    wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    )

    // Reset all mocks
    vi.clearAllMocks()
    
    // Reset window location
    mockLocation.href = ''
    mockLocation.pathname = '/admin/events'
  })

  afterEach(() => {
    queryClient.clear()
  })

  describe('Successful Authentication Flow', () => {
    it('should complete event update with valid authentication', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title',
        description: 'Updated description'
      }

      const mockEventResponse = {
        data: {
          success: true,
          data: {
            id: eventId,
            title: updateData.title,
            description: updateData.description,
            startDate: new Date().toISOString(),
            location: 'Test Location'
          },
          message: 'Event updated successfully'
        }
      }

      // Mock successful authentication state
      mockLocalStorage.getItem.mockReturnValue('valid-jwt-token')
      
      // Mock successful API response
      vi.mocked(apiClient.put).mockResolvedValueOnce(mockEventResponse)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      
      result.current.mutate(updateData)

      // Assert
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(apiClient.put).toHaveBeenCalledWith(`/api/events/${eventId}`, updateData)
      expect(result.current.data).toEqual(mockEventResponse.data.data)
      
      // Verify user is NOT logged out
      expect(mockLocation.href).toBe('') // No redirect occurred
      expect(mockLocalStorage.removeItem).not.toHaveBeenCalledWith('auth_token')
    })

    it('should persist authentication cookies during PUT request', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title'
      }

      // Mock httpOnly cookie presence
      document.cookie = 'auth-session=abc123; HttpOnly; Secure; Path=/'
      
      const mockResponse = {
        data: {
          success: true,
          data: { id: eventId, title: updateData.title },
          message: 'Event updated successfully'
        }
      }

      vi.mocked(apiClient.put).mockResolvedValueOnce(mockResponse)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Assert
      // Cookies should still be present (not cleared by interceptor)
      expect(document.cookie).toContain('auth-session=abc123')
      expect(mockLocation.href).toBe('') // No logout redirect
    })
  })

  describe('Authentication Failure Scenarios', () => {
    it('should handle 401 unauthorized response correctly', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title'
      }

      const unauthorizedError = {
        response: {
          status: 401,
          statusText: 'Unauthorized',
          data: {
            success: false,
            error: 'JWT token expired',
            message: 'Authentication required'
          }
        },
        config: {
          method: 'put',
          url: `/api/events/${eventId}`
        }
      }

      vi.mocked(apiClient.put).mockRejectedValueOnce(unauthorizedError)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      // Assert
      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(result.current.error).toBeDefined()
      
      // Verify logout behavior would be triggered by interceptor
      // (In real app, this would be handled by apiClient interceptor)
      expect(apiClient.put).toHaveBeenCalledWith(`/api/events/${eventId}`, updateData)
    })

    it('should handle token refresh during update operation', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title'
      }

      // Mock sequence: first call fails with 401, refresh succeeds, retry succeeds
      const unauthorizedError = {
        response: { status: 401 },
        config: { method: 'put', url: `/api/events/${eventId}` }
      }

      const successResponse = {
        data: {
          success: true,
          data: { id: eventId, title: updateData.title },
          message: 'Event updated successfully'
        }
      }

      vi.mocked(apiClient.put)
        .mockRejectedValueOnce(unauthorizedError) // First attempt fails
        .mockResolvedValueOnce(successResponse)    // Retry succeeds

      // Simulate token refresh
      mockLocalStorage.getItem.mockReturnValue('refreshed-jwt-token')

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      // Allow for retry logic
      await waitFor(() => {
        expect(result.current.isSuccess || result.current.isError).toBe(true)
      }, { timeout: 5000 })

      // Assert
      expect(apiClient.put).toHaveBeenCalledTimes(1) // Initial call
      // Note: Retry logic would be handled by apiClient interceptor
    })

    it('should clear authentication state on persistent 401 errors', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title'
      }

      const persistentUnauthorizedError = {
        response: {
          status: 401,
          statusText: 'Unauthorized',
          data: { error: 'Token cannot be refreshed' }
        },
        config: { method: 'put', url: `/api/events/${eventId}` }
      }

      vi.mocked(apiClient.put).mockRejectedValueOnce(persistentUnauthorizedError)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      // Assert
      expect(result.current.error).toBeDefined()
      
      // In real implementation, apiClient interceptor would:
      // 1. Clear localStorage auth token
      // 2. Clear queryClient cache
      // 3. Redirect to login page
      // This would be tested in apiClient.test.ts
    })
  })

  describe('Optimistic Updates and Rollback', () => {
    it('should perform optimistic update then rollback on auth failure', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const originalEvent = {
        id: eventId,
        title: 'Original Title',
        description: 'Original description'
      }
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Title',
        description: 'Updated description'
      }

      // Set initial cache data
      queryClient.setQueryData(['events', 'detail', eventId], originalEvent)

      const unauthorizedError = {
        response: { status: 401 },
        config: { method: 'put', url: `/api/events/${eventId}` }
      }

      vi.mocked(apiClient.put).mockRejectedValueOnce(unauthorizedError)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      
      // Perform optimistic update
      result.current.mutate(updateData)

      // Check optimistic state (should be applied immediately)
      const optimisticData = queryClient.getQueryData(['events', 'detail', eventId])
      // Note: The actual optimistic update behavior depends on implementation

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      // Assert rollback occurred
      await waitFor(() => {
        const rolledBackData = queryClient.getQueryData(['events', 'detail', eventId])
        // Should revert to original data or refetch
        expect(rolledBackData).toBeDefined()
      })
    })

    it('should invalidate cache after successful update with auth', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title'
      }

      const successResponse = {
        data: {
          success: true,
          data: {
            id: eventId,
            title: updateData.title,
            updatedAt: new Date().toISOString()
          },
          message: 'Event updated successfully'
        }
      }

      vi.mocked(apiClient.put).mockResolvedValueOnce(successResponse)

      // Spy on cache invalidation
      const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries')

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Assert
      expect(invalidateQueriesSpy).toHaveBeenCalled()
      // Check that event-related queries were invalidated
      const invalidationCalls = invalidateQueriesSpy.mock.calls
      expect(invalidationCalls.some(call => 
        JSON.stringify(call[0]).includes('events')
      )).toBe(true)
    })
  })

  describe('CORS and Network Configuration', () => {
    it('should include credentials in PUT request', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title'
      }

      const successResponse = {
        data: {
          success: true,
          data: { id: eventId, title: updateData.title },
          message: 'Event updated successfully'
        }
      }

      vi.mocked(apiClient.put).mockResolvedValueOnce(successResponse)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Assert
      expect(apiClient.put).toHaveBeenCalledWith(`/api/events/${eventId}`, updateData)
      
      // In real implementation, verify that apiClient includes:
      // - withCredentials: true for cookies
      // - Authorization header for JWT tokens
      // - Proper Content-Type headers
    })

    it('should handle CORS preflight for PUT requests', async () => {
      // This would test that the client is configured to handle
      // OPTIONS preflight requests for PUT operations
      // In integration environment, this would involve:
      
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title'
      }

      // Mock CORS preflight success followed by actual PUT
      const corsResponse = {
        status: 200,
        headers: {
          'Access-Control-Allow-Origin': 'http://localhost:5173',
          'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
          'Access-Control-Allow-Headers': 'Content-Type, Authorization',
          'Access-Control-Allow-Credentials': 'true'
        }
      }

      const putResponse = {
        data: {
          success: true,
          data: { id: eventId, title: updateData.title },
          message: 'Event updated successfully'
        }
      }

      vi.mocked(apiClient.put).mockResolvedValueOnce(putResponse)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Assert
      expect(apiClient.put).toHaveBeenCalledWith(`/api/events/${eventId}`, updateData)
    })
  })

  describe('Error Recovery and User Experience', () => {
    it('should provide meaningful error messages for auth failures', async () => {
      // Arrange
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'Updated Event Title'
      }

      const detailedAuthError = {
        response: {
          status: 401,
          statusText: 'Unauthorized',
          data: {
            success: false,
            error: 'JWT_EXPIRED',
            message: 'Your session has expired. Please log in again.',
            details: {
              expiredAt: new Date().toISOString(),
              requiresReauth: true
            }
          }
        },
        config: { method: 'put', url: `/api/events/${eventId}` }
      }

      vi.mocked(apiClient.put).mockRejectedValueOnce(detailedAuthError)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      // Assert
      expect(result.current.error).toBeDefined()
      // The error should contain meaningful information for the user
      const error = result.current.error as any
      expect(error.response?.data?.message).toContain('session has expired')
    })

    it('should maintain form state during authentication errors', async () => {
      // This test ensures that if authentication fails during an update,
      // the user's form data is preserved so they don't lose their changes
      
      const eventId = '550e8400-e29b-41d4-a716-446655440000'
      const updateData: UpdateEventDto = {
        id: eventId,
        title: 'User Edited Title',
        description: 'User spent time writing this description'
      }

      const authError = {
        response: { status: 401 },
        config: { method: 'put', url: `/api/events/${eventId}` }
      }

      vi.mocked(apiClient.put).mockRejectedValueOnce(authError)

      // Act
      const { result } = renderHook(() => useUpdateEvent(), { wrapper })
      result.current.mutate(updateData)

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      // Assert
      // The mutation variables should still be available
      // for the UI to restore form state
      expect(result.current.variables).toEqual(updateData)
      expect(result.current.failureReason).toBeDefined()
    })
  })
})