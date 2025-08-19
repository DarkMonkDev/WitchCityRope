# Functional Specification: React Router v7 Routing Validation
<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview
This functional specification defines the implementation of React Router v7 routing patterns for WitchCityRope's React migration. The system will validate comprehensive routing capabilities including role-based access control, httpOnly cookie authentication, type-safe navigation, and performance optimization. This technical validation establishes the foundation for all future feature development.

## Architecture

### React + API Microservices Architecture
**CRITICAL**: This is a React+API microservices architecture:
- **React Service** (Vite + React): UI/Auth at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5653
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Component Structure
```
/apps/web/src/
├── routes/
│   ├── index.tsx                    # Home page
│   ├── login.tsx                    # Authentication
│   ├── register.tsx                 # User registration
│   ├── dashboard/
│   │   ├── _layout.tsx             # Dashboard layout
│   │   └── index.tsx               # Dashboard home
│   ├── events/
│   │   ├── _layout.tsx             # Events layout
│   │   ├── index.tsx               # Public events list
│   │   ├── $eventId.tsx            # Event details
│   │   └── $eventId.edit.tsx       # Event editing (Teachers+)
│   ├── members/
│   │   ├── _guard.tsx              # Vetted member access guard
│   │   └── index.tsx               # Member directory
│   └── admin/
│       ├── _guard.tsx              # Admin access guard
│       ├── index.tsx               # Admin dashboard
│       └── users.tsx               # User management
├── components/
│   ├── ui/
│   │   ├── Layout.tsx              # Main app layout
│   │   ├── ErrorBoundary.tsx       # Route error handling
│   │   └── LoadingSkeleton.tsx     # Loading states
│   └── guards/
│       ├── AuthGuard.tsx           # Authentication guard
│       └── RoleGuard.tsx           # Role-based access guard
├── loaders/
│   ├── authLoader.ts               # Authentication validation
│   ├── roleLoader.ts               # Role-based data loading
│   └── eventLoader.ts              # Event data loading
├── utils/
│   ├── auth.ts                     # Authentication utilities
│   ├── session.ts                  # Session management
│   └── router.ts                   # Router configuration
└── types/
    ├── auth.ts                     # Authentication types
    ├── user.ts                     # User role types
    └── router.ts                   # Router type definitions
```

### Service Architecture
- **React Service**: UI components make HTTP calls to API
- **API Service**: Business logic with EF Core database access
- **No Direct Database Access**: React service NEVER directly accesses database
- **Session Management**: httpOnly cookies managed via API endpoints

## Data Models

### User Role Hierarchy
```typescript
enum UserRole {
  Guest = 0,           // Public access only
  GeneralMember = 1,   // Basic member features
  VettedMember = 2,    // Member directory, vetted events
  Teacher = 3,         // Workshop management, event editing
  Admin = 4            // Full platform administration
}

interface User {
  id: string;
  email: string;
  role: UserRole;
  firstName: string;
  lastName: string;
  isActive: boolean;
}
```

### Session and Authentication DTOs
```typescript
interface SessionData {
  userId: string;
  userRole: UserRole;
  email: string;
  isActive: boolean;
  expiresAt: Date;
}

interface AuthResponse {
  success: boolean;
  user?: User;
  message?: string;
  returnTo?: string;
}
```

### Route Context Models
```typescript
interface RouteContext {
  user: User | null;
  isAuthenticated: boolean;
  hasRole: (role: UserRole) => boolean;
  hasMinimumRole: (minRole: UserRole) => boolean;
}

interface LoaderData<T = any> {
  data: T;
  user: User | null;
  permissions: string[];
}
```

## API Specifications

### Authentication Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| POST | /api/auth/login | User login | LoginDto | AuthResponse |
| POST | /api/auth/logout | User logout | - | { success: boolean } |
| POST | /api/auth/register | User registration | RegisterDto | AuthResponse |
| GET | /api/auth/session | Get current session | - | SessionData |
| POST | /api/auth/refresh | Refresh session | - | AuthResponse |

### User Management Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/users/profile | Current user profile | - | User |
| PUT | /api/users/profile | Update profile | UpdateProfileDto | User |
| GET | /api/users | List users (Admin) | Query params | List<User> |
| GET | /api/users/{id} | User details | - | User |

