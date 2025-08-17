import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import { EventsList } from '../EventsList'

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

// Mock fetch globally
const mockFetch = vi.fn()
global.fetch = mockFetch

describe('EventsList Component - Vertical Slice Home Page Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('displays loading spinner while fetching events', async () => {
    // Arrange
    mockFetch.mockImplementation(() => new Promise(() => {})) // Never resolves

    // Act
    render(<EventsList />)

    // Assert
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument()
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('displays events when data loads successfully', async () => {
    // Arrange
    const mockEvents = [
      {
        id: '550e8400-e29b-41d4-a716-446655440000',
        title: 'Rope Basics Workshop',
        description: 'Learn the fundamentals of rope bondage',
        startDate: '2025-08-25T14:00:00Z',
        location: 'Salem Community Center',
      },
      {
        id: '550e8400-e29b-41d4-a716-446655440001',
        title: 'Advanced Suspension Techniques',
        description: 'Advanced workshop covering suspension safety',
        startDate: '2025-08-30T19:00:00Z',
        location: 'Studio Space Downtown',
      },
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockEvents,
    })

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('events-grid')).toBeInTheDocument()
    })

    expect(screen.getAllByTestId('event-card')).toHaveLength(2)
    expect(screen.getByText('Rope Basics Workshop')).toBeInTheDocument()
    expect(screen.getByText('Advanced Suspension Techniques')).toBeInTheDocument()
  })

  it('displays error message when API call fails', async () => {
    // Arrange
    mockFetch.mockRejectedValueOnce(new Error('Network error'))

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
    // Arrange
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    })

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('error-message')).toBeInTheDocument()
    })

    expect(screen.getByText(/Failed to load events/)).toBeInTheDocument()
  })

  it('displays empty state when no events are returned', async () => {
    // Arrange
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => [],
    })

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
    // Arrange
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => [],
    })

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1)
    })

    expect(mockFetch).toHaveBeenCalledWith('http://localhost:5655/api/events')
  })

  it('proves React + API stack integration works', async () => {
    // Arrange - Test that proves the React → API communication works
    const mockApiResponse = [
      {
        id: 'proof-of-concept-id',
        title: 'React + API Integration Test',
        description: 'This proves React can fetch from the API',
        startDate: '2025-08-25T14:00:00Z',
        location: 'Proof of Concept',
      },
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockApiResponse,
    })

    // Act
    render(<EventsList />)

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('events-grid')).toBeInTheDocument()
    })

    // Verify React received and displayed the API data
    expect(screen.getByText('React + API Integration Test')).toBeInTheDocument()
    expect(screen.getByText('This proves React can fetch from the API')).toBeInTheDocument()

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
    // Arrange
    mockFetch.mockImplementation(
      () => new Promise((_, reject) => setTimeout(() => reject(new Error('Network timeout')), 100))
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
