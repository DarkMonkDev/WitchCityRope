import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';

// User interface matching our API structure
interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  sceneName?: string;
  roles: string[];
}

// Auth state interface from functional specification
interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  permissions: string[];
  lastAuthCheck: Date | null;
}

// Auth actions interface from functional specification
interface AuthActions {
  login: (user: User) => void;
  logout: () => void;
  updateUser: (updates: Partial<User>) => void;
  checkAuth: () => Promise<void>;
  refreshAuth: () => Promise<void>;
  setLoading: (loading: boolean) => void;
}

// Combined store type
type AuthStore = AuthState & { actions: AuthActions };

// Helper function to calculate permissions from roles
const calculatePermissions = (roles: string[]): string[] => {
  const permissionMap: Record<string, string[]> = {
    admin: ['read', 'write', 'delete', 'manage_users', 'manage_events'],
    teacher: ['read', 'write', 'manage_events', 'manage_registrations'],
    vetted: ['read', 'write', 'register_events'],
    member: ['read', 'register_events'],
    guest: ['read']
  };

  const permissions = new Set<string>();
  roles.forEach(role => {
    const rolePermissions = permissionMap[role] || [];
    rolePermissions.forEach(permission => permissions.add(permission));
  });

  return Array.from(permissions);
};

// Zustand auth store implementation following functional specification pattern
const useAuthStore = create<AuthStore>()(
  devtools(
    persist(
      (set, get) => ({
        // State
        user: null,
        isAuthenticated: false,
        isLoading: true,
        permissions: [],
        lastAuthCheck: null,
        
        // Actions
        actions: {
          login: (user) => set(
            { 
              user, 
              isAuthenticated: true, 
              isLoading: false,
              permissions: calculatePermissions(user.roles),
              lastAuthCheck: new Date()
            },
            false,
            'auth/login'
          ),
          
          logout: () => {
            // Call API logout endpoint
            fetch('/api/auth/logout', {
              method: 'POST',
              credentials: 'include'
            }).finally(() => {
              set(
                { 
                  user: null, 
                  isAuthenticated: false, 
                  permissions: [],
                  isLoading: false,
                  lastAuthCheck: null
                },
                false,
                'auth/logout'
              );
            });
          },
          
          updateUser: (updates) => set(
            (state) => ({ 
              user: state.user ? { ...state.user, ...updates } : null,
              permissions: state.user 
                ? calculatePermissions({ ...state.user, ...updates }.roles) 
                : [],
              lastAuthCheck: new Date()
            }),
            false,
            'auth/updateUser'
          ),
          
          checkAuth: async () => {
            set({ isLoading: true }, false, 'auth/checkAuth/start');
            
            try {
              const response = await fetch('/api/auth/me', {
                credentials: 'include',
                headers: {
                  'Accept': 'application/json'
                }
              });
              
              if (response.ok) {
                const user = await response.json();
                get().actions.login(user);
              } else {
                get().actions.logout();
              }
            } catch (error) {
              console.error('Auth check failed:', error);
              get().actions.logout();
            }
          },
          
          refreshAuth: async () => {
            try {
              const response = await fetch('/api/auth/refresh', {
                method: 'POST',
                credentials: 'include'
              });
              
              if (response.ok) {
                const user = await response.json();
                get().actions.login(user);
              }
            } catch (error) {
              console.error('Auth refresh failed:', error);
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
          permissions: state.permissions,
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
export const useUserRoles = () => useAuthStore((state) => state.user?.roles || []);
export const usePermissions = () => useAuthStore((state) => state.permissions);

// Helper hook for role-based rendering
export const useHasRole = (requiredRole: string) => 
  useAuthStore((state) => state.user?.roles.includes(requiredRole) || false);

// Helper hook for permission-based rendering
export const useHasPermission = (requiredPermission: string) =>
  useAuthStore((state) => state.permissions.includes(requiredPermission));

// Export the store itself for testing
export { useAuthStore };

// Export types for use in other components
export type { User, AuthState, AuthActions };