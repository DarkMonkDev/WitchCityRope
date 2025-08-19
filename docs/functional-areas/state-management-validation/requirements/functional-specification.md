# Functional Specification: Zustand State Management Validation
<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview
This specification implements Zustand-based state management patterns for the WitchCityRope React application migration. The validation establishes authentication state, UI preferences, form persistence, and real-time update patterns while maintaining strict security with httpOnly cookie authentication and achieving <16ms performance targets.

## Architecture

### React Application Architecture
**CRITICAL**: This is a React + Vite application with .NET API backend:
- **Web Service** (React + Vite): UI/Auth at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5653
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### State Management Architecture
```
/apps/web/src/stores/
├── authStore.ts           # Authentication state with httpOnly cookie validation
├── uiStore.ts             # Global UI state with session persistence
├── formStore.ts           # Form state persistence across navigation
├── eventStore.ts          # Event-specific state for real-time updates
├── slices/
│   ├── userSlice.ts       # User profile management slice
│   ├── settingsSlice.ts   # Application settings slice
│   └── notificationSlice.ts # Notification queue slice
└── middleware/
    ├── persist.ts         # Custom persistence configuration
    ├── logger.ts          # Development logging middleware
    └── devtools.ts        # DevTools configuration
```

### Service Architecture
- **React Application**: Components subscribe to Zustand stores for state
- **API Integration**: HTTP calls to .NET API with httpOnly cookie authentication
- **No Token Storage**: Authentication tokens remain in httpOnly cookies only
- **Performance Target**: <16ms state updates, <85ms average update time

## Data Models

### Core Store Interfaces
```typescript
// Authentication Store
interface User {
  id: string;
  email: string;
  roles: ('admin' | 'teacher' | 'vetted' | 'member' | 'guest')[];
  firstName: string;
  lastName: string;
  consentLevel: 'none' | 'basic' | 'full';
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  permissions: string[];
  lastAuthCheck: Date | null;
}

interface AuthActions {
  login: (user: User) => void;
  logout: () => void;
  updateUser: (updates: Partial<User>) => void;
  checkAuth: () => Promise<void>;
  refreshAuth: () => Promise<void>;
}

// UI Store
interface UIState {
  sidebarOpen: boolean;
  currentModal: string | null;
  theme: 'light' | 'dark';
  notifications: Notification[];
  breadcrumbs: BreadcrumbItem[];
}

interface UIActions {
  toggleSidebar: () => void;
  openModal: (modalId: string, data?: any) => void;
  closeModal: () => void;
  setTheme: (theme: 'light' | 'dark') => void;
  addNotification: (notification: Notification) => void;
  removeNotification: (id: string) => void;
  setBreadcrumbs: (breadcrumbs: BreadcrumbItem[]) => void;
}

// Form Store
interface FormState {
  eventForm: Partial<EventFormData>;
  userRegistrationForm: Partial<UserRegistrationData>;
  consentForm: Partial<ConsentFormData>;
  isDirty: Record<string, boolean>;
}

interface FormActions {
  updateEventForm: (updates: Partial<EventFormData>) => void;
  clearEventForm: () => void;
  updateUserForm: (updates: Partial<UserRegistrationData>) => void;
  clearUserForm: () => void;
  setFormDirty: (formName: string, isDirty: boolean) => void;
}
```

## API Specifications

### Authentication Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| POST | /api/auth/login | Authenticate user | LoginRequest | User data |
| POST | /api/auth/logout | End user session | None | Success status |
| GET | /api/auth/me | Validate current session | None | User data |
| POST | /api/auth/refresh | Refresh authentication | None | User data |

### State Synchronization Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/users/preferences | Get UI preferences | None | UIPreferences |
| PUT | /api/users/preferences | Save UI preferences | UIPreferences | Success status |
| GET | /api/events/user-state | Get user event state | None | UserEventState |

## Component Specifications

### Store Implementation

#### 1. Authentication Store
```typescript
// /apps/web/src/stores/authStore.ts
import { create } from 'zustand';
import { devtools } from 'zustand/middleware';

type AuthStore = AuthState & { actions: AuthActions };

const useAuthStore = create<AuthStore>()(
  devtools(
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
        }
      }
    }),
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

// Helper function for role-based rendering
export const useHasRole = (requiredRole: string) => 
  useAuthStore((state) => state.user?.roles.includes(requiredRole) || false);
```

