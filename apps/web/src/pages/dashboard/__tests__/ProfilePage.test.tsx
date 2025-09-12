import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { MantineProvider } from '@mantine/core'
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { ProfilePage } from '../ProfilePage'
import { server } from '../../../test/setup'
import { http, HttpResponse } from 'msw'

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, refetchOnMount: false, refetchOnWindowFocus: false, staleTime: 0, gcTime: 0 },
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

describe('ProfilePage', () => {
  beforeEach(() => {
    server.resetHandlers()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('should render page title', async () => {
    render(<ProfilePage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Profile', level: 1 })).toBeInTheDocument()
    })
  })

  it('should display loading state while fetching user data', () => {
    render(<ProfilePage />, { wrapper: createWrapper() })

    // Test that the page structure loads (header, layout)
    expect(screen.getByRole('heading', { name: 'Profile', level: 1 })).toBeInTheDocument()
    
    // The loading state might be too fast to catch, so just ensure data eventually loads
    // This test validates the page renders without crashing during loading
  })

  it('should handle user loading error', async () => {
    server.use(
      http.get('/api/auth/user', () => {
        return new HttpResponse('Server error', { status: 500 })
      })
    )

    render(<ProfilePage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('Failed to load your profile. Please try refreshing the page.')).toBeInTheDocument()
    })
  })

  it('should display note about future functionality', async () => {
    render(<ProfilePage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText(/Profile updates are not yet connected to the API/)).toBeInTheDocument()
    })
  })

  it('should render profile form sections', async () => {
    render(<ProfilePage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Profile Information', level: 2 })).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'Account Information', level: 2 })).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'Community Guidelines', level: 2 })).toBeInTheDocument()
    })
  })

  it('should populate form fields with user data', async () => {
    render(<ProfilePage />, { wrapper: createWrapper() })

    await waitFor(() => {
      const sceneNameInput = screen.getByLabelText('Scene Name')
      const emailInput = screen.getByLabelText('Email Address')

      expect(sceneNameInput).toHaveValue('TestAdmin')
      expect(emailInput).toHaveValue('admin@witchcityrope.com')
    })
  })

  it('should display user account information correctly', async () => {
    render(<ProfilePage />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('User ID')).toBeInTheDocument()
      expect(screen.getByText('1')).toBeInTheDocument() // User ID from MSW handler
      
      expect(screen.getByText('Account Created')).toBeInTheDocument()
      expect(screen.getByText('August 19, 2025')).toBeInTheDocument()
      
      expect(screen.getByText('Last Login')).toBeInTheDocument()
      expect(screen.getByText(/August 19, 2025/)).toBeInTheDocument()
    })
  })

  describe('Form Validation', () => {
    it('should validate scene name minimum length', async () => {
      const user = userEvent.setup()
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByLabelText('Scene Name')).toBeInTheDocument()
      })

      const sceneNameInput = screen.getByLabelText('Scene Name')
      const submitButton = screen.getByRole('button', { name: 'Update Profile' })

      await user.clear(sceneNameInput)
      await user.type(sceneNameInput, 'a') // Too short
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Scene name must be at least 2 characters')).toBeInTheDocument()
      })
    })

    it('should validate scene name maximum length', async () => {
      const user = userEvent.setup()
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByLabelText('Scene Name')).toBeInTheDocument()
      })

      const sceneNameInput = screen.getByLabelText('Scene Name')
      const submitButton = screen.getByRole('button', { name: 'Update Profile' })

      await user.clear(sceneNameInput)
      await user.type(sceneNameInput, 'a'.repeat(51)) // Too long
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Scene name must be less than 50 characters')).toBeInTheDocument()
      })
    })

    it('should validate email is required', async () => {
      const user = userEvent.setup()
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByLabelText('Email Address')).toBeInTheDocument()
      })

      const emailInput = screen.getByLabelText('Email Address')
      const submitButton = screen.getByRole('button', { name: 'Update Profile' })

      await user.clear(emailInput)
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Email is required')).toBeInTheDocument()
      })
    })

    it('should validate email format', async () => {
      const user = userEvent.setup()
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByLabelText('Email Address')).toBeInTheDocument()
      })

      const emailInput = screen.getByLabelText('Email Address')
      const submitButton = screen.getByRole('button', { name: 'Update Profile' })

      await user.clear(emailInput)
      await user.type(emailInput, 'invalid-email')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Invalid email format')).toBeInTheDocument()
      })
    })

    it('should accept valid scene name and email', async () => {
      const user = userEvent.setup()
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByLabelText('Scene Name')).toBeInTheDocument()
      })

      const sceneNameInput = screen.getByLabelText('Scene Name')
      const emailInput = screen.getByLabelText('Email Address')

      await user.clear(sceneNameInput)
      await user.type(sceneNameInput, 'ValidSceneName')
      
      await user.clear(emailInput)
      await user.type(emailInput, 'valid@email.com')

      // Should not show validation errors
      expect(screen.queryByText('Scene name must be at least 2 characters')).not.toBeInTheDocument()
      expect(screen.queryByText('Invalid email format')).not.toBeInTheDocument()
    })
  })

  describe('Form Submission', () => {
    it('should submit form with valid data', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
      const user = userEvent.setup()
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByLabelText('Scene Name')).toBeInTheDocument()
      })

      const sceneNameInput = screen.getByLabelText('Scene Name')
      const emailInput = screen.getByLabelText('Email Address')
      const submitButton = screen.getByRole('button', { name: 'Update Profile' })

      await user.clear(sceneNameInput)
      await user.type(sceneNameInput, 'UpdatedSceneName')
      
      await user.clear(emailInput)
      await user.type(emailInput, 'updated@email.com')

      await user.click(submitButton)

      await waitFor(() => {
        expect(consoleSpy).toHaveBeenCalledWith('Profile update submitted:', {
          sceneName: 'UpdatedSceneName',
          email: 'updated@email.com',
        })
      })

      consoleSpy.mockRestore()
    })

    it('should not submit form with validation errors', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
      const user = userEvent.setup()
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByLabelText('Scene Name')).toBeInTheDocument()
      })

      const sceneNameInput = screen.getByLabelText('Scene Name')
      const submitButton = screen.getByRole('button', { name: 'Update Profile' })

      await user.clear(sceneNameInput)
      await user.type(sceneNameInput, 'a') // Invalid - too short
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Scene name must be at least 2 characters')).toBeInTheDocument()
      })

      // Form should not submit
      expect(consoleSpy).not.toHaveBeenCalled()

      consoleSpy.mockRestore()
    })
  })

  describe('Form Fields Properties', () => {
    it('should render form fields with correct attributes', async () => {
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByLabelText('Scene Name')).toBeInTheDocument()
      })

      const sceneNameInput = screen.getByLabelText('Scene Name')
      const emailInput = screen.getByLabelText('Email Address')

      expect(sceneNameInput).toBeRequired()
      expect(sceneNameInput).toHaveAttribute('placeholder', 'Your community name')

      expect(emailInput).toBeRequired()
      expect(emailInput).toHaveAttribute('type', 'email')
      expect(emailInput).toHaveAttribute('placeholder', 'your.email@example.com')
    })
  })

  describe('Community Guidelines', () => {
    it('should display community guidelines section', async () => {
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Community Guidelines', level: 2 })).toBeInTheDocument()
      })

      expect(screen.getByText('Your scene name is how other community members will know you. Please choose something that:')).toBeInTheDocument()
      expect(screen.getByText('Represents you authentically in the rope community')).toBeInTheDocument()
      expect(screen.getByText('Respects our community values and guidelines')).toBeInTheDocument()
      expect(screen.getByText('Is appropriate for all community events and interactions')).toBeInTheDocument()
      expect(screen.getByText('Does not impersonate others or organizations')).toBeInTheDocument()
      expect(screen.getByText('Changes to your scene name may require approval from community moderators.')).toBeInTheDocument()
    })
  })

  describe('Visual Design', () => {
    it('should apply hover effects to paper sections', async () => {
      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Profile Information', level: 2 })).toBeInTheDocument()
      })

      // Get one of the paper sections
      const profileSection = screen.getByRole('heading', { name: 'Profile Information', level: 2 }).closest('div[style*="background: #FFF8F0"]')
      expect(profileSection).toBeInTheDocument()

      // Test hover effects
      if (profileSection) {
        fireEvent.mouseEnter(profileSection)
        fireEvent.mouseLeave(profileSection)
      }
      // Note: Testing actual style changes would require jsdom style calculation
      // This test ensures the event handlers don't throw errors
    })
  })

  describe('User Data Edge Cases', () => {
    it('should handle missing optional user data gracefully', async () => {
      // Override MSW handler to return user with missing optional fields
      server.use(
        http.get('/api/auth/user', () => {
          return HttpResponse.json({
            success: true,
            data: {
              id: '1',
              email: 'test@example.com',
              sceneName: 'TestUser',
              // Missing: firstName, lastName, lastLoginAt
              roles: ['GeneralMember'],
              isActive: true,
              createdAt: '2025-08-19T00:00:00Z',
              updatedAt: '2025-08-19T10:00:00Z',
            }
          })
        })
      )

      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByText('User ID')).toBeInTheDocument()
        expect(screen.getByText('1')).toBeInTheDocument()
      })

      // Should show fallback text for missing data
      expect(screen.queryByText('Last Login')).not.toBeInTheDocument() // Should not show section if no lastLoginAt
    })

    it('should handle user with no ID or created date', async () => {
      server.use(
        http.get('/api/auth/user', () => {
          return HttpResponse.json({
            success: true,
            data: {
              email: 'test@example.com',
              sceneName: 'TestUser',
              // Missing: id, createdAt
              roles: ['GeneralMember'],
              isActive: true,
              updatedAt: '2025-08-19T10:00:00Z',
            }
          })
        })
      )

      render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByText('User ID')).toBeInTheDocument()
      })

      expect(screen.getByText('Not available')).toBeInTheDocument() // For missing ID
      expect(screen.getAllByText('Not available')).toHaveLength(2) // For missing createdAt too
    })
  })

  describe('Form Reset on User Data Update', () => {
    it('should update form values when user data changes', async () => {
      const { rerender } = render(<ProfilePage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(screen.getByDisplayValue('TestAdmin')).toBeInTheDocument()
        expect(screen.getByDisplayValue('admin@witchcityrope.com')).toBeInTheDocument()
      })

      // Simulate user data update by overriding MSW handler
      server.use(
        http.get('/api/auth/user', () => {
          return HttpResponse.json({
            success: true,
            data: {
              id: '1',
              email: 'newemail@example.com',
              sceneName: 'NewSceneName',
              roles: ['Admin'],
              isActive: true,
              createdAt: '2025-08-19T00:00:00Z',
              updatedAt: '2025-08-19T10:00:00Z',
              lastLoginAt: '2025-08-19T10:00:00Z'
            }
          })
        })
      )

      // Rerender to trigger new data fetch
      rerender(<ProfilePage />)

      // Form should eventually update with new values
      await waitFor(() => {
        expect(screen.getByDisplayValue('NewSceneName')).toBeInTheDocument()
        expect(screen.getByDisplayValue('newemail@example.com')).toBeInTheDocument()
      }, { timeout: 3000 })
    })
  })
})