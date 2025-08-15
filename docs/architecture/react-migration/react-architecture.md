# React Architecture Research

*Generated on August 13, 2025*

## Overview
This document researches modern React architecture patterns, including state management, routing, and build tools for the WitchCityRope migration from Blazor Server to React.

## Current WitchCityRope Architecture

### Existing Blazor Implementation
- **Architecture**: Server-side rendering with InteractiveServerRenderMode
- **State Management**: Server-side state with component lifecycle
- **Routing**: Blazor Router with attribute-based routing
- **Build System**: MSBuild with Razor compilation
- **Component Structure**: Feature-organized components with shared layouts
- **Styling**: CSS-in-Razor with scoped styles

### Component Organization
```
Features/
├── Admin/           # Administrative features
├── Auth/            # Authentication flows
├── Events/          # Event management
├── Members/         # Member dashboard and features
├── Public/          # Public content pages
├── Vetting/         # Member vetting system
└── Test/            # Development test components

Shared/
├── Components/      # Reusable UI components
├── Layouts/         # Application layouts
└── Validation/      # Form validation components
```

## State Management Analysis (2025)

### Decision Framework for WitchCityRope

Based on the application's complexity and requirements, here's the recommended approach:

#### **Recommended: Hybrid Approach with Zustand + React Query**

**Rationale**: WitchCityRope is a medium-to-large application with complex state needs but community-focused (not enterprise scale).

**Architecture:**
```typescript
// Global State: Zustand
const useAuthStore = create((set, get) => ({
  user: null,
  isAuthenticated: false,
  roles: [],
  login: async (credentials) => { /* */ },
  logout: () => { /* */ },
  hasRole: (role) => get().roles.includes(role)
}));

// Server State: React Query
const { data: events, isLoading } = useQuery({
  queryKey: ['events'],
  queryFn: fetchEvents,
  staleTime: 5 * 60 * 1000 // 5 minutes
});

// Local State: useState/useReducer
const [formData, setFormData] = useState(initialState);
```

### State Management Comparison

#### **1. Zustand (Recommended for WitchCityRope)**
**Pros:**
- Lightweight (~1KB gzipped)
- No provider needed - use anywhere
- Simple API with minimal boilerplate
- Excellent TypeScript support
- Good performance characteristics
- Easy to learn and implement

**Cons:**
- Smaller ecosystem than Redux
- No enforced patterns (can lead to inconsistency)
- Less suitable for very large enterprise applications

**Best Use Cases:**
- Medium-sized applications (like WitchCityRope)
- Feature-specific stores (authStore, uiStore, eventStore)
- Teams wanting simplicity with good performance

**Implementation Example:**
```typescript
// Auth store
const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  isAuthenticated: false,
  login: async (credentials) => {
    const user = await authService.login(credentials);
    set({ user, isAuthenticated: true });
  },
  logout: () => set({ user: null, isAuthenticated: false }),
  hasRole: (role) => get().user?.roles.includes(role) ?? false
}));

// UI state store
const useUIStore = create<UIState>((set) => ({
  sidebarOpen: false,
  currentTheme: 'light',
  toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
  setTheme: (theme) => set({ currentTheme: theme })
}));
```

#### **2. Redux Toolkit (Alternative for Enterprise Growth)**
**Pros:**
- Industry standard with extensive ecosystem
- Excellent DevTools and debugging
- Predictable state updates
- Great for large teams
- Time travel debugging

**Cons:**
- More boilerplate than alternatives
- Steeper learning curve
- Might be overkill for WitchCityRope's current needs

**When to Consider:**
- If the application grows significantly
- Need for complex middleware
- Large development team
- Enterprise compliance requirements

#### **3. Context API (For Specific Use Cases)**
**Pros:**
- Built into React
- No external dependencies
- Simple for small state needs

**Cons:**
- Performance issues with frequent updates
- Can cause unnecessary re-renders
- Not suitable for complex state logic

**Recommended Use in WitchCityRope:**
```typescript
// Theme context (infrequent updates)
const ThemeContext = createContext();

// User preferences (stable data)
const UserPreferencesContext = createContext();
```

### **React Query for Server State (Essential)**

React Query is **mandatory** for the WitchCityRope migration due to:

```typescript
// Event management with caching
const { data: events, isLoading, error } = useQuery({
  queryKey: ['events', filters],
  queryFn: () => eventsAPI.getEvents(filters),
  staleTime: 5 * 60 * 1000,
  refetchOnWindowFocus: false
});

// User management with mutations
const updateUserMutation = useMutation({
  mutationFn: usersAPI.updateUser,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['users'] });
    toast.success('User updated successfully');
  }
});
```

**Benefits for WitchCityRope:**
- Automatic caching for event lists, user data
- Optimistic updates for better UX
- Background refetching for real-time data
- Error handling and retry logic
- Perfect for admin dashboards and member features

## Build Tools Analysis (2025)

### **Recommended: Vite (Clear Winner)**

