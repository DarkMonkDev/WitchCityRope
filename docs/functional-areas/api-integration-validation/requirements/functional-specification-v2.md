# Functional Specification: API Integration Validation with TanStack Query v5
<!-- Last Updated: 2025-08-19 -->
<!-- Version: 2.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview
This specification establishes production-ready TanStack Query v5 patterns for WitchCityRope's React + .NET API architecture. Based on comprehensive research findings, we implement proven patterns for data fetching, caching, error handling, and optimistic updates that will serve as the foundation for all future feature development.

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5653
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Component Structure
```
/apps/web/src/
├── api/
│   ├── client.ts              # Axios setup with interceptors
│   ├── queryClient.ts         # TanStack Query configuration
│   └── queryKeys.ts           # Query key factory (TkDodo pattern)
├── features/
│   ├── events/
│   │   ├── api/
│   │   │   ├── queries.ts     # Event-specific queries
│   │   │   └── mutations.ts   # Event-specific mutations
│   │   ├── hooks/
│   │   │   ├── useEvents.ts
│   │   │   ├── useEvent.ts
│   │   │   └── useEventRegistration.ts
│   │   └── components/
│   └── members/
│       ├── api/
│       │   ├── queries.ts
│       │   └── mutations.ts
│       ├── hooks/
│       │   ├── useMembers.ts
│       │   ├── useMember.ts
│       │   └── useMemberUpdate.ts
│       └── components/
├── types/
│   ├── api.types.ts           # API response types
│   └── react-query.d.ts       # Global error type registration
└── test/
    ├── setup.ts               # Test configuration
    └── mocks/
        └── handlers.ts         # MSW API handlers
```

### Service Architecture
- **Web Service**: React components make HTTP calls to API via TanStack Query
- **API Service**: Business logic with EF Core database access
- **No Direct Database Access**: Web service NEVER directly accesses database

## Data Models

### API Response Types
```typescript
// types/api.types.ts
export interface ApiError {
  message: string
  status: number
  code?: string
  details?: Record<string, string[]>
}

export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  role: UserRole
  isVetted: boolean
  createdAt: string
  updatedAt: string
}

export interface Event {
  id: string
  title: string
  description: string
  startDate: string
  endDate: string
  maxAttendees: number
  currentAttendees: number
  isRegistrationOpen: boolean
  instructorId: string
  instructor: User
  attendees: User[]
}

export interface EventRegistration {
  id: string
  eventId: string
  userId: string
  registeredAt: string
  status: RegistrationStatus
}

export interface PaginatedResponse<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNext: boolean
  hasPrevious: boolean
}
```

### Global Error Type Registration
```typescript
// types/react-query.d.ts
declare module '@tanstack/react-query' {
  interface Register {
    defaultError: ApiError
  }
}
```

## API Specifications

### TanStack Query v5 Setup

#### 1. QueryClient Configuration
```typescript
// api/queryClient.ts
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,      // 5 minutes
      gcTime: 10 * 60 * 1000,        // 10 minutes (formerly cacheTime)
      retry: (failureCount, error) => {
        // Don't retry on 4xx client errors
        if (error instanceof Error && 'status' in error) {
          const status = (error as any).status
          if (status >= 400 && status < 500) return false
        }
        return failureCount < 3
      },
      retryDelay: attemptIndex => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
    mutations: {
      retry: false, // Don't retry mutations by default
      onError: (error) => {
        console.error('Mutation failed:', error)
        // Add toast notification here
      },
    },
  },
})
```

#### 2. Axios Configuration with Authentication
```typescript
// api/client.ts
import axios from 'axios'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5653',
  withCredentials: true, // Include httpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
})

// Response interceptor for auth errors
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Try to refresh token
      try {
        await api.post('/auth/refresh')
        // Retry original request
        return api.request(error.config)
      } catch (refreshError) {
        // Redirect to login
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)
```

#### 3. Query Key Factory (TkDodo Pattern)
```typescript
// api/queryKeys.ts
export const queryKeys = {
  // Authentication
  auth: () => ['auth'] as const,
  currentUser: () => [...queryKeys.auth(), 'current-user'] as const,
  
  // Users
  users: () => ['users'] as const,
  user: (id: string) => [...queryKeys.users(), id] as const,
  userEvents: (id: string) => [...queryKeys.user(id), 'events'] as const,
  
  // Events
  events: () => ['events'] as const,
  event: (id: string) => [...queryKeys.events(), id] as const,
  eventAttendees: (id: string) => [...queryKeys.event(id), 'attendees'] as const,
  infiniteEvents: (filters: EventFilters) => [...queryKeys.events(), 'infinite', filters] as const,
  
  // Event Registrations
  registrations: () => ['registrations'] as const,
  userRegistrations: (userId: string) => [...queryKeys.registrations(), 'user', userId] as const,
  eventRegistrations: (eventId: string) => [...queryKeys.registrations(), 'event', eventId] as const,
} as const
```

