import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { useLogin, useLogout } from '../mutations'
import { useAuthStore } from '../../../../stores/authStore'
import { server } from '../../../../test/setup'
import { http, HttpResponse } from 'msw'

// Mock react-router-dom
const mockNavigate = vi.fn()
vi.mock('react-router-dom', () => ({
  useNavigate: () => mockNavigate,
}))

// Mock global fetch for authStore logout calls
const mockFetch = vi.fn()
global.fetch = mockFetch

// Mock window.location for returnTo parameter testing
const mockLocation = {
  search: '',
}
Object.defineProperty(window, 'location', {
  value: mockLocation,
  writable: true,
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
      {children}
    </QueryClientProvider>
  )
}

describe('useLogin', () => {
  beforeEach(() => {
    // Reset auth store
    useAuthStore.getState().actions.logout()
    mockNavigate.mockClear()
    mockLocation.search = ''
    mockFetch.mockClear()
    // Mock fetch for auth store logout calls to resolve immediately
    mockFetch.mockResolvedValue({
      ok: true,
      status: 200,
    } as Response)
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('should login successfully and update auth store', async () => {
    const { result } = renderHook(() => useLogin(), {
      wrapper: createWrapper(),
    })

    expect(result.current.isPending).toBe(false)
    expect(result.current.isSuccess).toBe(false)

    // Trigger login mutation
    act(() => {
      result.current.mutate({
        email: 'admin@witchcityrope.com',
        password: 'Test123!',
      })
    })

    // Wait a bit for the mutation to start before checking pending state
    await waitFor(() => {
      expect(result.current.isPending || result.current.isSuccess).toBe(true)
    })

    // Wait for mutation to complete
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    // Verify mutation completed successfully
    expect(result.current.isPending).toBe(false)
    expect(result.current.isError).toBe(false)
    expect(result.current.data).toEqual({
      success: true,
      data: {
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        createdAt: '2025-08-19T00:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      },
      message: 'Login successful'
    })

    // Verify auth store was updated
    const authState = useAuthStore.getState()
    expect(authState.isAuthenticated).toBe(true)
    expect(authState.user).toEqual({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      createdAt: '2025-08-19T00:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    })

    // Verify navigation to dashboard
    expect(mockNavigate).toHaveBeenCalledWith('/dashboard', { replace: true })
  })

  it('should handle returnTo parameter in navigation', async () => {
    // Set returnTo parameter
    mockLocation.search = '?returnTo=%2Fprofile'

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

    // Verify navigation to returnTo URL
    expect(mockNavigate).toHaveBeenCalledWith('/profile', { replace: true })
  })

  it('should handle login failure', async () => {
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

    // Verify mutation failed
    expect(result.current.isPending).toBe(false)
    expect(result.current.isSuccess).toBe(false)
    expect(result.current.error).toBeTruthy()

    // Verify auth store was NOT updated
    const authState = useAuthStore.getState()
    expect(authState.isAuthenticated).toBe(false)
    expect(authState.user).toBe(null)

    // Verify no navigation occurred
    expect(mockNavigate).not.toHaveBeenCalled()
  })

  it('should not retry failed login attempts', async () => {
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    
    // Override MSW handler for failed login
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
    
    consoleSpy.mockRestore()
  })

  it('should invalidate queries on successful login', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    })
    
    // Mock invalidateQueries to verify it's called
    const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    )

    const { result } = renderHook(() => useLogin(), { wrapper })

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
})

describe('useLogout', () => {
  beforeEach(() => {
    // Set up authenticated state
    useAuthStore.getState().actions.login({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      createdAt: '2025-08-19T00:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    })
    mockNavigate.mockClear()
    mockFetch.mockClear()
    // Mock fetch for auth store logout calls to resolve immediately
    mockFetch.mockResolvedValue({
      ok: true,
      status: 200,
    } as Response)
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('should logout successfully and clear auth store', async () => {
    const { result } = renderHook(() => useLogout(), {
      wrapper: createWrapper(),
    })

    // Verify initial state is authenticated
    expect(useAuthStore.getState().isAuthenticated).toBe(true)

    act(() => {
      result.current.mutate()
    })

    // Wait for mutation to start or complete
    await waitFor(() => {
      expect(result.current.isPending || result.current.isSuccess).toBe(true)
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    // Verify mutation completed
    expect(result.current.isPending).toBe(false)
    expect(result.current.isError).toBe(false)

    // Verify auth store was cleared
    const authState = useAuthStore.getState()
    expect(authState.isAuthenticated).toBe(false)
    expect(authState.user).toBe(null)

    // Verify navigation to login
    expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true })
  })

  it('should handle logout API failure gracefully', async () => {
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    
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
      result.current.mutate()
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    // Even though API failed, auth store should still be cleared
    const authState = useAuthStore.getState()
    expect(authState.isAuthenticated).toBe(false)
    expect(authState.user).toBe(null)

    // Should still navigate to login
    expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true })
    
    consoleSpy.mockRestore()
  })

  it('should clear query cache on logout', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    })
    
    // Mock clear method to verify it's called
    const clearSpy = vi.spyOn(queryClient, 'clear')

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    )

    const { result } = renderHook(() => useLogout(), { wrapper })

    act(() => {
      result.current.mutate()
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    // Verify query cache was cleared
    expect(clearSpy).toHaveBeenCalled()
  })

  it('should not retry failed logout attempts', async () => {
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    
    const logoutHandler = vi.fn(() => {
      return new HttpResponse('Server error', { status: 500 })
    })
    
    server.use(
      http.post('http://localhost:5653/api/auth/logout', logoutHandler)
    )

    const { result } = renderHook(() => useLogout(), {
      wrapper: createWrapper(),
    })

    act(() => {
      result.current.mutate()
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    // Verify only one API call was made (no retries)
    expect(logoutHandler).toHaveBeenCalledTimes(1)
    
    consoleSpy.mockRestore()
  })
})