# Routing Patterns

**Purpose**: React Router v7 routing patterns, navigation, and URL management for WitchCityRope.
**When to Read**: When implementing routes, navigation, or URL parameters.
**Related**: [React Patterns](./react-patterns.md), [State Management](./state-management-patterns.md)

## React Router Version

**Current Version**: React Router v7
**Pattern**: File-based routing with React Router

## Route Definition

### Standard Route Pattern
```typescript
// src/routes/events/$id.tsx
import { useParams } from 'react-router-dom';
import type { EventDto } from '@witchcityrope/shared-types';

export function EventDetailPage() {
  const { id } = useParams<{ id: string }>();

  // Fetch and display event
  return <div>Event {id}</div>;
}
```

## Navigation

### Programmatic Navigation
```typescript
import { useNavigate } from 'react-router-dom';

export function MyComponent() {
  const navigate = useNavigate();

  const handleClick = () => {
    navigate('/events/123');
  };

  const handleGoBack = () => {
    navigate(-1);  // Go back one page
  };

  return (
    <>
      <button onClick={handleClick}>View Event</button>
      <button onClick={handleGoBack}>Go Back</button>
    </>
  );
}
```

### Link Navigation
```typescript
import { Link } from 'react-router-dom';

<Link to="/events/123">View Event</Link>
<Link to="/events">Back to Events</Link>
```

## URL Parameters

### Path Parameters
```typescript
// Route: /events/:id
import { useParams } from 'react-router-dom';

export function EventPage() {
  const { id } = useParams<{ id: string }>();

  return <div>Event ID: {id}</div>;
}
```

### Query Parameters
```typescript
import { useSearchParams } from 'react-router-dom';

export function EventListPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const filter = searchParams.get('filter') ?? 'all';
  const page = parseInt(searchParams.get('page') ?? '1', 10);

  const updateFilter = (newFilter: string) => {
    setSearchParams({ filter: newFilter, page: '1' });
  };

  return (
    <div>
      <div>Current filter: {filter}</div>
      <button onClick={() => updateFilter('upcoming')}>
        Show Upcoming
      </button>
    </div>
  );
}
```

## Protected Routes

### Authentication Guard Pattern
```typescript
import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';

export function ProtectedRoute() {
  const isAuthenticated = useAuthStore(state => state.isAuthenticated);

  if (!isAuthenticated) {
    return <Navigate to="/auth/login" replace />;
  }

  return <Outlet />;
}
```

### Role-Based Access
```typescript
import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';

interface RoleGuardProps {
  allowedRoles: string[];
}

export function RoleGuard({ allowedRoles }: RoleGuardProps) {
  const user = useAuthStore(state => state.user);

  if (!user) {
    return <Navigate to="/auth/login" replace />;
  }

  const hasRole = user.roles?.some(role =>
    allowedRoles.includes(role)
  );

  if (!hasRole) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <Outlet />;
}
```

## Nested Routes

```typescript
// Parent route: /admin
export function AdminLayout() {
  return (
    <div>
      <nav>
        <Link to="/admin/events">Events</Link>
        <Link to="/admin/users">Users</Link>
      </nav>
      <Outlet />  {/* Child routes render here */}
    </div>
  );
}

// Child route: /admin/events
export function AdminEventsPage() {
  return <div>Admin Events</div>;
}
```

## Route Loaders

### Data Loading Pattern
```typescript
import { useLoaderData } from 'react-router-dom';
import type { EventDto } from '@witchcityrope/shared-types';

// Loader function
export async function eventLoader({ params }: { params: { id: string } }) {
  const response = await fetch(`/api/events/${params.id}`);
  const event: EventDto = await response.json();
  return { event };
}

// Component
export function EventPage() {
  const { event } = useLoaderData<{ event: EventDto }>();

  return <div>{event.name}</div>;
}
```

## Error Boundaries

```typescript
import { useRouteError, isRouteErrorResponse } from 'react-router-dom';

export function ErrorBoundary() {
  const error = useRouteError();

  if (isRouteErrorResponse(error)) {
    return (
      <div>
        <h1>{error.status} {error.statusText}</h1>
        <p>{error.data}</p>
      </div>
    );
  }

  return (
    <div>
      <h1>Oops! Something went wrong</h1>
      <p>Please try again later.</p>
    </div>
  );
}
```

## Post-Login Return URLs

### Pattern for Redirect After Login
```typescript
import { useNavigate, useLocation } from 'react-router-dom';

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();

  // Get return URL from state
  const from = location.state?.from?.pathname || '/';

  const handleLogin = async (credentials) => {
    const success = await login(credentials);
    if (success) {
      // Redirect to original destination
      navigate(from, { replace: true });
    }
  };

  return <LoginForm onSubmit={handleLogin} />;
}

// Setting the return URL when redirecting to login
function ProtectedPage() {
  const isAuthenticated = useAuthStore(state => state.isAuthenticated);
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/auth/login" state={{ from: location }} replace />;
  }

  return <div>Protected Content</div>;
}
```

## Dynamic Route Generation

```typescript
import { generatePath } from 'react-router-dom';

const eventPath = generatePath('/events/:id', { id: '123' });
// Result: '/events/123'

const sessionPath = generatePath('/events/:eventId/sessions/:sessionId', {
  eventId: '123',
  sessionId: 'A',
});
// Result: '/events/123/sessions/A'
```

## Route Constants

### Centralized Route Definitions
```typescript
// src/routes/paths.ts
export const ROUTES = {
  HOME: '/',
  EVENTS: '/events',
  EVENT_DETAIL: '/events/:id',
  ADMIN: '/admin',
  ADMIN_EVENTS: '/admin/events',
  AUTH_LOGIN: '/auth/login',
  AUTH_REGISTER: '/auth/register',
} as const;

// Usage
import { ROUTES } from '@/routes/paths';

<Link to={ROUTES.EVENTS}>Events</Link>
<Link to={generatePath(ROUTES.EVENT_DETAIL, { id: '123' })}>
  Event Detail
</Link>
```

## Standards Maintenance

When implementing routing:
1. Use TypeScript for route parameters
2. Define routes in centralized constants
3. Protect routes requiring authentication
4. Handle loading and error states
5. Preserve return URLs for post-login redirects

---

*This document is maintained by the React Developer Agent.*