## Implementation Patterns

### 1. Basic CRUD Operations

#### Query Pattern
```typescript
// features/events/hooks/useEvent.ts
import { useQuery } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { Event } from '../../../types/api.types'

export function useEvent(eventId: string) {
  return useQuery({
    queryKey: queryKeys.event(eventId),
    queryFn: async (): Promise<Event> => {
      const response = await api.get(`/api/events/${eventId}`)
      return response.data
    },
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}
```

#### Mutation Pattern
```typescript
// features/events/hooks/useUpdateEvent.ts
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { Event, UpdateEventData } from '../../../types/api.types'

export function useUpdateEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (params: { id: string; data: UpdateEventData }): Promise<Event> => {
      const response = await api.put(`/api/events/${params.id}`, params.data)
      return response.data
    },
    onSuccess: (updatedEvent, variables) => {
      // Update single event cache
      queryClient.setQueryData(queryKeys.event(variables.id), updatedEvent)
      
      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    onError: (error) => {
      console.error('Failed to update event:', error)
      // Add toast notification
    },
  })
}
```

### 2. Optimistic Updates

#### CSS-Only Optimistic Updates (Simple Changes)
```typescript
// features/members/hooks/useUpdateProfile.ts
export function useUpdateProfile() {
  const updateMutation = useMutation({
    mutationFn: async (data: UpdateProfileData) => {
      const response = await api.put('/api/members/profile', data)
      return response.data
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })

  return updateMutation
}

// Usage in component
function ProfileForm() {
  const { data: user } = useCurrentUser()
  const updateProfile = useUpdateProfile()

  return (
    <form onSubmit={(e) => {
      e.preventDefault()
      updateProfile.mutate(formData)
    }}>
      {/* Show optimistic state during mutation */}
      {updateProfile.isPending ? (
        <div className="opacity-50 pointer-events-none">
          Saving: {updateProfile.variables?.firstName}...
        </div>
      ) : (
        <div>{user?.firstName}</div>
      )}
      
      <button 
        type="submit" 
        disabled={updateProfile.isPending}
        className={updateProfile.isPending ? 'opacity-50' : ''}
      >
        {updateProfile.isPending ? 'Saving...' : 'Save Changes'}
      </button>
    </form>
  )
}
```

#### Cache-Based Optimistic Updates (Complex Changes)
```typescript
// features/events/hooks/useEventRegistration.ts
export function useEventRegistration() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async ({ eventId, action }: { eventId: string; action: 'register' | 'unregister' }) => {
      const response = await api.post(`/api/events/${eventId}/registration`, { action })
      return response.data
    },
    onMutate: async ({ eventId, action }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(eventId) })
      
      // Snapshot previous value
      const previousEvent = queryClient.getQueryData<Event>(queryKeys.event(eventId))
      
      // Optimistically update event
      if (previousEvent) {
        queryClient.setQueryData(queryKeys.event(eventId), (old: Event) => ({
          ...old,
          currentAttendees: action === 'register' 
            ? old.currentAttendees + 1
            : old.currentAttendees - 1,
        }))
      }
      
      // Return rollback context
      return { previousEvent, eventId }
    },
    onError: (error, variables, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('Registration failed:', error)
    },
    onSettled: (data, error, variables) => {
      // Always refetch to ensure server state
      queryClient.invalidateQueries({ queryKey: queryKeys.event(variables.eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })
}
```

### 3. Pagination with useInfiniteQuery

```typescript
// features/events/hooks/useInfiniteEvents.ts
import { useInfiniteQuery } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { PaginatedResponse, Event, EventFilters } from '../../../types/api.types'

export function useInfiniteEvents(filters: EventFilters = {}) {
  return useInfiniteQuery({
    queryKey: queryKeys.infiniteEvents(filters),
    queryFn: async ({ pageParam = 1 }): Promise<PaginatedResponse<Event>> => {
      const response = await api.get('/api/events', {
        params: { page: pageParam, pageSize: 20, ...filters }
      })
      return response.data
    },
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      return lastPage.hasNext ? lastPage.page + 1 : undefined
    },
    maxPages: 10, // v5 feature - limit memory usage
    staleTime: 2 * 60 * 1000, // 2 minutes
  })
}

// Usage in component
function InfiniteEventList() {
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    error,
  } = useInfiniteEvents()

  const { ref, inView } = useInView()

  useEffect(() => {
    if (inView && hasNextPage) {
      fetchNextPage()
    }
  }, [inView, hasNextPage, fetchNextPage])

  if (error) return <ErrorMessage error={error} />

  return (
    <div>
      {data?.pages.map((page, i) => (
        <div key={i}>
          {page.data.map(event => (
            <EventCard key={event.id} event={event} />
          ))}
        </div>
      ))}
      
      {hasNextPage && (
        <div ref={ref}>
          {isFetchingNextPage && <LoadingSpinner />}
        </div>
      )}
    </div>
  )
}
```