#### 2. UI State Store
```typescript
// /apps/web/src/stores/uiStore.ts
import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';

type UIStore = UIState & { actions: UIActions };

const useUIStore = create<UIStore>()(
  devtools(
    persist(
      (set, get) => ({
        // State
        sidebarOpen: true,
        currentModal: null,
        theme: 'light',
        notifications: [],
        breadcrumbs: [],
        
        // Actions
        actions: {
          toggleSidebar: () => set(
            (state) => ({ sidebarOpen: !state.sidebarOpen }),
            false,
            'ui/toggleSidebar'
          ),
          
          openModal: (modalId, data) => set(
            { currentModal: modalId, modalData: data },
            false,
            'ui/openModal'
          ),
          
          closeModal: () => set(
            { currentModal: null, modalData: undefined },
            false,
            'ui/closeModal'
          ),
          
          setTheme: (theme) => {
            // Apply theme to document
            document.documentElement.setAttribute('data-theme', theme);
            set({ theme }, false, 'ui/setTheme');
          },
          
          addNotification: (notification) => set(
            (state) => ({
              notifications: [...state.notifications, {
                ...notification,
                id: notification.id || crypto.randomUUID(),
                timestamp: new Date()
              }]
            }),
            false,
            'ui/addNotification'
          ),
          
          removeNotification: (id) => set(
            (state) => ({
              notifications: state.notifications.filter(n => n.id !== id)
            }),
            false,
            'ui/removeNotification'
          ),
          
          setBreadcrumbs: (breadcrumbs) => set(
            { breadcrumbs },
            false,
            'ui/setBreadcrumbs'
          )
        }
      }),
      {
        name: 'ui-store',
        storage: createJSONStorage(() => sessionStorage),
        partialize: (state) => ({
          sidebarOpen: state.sidebarOpen,
          theme: state.theme
          // Don't persist notifications, modals, or breadcrumbs
        })
      }
    ),
    { 
      name: 'ui-store',
      enabled: process.env.NODE_ENV === 'development'
    }
  )
);

// Selector hooks
export const useSidebar = () => useUIStore((state) => ({
  isOpen: state.sidebarOpen,
  toggle: state.actions.toggleSidebar
}));

export const useModal = () => useUIStore((state) => ({
  currentModal: state.currentModal,
  open: state.actions.openModal,
  close: state.actions.closeModal
}));

export const useTheme = () => useUIStore((state) => ({
  theme: state.theme,
  setTheme: state.actions.setTheme
}));

export const useNotifications = () => useUIStore((state) => ({
  notifications: state.notifications,
  add: state.actions.addNotification,
  remove: state.actions.removeNotification
}));
```