**Why Vite for WitchCityRope:**
- **Speed**: Lightning-fast development server and HMR
- **Simplicity**: Minimal configuration required
- **Modern**: Built for ES modules and modern browsers
- **TypeScript**: Excellent out-of-the-box TypeScript support
- **Community**: Growing rapidly, industry momentum

**Performance Benefits:**
```bash
# Development server start times
Create React App: ~15-30 seconds
Vite: ~1-3 seconds

# Hot Module Replacement
Webpack: ~2-5 seconds
Vite: ~200-500ms
```

**Configuration Example:**
```typescript
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
      '@components': resolve(__dirname, 'src/components'),
      '@features': resolve(__dirname, 'src/features')
    }
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5653',
        changeOrigin: true
      }
    }
  }
});
```

### **Alternative: Next.js (If SSR Needed)**

**Consider Next.js if:**
- SEO is critical for public pages
- Need server-side rendering
- Want full-stack capabilities
- Planning to deploy on Vercel

**Current Assessment**: WitchCityRope is primarily a member-focused SPA, so Vite is more appropriate.

### **Avoid: Create React App**
- Officially deprecated in 2025
- Slower build times
- Limited configuration options
- Community moving away

## Routing Architecture (2025)

### **Recommended: React Router v7 (Stable Choice)**

**Why React Router for WitchCityRope:**
- **Mature Ecosystem**: Battle-tested in production
- **Familiar API**: Easy migration from current patterns
- **Community Support**: Extensive documentation and examples
- **Stability**: Less risk for production application

**Implementation Pattern:**
```typescript
// Router setup
const router = createBrowserRouter([
  {
    path: "/",
    element: <PublicLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: "about", element: <AboutPage /> },
      { path: "events", element: <EventsPage /> }
    ]
  },
  {
    path: "/auth",
    element: <AuthLayout />,
    children: [
      { path: "login", element: <LoginPage /> },
      { path: "register", element: <RegisterPage /> }
    ]
  },
  {
    path: "/admin",
    element: <AdminLayout />,
    loader: requireAuth(['Admin']),
    children: [
      { index: true, element: <AdminDashboard /> },
      { path: "users", element: <UserManagement /> },
      { path: "events", element: <EventManagement /> }
    ]
  },
  {
    path: "/member",
    element: <MemberLayout />,
    loader: requireAuth(['Member', 'VettedMember']),
    children: [
      { index: true, element: <MemberDashboard /> },
      { path: "profile", element: <ProfilePage /> },
      { path: "events", element: <MemberEvents /> }
    ]
  }
]);

// Protected route implementation
const requireAuth = (roles = []) => async () => {
  const { isAuthenticated, hasRole } = useAuthStore.getState();
  
  if (!isAuthenticated) {
    throw redirect('/auth/login');
  }
  
  if (roles.length > 0 && !roles.some(role => hasRole(role))) {
    throw redirect('/unauthorized');
  }
  
  return null;
};
```

### **Alternative: TanStack Router (Modern Choice)**

**Consider TanStack Router if:**
- Type safety is critical
- Building a new application from scratch
- Team has experience with modern routing patterns
- Want cutting-edge features

**Current Assessment**: React Router is more appropriate for WitchCityRope due to:
- Proven stability in production
- Easier team onboarding
- Less risk during migration
- Extensive community resources

## Component Architecture Patterns

### **Recommended Structure**

```
src/
├── components/          # Reusable UI components
│   ├── ui/             # Basic UI elements (Button, Input, etc.)
│   ├── forms/          # Form components
│   └── layout/         # Layout components
├── features/           # Feature-based organization
│   ├── auth/
│   │   ├── components/ # Auth-specific components
│   │   ├── hooks/      # Auth-related hooks
│   │   ├── services/   # Auth API calls
│   │   └── stores/     # Auth state management
│   ├── events/
│   ├── admin/
│   └── members/
├── hooks/              # Shared custom hooks
├── services/           # API and external services
├── stores/             # Global state stores
├── types/              # TypeScript type definitions
└── utils/              # Utility functions
```

### **Component Patterns**

#### **Compound Components for Complex UI**
```typescript
// Event card with compound pattern
const EventCard = ({ children }) => (
  <div className="event-card">{children}</div>
);

EventCard.Header = ({ title, date }) => (
  <div className="event-card-header">
    <h3>{title}</h3>
    <time>{date}</time>
  </div>
);

EventCard.Body = ({ description }) => (
  <div className="event-card-body">{description}</div>
);

EventCard.Actions = ({ children }) => (
  <div className="event-card-actions">{children}</div>
);

// Usage
<EventCard>
  <EventCard.Header title="Rope Basics" date="2025-08-20" />
  <EventCard.Body description="Introduction to rope bondage" />
  <EventCard.Actions>
    <Button>Register</Button>
  </EventCard.Actions>
</EventCard>
```