### 4. Error Handling

#### Global Error Boundary
```typescript
// components/QueryErrorBoundary.tsx
import { QueryErrorResetBoundary } from '@tanstack/react-query'
import { ErrorBoundary } from 'react-error-boundary'

function QueryErrorBoundary({ children }: { children: React.ReactNode }) {
  return (
    <QueryErrorResetBoundary>
      {({ reset }) => (
        <ErrorBoundary
          onReset={reset}
          fallbackRender={({ resetErrorBoundary, error }) => (
            <div className="error-container p-4 border border-red-300 rounded bg-red-50">
              <h2 className="text-lg font-semibold text-red-800">
                Something went wrong
              </h2>
              <p className="text-red-600 mt-2">
                {error.message || 'An unexpected error occurred'}
              </p>
              <button 
                onClick={resetErrorBoundary}
                className="mt-4 px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
              >
                Try again
              </button>
            </div>
          )}
        >
          {children}
        </ErrorBoundary>
      )}
    </QueryErrorResetBoundary>
  )
}
```

#### Authentication State Management
```typescript
// features/auth/hooks/useAuth.ts
export function useAuth() {
  return useQuery({
    queryKey: queryKeys.currentUser(),
    queryFn: async (): Promise<User> => {
      const response = await api.get('/auth/me')
      return response.data
    },
    retry: false, // Don't retry auth checks
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000,   // 10 minutes
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async () => {
      await api.post('/auth/logout')
    },
    onSuccess: () => {
      // Clear all queries on logout
      queryClient.clear()
      window.location.href = '/login'
    },
  })
}
```

### 5. Cache Management Utilities

```typescript
// utils/cache-utils.ts
import { QueryClient } from '@tanstack/react-query'
import { queryKeys } from '../api/queryKeys'
import type { User, Event } from '../types/api.types'

export function createCacheUtils(queryClient: QueryClient) {
  return {
    // User cache management
    invalidateUser: (userId: string) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user(userId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.users() })
    },
    
    setUser: (userId: string, user: User) => {
      queryClient.setQueryData(queryKeys.user(userId), user)
    },
    
    // Event cache management
    invalidateEvent: (eventId: string) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.event(eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.eventAttendees(eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    
    // Prefetching for performance
    prefetchEvent: (eventId: string) => {
      return queryClient.prefetchQuery({
        queryKey: queryKeys.event(eventId),
        queryFn: async () => {
          const response = await api.get(`/api/events/${eventId}`)
          return response.data
        },
        staleTime: 10 * 60 * 1000, // 10 minutes
      })
    },
    
    // Authentication cache management
    clearAuthCache: () => {
      queryClient.removeQueries({ queryKey: queryKeys.auth() })
    },
    
    // Emergency cache clearing
    clearAllCache: () => {
      queryClient.clear()
    },
  }
}
```

## Component Specifications

### Main App Setup
```typescript
// App.tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { queryClient } from './api/queryClient'
import { QueryErrorBoundary } from './components/QueryErrorBoundary'

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <QueryErrorBoundary>
        <Router>
          <Routes>
            {/* Your routes */}
          </Routes>
        </Router>
      </QueryErrorBoundary>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  )
}
```

### Cache Configuration by Data Type
```typescript
// config/cache-config.ts
export const witchCityRopeCacheConfig = {
  eventData: {
    staleTime: 5 * 60 * 1000,      // 5 minutes - events change occasionally
    gcTime: 15 * 60 * 1000,        // 15 minutes
  },
  memberData: {
    staleTime: 15 * 60 * 1000,     // 15 minutes - member profiles stable
    gcTime: 30 * 60 * 1000,        // 30 minutes
  },
  paymentStatus: {
    staleTime: 0,                  // Always fetch fresh - critical data
    gcTime: 1 * 60 * 1000,         // 1 minute only
  },
  staticContent: {
    staleTime: 60 * 60 * 1000,     // 1 hour - rarely changes
    gcTime: 2 * 60 * 60 * 1000,    // 2 hours
  },
} as const
```

