import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';
import type { UserDto } from '@witchcityrope/shared-types';

// Auth state interface from functional specification
interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  lastAuthCheck: Date | null;
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
        isLoading: true,
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
            set({ isLoading: true }, false, 'auth/checkAuth/start');
            
            try {
              // Check if we have a valid token first
              const currentState = get();
              if (!currentState.token || get().actions.isTokenExpired()) {
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
              
              // Import api client dynamically to avoid circular dependency
              const { apiClient } = await import('../lib/api/client');
              const response = await apiClient.get('/api/auth/user');
              
              // Handle nested response structure: { success: true, data: {...} }
              const user = response.data.data || response.data;
              
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
              console.error('Auth check failed:', error);
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
          lastAuthCheck: state.lastAuthCheck
          // NEVER persist token or tokenExpiresAt for security
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

// DEPRECATED: Use individual selectors above to prevent infinite loops
// This composite hook creates new objects on every render - DO NOT USE
// export const useAuth = () => ({ ... })

// Export the store itself for testing
export { useAuthStore };

// Export types for use in other components
export type { UserDto, AuthState, AuthActions };