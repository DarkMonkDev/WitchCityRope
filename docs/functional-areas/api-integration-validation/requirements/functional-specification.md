# Functional Specification: API Integration Validation with TanStack Query
<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview

This validation project establishes production-ready patterns for React frontend integration with our .NET API backend using TanStack Query. The goal is to prove all critical API interaction patterns work correctly before full feature development begins. This is throwaway code for pattern validation, not production features.

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI/Auth at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5653
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### TanStack Query Integration Architecture
```
React Components
     ↓
TanStack Query Hooks (useQuery, useMutation)
     ↓
API Client (Axios with interceptors)
     ↓
.NET API Controllers
     ↓
Entity Framework Core
     ↓
PostgreSQL Database
```

### Component Structure
```
/apps/web/src/lib/api/
├── client.ts              # Axios client with interceptors
├── queryClient.ts         # TanStack Query configuration
├── hooks/
│   ├── useEvents.ts       # Event CRUD operations
│   ├── useMembers.ts      # Member management queries
│   ├── useAuth.ts         # Authentication queries
│   └── useOptimistic.ts   # Optimistic update utilities
├── types/
│   ├── api.types.ts       # API response interfaces
│   ├── events.types.ts    # Event-specific types
│   └── members.types.ts   # Member-specific types
└── utils/
    ├── cache.ts           # Cache key factories
    ├── errors.ts          # Error handling utilities
    └── retry.ts           # Retry logic configuration
```

### Service Architecture
- **Web Service**: React components use TanStack Query hooks
- **API Service**: Existing controllers (Events, Auth, Members)
- **No Direct Database Access**: React NEVER directly accesses database

## Data Models

### Existing API Models
Based on existing `/apps/api/Models/`:

```typescript
// Event models (from EventDto.cs)
interface EventDto {
  id: string
  title: string
  description: string
  startDate: string // ISO date string
  location: string
}

interface CreateEventDto {
  title: string
  description: string
  startDate: string
  location: string
}

interface UpdateEventDto {
  id: string
  title?: string
  description?: string
  startDate?: string
  location?: string
}

// Auth models (from Auth/LoginResponse.cs)
interface LoginResponse {
  user: UserDto
  token: string
}

interface UserDto {
  id: string
  email: string
  sceneName: string
  createdAt: string
  lastLoginAt: string
}

// API Response wrapper (from AuthController.cs)
interface ApiResponse<T> {
  success: boolean
  data?: T
  error?: string
  details?: string
  message?: string
  timestamp: string
}
```

### TanStack Query Types
```typescript
// Query key types for type safety
type EventQueryKey = ['events'] | ['events', string] | ['events', 'list', EventFilters]
type MemberQueryKey = ['members'] | ['members', string] | ['members', 'list', MemberFilters]
type AuthQueryKey = ['auth', 'user'] | ['auth', 'profile']

// Query options types
interface PaginationParams {
  page?: number
  limit?: number
  offset?: number
}

interface EventFilters extends PaginationParams {
  startDate?: string
  location?: string
  search?: string
}

interface MemberFilters extends PaginationParams {
  role?: string
  status?: string
  search?: string
}
```

## API Specifications

### Existing Endpoints (from Controllers)
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/events | List events | EventFilters | ApiResponse<EventDto[]> |
| POST | /api/events | Create event | CreateEventDto | ApiResponse<EventDto> |
| PUT | /api/events/{id} | Update event | UpdateEventDto | ApiResponse<EventDto> |
| DELETE | /api/events/{id} | Delete event | - | ApiResponse<boolean> |
| POST | /api/auth/login | Authenticate | LoginDto | ApiResponse<LoginResponse> |
| POST | /api/auth/register | Register user | RegisterDto | ApiResponse<UserDto> |
| GET | /api/auth/user/{id} | Get user | - | ApiResponse<UserDto> |
| POST | /api/auth/logout | Logout | - | ApiResponse<object> |

