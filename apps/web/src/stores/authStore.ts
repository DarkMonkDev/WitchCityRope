import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';
import type { UserDto } from '../types/shared';

// Auth state interface from functional specification
interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  lastAuthCheck: Date | string | null; // Can be string when loaded from storage
}

// Auth actions interface from functional specification
interface AuthActions {
  login: (user: UserDto) => void;
  logout: () => void;
  updateUser: (updates: Partial<UserDto>) => void;
  checkAuth: () => Promise<void>;
  setLoading: (loading: boolean) => void;
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
        
        // Actions
        actions: {
          login: (user) => set(
            { 
              user,
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
              console.log('🔍 Checking auth with API...');
              // Use fetch directly with credentials to check auth via cookies
              const response = await fetch('/api/auth/user', {
                credentials: 'include'
              });
              
              if (!response.ok) {
                if (response.status === 401) {
                  // User is not authenticated
                  console.log('🔍 Auth check: User not authenticated');
                  set(
                    { 
                      user: null,
                      isAuthenticated: false, 
                      isLoading: false,
                      lastAuthCheck: new Date()
                    },
                    false,
                    'auth/checkAuth/not-authenticated'
                  );
                  return;
                }
                throw new Error('Failed to check authentication');
              }
              
              const data = await response.json();
              
              // Handle nested response structure: { success: true, data: {...} }
              const user = data.data || data;
              
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
              console.log('🔍 Auth check failed:', error.message);
              // Update state directly instead of calling logout action
              set(
                { 
                  user: null,
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
          )
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
          // Don't persist isLoading - should always start as false
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

// Token hooks no longer needed with httpOnly cookies

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