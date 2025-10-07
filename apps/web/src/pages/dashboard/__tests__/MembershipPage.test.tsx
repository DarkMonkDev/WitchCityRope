import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { MantineProvider } from '@mantine/core'
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { MembershipPage } from '../MembershipPage'
import { server } from '../../../test/setup'
import { http, HttpResponse } from 'msw'

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

describe('MembershipPage', () => {
  beforeEach(() => {
    server.resetHandlers()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('should render page title', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Membership')).toBeInTheDocument()
    })
  })

  it('should display loading state while fetching user data', () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    expect(screen.getByText('Membership')).toBeInTheDocument()
    expect(screen.getByText('Loading your membership information...')).toBeInTheDocument()
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('should handle user loading error', async () => {
    server.use(
      http.get('/api/auth/user', () => {
        return new HttpResponse('Server error', { status: 500 })
      })
    )

    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Failed to load your membership information. Please try refreshing the page.')).toBeInTheDocument()
    })
  })

  it('should display note about mock data', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText(/Membership data is currently using mock information/)).toBeInTheDocument()
    })
  })

  it('should display membership status overview', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Membership Status')).toBeInTheDocument()
      expect(screen.getByText('Active')).toBeInTheDocument()
      expect(screen.getByText('Member Level')).toBeInTheDocument()
      expect(screen.getByText('General Member')).toBeInTheDocument()
      expect(screen.getByText('Member Since')).toBeInTheDocument()
      expect(screen.getByText('Days as Member')).toBeInTheDocument()
    })
  })

  it('should calculate and display membership duration correctly', async () => {
    // Mock user with specific creation date
    server.use(
      http.get('/api/auth/user', () => {
        return HttpResponse.json({
          success: true,
          data: {
            id: '1',
            email: 'admin@witchcityrope.com',
            sceneName: 'TestAdmin',
            firstName: null,
            lastName: null,
            roles: ['Admin'],
            isActive: true,
            createdAt: '2025-08-01T00:00:00Z', // 21 days ago from 2025-08-22
            updatedAt: '2025-08-19T10:00:00Z',
            lastLoginAt: '2025-08-19T10:00:00Z'
          }
        })
      })
    )

    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Member Since')).toBeInTheDocument()
      expect(screen.getByText('August 1, 2025')).toBeInTheDocument()
      
      // Duration calculation (approximate, actual value depends on current date)
      expect(screen.getByText(/\d+ days/)).toBeInTheDocument()
    })
  })

  it('should display member benefits section', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Member Benefits')).toBeInTheDocument()
      
      expect(screen.getByText('Event Registration')).toBeInTheDocument()
      expect(screen.getByText('Priority access to register for workshops and community events')).toBeInTheDocument()
      
      expect(screen.getByText('Community Access')).toBeInTheDocument()
      expect(screen.getByText('Access to member-only discussions and resources')).toBeInTheDocument()
      
      expect(screen.getByText('Member Discounts')).toBeInTheDocument()
      expect(screen.getByText('Reduced pricing on workshops and special events')).toBeInTheDocument()
    })
  })

  it('should show all benefits as active', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Member Benefits')).toBeInTheDocument()
    })

    // Should have 3 "Active" badges for the benefits
    const activeBadges = screen.getAllByText('Active')
    expect(activeBadges).toHaveLength(3)
  })

  it('should display community standing section', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Community Standing')).toBeInTheDocument()
      
      expect(screen.getByText('Trust Level')).toBeInTheDocument()
      expect(screen.getByText('Good Standing')).toBeInTheDocument()
      
      expect(screen.getByText('Event Attendance')).toBeInTheDocument()
      expect(screen.getByText('Regular Attendee')).toBeInTheDocument()
      
      expect(screen.getByText('Community Participation')).toBeInTheDocument()
      expect(screen.getByText('Active Member')).toBeInTheDocument()
    })
  })

  it('should display good standing message', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('✓ Member in Good Standing')).toBeInTheDocument()
      expect(screen.getByText('You maintain good standing in the community with regular participation and positive interactions.')).toBeInTheDocument()
    })
  })

  it('should display membership actions section', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Membership Actions')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'View Community Guidelines' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Contact Support' })).toBeInTheDocument()
    })
  })

  it('should display help information', async () => {
    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Need Help?')).toBeInTheDocument()
      expect(screen.getByText(/If you have questions about your membership status or benefits/)).toBeInTheDocument()
    })
  })

  it('should handle user with no creation date', async () => {
    server.use(
      http.get('/api/auth/user', () => {
        return HttpResponse.json({
          success: true,
          data: {
            id: '1',
            email: 'admin@witchcityrope.com',
          sceneName: 'TestAdmin',
          firstName: null,
          lastName: null,
          roles: ['Admin'],
          isActive: true,
          // Missing createdAt
          updatedAt: '2025-08-19T10:00:00Z',
          lastLoginAt: '2025-08-19T10:00:00Z'
        })
      })
    )

    render(<MembershipPage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Member Since')).toBeInTheDocument()
      // Should still display some date (current date as fallback)
      expect(screen.getByText(/\d+ days/)).toBeInTheDocument()
    })
  })

  describe('Progress Bars', () => {
    it('should render progress bars for community standing metrics', async () => {
      render(<MembershipPage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByText('Community Standing')).toBeInTheDocument()
      })

      // Should have 3 progress bars (Trust Level, Event Attendance, Community Participation)
      const progressBars = screen.getAllByRole('progressbar')
      expect(progressBars.length).toBeGreaterThanOrEqual(3) // At least 3 (excluding loading spinner if present)
      
      // Check specific progress values are set (these are hardcoded in the component)
      // Note: Testing exact progress values would require checking CSS or ARIA attributes
    })
  })

  describe('Visual Design', () => {
    it('should apply hover effects to paper sections', async () => {
      render(<MembershipPage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByText('Membership Status')).toBeInTheDocument()
      })

      // Get one of the paper sections
      const statusSection = screen.getByText('Membership Status').closest('div[style*="background: #FFF8F0"]')
      expect(statusSection).toBeInTheDocument()

      // Test hover effects
      if (statusSection) {
        fireEvent.mouseEnter(statusSection)
        fireEvent.mouseLeave(statusSection)
      }
      // Note: Testing actual style changes would require jsdom style calculation
      // This test ensures the event handlers don't throw errors
    })
  })

  describe('Interactive Elements', () => {
    it('should handle action button clicks', async () => {
      const user = userEvent.setup()
      render(<MembershipPage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'View Community Guidelines' })).toBeInTheDocument()
      })

      const guidelinesButton = screen.getByRole('button', { name: 'View Community Guidelines' })
      const supportButton = screen.getByRole('button', { name: 'Contact Support' })

      // Should be able to click buttons without errors
      await user.click(guidelinesButton)
      await user.click(supportButton)
      
      // Note: These buttons don't have actual functionality yet (TODO items in component)
      // This test ensures they render and are clickable
    })
  })

  describe('Benefit Icons and Layout', () => {
    it('should display benefit icons and descriptions correctly', async () => {
      render(<MembershipPage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByText('Member Benefits')).toBeInTheDocument()
      })

      // Check for emoji icons (these are hardcoded in the component)
      expect(screen.getByText('📅')).toBeInTheDocument() // Event Registration
      expect(screen.getByText('👥')).toBeInTheDocument() // Community Access
      expect(screen.getByText('💰')).toBeInTheDocument() // Member Discounts
    })
  })

  describe('Badge Display', () => {
    it('should display membership status badge correctly', async () => {
      render(<MembershipPage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByText('Membership Status')).toBeInTheDocument()
      })

      // Should have the Active badge in the membership status section
      const statusSection = screen.getByText('Membership Status').closest('div')
      const activeBadge = statusSection?.querySelector('*:contains("Active")')
      
      // The "Active" text should be visible within the status section
      expect(screen.getByText('Active')).toBeInTheDocument()
    })
  })

  describe('Date Formatting', () => {
    it('should format membership date correctly', async () => {
      render(<MembershipPage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByText('Member Since')).toBeInTheDocument()
      })

      // Should show formatted date like "August 19, 2025"
      expect(screen.getByText(/\w+ \d{1,2}, \d{4}/)).toBeInTheDocument()
    })
  })
})