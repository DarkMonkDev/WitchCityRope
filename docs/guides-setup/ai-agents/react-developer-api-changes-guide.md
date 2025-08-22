# React Developer API Changes Guide - Vertical Slice Integration
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.1 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## 🚨 CRITICAL: DOCUMENTATION STRUCTURE ENFORCEMENT 🚨

**ZERO TOLERANCE POLICY - VIOLATIONS = IMMEDIATE SESSION FAILURE**

### Documentation Structure Rules (NEVER VIOLATE):
- ❌ **NEVER** create files in `/docs/` root (only 6 approved files allowed)
- ✅ **ALWAYS** check `/docs/architecture/functional-area-master-index.md` FIRST
- ✅ **ALWAYS** use proper functional area paths:
  - `/docs/functional-areas/[area]/` - Feature work
  - `/docs/guides-setup/` - Guides and setup
  - `/docs/lessons-learned/` - Lessons learned
  - `/docs/standards-processes/` - Standards
  - `/docs/architecture/` - Architecture decisions
  - `/docs/design/` - Design documents
  - `/docs/_archive/` - Archived content
- 🔍 **MANDATORY** structure validation: `bash /docs/architecture/docs-structure-validator.sh`
- 📝 **MANDATORY** file registry updates for ALL operations

### Pre-Flight Checklist (MANDATORY FOR EVERY SESSION):
- [ ] Check functional-area-master-index.md for proper location
- [ ] Verify NOT creating in /docs/ root
- [ ] Use existing functional area structure
- [ ] Update file registry for all operations
- [ ] Run structure validator after operations

### Violation Consequences:
- **Root pollution** → IMMEDIATE STOP + escalate to librarian
- **Multiple archives** → EMERGENCY + immediate fix required
- **/docs/docs/** → CATASTROPHIC + session termination
- **Shortcuts** → VIOLATION + agent retraining

---

## Executive Summary

This guide provides React developers with essential information about API architecture changes as WitchCityRope migrates to **Simple Vertical Slice Architecture**. The frontend integration remains largely unchanged due to consistent API contracts, but developers must understand the new endpoint organization patterns and improved response formats.

**🎯 PRIMARY RESOURCE**: Use `/docs/architecture/REACT-ARCHITECTURE-INDEX.md` as your **SINGLE SOURCE** for all React architecture documentation. This guide is one component referenced in that comprehensive index.

### Key Changes Overview
- **Minimal Impact on Frontend**: API contracts maintained for backward compatibility
- **Improved API Organization**: Feature-based endpoint grouping
- **Enhanced Response Formats**: Consistent response patterns and error handling
- **Better Type Generation**: NSwag continues to work with improved DTO design

---

## API Architecture Changes (Backend Context)

### Before: Traditional Controller Architecture
```csharp
// Old pattern - Controllers with complex dependencies
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator; // Complex MediatR pipeline
    
    [HttpGet]
    public async Task<ActionResult<List<EventDto>>> GetEvents(
        [FromQuery] GetEventsQuery query)
    {
        return await _mediator.Send(query);
    }
}
```

### After: Simple Vertical Slice Architecture
```csharp  
// New pattern - Direct service injection with minimal API
app.MapGet("/api/events", async (
    EventService eventService,
    CancellationToken cancellationToken) =>
    {
        var (success, response, error) = await eventService.GetEventsAsync(cancellationToken);
        return success ? Results.Ok(response) : Results.BadRequest(error);
    });
```

### Impact on Frontend
- ✅ **Same API endpoints**: `/api/events` still works
- ✅ **Same response formats**: EventDto types unchanged
- ✅ **Same authentication**: Cookie-based auth unchanged
- ✅ **NSwag generation**: Continues to work with improved DTOs

---

## API Contract Improvements

### 1. Consistent Response Formats

**Before**: Inconsistent response patterns
```typescript
// Some endpoints returned data directly
const events: Event[] = await response.json();

// Others wrapped in result objects  
const result: { data: Event[], success: boolean } = await response.json();
```

**After**: Consistent response patterns
```typescript
// All successful responses return data directly
const events: Event[] = await response.json();

// Error responses use standard Problem Details format
interface ProblemDetails {
    title: string;
    detail: string; 
    status: number;
    type?: string;
}
```

### 2. Enhanced Error Handling

**Before**: Mixed error formats
```typescript
// Different endpoints returned errors differently
try {
    const response = await fetch('/api/events');
    if (!response.ok) {
        // Could be plain text, JSON, or various formats
        const error = await response.text();
    }
} catch (error) {
    // Handle network errors
}
```

**After**: Standardized error responses
```typescript
// Consistent Problem Details format for all errors
try {
    const response = await fetch('/api/events');
    if (!response.ok) {
        const problem: ProblemDetails = await response.json();
        console.error(`${problem.title}: ${problem.detail}`);
        return;
    }
    
    const events: Event[] = await response.json();
} catch (error) {
    // Handle network errors
    console.error('Network error:', error);
}
```

### 3. Improved Pagination Patterns

**Before**: Inconsistent pagination
```typescript
// Different endpoints used different pagination formats
interface PagedResponse<T> {
    data: T[];
    totalCount?: number;
    pageSize?: number;
    currentPage?: number;
}
```

**After**: Standardized pagination
```typescript  
// Consistent pagination across all endpoints
interface PagedResponse<T> {
    data: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}

// Usage remains similar but more consistent
const pagedEvents: PagedResponse<Event> = await response.json();
```

---

## NSwag Type Generation Improvements

### Enhanced DTO Design

**Before**: Basic DTOs
```typescript
// Generated types were basic
export interface EventDto {
    id?: string;
    title?: string;
    date?: Date;
    // Optional everything, minimal documentation
}
```

**After**: Well-documented DTOs
```typescript
// Generated types have comprehensive documentation and validation
export interface EventResponse {
    /** Unique identifier for the event */
    id: string;
    
    /** Event title (required, max 200 characters) */
    title: string;
    
    /** Event date and time in UTC */
    date: Date;
    
    /** Event duration in minutes */
    durationMinutes: number;
    
    /** Maximum number of participants */
    maxParticipants: number;
    
    /** Current number of registered participants */
    currentParticipants: number;
    
    /** Instructor information */
    instructor: InstructorResponse;
    
    /** Whether user can register for this event */
    canRegister: boolean;
    
    /** Registration deadline */
    registrationDeadline?: Date;
}
```

### Improved Request Models
```typescript
// Better request models with validation hints
export interface CreateEventRequest {
    /** Event title (required, 3-200 characters) */
    title: string;
    