### Event Management Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/events | List events | Query params | List<EventDto> |
| GET | /api/events/{id} | Event details | - | EventDto |
| POST | /api/events | Create event (Teacher+) | CreateEventDto | EventDto |
| PUT | /api/events/{id} | Update event (Teacher+) | UpdateEventDto | EventDto |
| DELETE | /api/events/{id} | Delete event (Admin) | - | { success: boolean } |

## Component Specifications

### Main Route Components

#### Public Routes
- **Path**: `/`, `/about`, `/events` (public)
- **Authorization**: None required
- **Render Mode**: Standard React components
- **Key Features**: 
  - Fast loading (<100ms)
  - SEO-friendly URLs
  - No authentication redirects

#### Protected Routes
- **Path**: `/dashboard/*`
- **Authorization**: GeneralMember minimum
- **Render Mode**: Protected with route guards
- **Key Features**:
  - Automatic login redirect with return URL
  - Session validation on route entry
  - User context available to all child routes

#### Role-Based Routes
- **Path**: `/admin/*`
- **Authorization**: Admin role required
- **Render Mode**: Role guard with 403 error handling
- **Key Features**:
  - Server-side role validation
  - Graceful degradation for insufficient permissions
  - Audit logging for access attempts

### Route Guard Implementation

#### Authentication Guard
```typescript
// /apps/web/src/loaders/authLoader.ts
export async function authLoader({ request }: LoaderFunctionArgs) {
  const response = await fetch('/api/auth/session', {
    credentials: 'include'
  });
  
  if (!response.ok) {
    const returnTo = encodeURIComponent(new URL(request.url).pathname);
    throw redirect(`/login?returnTo=${returnTo}`);
  }
  
  const session: SessionData = await response.json();
  return { user: session };
}
```

#### Role-Based Guard
```typescript
// /apps/web/src/loaders/roleLoader.ts
export function requireMinimumRole(minimumRole: UserRole) {
  return async ({ request }: LoaderFunctionArgs) => {
    const { user } = await authLoader({ request } as any);
    
    if (!user || user.role < minimumRole) {
      throw data("Access Denied", { status: 403 });
    }
    
    return { user };
  };
}
```

### State Management
- **Authentication State**: React Context with session data
- **Route Context**: Loader data passed through route hierarchy
- **Error States**: Route-level error boundaries
- **Loading States**: Suspense boundaries with skeleton screens

## React Router v7 Implementation

### Router Configuration
```typescript
// /apps/web/src/main.tsx
import { createBrowserRouter, RouterProvider } from 'react-router';

const router = createBrowserRouter([
  {
    path: "/",
    element: <RootLayout />,
    errorElement: <RootErrorBoundary />,
    children: [
      // Public routes
      { index: true, element: <HomePage /> },
      { path: "about", element: <AboutPage /> },
      { path: "events", element: <PublicEventsPage /> },
      
      // Authentication routes
      { path: "login", element: <LoginPage /> },
      { path: "register", element: <RegisterPage /> },
      
      // Protected routes
      {
        path: "dashboard",
        element: <DashboardLayout />,
        loader: authLoader,
        children: [
          { index: true, element: <DashboardHome /> },
          { path: "profile", element: <ProfilePage /> }
        ]
      },
      
      // Role-based routes
      {
        path: "members",
        element: <MemberDirectoryPage />,
        loader: requireMinimumRole(UserRole.VettedMember)
      },
      {
        path: "admin",
        element: <AdminLayout />,
        loader: requireMinimumRole(UserRole.Admin),
        children: [
          { index: true, element: <AdminDashboard /> },
          { path: "users", element: <UserManagement /> }
        ]
      }
    ]
  }
]);
```

### Type-Safe Route Navigation
```typescript
// Automatic type generation for routes
import type { Route } from "./+types/event";

export function loader({ params }: Route.LoaderArgs) {
  // params.eventId is automatically typed
  return { event: await getEvent(params.eventId) };
}

export default function EventPage({
  loaderData
}: Route.ComponentProps) {
  // loaderData is typed as { event: Event }
  return <EventDetails event={loaderData.event} />;
}
```

### httpOnly Cookie Session Management
```typescript
// Session configuration (simulated for React app)
const sessionConfig = {
  name: "__session",
  httpOnly: true,
  secure: process.env.NODE_ENV === 'production',
  sameSite: "lax" as const,
  maxAge: 60 * 60 * 24 // 24 hours
};

// Session utilities
export async function getSession(): Promise<SessionData | null> {
  try {
    const response = await fetch('/api/auth/session', {
      credentials: 'include'
    });
    return response.ok ? await response.json() : null;
  } catch {
    return null;
  }
}

export async function createSession(loginData: LoginDto): Promise<AuthResponse> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(loginData)
  });
  return await response.json();
}
```