### New Endpoints for Validation
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/members | List members (paginated) | MemberFilters | ApiResponse<PaginatedResponse<UserDto>> |
| PUT | /api/members/{id}/status | Update member status | StatusUpdateDto | ApiResponse<UserDto> |
| GET | /api/events/{id}/registrations | Event registrations | - | ApiResponse<RegistrationDto[]> |
| POST | /api/events/{id}/register | Register for event | - | ApiResponse<RegistrationDto> |

## Component Specifications

### TanStack Query Client Setup
```typescript
// /apps/web/src/lib/api/queryClient.ts
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      cacheTime: 15 * 60 * 1000, // 15 minutes
      retry: (failureCount, error) => {
        // Custom retry logic based on error type
        if (error.response?.status === 401) return false
        return failureCount < 3
      },
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
    mutations: {
      retry: 1,
    },
  },
})
```

### Axios Client with Interceptors
```typescript
// /apps/web/src/lib/api/client.ts
import axios from 'axios'
import { queryClient } from './queryClient'

const API_BASE_URL = 'http://localhost:5653'

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Include cookies for auth
})

// Request interceptor for auth tokens
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth_token') // or from context
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    console.debug(`API Request: ${config.method?.toUpperCase()} ${config.url}`)
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => {
    console.debug(`API Response: ${response.status} ${response.config.url}`)
    return response
  },
  async (error) => {
    if (error.response?.status === 401) {
      // Clear auth state and redirect to login
      queryClient.clear()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)
```

### Event Management Hooks
```typescript
// /apps/web/src/lib/api/hooks/useEvents.ts
import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import type { EventDto, CreateEventDto, UpdateEventDto, EventFilters } from '../types'

// Query keys factory
export const eventKeys = {
  all: ['events'] as const,
  lists: () => [...eventKeys.all, 'list'] as const,
  list: (filters: EventFilters) => [...eventKeys.lists(), filters] as const,
  details: () => [...eventKeys.all, 'detail'] as const,
  detail: (id: string) => [...eventKeys.details(), id] as const,
}

// Fetch events list
export function useEvents(filters: EventFilters = {}) {
  return useQuery({
    queryKey: eventKeys.list(filters),
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<EventDto[]>>('/api/events', {
        params: filters,
      })
      return data.data || []
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

// Infinite query for pagination
export function useInfiniteEvents(filters: Omit<EventFilters, 'page'> = {}) {
  return useInfiniteQuery({
    queryKey: [...eventKeys.lists(), 'infinite', filters],
    queryFn: async ({ pageParam = 1 }) => {
      const { data } = await apiClient.get<ApiResponse<PaginatedResponse<EventDto>>>('/api/events', {
        params: { ...filters, page: pageParam, limit: 10 },
      })
      return data.data
    },
    getNextPageParam: (lastPage) => 
      lastPage.hasNext ? lastPage.page + 1 : undefined,
  })
}

// Fetch single event
export function useEvent(id: string) {
  return useQuery({
    queryKey: eventKeys.detail(id),
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<EventDto>>(`/api/events/${id}`)
      return data.data
    },
    enabled: !!id,
  })
}

// Create event mutation
export function useCreateEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (eventData: CreateEventDto) => {
      const { data } = await apiClient.post<ApiResponse<EventDto>>('/api/events', eventData)
      return data.data
    },
    onSuccess: (newEvent) => {
      // Invalidate and refetch events list
      queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
      
      // Optimistically add to cache
      queryClient.setQueryData(eventKeys.detail(newEvent.id), newEvent)
    },
  })
}

// Update event mutation with optimistic updates
export function useUpdateEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (eventData: UpdateEventDto) => {
      const { data } = await apiClient.put<ApiResponse<EventDto>>(`/api/events/${eventData.id}`, eventData)
      return data.data
    },
    onMutate: async (updatedEvent) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(updatedEvent.id) })
      
      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(eventKeys.detail(updatedEvent.id))
      
      // Optimistically update cache
      queryClient.setQueryData(eventKeys.detail(updatedEvent.id), (old: EventDto) => ({
        ...old,
        ...updatedEvent,
      }))
      
      return { previousEvent }
    },
    onError: (err, updatedEvent, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(updatedEvent.id), context.previousEvent)
      }
    },
    onSettled: (data, error, updatedEvent) => {
      // Always refetch after error or success
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(updatedEvent.id) })
      queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
    },
  })
}

// Delete event mutation
export function useDeleteEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/events/${id}`)
      return id
    },
    onSuccess: (deletedId) => {
      queryClient.removeQueries({ queryKey: eventKeys.detail(deletedId) })
      queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
    },
  })
}
```

### Member Management Hooks
```typescript
// /apps/web/src/lib/api/hooks/useMembers.ts
import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import type { UserDto, MemberFilters, StatusUpdateDto } from '../types'