    /** Event description (required, max 2000 characters) */
    description: string;
    
    /** Event date (must be future date) */
    date: Date;
    
    /** Duration in minutes (15-480 minutes) */  
    durationMinutes: number;
    
    /** Maximum participants (1-50) */
    maxParticipants: number;
    
    /** Instructor ID (must be valid instructor) */
    instructorId: string;
}
```

---

## Feature-Based API Organization

### New Endpoint Organization Pattern

**Before**: Controller-based organization
```
/api/Events/GetEvents
/api/Events/GetEvent/{id} 
/api/Events/CreateEvent
/api/Authentication/Login
/api/Authentication/Register
/api/Users/GetProfile
```

**After**: Feature-based organization (same URLs, better backend structure)
```
/api/events                    # Features/Events/Endpoints/
/api/events/{id}              # Features/Events/Endpoints/
/api/events                   # POST - Features/Events/Endpoints/
/api/auth/login               # Features/Authentication/Endpoints/
/api/auth/register            # Features/Authentication/Endpoints/
/api/users/profile            # Features/Users/Endpoints/
```

**Frontend Impact**: URLs remain the same, but better organized backend means:
- More consistent response formats
- Better error handling
- Improved performance
- Faster development of new features

---

## Integration Patterns

### 1. API Client Pattern (Recommended)

```typescript
// Create centralized API client for consistent error handling
export class ApiClient {
    private baseUrl: string;
    
    constructor(baseUrl: string = '/api') {
        this.baseUrl = baseUrl;
    }
    