### Code Splitting and Lazy Loading
```typescript
// Route-level code splitting
const AdminLayout = lazy(() => import('./components/admin/AdminLayout'));
const EventManagement = lazy(() => import('./components/events/EventManagement'));

// Granular lazy loading with performance monitoring
const routes = [
  {
    path: "/admin",
    lazy: async () => {
      const start = performance.now();
      const module = await import("./routes/admin");
      const loadTime = performance.now() - start;
      
      // Log performance metrics
      console.log(`Admin route loaded in ${loadTime}ms`);
      
      return {
        Component: module.Component,
        loader: module.loader,
        ErrorBoundary: module.ErrorBoundary
      };
    }
  }
];
```

### Error Handling Patterns
```typescript
// Root error boundary
export function RootErrorBoundary() {
  const error = useRouteError();
  
  if (isRouteErrorResponse(error)) {
    return (
      <div className="error-page">
        <h1>{error.status} {error.statusText}</h1>
        <p>{error.data}</p>
        {error.status === 403 && (
          <Link to="/login">Login Required</Link>
        )}
        {error.status === 404 && (
          <Link to="/">Return Home</Link>
        )}
      </div>
    );
  }
  
  return (
    <div className="error-page">
      <h1>Something went wrong</h1>
      <p>{error instanceof Error ? error.message : "Unknown error"}</p>
      <button onClick={() => window.location.reload()}>
        Try Again
      </button>
    </div>
  );
}

// Route-specific error handling
{
  path: "/events/:eventId",
  element: <EventDetails />,
  errorElement: <EventNotFoundError />,
  loader: async ({ params }) => {
    const event = await getEvent(params.eventId);
    if (!event) {
      throw data("Event not found", { status: 404 });
    }
    return { event };
  }
}
```

## Integration Points

### Authentication System Integration
- **Login Flow**: React → API `/auth/login` → httpOnly cookie
- **Session Validation**: Route loaders validate with API `/auth/session`
- **Logout**: React → API `/auth/logout` → cookie cleared
- **Auto-refresh**: Background session renewal before expiration

### WitchCityRope Role System
- **Role Hierarchy**: Guest < GeneralMember < VettedMember < Teacher < Admin
- **Route Access**: Minimum role requirements enforced at route level
- **Permission Inheritance**: Higher roles automatically have lower role permissions
- **Role Validation**: Server-side validation for all protected routes

### Event Management Integration
- **Public Events**: Accessible to all users including guests
- **Vetted Events**: Require VettedMember+ role
- **Workshop Management**: Teacher+ role for creation/editing
- **Event Registration**: Member+ role with payment integration

### Mobile Performance Integration
- **Touch Optimization**: Route transitions optimized for mobile devices
- **Loading States**: Mobile-specific skeleton screens
- **Performance Monitoring**: Route timing for mobile performance targets
- **Offline Handling**: Service worker integration for offline route caching

## Security Requirements

### Authentication Security
- **httpOnly Cookies**: Prevent XSS attacks on authentication tokens
- **CSRF Protection**: Anti-forgery tokens for state-changing operations
- **Session Timeout**: Automatic logout after inactivity
- **Secure Transport**: HTTPS required for production cookies

### Authorization Security
- **Server-Side Validation**: All protected routes validate permissions server-side
- **Role Enforcement**: Client-side guards backed by server validation
- **Audit Logging**: Administrative route access logged for security
- **Privilege Escalation Prevention**: Role changes require admin approval

### Data Protection
- **Sensitive Data**: No sensitive information in URL parameters
- **Query Sanitization**: All query parameters validated and sanitized
- **History Protection**: No sensitive data persisted in browser history
- **Route State**: No sensitive data in client-side route state

## Performance Requirements

### Route Transition Performance
- **Cached Routes**: <100ms transition time
- **New Data Routes**: <300ms with loading indicators
- **Bundle Size**: Initial bundle <200KB, route chunks <50KB each
- **Memory Management**: No memory leaks during navigation