#### 3. Form Persistence Store
```typescript
// /apps/web/src/stores/formStore.ts
import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';

type FormStore = FormState & { actions: FormActions };

const useFormStore = create<FormStore>()(
  devtools(
    persist(
      (set, get) => ({
        // State
        eventForm: {},
        userRegistrationForm: {},
        consentForm: {},
        isDirty: {},
        
        // Actions
        actions: {
          updateEventForm: (updates) => set(
            (state) => ({
              eventForm: { ...state.eventForm, ...updates },
              isDirty: { ...state.isDirty, eventForm: true }
            }),
            false,
            'forms/updateEventForm'
          ),
          
          clearEventForm: () => set(
            (state) => ({
              eventForm: {},
              isDirty: { ...state.isDirty, eventForm: false }
            }),
            false,
            'forms/clearEventForm'
          ),
          
          updateUserForm: (updates) => set(
            (state) => ({
              userRegistrationForm: { ...state.userRegistrationForm, ...updates },
              isDirty: { ...state.isDirty, userRegistrationForm: true }
            }),
            false,
            'forms/updateUserForm'
          ),
          
          clearUserForm: () => set(
            (state) => ({
              userRegistrationForm: {},
              isDirty: { ...state.isDirty, userRegistrationForm: false }
            }),
            false,
            'forms/clearUserForm'
          ),
          
          setFormDirty: (formName, isDirty) => set(
            (state) => ({
              isDirty: { ...state.isDirty, [formName]: isDirty }
            }),
            false,
            'forms/setDirty'
          )
        }
      }),
      {
        name: 'form-store',
        storage: createJSONStorage(() => sessionStorage),
        partialize: (state) => {
          // Only persist non-sensitive form data
          const { consentForm, ...persistedState } = state;
          return persistedState;
        }
      }
    ),
    { 
      name: 'form-store',
      enabled: process.env.NODE_ENV === 'development'
    }
  )
);

// React Hook Form integration
export const useEventFormWithPersistence = () => {
  const { eventForm, actions } = useFormStore();
  
  const form = useForm<EventFormData>({
    defaultValues: eventForm,
    mode: 'onChange'
  });
  
  // Persist form changes to Zustand
  const watchedValues = form.watch();
  
  useEffect(() => {
    const subscription = form.watch((data) => {
      actions.updateEventForm(data);
    });
    
    return () => subscription.unsubscribe();
  }, [form, actions]);
  
  return {
    ...form,
    clearPersistedData: actions.clearEventForm,
    isDirty: useFormStore((state) => state.isDirty.eventForm)
  };
};
```

### State Management Integration Patterns

#### 1. TanStack Query Integration
```typescript
// /apps/web/src/hooks/useEvents.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useEventStore } from '../stores/eventStore';

export const useEvents = (filters: EventFilters) => {
  const { actions } = useEventStore();
  const queryClient = useQueryClient();
  
  const query = useQuery({
    queryKey: ['events', filters],
    queryFn: () => fetchEvents(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
  
  // Optimistic updates mutation
  const updateEventMutation = useMutation({
    mutationFn: updateEvent,
    onMutate: async (newEvent) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['events'] });
      
      // Optimistically update Zustand store
      actions.optimisticUpdate(newEvent);
      
      // Return context for rollback
      const previousEvents = queryClient.getQueryData(['events', filters]);
      return { previousEvents };
    },
    onError: (err, newEvent, context) => {
      // Rollback optimistic update
      if (context?.previousEvents) {
        actions.rollbackUpdate(newEvent.id);
      }
    },
    onSettled: () => {
      // Refresh data
      queryClient.invalidateQueries({ queryKey: ['events'] });
    }
  });
  
  return {
    ...query,
    events: query.data || [],
    updateEvent: updateEventMutation.mutate,
    isUpdating: updateEventMutation.isPending
  };
};
```

#### 2. Real-Time Updates with WebSocket
```typescript
// /apps/web/src/hooks/useRealTimeEvents.ts
export const useRealTimeEvents = () => {
  const { actions } = useEventStore();
  const { isAuthenticated } = useAuth();
  
  useEffect(() => {
    if (!isAuthenticated) return;
    
    const ws = new WebSocket(process.env.VITE_WS_URL);
    
    ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      
      switch (data.type) {
        case 'EVENT_UPDATED':
          actions.updateEvent(data.event);
          break;
        case 'REGISTRATION_COUNT_CHANGED':
          actions.updateRegistrationCount(data.eventId, data.count);
          break;
        case 'USER_REGISTERED':
          actions.addRegistration(data.eventId, data.userId);
          break;
      }
    };
    
    return () => {
      ws.close();
    };
  }, [isAuthenticated, actions]);
};
```

### Performance Optimization Patterns

#### 1. Selective Subscriptions
```typescript
// ❌ Bad - subscribes to entire store, causes unnecessary re-renders
const App = () => {
  const store = useUIStore();
  return <div>{store.sidebarOpen && <Sidebar />}</div>;
};

// ✅ Good - selective subscription
const App = () => {
  const sidebarOpen = useUIStore((state) => state.sidebarOpen);
  return <div>{sidebarOpen && <Sidebar />}</div>;
};

// ✅ Better - custom hook with memoization
const App = () => {
  const { isOpen } = useSidebar();
  return <div>{isOpen && <Sidebar />}</div>;
};
```