export const memberKeys = {
  all: ['members'] as const,
  lists: () => [...memberKeys.all, 'list'] as const,
  list: (filters: MemberFilters) => [...memberKeys.lists(), filters] as const,
  details: () => [...memberKeys.all, 'detail'] as const,
  detail: (id: string) => [...memberKeys.details(), id] as const,
}

// Infinite query for member directory with pagination
export function useInfiniteMembers(filters: Omit<MemberFilters, 'page'> = {}) {
  return useInfiniteQuery({
    queryKey: [...memberKeys.lists(), 'infinite', filters],
    queryFn: async ({ pageParam = 1 }) => {
      const { data } = await apiClient.get<ApiResponse<PaginatedResponse<UserDto>>>('/api/members', {
        params: { ...filters, page: pageParam, limit: 20 },
      })
      return data.data
    },
    getNextPageParam: (lastPage) => 
      lastPage.hasNext ? lastPage.page + 1 : undefined,
    staleTime: 10 * 60 * 1000, // 10 minutes for member data
  })
}

// Update member status with optimistic updates
export function useUpdateMemberStatus() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async ({ id, status }: { id: string; status: string }) => {
      const { data } = await apiClient.put<ApiResponse<UserDto>>(`/api/members/${id}/status`, { status })
      return data.data
    },
    onMutate: async ({ id, status }) => {
      await queryClient.cancelQueries({ queryKey: memberKeys.detail(id) })
      
      const previousMember = queryClient.getQueryData(memberKeys.detail(id))
      
      // Optimistically update member status
      queryClient.setQueryData(memberKeys.detail(id), (old: UserDto) => ({
        ...old,
        status,
      }))
      
      return { previousMember }
    },
    onError: (err, { id }, context) => {
      if (context?.previousMember) {
        queryClient.setQueryData(memberKeys.detail(id), context.previousMember)
      }
    },
    onSettled: (data, error, { id }) => {
      queryClient.invalidateQueries({ queryKey: memberKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: memberKeys.lists() })
    },
  })
}
```

### Authentication Hooks
```typescript
// /apps/web/src/lib/api/hooks/useAuth.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../client'
import type { LoginCredentials, RegisterCredentials, LoginResponse, UserDto } from '../types'

export const authKeys = {
  all: ['auth'] as const,
  user: () => [...authKeys.all, 'user'] as const,
  profile: () => [...authKeys.all, 'profile'] as const,
}

// Current user query
export function useCurrentUser() {
  return useQuery({
    queryKey: authKeys.user(),
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<UserDto>>('/api/auth/me')
      return data.data
    },
    staleTime: 30 * 60 * 1000, // 30 minutes
    retry: false, // Don't retry auth failures
  })
}

// Login mutation
export function useLogin() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (credentials: LoginCredentials) => {
      const { data } = await apiClient.post<ApiResponse<LoginResponse>>('/api/auth/login', credentials)
      return data.data
    },
    onSuccess: (loginResponse) => {
      // Store token and user data
      localStorage.setItem('auth_token', loginResponse.token)
      queryClient.setQueryData(authKeys.user(), loginResponse.user)
    },
  })
}

