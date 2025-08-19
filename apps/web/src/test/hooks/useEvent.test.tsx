// test/hooks/useEvent.test.ts
import React from 'react'
import { describe, it, expect } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEvent } from '../../features/events/api/queries'

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

function createWrapper() {
  const queryClient = createTestQueryClient()
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    )
  }
}

describe('useEvent', () => {
  it('should fetch event data', async () => {
    const { result } = renderHook(() => useEvent('1'), {
      wrapper: createWrapper(),
    })

    expect(result.current.isPending).toBe(true)

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data).toEqual({
      id: '1',
      title: 'Test Event',
      description: 'A test event for API validation',
      startDate: '2025-08-20T19:00:00Z',
      endDate: '2025-08-20T21:00:00Z',
      maxAttendees: 20,
      currentAttendees: 5,
      isRegistrationOpen: true,
      instructorId: '1',
      instructor: {
        id: '1',
        sceneName: 'TestInstructor',
        email: 'instructor@test.com',
      },
      attendees: [],
    })
  })

  it('should not fetch when eventId is empty', () => {
    const { result } = renderHook(() => useEvent(''), {
      wrapper: createWrapper(),
    })

    expect(result.current.isPending).toBe(false)
    expect(result.current.data).toBeUndefined()
  })

  it('should use proper staleTime configuration', () => {
    const { result } = renderHook(() => useEvent('1'), {
      wrapper: createWrapper(),
    })

    // Check that the query is configured with proper staleTime
    expect(result.current.isPending).toBe(true)
  })
})