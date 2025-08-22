import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { useCurrentUser } from '../../features/auth/api/queries'
import { useEvents } from '../../features/events/api/queries'
import { server } from '../setup'
import { http, HttpResponse } from 'msw'
import type { Event } from '../../types/api.types'

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  )
}

describe('Dashboard Integration Tests', () => {
  beforeEach(() => {
    server.resetHandlers()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('useCurrentUser Integration', () => {
    it('should fetch current user data successfully', async () => {
      const { result } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      // Initially loading
      expect(result.current.isLoading).toBe(true)
      expect(result.current.data).toBeUndefined()

      // Wait for data to load
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Verify user data structure
      expect(result.current.data).toEqual({
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        firstName: null,
        lastName: null,
        roles: ['Admin'],
        isActive: true,
        createdAt: '2025-08-19T00:00:00Z',
        updatedAt: '2025-08-19T10:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      })

      expect(result.current.isLoading).toBe(false)
      expect(result.current.error).toBeNull()
    })

    it('should handle user fetch error', async () => {
      // Override MSW handler for error
      server.use(
        http.get('http://localhost:5655/api/Protected/profile', () => {
          return new HttpResponse('Server error', { status: 500 })
        })
      )

      const { result } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(result.current.data).toBeUndefined()
      expect(result.current.error).toBeTruthy()
    })

    it('should handle network timeout', async () => {
      // Override MSW handler for timeout simulation
      server.use(
        http.get('http://localhost:5655/api/Protected/profile', async () => {
          // Simulate long delay
          await new Promise(resolve => setTimeout(resolve, 5000))
          return HttpResponse.json({})
        })
      )

      const { result } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      // Should eventually timeout and error
      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      }, { timeout: 6000 })
    })
  })

  describe('useEvents Integration', () => {
    it('should fetch events data successfully', async () => {
      const mockEvents: Event[] = [
        {
          id: '1',
          title: 'Integration Test Event',
          description: 'Test event for integration',
          startDate: new Date(Date.now() + 86400000).toISOString(),
          endDate: new Date(Date.now() + 90000000).toISOString(),
          maxAttendees: 20,
          currentAttendees: 5,
          isRegistrationOpen: true,
          instructorId: '1',
        },
        {
          id: '2',
          title: 'Another Test Event',
          description: 'Second test event',
          startDate: new Date(Date.now() + 172800000).toISOString(),
          endDate: new Date(Date.now() + 176400000).toISOString(),
          maxAttendees: 15,
          currentAttendees: 8,
          isRegistrationOpen: false,
          instructorId: '1',
        },
      ]

      server.use(
        http.get('http://localhost:5655/api/events', () => {
          return HttpResponse.json(mockEvents)
        })
      )

      const { result } = renderHook(() => useEvents(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data).toEqual(mockEvents)
      expect(result.current.data).toHaveLength(2)
      expect(result.current.data?.[0]?.title).toBe('Integration Test Event')
    })

    it('should handle events fetch error', async () => {
      server.use(
        http.get('http://localhost:5655/api/events', () => {
          return new HttpResponse('Server error', { status: 500 })
        })
      )

      const { result } = renderHook(() => useEvents(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(result.current.data).toBeUndefined()
      expect(result.current.error).toBeTruthy()
    })

    it('should handle empty events response', async () => {
      server.use(
        http.get('http://localhost:5655/api/events', () => {
          return HttpResponse.json([])
        })
      )

      const { result } = renderHook(() => useEvents(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data).toEqual([])
      expect(result.current.data).toHaveLength(0)
    })
  })

  describe('Dashboard Data Integration', () => {
    it('should fetch both user and events data for dashboard', async () => {
      const mockEvents: Event[] = [
        {
          id: '1',
          title: 'Dashboard Event',
          description: 'Event for dashboard',
          startDate: new Date(Date.now() + 86400000).toISOString(),
          endDate: new Date(Date.now() + 90000000).toISOString(),
          maxAttendees: 20,
          currentAttendees: 5,
          isRegistrationOpen: true,
          instructorId: '1',
        },
      ]

      server.use(
        http.get('http://localhost:5655/api/events', () => {
          return HttpResponse.json(mockEvents)
        })
      )

      const { result: userResult } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      const { result: eventsResult } = renderHook(() => useEvents(), {
        wrapper: createWrapper(),
      })

      // Wait for both to load
      await waitFor(() => {
        expect(userResult.current.isSuccess).toBe(true)
        expect(eventsResult.current.isSuccess).toBe(true)
      })

      // Verify both data sets are available
      expect(userResult.current.data?.sceneName).toBe('TestAdmin')
      expect(eventsResult.current.data).toHaveLength(1)
      expect(eventsResult.current.data?.[0]?.title).toBe('Dashboard Event')
    })

    it('should handle mixed success/error states', async () => {
      // User succeeds, events fail
      server.use(
        http.get('http://localhost:5655/api/events', () => {
          return new HttpResponse('Server error', { status: 500 })
        })
      )

      const { result: userResult } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      const { result: eventsResult } = renderHook(() => useEvents(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(userResult.current.isSuccess).toBe(true)
        expect(eventsResult.current.isError).toBe(true)
      })

      // User data should be available
      expect(userResult.current.data?.sceneName).toBe('TestAdmin')
      
      // Events should be in error state
      expect(eventsResult.current.data).toBeUndefined()
      expect(eventsResult.current.error).toBeTruthy()
    })
  })

  describe('API Response Validation', () => {
    it('should handle malformed user response', async () => {
      server.use(
        http.get('http://localhost:5655/api/Protected/profile', () => {
          return HttpResponse.json({
            // Missing required fields
            email: 'test@example.com',
            // No id, sceneName, etc.
          })
        })
      )

      const { result } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Should handle partial data gracefully
      expect(result.current.data?.email).toBe('test@example.com')
      expect(result.current.data?.id).toBeUndefined()
    })

    it('should handle malformed events response', async () => {
      server.use(
        http.get('http://localhost:5655/api/events', () => {
          return HttpResponse.json([
            {
              id: '1',
              title: 'Valid Event',
              // Missing other required fields
            },
            {
              // Completely malformed event
              invalid: 'data'
            }
          ])
        })
      )

      const { result } = renderHook(() => useEvents(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Should return the data as received (TypeScript provides compile-time safety)
      expect(result.current.data).toHaveLength(2)
      expect(result.current.data?.[0]?.title).toBe('Valid Event')
    })
  })

  describe('Query Caching and Refetching', () => {
    it('should cache user data between hook calls', async () => {
      const { result: firstResult } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(firstResult.current.isSuccess).toBe(true)
      })

      // Create second hook instance (simulates different component using same query)
      const { result: secondResult } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      // Second call should use cached data (no loading state)
      expect(secondResult.current.isLoading).toBe(false)
      expect(secondResult.current.data).toEqual(firstResult.current.data)
    })

    it('should handle query invalidation', async () => {
      const queryClient = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
      })

      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <QueryClientProvider client={queryClient}>
          {children}
        </QueryClientProvider>
      )

      const { result } = renderHook(() => useCurrentUser(), { wrapper })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Invalidate the query
      act(() => {
        queryClient.invalidateQueries({ queryKey: ['user'] })
      })

      // Should refetch
      await waitFor(() => {
        expect(result.current.isLoading).toBe(false) // Will finish refetching
        expect(result.current.data).toBeTruthy()
      })
    })
  })

  describe('Error Recovery', () => {
    it('should recover from temporary network errors', async () => {
      // Start with error
      server.use(
        http.get('http://localhost:5655/api/Protected/profile', () => {
          return new HttpResponse('Server error', { status: 500 })
        })
      )

      const { result } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      // Fix the handler
      server.use(
        http.get('http://localhost:5655/api/Protected/profile', () => {
          return HttpResponse.json({
            id: '1',
            email: 'recovered@example.com',
            sceneName: 'RecoveredUser',
            roles: ['GeneralMember'],
            isActive: true,
            createdAt: '2025-08-19T00:00:00Z',
            updatedAt: '2025-08-19T10:00:00Z',
            lastLoginAt: '2025-08-19T10:00:00Z'
          })
        })
      )

      // Trigger refetch
      act(() => {
        result.current.refetch()
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      expect(result.current.data?.email).toBe('recovered@example.com')
    })
  })

  describe('Concurrent Requests', () => {
    it('should handle multiple simultaneous dashboard data requests', async () => {
      const mockEvents: Event[] = [
        {
          id: '1',
          title: 'Concurrent Test Event',
          description: 'Event for concurrent testing',
          startDate: new Date(Date.now() + 86400000).toISOString(),
          endDate: new Date(Date.now() + 90000000).toISOString(),
          maxAttendees: 20,
          currentAttendees: 5,
          isRegistrationOpen: true,
          instructorId: '1',
        },
      ]

      server.use(
        http.get('http://localhost:5655/api/events', () => {
          return HttpResponse.json(mockEvents)
        })
      )

      // Render multiple hooks simultaneously
      const { result: userResult1 } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      const { result: userResult2 } = renderHook(() => useCurrentUser(), {
        wrapper: createWrapper(),
      })

      const { result: eventsResult1 } = renderHook(() => useEvents(), {
        wrapper: createWrapper(),
      })

      const { result: eventsResult2 } = renderHook(() => useEvents(), {
        wrapper: createWrapper(),
      })

      // All should resolve successfully
      await waitFor(() => {
        expect(userResult1.current.isSuccess).toBe(true)
        expect(userResult2.current.isSuccess).toBe(true)
        expect(eventsResult1.current.isSuccess).toBe(true)
        expect(eventsResult2.current.isSuccess).toBe(true)
      })

      // All should have same data (cached)
      expect(userResult1.current.data).toEqual(userResult2.current.data)
      expect(eventsResult1.current.data).toEqual(eventsResult2.current.data)
    })
  })
})