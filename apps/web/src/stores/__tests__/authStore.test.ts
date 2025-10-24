import { describe, it, expect, beforeEach, vi, MockedFunction } from 'vitest';
import { useAuthStore } from '../authStore';
import type { UserDto } from '@witchcityrope/shared-types';

// Environment-based API URL - NO MORE HARD-CODED PORTS
const getApiBaseUrl = () => {
  return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655'
}
const API_BASE_URL = getApiBaseUrl()

// Mock JWT token data (kept for potential future use)
const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock_token_payload';
const mockExpiresAt = new Date(Date.now() + 60 * 60 * 1000); // 1 hour from now

describe('AuthStore', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    // Reset store state - let MSW handle all API calls
    // DO NOT mock global.fetch - it conflicts with MSW handlers
    useAuthStore.getState().actions.logout();
  });

  // Aligned with NSwag generated UserDto structure
  const mockUser: UserDto = {
    id: '1',
    email: 'test@witchcityrope.com',
    sceneName: 'TestUser',
    role: 'GeneralMember',
    isActive: true,
    createdAt: '2025-08-19T10:00:00Z',
    lastLoginAt: '2025-08-19T10:00:00Z',
    // lastLoginAt: '2025-08-19T10:00:00Z' // TODO: Fix type resolution issue
  };

  describe('login action', () => {
    it('should update state correctly on login', () => {
      const { actions } = useAuthStore.getState();
      
      actions.login(mockUser);
      
      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual(mockUser);
      expect(state.isLoading).toBe(false);
      expect(state.lastAuthCheck).toBeInstanceOf(Date);
    });

    it('should handle different scene names correctly', () => {
      const userWithDifferentName: UserDto = {
        ...mockUser,
        sceneName: 'RopeEnthusiast42'
      };
      
      const { actions } = useAuthStore.getState();
      actions.login(userWithDifferentName);
      
      const state = useAuthStore.getState();
      expect(state.user?.sceneName).toBe('RopeEnthusiast42');
      expect(state.isAuthenticated).toBe(true);
    });
  });

  describe('logout action', () => {
    it('should clear state', () => {
      // Setup authenticated state
      const { actions } = useAuthStore.getState();
      actions.login(mockUser);
      
      // Verify initial authenticated state
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
      expect(useAuthStore.getState().user).toEqual(mockUser);
      
      // Call logout
      actions.logout();
      
      // Verify state was cleared immediately (no API call in store action)
      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.user).toBeNull();
      expect(state.isLoading).toBe(false);
      expect(state.lastAuthCheck).toBeNull();
    });
  });

  describe('updateUser action', () => {
    it('should update user data', () => {
      const { actions } = useAuthStore.getState();
      
      // Setup authenticated state
      actions.login(mockUser);
      
      // Update user with new scene name
      actions.updateUser({ sceneName: 'UpdatedSceneName' });
      
      const state = useAuthStore.getState();
      expect(state.user?.sceneName).toBe('UpdatedSceneName');
      expect(state.lastAuthCheck).toBeInstanceOf(Date);
    });

    it('should handle null user gracefully', () => {
      const { actions } = useAuthStore.getState();
      
      // Ensure user is null
      actions.logout();
      
      // Try to update null user
      actions.updateUser({ sceneName: 'UpdatedName' });
      
      const state = useAuthStore.getState();
      expect(state.user).toBeNull();
    });
  });

  describe('checkAuth action', () => {
    it('should set loading true and authenticate if API succeeds', async () => {
      const { actions } = useAuthStore.getState();
      const checkAuthPromise = actions.checkAuth();
      
      // Should set loading immediately
      expect(useAuthStore.getState().isLoading).toBe(true);
      
      await checkAuthPromise;
      
      // Should authenticate user - MSW will return admin user
      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual({
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        firstName: null,
        lastName: null,
        roles: ['Admin'],
        isActive: true,
        createdAt: '2025-08-19T00:00:00Z',
        updatedAt: '2025-08-19T10:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z',
      });
      expect(state.isLoading).toBe(false);
    });

    it('should handle flat response structure', async () => {
      const { actions } = useAuthStore.getState();
      await actions.checkAuth();

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual({
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        firstName: null,
        lastName: null,
        roles: ['Admin'],
        isActive: true,
        createdAt: '2025-08-19T00:00:00Z',
        updatedAt: '2025-08-19T10:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z',
      });
    });

    it('should logout if API fails', async () => {
      // Override MSW handler to return 401 for this test
      const { server } = await import('../../test/setup');
      const { http, HttpResponse } = await import('msw');

      // authStore calls relative URL '/api/auth/user', so override that handler
      server.use(
        http.get('/api/auth/user', () => {
          return new HttpResponse(null, { status: 401 })
        })
      );

      const { actions } = useAuthStore.getState();
      await actions.checkAuth();

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.user).toBeNull();
      expect(state.isLoading).toBe(false);
    });

    it('should handle network errors gracefully', async () => {
      // Override MSW handler to simulate network error for this test
      const { server } = await import('../../test/setup');
      const { http, HttpResponse } = await import('msw');

      // authStore calls relative URL '/api/auth/user', so override that handler
      server.use(
        http.get('/api/auth/user', () => {
          return HttpResponse.error()
        })
      );

      const { actions } = useAuthStore.getState();
      await actions.checkAuth();

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.user).toBeNull();
      expect(state.isLoading).toBe(false);
    });
  });

  describe('setLoading action', () => {
    it('should update loading state', () => {
      const { actions } = useAuthStore.getState();
      
      actions.setLoading(false);
      expect(useAuthStore.getState().isLoading).toBe(false);
      
      actions.setLoading(true);
      expect(useAuthStore.getState().isLoading).toBe(true);
    });
  });

  describe('store state access', () => {
    it('should provide correct state values', () => {
      const { actions } = useAuthStore.getState();
      actions.login(mockUser);
      
      // Test direct state access
      const state = useAuthStore.getState();
      
      expect(state.user).toEqual(mockUser);
      expect(state.isAuthenticated).toBe(true);
      expect(state.isLoading).toBe(false);
      expect(state.user?.sceneName).toBe('TestUser');
      expect(state.user?.email).toBe('test@witchcityrope.com');
    });
  });

  describe('selector hooks', () => {
    it('should provide scene name via direct state access', () => {
      const { actions } = useAuthStore.getState();
      actions.login(mockUser);
      
      // Test selector function directly with state
      const state = useAuthStore.getState();
      const sceneName = state.user?.sceneName || '';
      expect(sceneName).toBe('TestUser');
    });
  });
});