// Logout mutation
export function useLogout() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async () => {
      await apiClient.post('/api/auth/logout')
    },
    onSuccess: () => {
      localStorage.removeItem('auth_token')
      queryClient.clear() // Clear all cached data
    },
  })
}
```

### Optimistic Update Utilities
```typescript
// /apps/web/src/lib/api/hooks/useOptimistic.ts
import { useMutation, useQueryClient } from '@tanstack/react-query'

interface OptimisticUpdateOptions<TData, TVariables> {
  queryKey: unknown[]
  mutationFn: (variables: TVariables) => Promise<TData>
  updateFn: (oldData: TData, variables: TVariables) => TData
  onError?: (error: Error, variables: TVariables, context: unknown) => void
}

export function useOptimisticUpdate<TData, TVariables>({
  queryKey,
  mutationFn,
  updateFn,
  onError,
}: OptimisticUpdateOptions<TData, TVariables>) {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn,
    onMutate: async (variables) => {
      await queryClient.cancelQueries({ queryKey })
      
      const previousData = queryClient.getQueryData<TData>(queryKey)
      
      if (previousData) {
        queryClient.setQueryData(queryKey, updateFn(previousData, variables))
      }
      
      return { previousData }
    },
    onError: (error, variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      onError?.(error, variables, context)
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
```

## Integration Points

### WebSocket Integration
```typescript
// /apps/web/src/lib/api/websocket.ts
import { queryClient } from './queryClient'
import { eventKeys } from './hooks/useEvents'

class WebSocketManager {
  private ws: WebSocket | null = null
  
  connect() {
    this.ws = new WebSocket('ws://localhost:5653/ws')
    
    this.ws.onmessage = (event) => {
      const message = JSON.parse(event.data)
      this.handleMessage(message)
    }
  }
  
  private handleMessage(message: any) {
    switch (message.type) {
      case 'EVENT_UPDATED':
        // Invalidate event queries to refetch fresh data
        queryClient.invalidateQueries({ queryKey: eventKeys.detail(message.eventId) })
        queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
        break
      
      case 'EVENT_REGISTRATION':
        // Update registration count optimistically
        queryClient.setQueryData(
          eventKeys.detail(message.eventId),
          (oldEvent: EventDto) => ({
            ...oldEvent,
            registrationCount: message.newCount,
          })
        )
        break
    }
  }
}

export const wsManager = new WebSocketManager()
```

## Security Requirements

### Authentication Flow
1. User logs in via `/api/auth/login`
2. API returns JWT token and user data
3. Token stored in memory (not localStorage for security)
4. Token included in Authorization header for all requests
5. 401 responses trigger automatic logout and cache clearing

### Data Protection
```typescript
// Sensitive data caching rules
const sensitiveQueries = {
  // Never cache payment or private data
  payments: { cacheTime: 0, staleTime: 0 },
  personalInfo: { cacheTime: 5 * 60 * 1000, staleTime: 0 },
  
  // Standard caching for public data
  events: { cacheTime: 15 * 60 * 1000, staleTime: 5 * 60 * 1000 },
  members: { cacheTime: 10 * 60 * 1000, staleTime: 5 * 60 * 1000 },
}
```

## Performance Requirements

### Response Time Targets
- **Initial Data Load**: <500ms for cached data
- **Optimistic Updates**: <50ms UI response time  
- **Background Refetch**: <2 seconds for data freshness
- **Cache Hit Ratio**: >90% for frequently accessed data

### Bundle Size Impact
- TanStack Query: ~25KB gzipped
- Axios: ~15KB gzipped
- Total addition: <50KB to bundle

### Memory Management
```typescript
// Configure garbage collection for queries
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      cacheTime: 15 * 60 * 1000, // 15 minutes
      staleTime: 5 * 60 * 1000,  // 5 minutes
    },
  },
})