#### 2. Shallow Equality for Multiple Values
```typescript
import { useShallow } from 'zustand/react/shallow';

// Multiple values without unnecessary re-renders
const UserProfile = () => {
  const { user, isAuthenticated, permissions } = useAuthStore(
    useShallow((state) => ({
      user: state.user,
      isAuthenticated: state.isAuthenticated,
      permissions: state.permissions
    }))
  );
  
  return (
    <div>
      {isAuthenticated && user && (
        <div>
          <h1>{user.firstName} {user.lastName}</h1>
          <div>Permissions: {permissions.join(', ')}</div>
        </div>
      )}
    </div>
  );
};
```

## Integration Points

### Authentication System
- HTTP-only cookie validation through `/api/auth/me` endpoint
- Automatic token refresh via `/api/auth/refresh`
- Secure logout through `/api/auth/logout`
- Role-based access control integration

### Form Systems
- React Hook Form integration with Zustand persistence
- Session-based form data persistence
- Dirty state tracking for unsaved changes warnings
- Multi-step form state management

### Real-Time Updates
- WebSocket integration for live event updates
- Optimistic updates with rollback capability
- Registration count real-time synchronization
- User activity status updates

### UI Components
- Mantine component integration with Zustand state
- Theme persistence and application
- Modal state management
- Notification queue management

## Security Requirements

### Authentication Security
- No JWT tokens stored in client-side storage
- Authentication state derived from API validation only
- Secure logout clears all authentication state
- Role validation occurs server-side

### Data Protection
- Sensitive form data not persisted in browser storage
- Consent form data handled separately from general forms
- Session-only persistence for temporary UI state
- Development-only DevTools exposure

### XSS Prevention
- No executable code stored in state
- Input sanitization for user-generated content in state
- Secure HTML rendering from state data

## Performance Requirements

### Update Performance
- State updates: <16ms for UI-blocking changes
- Store subscription setup: <1ms
- Memory overhead: <10MB for all stores combined
- Bundle size impact: <5KB additional JavaScript

### Optimization Targets
- Component re-render prevention through selective subscriptions
- Shallow equality checks for multi-value selections
- Memoization for computed state values
- Batched updates for related state changes

### Monitoring
- DevTools integration for development debugging
- Performance metrics tracking in development
- State change history for debugging
- Memory usage monitoring

## Testing Requirements

### Unit Testing
```typescript
// Store unit tests
describe('AuthStore', () => {
  beforeEach(() => {
    useAuthStore.getState().actions.logout();
  });
  
  it('should handle login correctly', () => {
    const mockUser = { id: '1', email: 'test@example.com', roles: ['member'] };
    
    useAuthStore.getState().actions.login(mockUser);
    
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.user).toEqual(mockUser);
    expect(state.permissions).toContain('read');
  });
  
  it('should handle logout correctly', () => {
    // Setup authenticated state
    useAuthStore.getState().actions.login(mockUser);
    
    useAuthStore.getState().actions.logout();
    
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
    expect(state.permissions).toEqual([]);
  });
});
```

### Integration Testing
```typescript
// Component integration with store
describe('LoginForm Integration', () => {
  it('should update auth store on successful login', async () => {
    render(<LoginForm />);
    
    // Mock successful API response
    server.use(
      rest.post('/api/auth/login', (req, res, ctx) => {
        return res(ctx.json({ id: '1', email: 'test@example.com' }));
      })
    );
    
    fireEvent.change(screen.getByLabelText('Email'), {
      target: { value: 'test@example.com' }
    });
    fireEvent.change(screen.getByLabelText('Password'), {
      target: { value: 'password' }
    });
    fireEvent.click(screen.getByRole('button', { name: 'Login' }));
    
    await waitFor(() => {
      const authState = useAuthStore.getState();
      expect(authState.isAuthenticated).toBe(true);
      expect(authState.user?.email).toBe('test@example.com');
    });
  });
});
```

