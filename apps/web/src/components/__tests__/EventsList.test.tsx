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

// We'll use MSW handlers instead of mock fetch

describe('EventsList Component - Vertical Slice Home Page Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('displays loading spinner while fetching events', async () => {
    // Arrange - MSW handler that never resolves
    server.use(
      http.get('http://localhost:5655/api/events', () => {
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
    // Use default MSW handler which returns Test Event 1 and Test Event 2

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('events-grid')).toBeInTheDocument()
    })

    expect(screen.getAllByTestId('event-card')).toHaveLength(2)
    expect(screen.getByText('Test Event 1')).toBeInTheDocument()
    expect(screen.getByText('Test Event 2')).toBeInTheDocument()
  })

  it('displays error message when API call fails', async () => {
    // Arrange - MSW handler that simulates network error
    server.use(
      http.get('http://localhost:5655/api/events', () => {
        return HttpResponse.error()
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('error-message')).toBeInTheDocument()
    })

    expect(screen.getByText('Error:')).toBeInTheDocument()
    expect(screen.getByText(/Failed to load events/)).toBeInTheDocument()
    expect(
      screen.getByText(/API service is running on http:\/\/localhost:5655/)
    ).toBeInTheDocument()
  })

  it('displays error message when API returns non-200 status', async () => {
    // Arrange - MSW handler that returns 500 error
    server.use(
      http.get('http://localhost:5655/api/events', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('error-message')).toBeInTheDocument()
    })

    expect(screen.getByText(/Failed to load events/)).toBeInTheDocument()
  })

  it('displays empty state when no events are returned', async () => {
    // Arrange - MSW handler that returns empty array
    server.use(
      http.get('http://localhost:5655/api/events', () => {
        return HttpResponse.json([])
      })
    )

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('empty-state')).toBeInTheDocument()
    })

    expect(screen.getByText('No events available')).toBeInTheDocument()
    expect(screen.getByText(/The API returned no events/)).toBeInTheDocument()
  })

  it('calls correct API endpoint for events', async () => {
    // This test is implicit with MSW - if the request reaches the right endpoint,
    // the handler will be triggered and return data
    
    // Act
    render(<EventsList />)

    // Assert - if the component renders events, the correct endpoint was called
    await waitFor(() => {
      expect(screen.getByTestId('events-grid')).toBeInTheDocument()
    })

    expect(screen.getAllByTestId('event-card')).toHaveLength(2)
  })

  it('proves React + API stack integration works', async () => {
    // Act - Use default MSW handler
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('events-grid')).toBeInTheDocument()
    })

    // Verify React received and displayed the API data
    expect(screen.getByText('Test Event 1')).toBeInTheDocument()
    expect(screen.getByText('First test event')).toBeInTheDocument()

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
    server.use(
      http.get('http://localhost:5655/api/events', async () => {
        await new Promise(resolve => setTimeout(resolve, 100))
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
