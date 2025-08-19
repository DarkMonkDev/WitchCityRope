import { describe, it, expect, beforeEach, vi, MockedFunction } from 'vitest';
import { useAuthStore, type User } from '../authStore';

const mockFetch = vi.fn() as MockedFunction<typeof fetch>;

describe('AuthStore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    // Mock fetch directly with default success response
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ success: true })
    } as Response);
    global.fetch = mockFetch;
    
    // Reset store state after setting up mocks
    useAuthStore.getState().actions.logout();
  });

  // Aligned with API DTO structure - User interface matches backend response
  const mockUser: User = {
    id: '1',
    email: 'test@witchcityrope.com',
    sceneName: 'TestUser',
    createdAt: '2025-08-19T10:00:00Z',
    lastLoginAt: '2025-08-19T10:00:00Z'
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
      const userWithDifferentName: User = {
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
    it('should clear state and call API logout', async () => {
      // Setup authenticated state
      const { actions } = useAuthStore.getState();
      actions.login(mockUser);
      
      // Mock successful logout
      mockFetch.mockResolvedValueOnce({
        ok: true
      } as Response);
      
      // Call logout
      actions.logout();
      
      // Verify API call
      expect(mockFetch).toHaveBeenCalledWith('/api/auth/logout', {
        method: 'POST',
        credentials: 'include'
      });
      
      // Allow fetch promise to resolve
      await new Promise(resolve => setTimeout(resolve, 0));
      
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
      // Mock successful auth check with nested response structure
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ 
          success: true, 
          data: mockUser 
        })
      } as Response);
      
      const { actions } = useAuthStore.getState();
      const checkAuthPromise = actions.checkAuth();
      
      // Should set loading immediately
      expect(useAuthStore.getState().isLoading).toBe(true);
      
      await checkAuthPromise;
      
      // Should authenticate user
      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual(mockUser);
      expect(state.isLoading).toBe(false);
    });

    it('should handle flat response structure', async () => {
      // Mock successful auth check with flat response structure
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockUser
      } as Response);
      
      const { actions } = useAuthStore.getState();
      await actions.checkAuth();
      
      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual(mockUser);
    });

    it('should logout if API fails', async () => {
      // Mock failed auth check
      mockFetch.mockResolvedValueOnce({
        ok: false
      } as Response);
      
      const { actions } = useAuthStore.getState();
      await actions.checkAuth();
      
      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.user).toBeNull();
      expect(state.isLoading).toBe(false);
    });

    it('should handle network errors gracefully', async () => {
      // Mock network error
      mockFetch.mockRejectedValueOnce(new Error('Network error'));
      
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
    it('should provide scene name via useUserSceneName', () => {
      const { actions } = useAuthStore.getState();
      actions.login(mockUser);
      
      const sceneName = useAuthStore((state) => state.user?.sceneName || '');
      expect(sceneName).toBe('TestUser');
    });
  });
});