## Testing Strategy

### Test Setup Configuration
```typescript
// test/setup.ts
import { beforeAll, afterAll, afterEach } from 'vitest'
import { setupServer } from 'msw/node'
import { handlers } from './mocks/handlers'

export const server = setupServer(...handlers)

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' })
})

afterAll(() => {
  server.close()
})

afterEach(() => {
  server.resetHandlers()
})
```

### MSW Handlers for .NET API
```typescript
// test/mocks/handlers.ts
import { http, HttpResponse } from 'msw'
import type { User, Event, PaginatedResponse } from '../../src/types/api.types'

export const handlers = [
  // Authentication
  http.get('/auth/me', () => {
    return HttpResponse.json({
      id: '1',
      email: 'admin@witchcityrope.com',
      firstName: 'Test',
      lastName: 'Admin',
      role: 'Admin',
      isVetted: true,
    } as User)
  }),

  http.post('/auth/logout', () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // Events
  http.get('/api/events/:id', ({ params }) => {
    return HttpResponse.json({
      id: params.id,
      title: 'Test Event',
      description: 'A test event',
      startDate: '2025-08-20T19:00:00Z',
      endDate: '2025-08-20T21:00:00Z',
      maxAttendees: 20,
      currentAttendees: 5,
      isRegistrationOpen: true,
    } as Event)
  }),

  http.get('/api/events', ({ request }) => {
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')
    
    return HttpResponse.json({
      data: [
        {
          id: '1',
          title: 'Test Event 1',
          currentAttendees: 5,
          maxAttendees: 20,
        },
        {
          id: '2',
          title: 'Test Event 2',
          currentAttendees: 10,
          maxAttendees: 15,
        },
      ] as Event[],
      page,
      pageSize,
      totalCount: 25,
      totalPages: 2,
      hasNext: page < 2,
      hasPrevious: page > 1,
    } as PaginatedResponse<Event>)
  }),

  // Event Registration
  http.post('/api/events/:eventId/registration', async ({ params, request }) => {
    const body = await request.json()
    return HttpResponse.json({
      id: `reg-${params.eventId}`,
      eventId: params.eventId,
      userId: '1',
      status: body.action === 'register' ? 'Confirmed' : 'Cancelled',
    })
  }),
]
```

### Testing Custom Hooks
```typescript
// test/hooks/useEvent.test.ts
import { describe, it, expect } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEvent } from '../../src/features/events/hooks/useEvent'

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

function createWrapper() {
  const queryClient = createTestQueryClient()
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  )
}

describe('useEvent', () => {
  it('should fetch event data', async () => {
    const { result } = renderHook(() => useEvent('1'), {
      wrapper: createWrapper(),
    })

    expect(result.current.isPending).toBe(true)

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data).toEqual({
      id: '1',
      title: 'Test Event',
      description: 'A test event',
      currentAttendees: 5,
      maxAttendees: 20,
    })
  })

  it('should not fetch when eventId is empty', () => {
    const { result } = renderHook(() => useEvent(''), {
      wrapper: createWrapper(),
    })

    expect(result.current.isPending).toBe(false)
    expect(result.current.data).toBeUndefined()
  })
})
```

## Security Requirements

### Data Protection
- Sensitive data (payment info, private details) never cached
- Cache cleared on logout automatically
- No personal data in query keys
- Secure token handling in interceptors via httpOnly cookies

### Access Control
```typescript
// hooks/useRoleBasedQuery.ts
export function useRoleBasedQuery<T>(
  role: UserRole,
  queryKey: QueryKey,
  queryFn: QueryFunction<T>,
  options?: UseQueryOptions<T>
) {
  const { data: user } = useAuth()
  
  return useQuery({
    ...options,
    queryKey,
    queryFn,
    enabled: user?.role === role && (options?.enabled ?? true),
  })
}

// Usage
const { data: adminData } = useRoleBasedQuery(
  'Admin',
  queryKeys.adminStats(),
  () => api.get('/api/admin/stats').then(res => res.data)
)
```

## Performance Requirements

### Response Time Targets (FROM RESEARCH)
- **Initial Data Load**: <500ms for cached data
- **Optimistic Updates**: <50ms UI response time  
- **Background Refetch**: <2 seconds for data freshness
- **Cache Hit Ratio**: >90% for frequently accessed data

### Resource Utilization
- **Memory Usage**: <50MB additional for query cache
- **Network Requests**: 60% reduction through intelligent caching
- **Bundle Size**: ~30KB additional for TanStack Query v5