### Loading State Management
```typescript
// Mobile-optimized loading patterns
function EventDetailsPage() {
  return (
    <Suspense fallback={<MobileEventSkeleton />}>
      <div className="event-details mobile-optimized">
        <Suspense fallback={<LoadingSpinner />}>
          <EventDetails />
        </Suspense>
      </div>
    </Suspense>
  );
}

// Performance monitoring
function useRoutePerformance() {
  useEffect(() => {
    const start = performance.now();
    return () => {
      const end = performance.now();
      const loadTime = end - start;
      
      // Log slow routes
      if (loadTime > 300) {
        console.warn(`Slow route load: ${loadTime}ms`);
      }
    };
  }, []);
}
```

### Code Splitting Strategy
- **Route-Level**: Major sections (admin, events, members) in separate chunks
- **Component-Level**: Heavy components loaded on demand
- **Library Splitting**: Common libraries in separate vendor chunk
- **Dynamic Imports**: Feature-specific code loaded when needed

## Validation Scenarios

### 1. Public Route Access (Guest Users)
**Scenario**: Unauthenticated user navigates to public content
- **Given**: User visits `/events` without authentication
- **When**: Route loads
- **Then**: Content displays immediately without redirects
- **Validation**: No authentication checks, fast loading (<100ms)

### 2. Protected Route Security (Authentication Required)
**Scenario**: Unauthenticated user attempts protected route access
- **Given**: User navigates to `/dashboard`
- **When**: Route loader executes
- **Then**: Redirect to `/login?returnTo=%2Fdashboard`
- **Validation**: Authentication enforced, return URL preserved

### 3. Role-Based Access Control (Authorization)
**Scenario**: General Member attempts admin route access
- **Given**: GeneralMember navigates to `/admin/users`
- **When**: Role guard executes
- **Then**: 403 error page displayed with appropriate message
- **Validation**: Role hierarchy enforced, graceful error handling

### 4. Deep Linking and Bookmarking
**Scenario**: Vetted Member accesses direct event link
- **Given**: Direct URL `/events/rope-basics-101` 
- **When**: Page loads from bookmark
- **Then**: Event details display correctly with authentication
- **Validation**: Deep linking works, authentication transparent

### 5. Query Parameter Persistence
**Scenario**: Member filters events and refreshes page
- **Given**: URL `/events?category=beginner&level=introductory`
- **When**: Page refresh occurs
- **Then**: Filters maintained and applied to event list
- **Validation**: State persistence, back/forward navigation

### 6. Nested Route Navigation
**Scenario**: Teacher manages workshop details
- **Given**: At `/events/my-workshop-123/edit`
- **When**: Navigate to `/events/my-workshop-123/attendees`
- **Then**: Context maintained, breadcrumbs updated
- **Validation**: Parent-child relationship, state sharing

### 7. Loading States and Transitions
**Scenario**: Member navigates to data-heavy page
- **Given**: Click link to `/members` (large dataset)
- **When**: Route begins loading
- **Then**: Loading skeleton displays, smooth transition
- **Validation**: User feedback, no content flash

### 8. Error Handling and Recovery
**Scenario**: User navigates to non-existent route
- **Given**: URL `/nonexistent-page`
- **When**: Route not found
- **Then**: Branded 404 page with navigation options
- **Validation**: Error boundary, user guidance

## Testing Approach

### Unit Testing (React Testing Library)
```typescript
// Route component testing
describe('ProtectedRoute', () => {
  it('redirects unauthenticated users to login', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 401
    });
    
    render(<RouterProvider router={testRouter} />);
    
    expect(screen.getByText('Login')).toBeInTheDocument();
    expect(window.location.pathname).toBe('/login');
  });
});

// Role guard testing
describe('AdminRoute', () => {
  it('denies access to non-admin users', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ role: UserRole.GeneralMember })
    });
    
    render(<AdminRoute />);
    
    expect(screen.getByText('Access Denied')).toBeInTheDocument();
  });
});
```

### Integration Testing (Route Flows)
```typescript
// Authentication flow testing
describe('Login Flow', () => {
  it('redirects to protected route after successful login', async () => {
    // Navigate to protected route
    fireEvent.click(screen.getByText('Dashboard'));
    
    // Should redirect to login
    expect(screen.getByText('Login')).toBeInTheDocument();
    
    // Complete login
    fireEvent.change(screen.getByLabelText('Email'), {
      target: { value: 'user@example.com' }
    });
    fireEvent.click(screen.getByText('Login'));
    
    // Should return to dashboard
    await waitFor(() => {
      expect(screen.getByText('Dashboard')).toBeInTheDocument();
    });
  });
});
```