#### **Custom Hooks for Feature Logic**
```typescript
// Event management hook
const useEventManagement = () => {
  const { data: events, isLoading } = useQuery({
    queryKey: ['events'],
    queryFn: eventsAPI.getEvents
  });

  const createEventMutation = useMutation({
    mutationFn: eventsAPI.createEvent,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['events'] });
    }
  });

  return {
    events,
    isLoading,
    createEvent: createEventMutation.mutate,
    isCreating: createEventMutation.isPending
  };
};
```

## Performance Considerations

### **Code Splitting Strategy**
```typescript
// Route-based code splitting
const AdminDashboard = lazy(() => import('./features/admin/Dashboard'));
const UserManagement = lazy(() => import('./features/admin/UserManagement'));

// Component-based code splitting for large features
const EventRegistrationModal = lazy(() => 
  import('./features/events/EventRegistrationModal')
);
```

### **Optimization Patterns**
```typescript
// Memoization for expensive operations
const MemberStats = memo(({ memberId }) => {
  const stats = useMemo(() => 
    calculateMemberStats(memberId), 
    [memberId]
  );
  
  return <div>{/* Stats display */}</div>;
});

// Virtual scrolling for large lists
const UserList = ({ users }) => {
  return (
    <FixedSizeList
      height={600}
      itemCount={users.length}
      itemSize={50}
    >
      {({ index, style }) => (
        <div style={style}>
          <UserRow user={users[index]} />
        </div>
      )}
    </FixedSizeList>
  );
};
```

## TypeScript Integration

### **Recommended TypeScript Setup**
```typescript
// Type definitions for entities
interface User {
  id: string;
  email: string;
  sceneName: string;
  roles: UserRole[];
  isActive: boolean;
  createdAt: Date;
}

interface Event {
  id: string;
  title: string;
  description: string;
  startDate: Date;
  endDate: Date;
  capacity: number;
  registrations: Registration[];
  status: EventStatus;
}

// API response types
type ApiResponse<T> = {
  data: T;
  success: boolean;
  message?: string;
};

// Store types
interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => void;
  hasRole: (role: UserRole) => boolean;
}
```

## Migration Strategy Recommendations

### **Phase 1: Foundation Setup**
1. **Initialize Vite project** with TypeScript
2. **Set up Zustand stores** for global state
3. **Configure React Query** for server state
4. **Implement React Router** with basic routes
5. **Create component library** foundation

### **Phase 2: Authentication Migration**
1. **Migrate auth flows** (login, register, logout)
2. **Implement protected routes** with role-based access
3. **Set up API integration** with existing .NET backend
4. **Migrate user management** features

### **Phase 3: Feature Migration**
1. **Member dashboard** and profile features
2. **Event management** (public and admin)
3. **Vetting system** migration
4. **Administrative features**

### **Phase 4: Optimization**
1. **Performance optimization** and code splitting
2. **Error boundary** implementation
3. **Testing setup** and coverage
4. **Production deployment** preparation

## Recommended Technology Stack for WitchCityRope

### **Core Architecture**
- **Build Tool**: Vite
- **State Management**: Zustand + React Query
- **Routing**: React Router v7
- **Styling**: CSS Modules or Styled Components
- **Type Safety**: TypeScript (strict mode)

### **Supporting Libraries**
- **Forms**: React Hook Form + Zod validation
- **HTTP Client**: Axios with interceptors
- **Date Handling**: date-fns
- **Icons**: Lucide React or Heroicons
- **Animations**: Framer Motion (if needed)

### **Development Tools**
- **Linting**: ESLint + Prettier
- **Testing**: Vitest + Testing Library
- **Bundle Analysis**: Vite Bundle Analyzer
- **DevTools**: React DevTools + Query DevTools

## Performance Benchmarks (Expected)

### **Development Experience**
- **Dev server start**: ~2 seconds (vs 15+ with CRA)
- **Hot reload**: ~300ms (vs 2-5s with Webpack)
- **Build time**: ~30 seconds (vs 2+ minutes)

### **Runtime Performance**
- **First Contentful Paint**: ~1.2s
- **Largest Contentful Paint**: ~2.1s
- **Time to Interactive**: ~2.8s
- **Bundle Size**: ~150KB gzipped (main chunk)

## Conclusion

The recommended React architecture for WitchCityRope emphasizes:

1. **Simplicity**: Vite + Zustand for easy development and maintenance
2. **Performance**: React Query for efficient server state management
3. **Stability**: React Router for proven routing solutions
4. **Scalability**: Feature-based organization for future growth
5. **Developer Experience**: TypeScript and modern tooling

This architecture provides a solid foundation for migrating from Blazor while maintaining performance and adding modern development benefits. The hybrid state management approach (Zustand + React Query) offers the perfect balance of simplicity and power for WitchCityRope's specific needs.

The technology choices prioritize:
- **Team productivity** over cutting-edge features
- **Proven solutions** over experimental libraries
- **Community support** over vendor-specific tools
- **Maintainability** over micro-optimizations

This approach ensures a successful migration with minimal risk and maximum developer satisfaction.