import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { MantineProvider } from '@mantine/core'
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { DashboardPage } from '../DashboardPage'
import { server } from '../../../test/setup'
import { http, HttpResponse } from 'msw'
import type { Event } from '../../../types/api.types'

// Mock react-router-dom Link component for navigation testing
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    Link: ({ to, children }: { to: string; children: React.ReactNode }) => (
      <a href={to} data-testid={`link-${to.replace('/', '-')}`}>
        {children}
      </a>
    ),
  }
})

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MantineProvider>
        <BrowserRouter>
          {children}
        </BrowserRouter>
      </MantineProvider>
    </QueryClientProvider>
  )
}

describe('DashboardPage', () => {
  beforeEach(() => {
    // Reset any MSW handlers
    server.resetHandlers()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('should render welcome message with user data', async () => {
    render(<DashboardPage />, { wrapper: createWrapper() })

    // Should show loading initially
    expect(screen.getByText('Loading...')).toBeInTheDocument()

    // Wait for user data to load and display welcome message
    await waitFor(() => {
      expect(screen.getByText(/Welcome back, TestAdmin/)).toBeInTheDocument()
    })
  })

  it('should display loading state while fetching user data', () => {
    render(<DashboardPage />, { wrapper: createWrapper() })

    expect(screen.getByText('Loading...')).toBeInTheDocument()
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('should handle user loading error', async () => {
    // Override MSW handler for user error
    server.use(
      http.get('http://localhost:5655/api/Protected/profile', () => {
        return new HttpResponse('Server error', { status: 500 })
      })
    )

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Failed to load user information')).toBeInTheDocument()
    })
  })

  it('should display upcoming events when data loads successfully', async () => {
    // Mock upcoming events
    const mockEvents: Event[] = [
      {
        id: '1',
        title: 'Upcoming Workshop',
        description: 'Test workshop',
        startDate: new Date(Date.now() + 86400000).toISOString(), // Tomorrow
        endDate: new Date(Date.now() + 90000000).toISOString(),
        maxAttendees: 20,
        currentAttendees: 5,
        isRegistrationOpen: true,
        instructorId: '1',
      },
      {
        id: '2',
        title: 'Another Event',
        description: 'Second test event',
        startDate: new Date(Date.now() + 172800000).toISOString(), // Day after tomorrow
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

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Your Upcoming Events')).toBeInTheDocument()
    })

    await waitFor(() => {
      expect(screen.getByText('Upcoming Workshop')).toBeInTheDocument()
      expect(screen.getByText('Another Event')).toBeInTheDocument()
    })

    // Check status badges
    expect(screen.getByText('Open')).toBeInTheDocument()
    expect(screen.getByText('Closed')).toBeInTheDocument()
  })

  it('should show loading state while fetching events', () => {
    render(<DashboardPage />, { wrapper: createWrapper() })

    expect(screen.getByText('Loading your upcoming events...')).toBeInTheDocument()
  })

  it('should handle events loading error', async () => {
    // Override MSW handler for events error
    server.use(
      http.get('http://localhost:5655/api/events', () => {
        return new HttpResponse('Server error', { status: 500 })
      })
    )

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Failed to load events. Please try refreshing the page.')).toBeInTheDocument()
    })
  })

  it('should display empty state when no upcoming events', async () => {
    // Mock empty events response
    server.use(
      http.get('http://localhost:5655/api/events', () => {
        return HttpResponse.json([])
      })
    )

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('No upcoming events')).toBeInTheDocument()
    })

    expect(screen.getByText('Browse our events and sign up for classes and community gatherings.')).toBeInTheDocument()
  })

  it('should display only future events and sort by date', async () => {
    const now = Date.now()
    const mockEvents: Event[] = [
      {
        id: '1',
        title: 'Past Event',
        description: 'Should not appear',
        startDate: new Date(now - 86400000).toISOString(), // Yesterday
        endDate: new Date(now - 82800000).toISOString(),
        maxAttendees: 20,
        currentAttendees: 5,
        isRegistrationOpen: false,
        instructorId: '1',
      },
      {
        id: '2',
        title: 'Second Future Event',
        description: 'Should appear second',
        startDate: new Date(now + 172800000).toISOString(), // Day after tomorrow
        endDate: new Date(now + 176400000).toISOString(),
        maxAttendees: 15,
        currentAttendees: 8,
        isRegistrationOpen: true,
        instructorId: '1',
      },
      {
        id: '3',
        title: 'First Future Event',
        description: 'Should appear first',
        startDate: new Date(now + 86400000).toISOString(), // Tomorrow
        endDate: new Date(now + 90000000).toISOString(),
        maxAttendees: 10,
        currentAttendees: 3,
        isRegistrationOpen: true,
        instructorId: '1',
      },
    ]

    server.use(
      http.get('http://localhost:5655/api/events', () => {
        return HttpResponse.json(mockEvents)
      })
    )

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('First Future Event')).toBeInTheDocument()
      expect(screen.getByText('Second Future Event')).toBeInTheDocument()
    })

    // Should not show past event
    expect(screen.queryByText('Past Event')).not.toBeInTheDocument()

    // Check order - First Future Event should appear before Second Future Event
    const events = screen.getAllByText(/Future Event/)
    expect(events[0]).toHaveTextContent('First Future Event')
    expect(events[1]).toHaveTextContent('Second Future Event')
  })

  it('should limit upcoming events to 5 maximum', async () => {
    const now = Date.now()
    const mockEvents: Event[] = Array.from({ length: 10 }, (_, i) => ({
      id: `${i + 1}`,
      title: `Event ${i + 1}`,
      description: `Test event ${i + 1}`,
      startDate: new Date(now + (i + 1) * 86400000).toISOString(),
      endDate: new Date(now + (i + 1) * 86400000 + 3600000).toISOString(),
      maxAttendees: 20,
      currentAttendees: 5,
      isRegistrationOpen: true,
      instructorId: '1',
    }))

    server.use(
      http.get('http://localhost:5655/api/events', () => {
        return HttpResponse.json(mockEvents)
      })
    )

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Event 1')).toBeInTheDocument()
      expect(screen.getByText('Event 5')).toBeInTheDocument()
    })

    // Should not show more than 5 events
    expect(screen.queryByText('Event 6')).not.toBeInTheDocument()
    expect(screen.queryByText('Event 7')).not.toBeInTheDocument()
  })

  it('should render quick action links correctly', async () => {
    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Quick Actions')).toBeInTheDocument()
    })

    // Check all quick action links are present
    expect(screen.getByTestId('link--events')).toBeInTheDocument()
    expect(screen.getByTestId('link--dashboard-profile')).toBeInTheDocument()
    expect(screen.getByTestId('link--dashboard-membership')).toBeInTheDocument()
    expect(screen.getByTestId('link--dashboard-security')).toBeInTheDocument()

    // Check link text content
    expect(screen.getByText('Browse All Events')).toBeInTheDocument()
    expect(screen.getByText('Update Profile')).toBeInTheDocument()
    expect(screen.getByText('Membership Status')).toBeInTheDocument()
    expect(screen.getByText('Security Settings')).toBeInTheDocument()
  })

  it('should format event times correctly', async () => {
    const mockEvents: Event[] = [
      {
        id: '1',
        title: 'Time Test Event',
        description: 'Test time formatting',
        startDate: '2025-12-25T19:30:00Z',
        endDate: '2025-12-25T21:45:00Z',
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

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Time Test Event')).toBeInTheDocument()
    })

    // Time should be formatted as "7:30 PM - 9:45 PM"
    expect(screen.getByText(/7:30 PM - 9:45 PM/)).toBeInTheDocument()
  })

  it('should format event dates correctly in calendar boxes', async () => {
    const mockEvents: Event[] = [
      {
        id: '1',
        title: 'Date Test Event',
        description: 'Test date formatting',
        startDate: '2025-12-25T19:30:00Z', // Christmas
        endDate: '2025-12-25T21:45:00Z',
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

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Date Test Event')).toBeInTheDocument()
    })

    // Should show day number and month abbreviation
    expect(screen.getByText('25')).toBeInTheDocument() // Day
    expect(screen.getByText('Dec')).toBeInTheDocument() // Month
  })

  it('should handle mixed loading states correctly', async () => {
    // User loads successfully, events fail
    server.use(
      http.get('http://localhost:5655/api/events', () => {
        return new HttpResponse('Server error', { status: 500 })
      })
    )

    render(<DashboardPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      // User should load successfully
      expect(screen.getByText(/Welcome back, TestAdmin/)).toBeInTheDocument()
      
      // Events should show error
      expect(screen.getByText('Failed to load events. Please try refreshing the page.')).toBeInTheDocument()
    })
  })
})