### Performance Testing
```typescript
// Route transition performance
describe('Performance', () => {
  it('loads cached routes within 100ms', async () => {
    const start = performance.now();
    
    fireEvent.click(screen.getByText('Events'));
    
    await waitFor(() => {
      expect(screen.getByText('Events List')).toBeInTheDocument();
    });
    
    const loadTime = performance.now() - start;
    expect(loadTime).toBeLessThan(100);
  });
});
```

### End-to-End Testing (Playwright)
```typescript
// Role-based access testing
test('admin can access all routes', async ({ page }) => {
  await page.goto('/login');
  await page.fill('[data-testid=email]', 'admin@witchcityrope.com');
  await page.fill('[data-testid=password]', 'Test123!');
  await page.click('[data-testid=login-button]');
  
  // Test admin routes
  await page.goto('/admin/users');
  await expect(page.getByText('User Management')).toBeVisible();
  
  // Test teacher routes
  await page.goto('/workshops/create');
  await expect(page.getByText('Create Workshop')).toBeVisible();
});
```

## Migration Requirements

### React Router v6 to v7 Migration
1. **Phase 1**: Install React Router v7 alongside v6
2. **Phase 2**: Update import statements from `react-router-dom` to `react-router`
3. **Phase 3**: Implement new loader patterns for authentication
4. **Phase 4**: Convert to type-safe route definitions
5. **Phase 5**: Remove v6 dependencies and legacy patterns

### Data Migration
- **Session Data**: Migrate existing JWT to httpOnly cookie sessions
- **Route State**: Convert localStorage route state to URL parameters
- **Authentication**: Update authentication flow to use new session patterns

### Backward Compatibility
- **API Compatibility**: Maintain existing API endpoints during transition
- **URL Structure**: Preserve existing URL patterns for bookmarked links
- **User Sessions**: Graceful handling of existing authentication tokens

## Dependencies

### Required Packages
```json
{
  "react-router": "^7.0.0",
  "@types/react-router": "^7.0.0",
  "react": "^18.2.0",
  "react-dom": "^18.2.0",
  "typescript": "^5.0.0"
}
```

### Optional Performance Packages
```json
{
  "@loadable/component": "^5.16.0",
  "react-helmet-async": "^1.3.0",
  "web-vitals": "^3.0.0"
}
```

### Development Dependencies
```json
{
  "@testing-library/react": "^13.4.0",
  "@testing-library/react-router": "^1.0.0",
  "playwright": "^1.40.0",
  "vitest": "^1.0.0"
}
```

## Acceptance Criteria

### Technical Criteria
- [ ] All 8 routing patterns implemented and tested
- [ ] Protected routes enforce authentication at loader level
- [ ] Role-based access control validates server-side
- [ ] Deep linking works for all accessible routes
- [ ] Query parameters persist across navigation
- [ ] Route transitions meet performance targets (<100ms cached, <300ms new)
- [ ] Error boundaries provide helpful user guidance
- [ ] Code splitting reduces initial bundle size by >15%
- [ ] Memory usage remains stable during extensive navigation
- [ ] httpOnly cookies properly managed for authentication

### Security Criteria
- [ ] Authentication bypassing impossible at route level
- [ ] Role escalation prevented by server-side validation
- [ ] Session timeout properly redirects to login
- [ ] CSRF protection implemented for sensitive routes
- [ ] No sensitive data exposed in URL parameters
- [ ] Audit logging active for administrative route access

### Performance Criteria
- [ ] Initial bundle size <200KB gzipped
- [ ] Route chunks <50KB each
- [ ] Route transitions <100ms for cached content
- [ ] Route transitions <300ms for new data
- [ ] No memory leaks during navigation stress testing
- [ ] Mobile performance targets met on 3G networks

### User Experience Criteria
- [ ] Smooth transitions between all route types
- [ ] Loading states provide immediate feedback
- [ ] Error pages offer clear recovery options
- [ ] Back/forward buttons work correctly
- [ ] Bookmarking and sharing work for all accessible content
- [ ] Mobile navigation feels native and responsive

---

*This functional specification establishes the technical foundation for React Router v7 implementation in WitchCityRope. All routing patterns validated here will be used across the entire React application for consistent, secure, and performant navigation.*