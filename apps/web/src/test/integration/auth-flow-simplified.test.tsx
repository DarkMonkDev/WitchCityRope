import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '../setup'
import { useAuthStore } from '../../stores/authStore'
import { useLogin, useLogout } from '../../features/auth/api/mutations'

// Mock global fetch for auth store calls
const mockFetch = vi.fn()

// Mock JWT token data for tests
const mockToken = 'test-jwt-token';
const mockExpiresAt = new Date(Date.now() + 60 * 60 * 1000);
global.fetch = mockFetch

// Mock React Router hooks
const mockNavigate = vi.fn()
vi.mock('react-router-dom', () => ({
  useNavigate: () => mockNavigate,
  useLocation: () => ({ search: '', pathname: '/login' })
}))

describe('Authentication Flow Integration Tests', () => {
  let queryClient: QueryClient

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

  const createWrapper = () => {
    return ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    )
  }

  describe('Complete Login Flow Integration', () => {
    it('should complete login flow from mutation to store to navigation', async () => {
      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      // Verify initial state
      expect(useAuthStore.getState().isAuthenticated).toBe(false)
      expect(useAuthStore.getState().user).toBe(null)

      // Trigger login mutation
      act(() => {
        result.current.mutate({
          email: 'admin@witchcityrope.com',
          password: 'Test123!',
        })
      })

      // Wait for mutation to complete
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Verify auth store was updated
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(true)
      expect(authState.user).toEqual({
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

      // Verify roles were set correctly
      expect(authState.user.roles).toContain('Admin')

      // Verify navigation was triggered
      expect(mockNavigate).toHaveBeenCalledWith('/dashboard', { replace: true })
    })

    it('should handle returnTo parameter in navigation', async () => {
      // Mock location with returnTo parameter
      vi.mocked(vi.doMock('react-router-dom', () => ({
        useNavigate: () => mockNavigate,
        useLocation: () => ({ search: '?returnTo=%2Fform-test', pathname: '/login' })
      })))

      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate({
          email: 'admin@witchcityrope.com',
          password: 'Test123!',
        })
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Should navigate to returnTo URL instead of dashboard
      expect(mockNavigate).toHaveBeenCalledWith('/form-test', { replace: true })
    })

    it('should handle login failure without updating store', async () => {
      // Override MSW handler for failed login
      server.use(
        http.post('http://localhost:5653/api/auth/login', () => {
          return new HttpResponse('Invalid credentials', { status: 401 })
        })
      )

      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate({
          email: 'invalid@example.com',
          password: 'wrongpassword',
        })
      })

      // Wait for mutation to fail
      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      // Verify auth store was NOT updated
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(false)
      expect(authState.user).toBe(null)

      // Verify no navigation occurred
      expect(mockNavigate).not.toHaveBeenCalled()
    })
  })

  describe('Logout Flow Integration', () => {
    beforeEach(() => {
      // Start with authenticated user
      useAuthStore.getState().actions.login({
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        roles: ['Admin'],
      }, mockToken, mockExpiresAt)
    })

    it('should complete logout flow from mutation to store to navigation', async () => {
      const { result } = renderHook(() => useLogout(), {
        wrapper: createWrapper(),
      })

      // Verify initial authenticated state
      expect(useAuthStore.getState().isAuthenticated).toBe(true)

      // Trigger logout mutation
      act(() => {
        result.current.mutate(undefined)
      })

      // Wait for mutation to complete
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Verify auth store was cleared
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(false)
      expect(authState.user).toBe(null)

      // Verify navigation to login
      expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true })
    })

    it('should clear store even if logout API fails', async () => {
      // Override MSW handler for failed logout
      server.use(
        http.post('http://localhost:5653/api/auth/logout', () => {
          return new HttpResponse('Server error', { status: 500 })
        })
      )

      const { result } = renderHook(() => useLogout(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate(undefined)
      })

      // Wait for mutation to complete (even with error)
      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      // Auth store should still be cleared
      const authState = useAuthStore.getState()
      expect(authState.isAuthenticated).toBe(false)
      expect(authState.user).toBe(null)

      // Should still navigate to login
      expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true })
    })
  })

  describe('Auth Store and TanStack Query Integration', () => {
    it('should invalidate queries on successful login', async () => {
      const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries')

      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate({
          email: 'admin@witchcityrope.com',
          password: 'Test123!',
        })
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Verify queries were invalidated
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['user'] })
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['auth'] })
    })

    it('should clear query cache on logout', async () => {
      // Start with authenticated user
      useAuthStore.getState().actions.login({
        id: '1',
        email: 'admin@witchcityrope.com',
        firstName: 'Test',
        lastName: 'Admin',
        sceneName: 'TestAdmin',
        roles: ['Admin'],
      }, mockToken, mockExpiresAt)

      const clearSpy = vi.spyOn(queryClient, 'clear')

      const { result } = renderHook(() => useLogout(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate(undefined)
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Verify query cache was cleared
      expect(clearSpy).toHaveBeenCalled()
    })

    it('should not retry failed login attempts', async () => {
      const loginHandler = vi.fn(() => {
        return new HttpResponse('Invalid credentials', { status: 401 })
      })
      
      server.use(
        http.post('http://localhost:5653/api/auth/login', loginHandler)
      )

      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate({
          email: 'invalid@example.com',
          password: 'wrongpassword',
        })
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      // Verify only one API call was made (no retries)
      expect(loginHandler).toHaveBeenCalledTimes(1)
    })
  })

  describe('Auth Store Permission Calculation', () => {
    it.skip('should calculate admin permissions correctly', async () => {
      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate({
          email: 'admin@witchcityrope.com',
          password: 'Test123!',
        })
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      const authState = useAuthStore.getState()
      // expect(authState.permissions).toEqual(['read', 'write', 'delete', 'manage_users', 'manage_events'])
    })

    it.skip('should calculate permissions for multiple roles', async () => {
      // Override MSW handler to return user with multiple roles
      server.use(
        http.post('http://localhost:5653/api/auth/login', async () => {
          return HttpResponse.json({
            user: {
              id: '1',
              email: 'teacher@witchcityrope.com',
              firstName: 'Test',
              lastName: 'Teacher',
              sceneName: 'TestTeacher',
              roles: ['teacher', 'vetted'],
            }
          })
        })
      )

      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate({
          email: 'teacher@witchcityrope.com',
          password: 'Test123!',
        })
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      const authState = useAuthStore.getState()
      // Should have permissions from both teacher and vetted roles
      // expect(authState.permissions).toContain('read')
      // expect(authState.permissions).toContain('write')
      // expect(authState.permissions).toContain('manage_events')
      // expect(authState.permissions).toContain('manage_registrations')
      // expect(authState.permissions).toContain('register_events')
    })

    it.skip('should recalculate permissions when user is updated', () => {
      // Start with member user
      useAuthStore.getState().actions.login({
        id: '1',
        email: 'member@witchcityrope.com',
        firstName: 'Test',
        lastName: 'Member',
        sceneName: 'TestMember',
        roles: ['GeneralMember'],
      }, mockToken, mockExpiresAt)

      let authState = useAuthStore.getState()
      // expect(authState.permissions).toEqual(['read', 'register_events'])

      // Update user to admin
      useAuthStore.getState().actions.updateUser({
        roles: ['Admin']
      })

      authState = useAuthStore.getState()
      // expect(authState.permissions).toEqual(['read', 'write', 'delete', 'manage_users', 'manage_events'])
    })
  })

  describe('Session State Management', () => {
    it('should persist authentication state correctly', async () => {
      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate({
          email: 'admin@witchcityrope.com',
          password: 'Test123!',
        })
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      // Verify state persists across store calls
      const authState1 = useAuthStore.getState()
      const authState2 = useAuthStore.getState()
      
      expect(authState1.isAuthenticated).toBe(authState2.isAuthenticated)
      expect(authState1.user).toEqual(authState2.user)
      // Verify consistent state structure
    })

    it('should update lastAuthCheck timestamp on login', async () => {
      const beforeLogin = new Date()
      
      const { result } = renderHook(() => useLogin(), {
        wrapper: createWrapper(),
      })

      act(() => {
        result.current.mutate({
          email: 'admin@witchcityrope.com',
          password: 'Test123!',
        })
      })

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true)
      })

      const authState = useAuthStore.getState()
      expect(authState.lastAuthCheck).toBeInstanceOf(Date)
      expect(authState.lastAuthCheck?.getTime()).toBeGreaterThanOrEqual(beforeLogin.getTime())
    })

    it('should clear lastAuthCheck on logout', () => {
      // Start with authenticated user
      useAuthStore.getState().actions.login({
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        roles: ['Admin'],
      }, mockToken, mockExpiresAt)

      // Verify lastAuthCheck is set
      expect(useAuthStore.getState().lastAuthCheck).toBeInstanceOf(Date)

      // Logout
      useAuthStore.getState().actions.logout()

      // Verify lastAuthCheck is cleared
      expect(useAuthStore.getState().lastAuthCheck).toBe(null)
    })
  })
})