// Clear cache on memory pressure
if ('memory' in performance) {
  setInterval(() => {
    if (performance.memory.usedJSHeapSize > 50 * 1024 * 1024) { // 50MB
      queryClient.clear()
    }
  }, 60000) // Check every minute
}
```

## Testing Requirements

### Unit Testing Patterns
```typescript
// Test TanStack Query hooks with MSW
import { renderHook, waitFor } from '@testing-library/react'
import { rest } from 'msw'
import { setupServer } from 'msw/node'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEvents } from '../hooks/useEvents'

const server = setupServer(
  rest.get('/api/events', (req, res, ctx) => {
    return res(ctx.json({
      success: true,
      data: [{ id: '1', title: 'Test Event', /* ... */ }]
    }))
  })
)

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } }
  })
  return ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

test('useEvents hook fetches events successfully', async () => {
  const { result } = renderHook(() => useEvents(), { wrapper: createWrapper() })
  
  await waitFor(() => expect(result.current.isSuccess).toBe(true))
  expect(result.current.data).toHaveLength(1)
  expect(result.current.data[0].title).toBe('Test Event')
})
```

### Integration Testing
```typescript
// Test optimistic updates with error recovery
test('event update with optimistic updates and rollback', async () => {
  // Setup: Mock successful then failed API calls
  server.use(
    rest.put('/api/events/1', (req, res, ctx) => {
      return res.once(ctx.status(500))
    })
  )
  
  const { result } = renderHook(() => useUpdateEvent(), { wrapper: createWrapper() })
  
  // Trigger optimistic update
  result.current.mutate({ id: '1', title: 'Updated Title' })
  
  // Verify rollback on error
  await waitFor(() => expect(result.current.isError).toBe(true))
  // Verify original data restored
})
```

### Performance Testing
```typescript
// Benchmark query performance
test('query performance benchmarks', async () => {
  const start = performance.now()
  
  const { result } = renderHook(() => useEvents(), { wrapper: createWrapper() })
  
  await waitFor(() => expect(result.current.isSuccess).toBe(true))
  
  const duration = performance.now() - start
  expect(duration).toBeLessThan(100) // Should complete in <100ms
})
```

## Migration Requirements

### No Breaking Changes
This validation project adds new dependencies without modifying existing code:
- Existing `authService.ts` remains functional
- Current React patterns continue to work
- New TanStack Query patterns are additive

### Incremental Adoption
```typescript
// Components can gradually adopt TanStack Query
// Old pattern (still works):
const [events, setEvents] = useState([])
useEffect(() => {
  fetchEvents().then(setEvents)
}, [])

// New pattern (optional migration):
const { data: events, isLoading } = useEvents()
```

## Dependencies

### Required Packages
```json
{
  "dependencies": {
    "@tanstack/react-query": "^5.20.0",
    "@tanstack/react-query-devtools": "^5.20.0",
    "axios": "^1.6.0"
  }
}
```

### Development Dependencies
```json
{
  "devDependencies": {
    "msw": "^2.0.0",
    "@testing-library/react-hooks": "^8.0.0"
  }
}
```

## Validation Scenarios

### Scenario 1: Event Registration Flow (Happy Path)
```typescript
// Test complete optimistic update flow
const EventRegistration = ({ eventId }: { eventId: string }) => {
  const { data: event } = useEvent(eventId)
  const registerMutation = useRegisterForEvent()
  
  const handleRegister = () => {
    registerMutation.mutate(eventId, {
      onSuccess: () => {
        // Show success message
        toast.success('Registered successfully!')
      },
      onError: (error) => {
        // Show error with rollback
        toast.error('Registration failed. Please try again.')
      }
    })
  }
  
  return (
    <button 
      onClick={handleRegister}
      disabled={registerMutation.isLoading}
    >
      {registerMutation.isLoading ? 'Registering...' : 'Register'}
    </button>
  )
}
```

### Scenario 2: Real-time Updates
```typescript
// Test WebSocket integration with cache updates
useEffect(() => {
  wsManager.connect()
  
  return () => wsManager.disconnect()
}, [])

