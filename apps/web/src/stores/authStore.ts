import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';

// User interface matching API structure from vertical slice
export interface User {
  id: string;
  email: string;
  sceneName: string;
  createdAt: string;
  lastLoginAt?: string;
}

// Auth state interface from functional specification
interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  lastAuthCheck: Date | null;
}

// Auth actions interface from functional specification
interface AuthActions {
  login: (user: User) => void;
  logout: () => void;
  updateUser: (updates: Partial<User>) => void;
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
        isLoading: true,
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
            set({ isLoading: true }, false, 'auth/checkAuth/start');
            
            try {
              // Import api client dynamically to avoid circular dependency
              const { api } = await import('../api/client');
              const response = await api.get('/api/auth/user');
              
              // Handle nested response structure: { success: true, data: {...} }
              const user = response.data.data || response.data;
              get().actions.login(user);
            } catch (error) {
              console.error('Auth check failed:', error);
              get().actions.logout();
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

// Selector hooks for performance optimization
export const useAuth = () => useAuthStore((state) => ({
  user: state.user,
  isAuthenticated: state.isAuthenticated,
  isLoading: state.isLoading
}));

export const useAuthActions = () => useAuthStore((state) => state.actions);
export const useUserSceneName = () => useAuthStore((state) => state.user?.sceneName || '');

// Export the store itself for testing
export { useAuthStore };

// Export types for use in other components
export type { User, AuthState, AuthActions };