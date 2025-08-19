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

  const mockUser: User = {
    id: '1',
    email: 'test@witchcityrope.com',
    firstName: 'Test',
    lastName: 'User',
    sceneName: 'TestUser',
    roles: ['member']
  };

  describe('login action', () => {
    it('should update state correctly on login', () => {
      const { actions } = useAuthStore.getState();
      
      actions.login(mockUser);
      
      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual(mockUser);
      expect(state.isLoading).toBe(false);
      expect(state.permissions).toContain('read');
      expect(state.permissions).toContain('register_events');
      expect(state.lastAuthCheck).toBeInstanceOf(Date);
    });

    it('should calculate admin permissions correctly', () => {
      const adminUser: User = {
        ...mockUser,
        roles: ['admin']
      };
      
      const { actions } = useAuthStore.getState();
      actions.login(adminUser);
      
      const state = useAuthStore.getState();
      expect(state.permissions).toContain('read');
      expect(state.permissions).toContain('write');
      expect(state.permissions).toContain('delete');
      expect(state.permissions).toContain('manage_users');
      expect(state.permissions).toContain('manage_events');
    });

    it('should calculate teacher permissions correctly', () => {
      const teacherUser: User = {
        ...mockUser,
        roles: ['teacher']
      };
      
      const { actions } = useAuthStore.getState();
      actions.login(teacherUser);
      
      const state = useAuthStore.getState();
      expect(state.permissions).toContain('read');
      expect(state.permissions).toContain('write');
      expect(state.permissions).toContain('manage_events');
      expect(state.permissions).toContain('manage_registrations');
      expect(state.permissions).not.toContain('manage_users');
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
      expect(state.permissions).toEqual([]);
      expect(state.isLoading).toBe(false);
      expect(state.lastAuthCheck).toBeNull();
    });
  });

  describe('updateUser action', () => {
    it('should update user data and recalculate permissions', () => {
      const { actions } = useAuthStore.getState();
      
      // Setup authenticated state
      actions.login(mockUser);
      
      // Update user with new role
      actions.updateUser({ roles: ['teacher'] });
      
      const state = useAuthStore.getState();
      expect(state.user?.roles).toEqual(['teacher']);
      expect(state.permissions).toContain('manage_events');
      expect(state.lastAuthCheck).toBeInstanceOf(Date);
    });

    it('should handle null user gracefully', () => {
      const { actions } = useAuthStore.getState();
      
      // Ensure user is null
      actions.logout();
      
      // Try to update null user
      actions.updateUser({ firstName: 'Updated' });
      
      const state = useAuthStore.getState();
      expect(state.user).toBeNull();
      expect(state.permissions).toEqual([]);
    });
  });

  describe('checkAuth action', () => {
    it('should set loading true and authenticate if API succeeds', async () => {
      // Mock successful auth check
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockUser
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
      expect(state.user?.roles).toEqual(['member']);
      expect(state.permissions).toContain('read');
      expect(state.permissions).toContain('register_events');
    });
  });
});