// Cache automatically updates when WebSocket receives event updates
```

### Scenario 3: Error Recovery
```typescript
// Test network failure handling
const EventsList = () => {
  const { data: events, error, refetch } = useEvents()
  
  if (error) {
    return (
      <div>
        <p>Failed to load events. Please try again.</p>
        <button onClick={() => refetch()}>Retry</button>
      </div>
    )
  }
  
  return <div>{/* Render events */}</div>
}
```

## Acceptance Criteria

### Technical Validation (Must Achieve 100%)
- [ ] All 8 patterns implemented with working examples
- [ ] TypeScript compilation without errors
- [ ] Performance benchmarks meet targets (sub-100ms response)
- [ ] Error scenarios handled gracefully with rollback
- [ ] Integration tests pass for all patterns
- [ ] WebSocket updates integrate with query cache
- [ ] Optimistic updates work with proper rollback
- [ ] Cache invalidation strategies function correctly

### Pattern Validation (Must Achieve 100%)
- [ ] Basic CRUD operations with useQuery/useMutation
- [ ] Optimistic updates with error rollback  
- [ ] Pagination with useInfiniteQuery
- [ ] Background refetching without UI disruption
- [ ] Request/response interceptors for auth
- [ ] Cache invalidation on data changes
- [ ] Error boundaries and retry logic
- [ ] TypeScript type safety throughout

### Integration Validation (Must Achieve 95%)
- [ ] Works with existing .NET API endpoints
- [ ] Integrates with current authentication flow
- [ ] Compatible with existing React components
- [ ] WebSocket events update query cache
- [ ] Performance meets targets under load
- [ ] Memory usage stays within bounds
- [ ] Bundle size impact acceptable (<50KB)

## Success Metrics

### Performance Targets
- **Query Cache Hit Rate**: >90%
- **Background Refetch Time**: <2 seconds
- **Optimistic Update Response**: <50ms
- **Memory Usage**: <50MB for query cache
- **Bundle Size Addition**: <50KB

### Code Quality Targets
- **TypeScript Coverage**: 100% (no `any` types)
- **Test Coverage**: >90% for query hooks
- **Error Handling**: All failure scenarios covered
- **Documentation**: Complete pattern documentation

## Quality Gate Checklist

- [ ] All user roles addressed in validation scenarios
- [ ] Clear acceptance criteria for each pattern
- [ ] Business value clearly defined for technical validation  
- [ ] Edge cases considered (network failures, conflicts)
- [ ] Security requirements documented and implemented
- [ ] Performance expectations set with measurable targets
- [ ] Mobile experience considered
- [ ] Examples provided for each pattern
- [ ] Success metrics defined and measurable
- [ ] Integration with existing architecture validated
- [ ] TypeScript patterns established and documented
- [ ] Error handling comprehensive with rollback
- [ ] Cache management strategies defined and tested
- [ ] Real-time update patterns validated
- [ ] Documentation standards maintained

## Implementation Notes

### This is Throwaway Code
- Purpose is pattern validation, not feature delivery
- Code artifacts will be reference examples, not production
- Focus on proving concepts and documenting patterns  
- Lessons learned will inform all future development

### Next Steps After Validation
1. **Pattern Documentation**: Create reusable implementation guides
2. **Type Definitions**: Establish standard TypeScript patterns
3. **Testing Strategies**: Document how to test TanStack Query integrations
4. **Performance Monitoring**: Set up metrics collection
5. **Team Training**: Share patterns with development team

This validation project provides the technical foundation for building robust, performant React features with confidence in our chosen technology stack.