### Performance Optimizations
```typescript
// Performance monitoring utility
export function createPerformanceMonitor(queryClient: QueryClient) {
  return {
    getCacheStats: () => {
      const cache = queryClient.getQueryCache()
      return {
        queryCount: cache.getAll().length,
        memoryUsage: JSON.stringify(cache.getAll()).length,
      }
    },
    
    logSlowQueries: (threshold = 1000) => {
      queryClient.getQueryCache().subscribe((event) => {
        if (event.query.state.dataUpdateCount > 0) {
          const duration = Date.now() - event.query.state.dataUpdatedAt
          if (duration > threshold) {
            console.warn(`Slow query detected: ${JSON.stringify(event.query.queryKey)} took ${duration}ms`)
          }
        }
      })
    },
  }
}
```

## Migration Requirements

### Implementation Phases
1. **Phase 1**: Install and configure TanStack Query v5 alongside existing fetch calls
2. **Phase 2**: Migrate high-frequency endpoints (events, user profile) 
3. **Phase 3**: Add optimistic updates for user actions
4. **Phase 4**: Implement background refresh for real-time features
5. **Phase 5**: Complete migration and remove manual fetch logic

### Data Transformation
- Current fetch() calls → TanStack Query useQuery hooks
- Manual loading states → Automatic isPending states
- Manual error handling → Global error boundaries
- Manual cache management → Automatic cache invalidation

## Dependencies

### Required Packages
```json
{
  "dependencies": {
    "@tanstack/react-query": "^5.85.3",
    "axios": "^1.6.0"
  },
  "devDependencies": {
    "@tanstack/react-query-devtools": "^5.85.3",
    "msw": "^2.0.0",
    "react-error-boundary": "^4.0.11"
  }
}
```

### Configuration Files Required
- `types/react-query.d.ts` - Global error type registration
- `api/queryClient.ts` - QueryClient configuration
- `api/client.ts` - Axios setup with interceptors
- `api/queryKeys.ts` - Query key factory
- `test/setup.ts` - Test configuration
- `test/mocks/handlers.ts` - MSW handlers

## Acceptance Criteria

### Technical Validation (Must Achieve 100%)
- [ ] All 8 integration patterns implemented with working examples
- [ ] TypeScript compilation without errors using v5 syntax
- [ ] Performance benchmarks meet targets (<100ms optimistic updates)
- [ ] Error scenarios handled gracefully with proper rollback
- [ ] Integration tests pass for all patterns using MSW
- [ ] Documentation complete with code examples from research

### Pattern Validation Requirements
- [ ] **Basic CRUD**: useQuery and useMutation with proper TypeScript typing
- [ ] **Optimistic Updates**: Both CSS-only and cache-based patterns working
- [ ] **Cache Invalidation**: Smart cache updates when data changes
- [ ] **Error Handling**: Global error boundaries with query reset capability
- [ ] **Pagination**: useInfiniteQuery with proper memory management
- [ ] **Real-time Integration**: WebSocket updates integrated with cache
- [ ] **Authentication**: httpOnly cookie handling with automatic token refresh
- [ ] **TypeScript**: Global error types and query key factory patterns

### Business Validation (Must Achieve 95%)
- [ ] Team confident in implementation approach (>90% confidence)
- [ ] Patterns reusable across all feature development
- [ ] User experience meets responsiveness expectations
- [ ] Security requirements satisfied (no localStorage for auth)
- [ ] Community standards maintained in error messaging
- [ ] No critical technical blockers identified

## Quality Checklist

- [ ] Follows TanStack Query v5 unified API patterns from research
- [ ] Uses proven query key factory pattern (TkDodo recommendations)
- [ ] Implements global error type registration (v5 feature)
- [ ] Uses gcTime instead of deprecated cacheTime
- [ ] Uses isPending instead of deprecated isLoading
- [ ] Includes proper TypeScript typing for all API interactions
- [ ] Implements both optimistic update patterns from research
- [ ] Uses MSW for realistic API testing
- [ ] Respects Web+API architecture boundaries
- [ ] Includes performance monitoring and cache management utilities
- [ ] Documents all patterns with working code examples
- [ ] Addresses security requirements for httpOnly cookie authentication

**CRITICAL**: This specification is based entirely on proven patterns from the research document. All code examples are adapted from TkDodo's recommendations and official TanStack Query v5 documentation. No patterns are invented - only established, battle-tested approaches are included.

Remember: This is the foundation for all future React development. Every pattern established here will be replicated across features, so accuracy and completeness are essential.