### Performance Testing
```typescript
// Performance benchmarks
describe('Store Performance', () => {
  it('should update state within 16ms', async () => {
    const startTime = performance.now();
    
    useUIStore.getState().actions.toggleSidebar();
    
    const endTime = performance.now();
    expect(endTime - startTime).toBeLessThan(16);
  });
  
  it('should handle 100 notifications without performance degradation', () => {
    const startTime = performance.now();
    
    for (let i = 0; i < 100; i++) {
      useUIStore.getState().actions.addNotification({
        type: 'info',
        message: `Notification ${i}`
      });
    }
    
    const endTime = performance.now();
    expect(endTime - startTime).toBeLessThan(100); // 100ms for 100 operations
  });
});
```

## Migration Requirements

### From React Context
- Replace Context providers with Zustand stores
- Convert useContext hooks to Zustand selectors
- Migrate reducer logic to Zustand actions
- Remove provider component hierarchy

### From Local State
- Identify shared state patterns in existing components
- Convert useState to Zustand for cross-component state
- Maintain local state for component-specific UI

### Development Workflow
- Incremental migration starting with authentication
- Parallel development of new features with Zustand
- Testing integration at each migration step
- Documentation updates for new patterns

## Dependencies

### Required Packages
```json
{
  "zustand": "^5.0.7",
  "immer": "^10.0.3"
}
```

### DevDependencies
```json
{
  "@types/node": "^20.0.0"
}
```

### Configuration Requirements
```typescript
// vite.config.ts - Ensure proper development setup
export default defineConfig({
  define: {
    'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV)
  }
});
```

## Acceptance Criteria

Technical criteria for completion:
- [ ] Authentication store functional with httpOnly cookie validation
- [ ] UI state store with session persistence working
- [ ] Form persistence across navigation functional
- [ ] Real-time updates integration complete
- [ ] Performance benchmarks achieved (<16ms updates)
- [ ] DevTools integration functional in development
- [ ] Security validation complete (no token storage)
- [ ] All unit tests passing (80% coverage minimum)
- [ ] Integration tests with components passing
- [ ] Performance tests meeting requirements
- [ ] Migration path documented
- [ ] Developer documentation complete with examples

## Validation Scenarios

### Scenario 1: User Authentication Flow
1. User accesses protected route without authentication
2. Auth store triggers automatic auth check via `/api/auth/me`
3. Unauthenticated user redirected to login
4. Successful login updates auth store with user data
5. User navigates between pages with persistent auth state
6. Session refresh happens transparently
7. Logout clears all auth state and redirects

### Scenario 2: Form State Persistence
1. User starts filling event creation form
2. Form data automatically persists to session storage
3. User navigates away from form
4. User returns to form with data restored
5. User completes form submission
6. Form state cleared after successful submission

### Scenario 3: Real-Time Event Management
1. Teacher opens event management dashboard
2. Real-time WebSocket connection established
3. Students register for events from other devices
4. Registration counts update automatically in teacher's view
5. Event capacity changes reflected immediately
6. Connection recovery handled gracefully

### Scenario 4: UI State Consistency
1. User configures sidebar to collapsed state
2. UI state persists across browser tabs
3. Theme selection applies globally
4. Modal state managed consistently
5. Notifications queue functions properly
6. Session restore maintains UI preferences

### Scenario 5: Performance Under Load
1. Application loads with multiple concurrent users
2. State updates complete within 16ms
3. Memory usage remains under 10MB overhead
4. Bundle size impact measured at <5KB
5. Component re-renders optimized through selectors
6. Real-time updates don't impact UI responsiveness

## Quality Gate Checklist (95% Required)
- [ ] All store patterns implemented and tested
- [ ] Authentication security validated (no localStorage)
- [ ] Performance benchmarks achieved and documented
- [ ] DevTools integration complete and development-only
- [ ] Form persistence working across navigation
- [ ] Real-time updates functional with error recovery
- [ ] Component integration tested and optimized
- [ ] Memory management validated (cleanup on unmount)
- [ ] Global vs local state decision framework documented
- [ ] Migration documentation complete with examples
- [ ] Security review completed
- [ ] Browser compatibility tested
- [ ] Accessibility considerations addressed
- [ ] Production build tested with DevTools disabled
- [ ] Error boundaries and recovery mechanisms tested

---

*This functional specification establishes production-ready Zustand state management patterns for the WitchCityRope React application, ensuring secure, performant, and maintainable state handling across all features.*