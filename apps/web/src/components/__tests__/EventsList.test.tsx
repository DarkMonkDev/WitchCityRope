import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import { EventsList } from '../EventsList'
import { server } from '../../test/setup'
import { http, HttpResponse } from 'msw'

// Mock the EventCard component since we're testing EventsList in isolation
vi.mock('../EventCard', () => ({
  EventCard: ({ event }: { event: { id: string; title: string; description: string } }) => (
    <div data-testid="event-card">
      <h3>{event.title}</h3>
      <p>{event.description}</p>
    </div>
  ),
}))

// Mock the LoadingSpinner component
vi.mock('../LoadingSpinner', () => ({
  LoadingSpinner: () => <div data-testid="loading-spinner">Loading...</div>,
}))

describe('EventsList Component - Vertical Slice Home Page Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('displays loading spinner while fetching events', async () => {
    // Arrange - MSW handler that never resolves
    // Use relative path because component uses getApiUrl which returns '/api/events' in dev mode
    server.use(
      http.get('/api/events', () => {
        return new Promise(() => {}) // Never resolves
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument()
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('displays events when data loads successfully', async () => {
    // Arrange - Provide mock events for this unit test
    server.use(
      http.get('/api/events', () => {
        return HttpResponse.json({
          success: true,
          data: [
            {
              id: '1',
              title: 'Rope Bondage Fundamentals',
              description: 'Learn the basics of safe rope bondage with experienced instructors',
              startDate: '2025-08-20T19:00:00Z',
              endDate: '2025-08-20T21:00:00Z',
              capacity: 20,
              registrationCount: 5,
              eventType: 'class',
            },
            {
              id: '2',
              title: 'Community Social Night',
              description: 'Join fellow community members for socializing and light play',
              startDate: '2025-08-21T19:00:00Z',
              endDate: '2025-08-21T21:00:00Z',
              capacity: 15,
              registrationCount: 10,
              eventType: 'social',
            },
          ],
        })
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('events-grid')).toBeInTheDocument()
    })

    expect(screen.getAllByTestId('event-card')).toHaveLength(2)
    expect(screen.getByText('Rope Bondage Fundamentals')).toBeInTheDocument()
    expect(screen.getByText('Community Social Night')).toBeInTheDocument()
  })

  it('displays error message when API call fails', async () => {
    // Arrange - MSW handler that simulates network error
    // Use relative path because component uses getApiUrl which returns '/api/events' in dev mode
    server.use(
      http.get('/api/events', () => {
        return HttpResponse.error()
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(
      () => {
        expect(screen.getByTestId('error-message')).toBeInTheDocument()
      },
      { timeout: 3000 }
    )

    expect(screen.getByText('Error:')).toBeInTheDocument()
    expect(screen.getByText(/Failed to load events/)).toBeInTheDocument()
    // NOTE: In dev mode, getApiBaseUrl() returns empty string, so no URL is shown in the error message
    // The component shows: "Failed to load events. Please check that the API service is running on "
  })

  it('displays error message when API returns non-200 status', async () => {
    // Arrange - MSW handler that returns 500 error
    // Use relative path because component uses getApiUrl which returns '/api/events' in dev mode
    server.use(
      http.get('/api/events', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(
      () => {
        expect(screen.getByTestId('error-message')).toBeInTheDocument()
      },
      { timeout: 3000 }
    )

    expect(screen.getByText(/Failed to load events/)).toBeInTheDocument()
  })

  it('displays empty state when no events are returned', async () => {
    // Arrange - MSW handler that returns empty data wrapped in API response format
    // Use relative path because component uses getApiUrl which returns '/api/events' in dev mode
    server.use(
      http.get('/api/events', () => {
        // Match EventsList component logic: component checks data.data OR data array
        return HttpResponse.json({ success: true, data: [] })
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(
      () => {
        expect(screen.getByTestId('empty-state')).toBeInTheDocument()
      },
      { timeout: 3000 }
    )

    expect(screen.getByText('No events available')).toBeInTheDocument()
    expect(screen.getByText(/The API returned no events/)).toBeInTheDocument()
  })

  it('calls correct API endpoint for events', async () => {
    // Arrange - Provide mock events for this unit test
    server.use(
      http.get('/api/events', () => {
        return HttpResponse.json({
          success: true,
          data: [
            {
              id: '1',
              title: 'Test Event 1',
              description: 'Description 1',
              startDate: '2025-08-20T19:00:00Z',
              eventType: 'class',
            },
            {
              id: '2',
              title: 'Test Event 2',
              description: 'Description 2',
              startDate: '2025-08-21T19:00:00Z',
              eventType: 'social',
            },
          ],
        })
      })
    )

    // Act
    render(<EventsList />)

    // Assert - if the component renders events, the correct endpoint was called
    await waitFor(() => {
      expect(screen.getByTestId('events-grid')).toBeInTheDocument()
    })

    expect(screen.getAllByTestId('event-card')).toHaveLength(2)
  })

  it('proves React + API stack integration works', async () => {
    // Arrange - Provide mock events for this unit test
    server.use(
      http.get('/api/events', () => {
        return HttpResponse.json({
          success: true,
          data: [
            {
              id: '1',
              title: 'Rope Bondage Fundamentals',
              description: 'Learn the basics of safe rope bondage with experienced instructors',
              startDate: '2025-08-20T19:00:00Z',
              eventType: 'class',
            },
          ],
        })
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('events-grid')).toBeInTheDocument()
    })

    // Verify React received and displayed the MSW mock data
    expect(screen.getByText('Rope Bondage Fundamentals')).toBeInTheDocument()
    expect(
      screen.getByText('Learn the basics of safe rope bondage with experienced instructors')
    ).toBeInTheDocument()

    // Verify the component structure matches requirements
    const eventsGrid = screen.getByTestId('events-grid')
    expect(eventsGrid).toHaveClass(
      'grid',
      'grid-cols-1',
      'md:grid-cols-2',
      'lg:grid-cols-3',
      'gap-6'
    )
  })

  it('handles network timeout gracefully', async () => {
    // Arrange - MSW handler that delays and then returns error
    // Use relative path because component uses getApiUrl which returns '/api/events' in dev mode
    server.use(
      http.get('/api/events', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.error()
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(
      () => {
        expect(screen.getByTestId('error-message')).toBeInTheDocument()
      },
      { timeout: 2000 }
    )

    expect(screen.getByText(/Failed to load events/)).toBeInTheDocument()
  })
})
