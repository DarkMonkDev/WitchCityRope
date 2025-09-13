import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';
import type { UserDto } from '../types/shared';

// Auth state interface from functional specification
interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  lastAuthCheck: Date | string | null; // Can be string when loaded from storage
  token: string | null;
  tokenExpiresAt: Date | null;
}

// Auth actions interface from functional specification
interface AuthActions {
  login: (user: UserDto, token: string, expiresAt: Date) => void;
  logout: () => void;
  updateUser: (updates: Partial<UserDto>) => void;
  checkAuth: () => Promise<void>;
  setLoading: (loading: boolean) => void;
  getToken: () => string | null;
  isTokenExpired: () => boolean;
}

// Combined store type
type AuthStore = AuthState & { actions: AuthActions };


// Zustand auth store implementation following functional specification pattern
const useAuthStore = create<AuthStore>()(
  devtools(
    persist(
      (set, get) => ({
        // State
        user: null,
        isAuthenticated: false,
        isLoading: false, // Changed to false to prevent initial render loops
        lastAuthCheck: null,
        token: null,
        tokenExpiresAt: null,
        
        // Actions
        actions: {
          login: (user, token, expiresAt) => set(
            { 
              user, 
              token,
              tokenExpiresAt: expiresAt,
              isAuthenticated: true, 
              isLoading: false,
              lastAuthCheck: new Date()
            },
            false,
            'auth/login'
          ),
          
          logout: () => set(
            { 
              user: null,
              token: null,
              tokenExpiresAt: null,
              isAuthenticated: false, 
              isLoading: false,
              lastAuthCheck: null
            },
            false,
            'auth/logout'
          ),
          
          updateUser: (updates) => set(
            (state) => ({ 
              user: state.user ? { ...state.user, ...updates } : null,
              lastAuthCheck: new Date()
            }),
            false,
            'auth/updateUser'
          ),
          
          checkAuth: async () => {
            const currentState = get();
            
            // Prevent repeated auth checks within a short time period
            if (currentState.lastAuthCheck) {
              // Handle case where lastAuthCheck might be a string from localStorage
              const lastCheckTime = typeof currentState.lastAuthCheck === 'string' 
                ? new Date(currentState.lastAuthCheck).getTime()
                : currentState.lastAuthCheck.getTime();
              const timeSinceLastCheck = Date.now() - lastCheckTime;
              if (timeSinceLastCheck < 30000) { // 30 seconds cooldown to prevent auth check on every action
                console.log('🔍 Auth check skipped - recent check within 30 seconds');
                return;
              }
            }
            
            // Skip auth check if already loading to prevent concurrent calls
            if (currentState.isLoading) {
              console.log('🔍 Auth check skipped - already loading');
              return;
            }
            
            set({ isLoading: true }, false, 'auth/checkAuth/start');
            
            try {
              // Check if we have a valid token first
              if (!currentState.token || get().actions.isTokenExpired()) {
                console.log('🔍 No valid token found - setting unauthenticated state');
                // No valid token, user is not authenticated
                set(
                  { 
                    user: null,
                    token: null,
                    tokenExpiresAt: null,
                    isAuthenticated: false, 
                    isLoading: false,
                    lastAuthCheck: new Date()
                  },
                  false,
                  'auth/checkAuth/no-token'
                );
                return;
              }
              
              console.log('🔍 Checking auth with API...');
              // Import api client dynamically to avoid circular dependency
              const { apiClient } = await import('../lib/api/client');
              const response = await apiClient.get('/api/auth/user');
              
              // Handle nested response structure: { success: true, data: {...} }
              const user = response.data.data || response.data;
              
              console.log('🔍 Auth check successful:', user.sceneName);
              // Update state directly instead of calling login action to avoid circular updates
              set(
                { 
                  user, 
                  isAuthenticated: true, 
                  isLoading: false,
                  lastAuthCheck: new Date()
                },
                false,
                'auth/checkAuth/success'
              );
            } catch (error) {
              console.log('🔍 Auth check failed:', error.response?.status || error.message);
              // Update state directly instead of calling logout action
              set(
                { 
                  user: null,
                  token: null,
                  tokenExpiresAt: null,
                  isAuthenticated: false, 
                  isLoading: false,
                  lastAuthCheck: new Date()
                },
                false,
                'auth/checkAuth/failed'
              );
            }
          },
          

          setLoading: (loading) => set(
            { isLoading: loading },
            false,
            'auth/setLoading'
          ),
          
          getToken: () => {
            const state = get();
            if (!state.token || get().actions.isTokenExpired()) {
              return null;
            }
            return state.token;
          },
          
          isTokenExpired: () => {
            const state = get();
            if (!state.tokenExpiresAt) return true;
            return new Date() >= state.tokenExpiresAt;
          }
        }
      }),
      {
        name: 'auth-store',
        storage: createJSONStorage(() => sessionStorage), // Use sessionStorage for security
        partialize: (state) => ({
          // Only persist essential, non-sensitive data
          user: state.user,
          isAuthenticated: state.isAuthenticated,
          lastAuthCheck: state.lastAuthCheck,
          // TEMPORARY FIX: Persist token for event updates to work
          // TODO: Move to httpOnly cookie-based authentication properly
          token: state.token,
          tokenExpiresAt: state.tokenExpiresAt
          // Don't persist isLoading - should always start as true for auth check
        })
      }
    ),
    { 
      name: 'auth-store',
      enabled: process.env.NODE_ENV === 'development'
    }
  )
);

// Individual selector hooks for performance optimization - prevents infinite loops
export const useUser = () => useAuthStore((state) => state.user);
export const useIsAuthenticated = () => useAuthStore((state) => state.isAuthenticated);
export const useIsLoading = () => useAuthStore((state) => state.isLoading);
export const useAuthActions = () => useAuthStore((state) => state.actions);
export const useUserSceneName = () => useAuthStore((state) => state.user?.sceneName || '');
export const useToken = () => useAuthStore((state) => state.actions.getToken());
export const useIsTokenExpired = () => useAuthStore((state) => state.actions.isTokenExpired());

// Composite hook for components that need multiple auth values
// Note: This creates new objects on every render, use individual selectors when possible
export const useAuth = () => {
  const user = useAuthStore((state) => state.user);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const isLoading = useAuthStore((state) => state.isLoading);
  
  return {
    user,
    isAuthenticated,
    isLoading
  };
};

// Export the store itself for testing
export { useAuthStore };

// Expose auth store to window for API client access (development only)
if (typeof window !== 'undefined' && process.env.NODE_ENV === 'development') {
  (window as any).__AUTH_STORE__ = useAuthStore;
}

// Export types for use in other components
export type { UserDto, AuthState, AuthActions };