    async get<T>(endpoint: string): Promise<T> {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            credentials: 'include' // Include cookies for auth
        });
        
        if (!response.ok) {
            const problem = await this.handleError(response);
            throw new ApiError(problem);
        }
        
        return response.json();
    }
    
    async post<TRequest, TResponse>(
        endpoint: string, 
        data: TRequest
    ): Promise<TResponse> {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            const problem = await this.handleError(response);
            throw new ApiError(problem);
        }
        
        return response.json();
    }
    
    private async handleError(response: Response): Promise<ProblemDetails> {
        try {
            return await response.json();
        } catch {
            return {
                title: 'API Error',
                detail: `HTTP ${response.status}: ${response.statusText}`,
                status: response.status
            };
        }
    }
}

// Custom error class for API errors
export class ApiError extends Error {
    constructor(public problem: ProblemDetails) {
        super(problem.detail);
        this.name = 'ApiError';
    }
}

// Usage
const apiClient = new ApiClient();

// Get events
const events = await apiClient.get<Event[]>('/events');

// Create event
const newEvent = await apiClient.post<CreateEventRequest, EventResponse>(
    '/events',
    { title: 'New Event', /* ... */ }
);
```

### 2. React Hook Integration

```typescript
// Custom hooks for API integration
export function useEvents() {
    const [events, setEvents] = useState<Event[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    
    const loadEvents = useCallback(async () => {
        setLoading(true);
        setError(null);
        
        try {
            const apiClient = new ApiClient();
            const events = await apiClient.get<Event[]>('/events');
            setEvents(events);
        } catch (err) {
            if (err instanceof ApiError) {
                setError(err.problem.detail);
            } else {
                setError('Failed to load events');
            }
        } finally {
            setLoading(false);
        }
    }, []);
    
    useEffect(() => {
        loadEvents();
    }, [loadEvents]);
    
    return { events, loading, error, reload: loadEvents };
}

// Usage in component
export function EventsPage() {
    const { events, loading, error, reload } = useEvents();
    
    if (loading) return <div>Loading events...</div>;
    if (error) return <div>Error: {error}</div>;
    
    return (
        <div>
            <button onClick={reload}>Refresh</button>
            {events.map(event => (
                <EventCard key={event.id} event={event} />
            ))}
        </div>
    );
}
```

---

## Authentication Integration

### Cookie-Based Authentication (Unchanged)
```typescript
// Authentication remains the same - cookie-based
export async function login(email: string, password: string): Promise<void> {
    const apiClient = new ApiClient();
    
    try {
        await apiClient.post<LoginRequest, LoginResponse>('/auth/login', {
            email,
            password
        });
        
        // Cookie automatically set by server
        // Redirect or update UI state
    } catch (err) {
        if (err instanceof ApiError) {
            throw new Error(err.problem.detail);
        }
        throw err;
    }
}

// Logout
export async function logout(): Promise<void> {
    const apiClient = new ApiClient();
    await apiClient.post('/auth/logout', {});
    // Cookie automatically cleared by server
}

// Check authentication status  
export async function getCurrentUser(): Promise<UserResponse | null> {
    try {
        const apiClient = new ApiClient();
        return await apiClient.get<UserResponse>('/auth/me');
    } catch (err) {
        if (err instanceof ApiError && err.problem.status === 401) {
            return null; // Not authenticated
        }
        throw err;
    }
}
```

---

## Type Generation Workflow

### Updated NSwag Generation Process

**1. Generate Types After API Changes**
```bash
# Run type generation after backend changes
npm run generate:types
```

**2. Verify Generated Types**
```typescript  
// Check that generated types include new improvements
import { 
    EventResponse,        // Enhanced with documentation
    CreateEventRequest,   // Improved validation info  
    PagedResponse,        // Consistent pagination
    ProblemDetails        // Standard error format
} from './api/generated-types';
```

**3. Update Components**
```typescript
// Update components to use improved types
interface EventCardProps {
    event: EventResponse; // Use generated type with full documentation
}

export function EventCard({ event }: EventCardProps) {
    return (
        <div>
            <h3>{event.title}</h3>
            <p>Date: {new Date(event.date).toLocaleDateString()}</p>
            <p>Duration: {event.durationMinutes} minutes</p>
            <p>Participants: {event.currentParticipants}/{event.maxParticipants}</p>
            {event.canRegister && (
                <button>Register</button>
            )}
        </div>
    );
}
```

---

## Migration Checklist

### ✅ When Backend Changes API Endpoint

**Immediate Actions**:
- [ ] Run `npm run generate:types` to update TypeScript types
- [ ] Test existing API calls still work
- [ ] Update any hardcoded response format assumptions
- [ ] Verify error handling works with new Problem Details format

**Testing**:
- [ ] Test happy path scenarios
- [ ] Test error scenarios (network errors, validation errors, server errors)
- [ ] Test authentication flows (login, logout, protected routes)
- [ ] Test pagination if applicable

**Code Updates (If Needed)**:
- [ ] Update API client to use consistent error handling
- [ ] Replace direct fetch calls with centralized API client
- [ ] Update error handling to use Problem Details format
- [ ] Update pagination handling if changed

### ✅ When New Features Added

**Type Generation**:
- [ ] Generate new types: `npm run generate:types`
- [ ] Import new types in components
- [ ] Use generated request/response models

**Component Updates**:
- [ ] Create new API service functions
- [ ] Add new React hooks for data fetching
- [ ] Implement new UI components
- [ ] Add error handling for new endpoints

---

## Troubleshooting

### Common Issues and Solutions

**1. Type Generation Fails**
```bash
# Ensure API is running
npm run dev:api

# Check API is accessible
curl http://localhost:5653/swagger/v1/swagger.json

# Regenerate types
npm run generate:types
```

**2. API Calls Return Different Format**
```typescript
// Check if endpoint was migrated to new format
// Old format might return data directly
// New format might wrap in consistent response structure

// Add logging to debug response format
const response = await fetch('/api/events');
const data = await response.json();
console.log('API Response:', data); // Debug actual format
```

**3. Authentication Issues After Changes**
```typescript
// Ensure cookies are included in requests
fetch('/api/events', {
    credentials: 'include' // Essential for cookie auth
});

// Check auth status
const user = await getCurrentUser();
if (!user) {
    // Redirect to login
}
```

**4. Error Handling Inconsistencies**
```typescript
// Standardize error handling across all API calls
try {
    const data = await apiClient.get('/events');
} catch (err) {
    if (err instanceof ApiError) {
        // Handle structured API error
        console.error(err.problem.title, err.problem.detail);
    } else {
        // Handle network or other errors
        console.error('Unexpected error:', err);
    }
}
```

---

## Performance Considerations

### Optimizations from New Architecture

**1. Improved Response Times**
- Backend eliminates MediatR pipeline overhead
- Direct Entity Framework queries are faster
- Reduced memory allocation from simpler patterns

**2. Better Caching Opportunities**
```typescript
// New consistent response formats enable better caching
const eventCache = new Map<string, Event[]>();

export function useEventsWithCache() {
    const cacheKey = 'events';
    
    if (eventCache.has(cacheKey)) {
        return eventCache.get(cacheKey)!;
    }
    
    // Fetch and cache
    const events = await apiClient.get<Event[]>('/events');
    eventCache.set(cacheKey, events);
    return events;
}
```

**3. Smaller Bundle Sizes**
- More focused DTO models reduce generated type sizes
- Better tree-shaking with feature-based organization

---

## Success Metrics

### Frontend Integration Health
- **Type Generation**: Successful generation after every backend change
- **API Compatibility**: No breaking changes to existing frontend code  
- **Error Handling**: Consistent error experience across all features
- **Performance**: Response times maintained or improved

### Developer Experience
- **Faster Development**: Consistent patterns speed up new feature development
- **Better Debugging**: Clearer error messages and structured responses
- **Improved Maintainability**: Centralized API client reduces code duplication
- **Enhanced Type Safety**: Better generated types catch more errors at compile time

---

Remember: The API architecture modernization is designed to **minimize frontend impact** while providing **significant backend improvements**. Focus on consistent error handling, proper type generation workflow, and taking advantage of the improved API contracts when they become available.