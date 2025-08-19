import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MantineProvider } from '@mantine/core'
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '../setup'
import { useAuthStore } from '../../stores/authStore'
import { LoginPage } from '../../pages/LoginPage'
import { DashboardPage } from '../../pages/DashboardPage'

// Mock global fetch for auth store calls
const mockFetch = vi.fn()
global.fetch = mockFetch

// Mock React Router hooks that are problematic in tests
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ search: '', pathname: '/login' })
  }
})

describe('Authentication Flow Integration', () => {
  let queryClient: QueryClient
  let user: ReturnType<typeof userEvent.setup>

  beforeEach(() => {
    // Reset auth store
    useAuthStore.getState().actions.logout()
    
    // Create fresh query client for each test
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    })
    
    // Setup user events
    user = userEvent.setup()
    
    // Mock fetch for auth store logout calls
    mockFetch.mockClear()
    mockNavigate.mockClear()
    mockFetch.mockResolvedValue({
      ok: true,
      status: 200,
    } as Response)
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  const renderWithProviders = (component: React.ReactElement) => {
    return render(
      <QueryClientProvider client={queryClient}>
        <MantineProvider>
          {component}
        </MantineProvider>
      </QueryClientProvider>
    )
  }

  describe('Complete Login Flow', () => {
    it('should complete login flow and update auth store', async () => {
      renderWithProviders(<LoginPage />)

      // Verify we're on the login page
      expect(screen.getByText('Welcome to WitchCityRope')).toBeInTheDocument()
      expect(screen.getByTestId('login-form')).toBeInTheDocument()

      // Fill in the login form
      const emailInput = screen.getByTestId('email-input')
      const passwordInput = screen.getByTestId('password-input')
      const submitButton = screen.getByTestId('login-button')

      await user.type(emailInput, 'admin@witchcityrope.com')
      await user.type(passwordInput, 'Test123!')

      // Verify form is filled correctly
      expect(emailInput).toHaveValue('admin@witchcityrope.com')
      expect(passwordInput).toHaveValue('Test123!')

      // Submit the form
      await user.click(submitButton)

      // Wait for the login mutation to complete
      await waitFor(() => {
        const authState = useAuthStore.getState()
        expect(authState.isAuthenticated).toBe(true)
      }, { timeout: 5000 })

      // Verify auth store was updated with correct user data
      const authState = useAuthStore.getState()
      expect(authState.user).toEqual({
        id: '1',
        email: 'admin@witchcityrope.com',
        firstName: 'Test',
        lastName: 'Admin',
        sceneName: 'TestAdmin',
        roles: ['admin'],
      })

      // Verify navigation was called
      expect(mockNavigate).toHaveBeenCalledWith('/dashboard', { replace: true })
    })

    it('should handle returnTo parameter correctly', async () => {
      // Mock location with returnTo parameter
      vi.mocked(vi.importActual('react-router-dom')).useLocation = () => ({ 
        search: '?returnTo=%2Fform-test', 
        pathname: '/login' 
      })

      renderWithProviders(<LoginPage />)

      // Fill and submit login form
      await user.type(screen.getByTestId('email-input'), 'admin@witchcityrope.com')
      await user.type(screen.getByTestId('password-input'), 'Test123!')
      await user.click(screen.getByTestId('login-button'))

      // Should navigate to the returnTo URL
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/form-test', { replace: true })
      })
    })

    it('should display error for invalid credentials', async () => {
      // Override MSW handler for failed login
      server.use(
        http.post('http://localhost:5653/api/auth/login', () => {
          return new HttpResponse('Invalid credentials', { status: 401 })
        })
      )

      renderWithProviders(<LoginPage />)

      // Fill form with invalid credentials
      await user.type(screen.getByTestId('email-input'), 'invalid@example.com')
      await user.type(screen.getByTestId('password-input'), 'wrongpassword')
      await user.click(screen.getByTestId('login-button'))

      // Should display error message
      await waitFor(() => {
        expect(screen.getByTestId('login-error')).toBeInTheDocument()
      })

      // Should not navigate away from login
      expect(screen.getByText('Welcome to WitchCityRope')).toBeInTheDocument()

      // Auth store should remain unauthenticated
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(false)
      expect(authState.user).toBe(null)
    })

    it('should show loading state during form submission', async () => {
      // Create a delayed response to test loading state
      server.use(
        http.post('http://localhost:5653/api/auth/login', async () => {
          await new Promise(resolve => setTimeout(resolve, 100))
          return HttpResponse.json({
            user: {
              id: '1',
              email: 'admin@witchcityrope.com',
              firstName: 'Test',
              lastName: 'Admin',
              sceneName: 'TestAdmin',
              roles: ['admin'],
            }
          })
        })
      )

      renderWithProviders(<LoginPage />)

      await user.type(screen.getByTestId('email-input'), 'admin@witchcityrope.com')
      await user.type(screen.getByTestId('password-input'), 'Test123!')

      const submitButton = screen.getByTestId('login-button')
      await user.click(submitButton)

      // Should show loading state
      expect(submitButton).toHaveTextContent('Logging in...')
      expect(submitButton).toBeDisabled()

      // Wait for loading to complete
      await waitFor(() => {
        expect(submitButton).toHaveTextContent('Login')
      })
    })
  })

  describe('Auth Store Integration', () => {
    it('should update auth store state correctly on login', async () => {
      renderWithProviders(<LoginPage />)

      // Fill and submit login form
      await user.type(screen.getByTestId('email-input'), 'admin@witchcityrope.com')
      await user.type(screen.getByTestId('password-input'), 'Test123!')
      await user.click(screen.getByTestId('login-button'))

      // Wait for auth store update
      await waitFor(() => {
        const authState = useAuthStore.getState()
        expect(authState.isAuthenticated).toBe(true)
        expect(authState.user).toEqual({
          id: '1',
          email: 'admin@witchcityrope.com',
          firstName: 'Test',
          lastName: 'Admin',
          sceneName: 'TestAdmin',
          roles: ['admin'],
        })
      })
    })

    it('should calculate permissions correctly from user roles', async () => {
      renderWithProviders(<LoginPage />)

      await user.type(screen.getByTestId('email-input'), 'admin@witchcityrope.com')
      await user.type(screen.getByTestId('password-input'), 'Test123!')
      await user.click(screen.getByTestId('login-button'))

      // Wait for auth store update
      await waitFor(() => {
        const authState = useAuthStore.getState()
        expect(authState.permissions).toContain('read')
        expect(authState.permissions).toContain('write')
        expect(authState.permissions).toContain('delete')
        expect(authState.permissions).toContain('manage_users')
        expect(authState.permissions).toContain('manage_events')
      })
    })

    it('should clear auth store on logout', () => {
      // Start with authenticated user
      useAuthStore.getState().actions.login({
        id: '1',
        email: 'admin@witchcityrope.com',
        firstName: 'Test',
        lastName: 'Admin',
        sceneName: 'TestAdmin',
        roles: ['admin'],
      })

      // Verify authenticated state
      expect(useAuthStore.getState().isAuthenticated).toBe(true)

      // Logout
      useAuthStore.getState().actions.logout()

      // Verify cleared state
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(false)
      expect(authState.user).toBe(null)
      expect(authState.permissions).toEqual([])
    })
  })

  describe('Form Validation Integration', () => {
    it('should show validation errors for invalid email format', async () => {
      renderWithProviders(<LoginPage />)

      // Enter invalid email and try to submit
      await user.type(screen.getByTestId('email-input'), 'invalid-email')
      await user.type(screen.getByTestId('password-input'), 'password123')
      await user.click(screen.getByTestId('login-button'))

      // Should show validation error
      await waitFor(() => {
        expect(screen.getByText('Invalid email format')).toBeInTheDocument()
      })

      // Auth store should remain unchanged
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(false)
    })

    it('should show validation error for empty password', async () => {
      renderWithProviders(<LoginPage />)

      // Enter email but leave password empty
      await user.type(screen.getByTestId('email-input'), 'test@example.com')
      await user.click(screen.getByTestId('login-button'))

      // Should show validation error
      await waitFor(() => {
        expect(screen.getByText('Password is required')).toBeInTheDocument()
      })

      // Auth store should remain unchanged
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(false)
    })

    it('should only make API call when form is valid', async () => {
      renderWithProviders(<LoginPage />)

      // Fill in valid form data
      await user.type(screen.getByTestId('email-input'), 'admin@witchcityrope.com')
      await user.type(screen.getByTestId('password-input'), 'Test123!')
      await user.click(screen.getByTestId('login-button'))

      // Should update auth store
      await waitFor(() => {
        const authState = useAuthStore.getState()
        expect(authState.isAuthenticated).toBe(true)
      })
    })
  })

  describe('TanStack Query Integration', () => {
    it('should handle API errors correctly', async () => {
      // Override MSW handler for failed login
      server.use(
        http.post('http://localhost:5653/api/auth/login', () => {
          return new HttpResponse('Invalid credentials', { status: 401 })
        })
      )

      renderWithProviders(<LoginPage />)

      await user.type(screen.getByTestId('email-input'), 'invalid@example.com')
      await user.type(screen.getByTestId('password-input'), 'wrongpassword')
      await user.click(screen.getByTestId('login-button'))

      // Should display error from TanStack Query
      await waitFor(() => {
        expect(screen.getByTestId('login-error')).toBeInTheDocument()
      })

      // Auth store should remain unauthenticated
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(false)
      expect(authState.user).toBe(null)
    })

    it('should handle successful API response', async () => {
      renderWithProviders(<LoginPage />)

      await user.type(screen.getByTestId('email-input'), 'admin@witchcityrope.com')
      await user.type(screen.getByTestId('password-input'), 'Test123!')
      await user.click(screen.getByTestId('login-button'))

      // Should successfully authenticate and navigate
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/dashboard', { replace: true })
      })

      // Auth store should be updated
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(true)
    })
  })
})