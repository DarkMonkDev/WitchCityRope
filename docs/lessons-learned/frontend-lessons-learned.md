# Frontend Lessons Learned - WitchCityRope React

## CRITICAL TypeScript Compilation Issues ⚠️

**Problem**: TypeScript compilation failing with 74 errors after restoration
**Root Causes**: 
1. UserDto type resolution issue - `lastLoginAt` field missing from resolved type
2. Test mocks using outdated DTO structures (EventSessionDto, TicketTypeDto)
3. Mantine v6 -> v7 API changes (`spacing` prop removed)
4. Missing vitest types in tsconfig

**Prevention Strategy**: Always verify shared-types package is properly built and linked before major restoration work

## Data-TestId Implementation for E2E Testing

**Problem**: Playwright E2E tests need reliable element selectors to avoid brittleness
**Solution**: Add comprehensive data-testid attributes to all interactive React components

### Naming Convention:
- Pages: `data-testid="page-{name}"` (e.g., `page-login`, `page-events`)
- Forms: `data-testid="form-{name}"` (e.g., `form-login`, `form-profile`)
- Inputs: `data-testid="input-{field-name}"` (e.g., `input-email`, `input-search`)
- Buttons: `data-testid="button-{action}"` (e.g., `button-login`, `button-register`)
- Cards: `data-testid="card-{type}"` (e.g., `card-event`)
- Navigation: `data-testid="nav-{location}"` (e.g., `nav-main`)
- Links: `data-testid="link-{destination}"` (e.g., `link-events`, `link-dashboard`)
- Lists: `data-testid="list-{name}"` (e.g., `list-events`, `list-registrations`)
- Sections: `data-testid="section-{name}"` (e.g., `section-hero`, `section-sessions`)
- Widgets: `data-testid="widget-{name}"` (e.g., `widget-events`, `widget-profile`)

### Usage for Playwright Tests:
```typescript
// ✅ CORRECT - Reliable selectors
await page.locator('[data-testid="login-button"]').click();
await page.fill('[data-testid="email-input"]', 'test@example.com');
await expect(page.locator('[data-testid="page-dashboard"]')).toBeVisible();
```

## Critical Button Sizing Issue - WCRButton Component

**Problem**: Button text gets cut off when not properly sized - recurring issue across the application
**Root Causes**: 
1. Explicit height styles that are too small for text metrics
2. Line-height that doesn't accommodate font size properly
3. Fixed height combined with padding causing overflow
4. Inconsistent button styling across components

**Solution - WCRButton Component**:
Created `/apps/web/src/components/ui/WCRButton.tsx` - a standardized button component that prevents text cutoff issues globally.

**Key Features**:
1. **Padding-based sizing**: No fixed heights, uses proper padding
2. **Relative line-height**: Always 1.2 relative to font size
3. **Size variants**: compact-xs, compact-sm, xs, sm, md, lg, xl
4. **Style variants**: primary, secondary, outline, compact, danger
5. **WCR theme integration**: Uses wcr color scheme

**Usage Examples**:
```tsx
import { WCRButton } from '../components/ui';

// ✅ CORRECT - Standard primary button
<WCRButton variant="primary" size="md">Save Event</WCRButton>

// ✅ CORRECT - Compact button with no text cutoff
<WCRButton variant="compact" size="compact-sm">Copy</WCRButton>

// ✅ CORRECT - Secondary gradient style
<WCRButton variant="secondary" size="lg">Add Position</WCRButton>

// ✅ CORRECT - Outline style
<WCRButton variant="outline" size="sm">Cancel</WCRButton>
```

**Prevention Strategy**: 
- **ALWAYS use WCRButton** instead of Mantine Button for consistency
- **No custom button styling** - use variant/size props instead
- **Import from ui/index**: `import { WCRButton } from '../components/ui'`
- **Test all sizes**: Verify text renders properly in all variant/size combinations

## TinyMCE Editor Implementation

**Problem**: Removing working TinyMCE implementation and replacing with basic textarea
**Solution**: ALWAYS verify TinyMCE is working before making changes
**Implementation**:
```tsx
import { Editor } from '@tinymce/tinymce-react';

<Editor
  value={value}
  onEditorChange={(content) => setValue(content)}
  init={{
    height: 300,
    menubar: false,
    plugins: 'advlist autolink lists link charmap preview anchor textcolor colorpicker',
    toolbar: 'undo redo | blocks | bold italic underline strikethrough | link | bullist numlist | indent outdent | removeformat',
    branding: false,
  }}
/>
```

## TinyMCE CDN Loading and Debugging

**Problem**: Misinterpreting 307 redirects as TinyMCE loading failures
**Solution**: 307 redirects from TinyMCE CDN are NORMAL - they route to optimal edge servers
**Key Points**:
- TinyMCE takes 3-5 seconds to initialize from CDN
- Network 307 redirects are expected behavior, not errors  
- Use Playwright to verify actual functionality vs network logs
- API key `3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp` works correctly

## TinyMCE v8 Plugin Compatibility - CRITICAL FIX

**Problem**: TinyMCE v8 deprecated `textcolor` and `colorpicker` plugins causing console errors
**Solution**: Remove deprecated plugins from all Editor configurations
**Before**:
```typescript
plugins: 'advlist autolink lists link charmap preview anchor textcolor colorpicker'
```
**After**:
```typescript
apiKey="3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp"
plugins: 'advlist autolink lists link charmap preview anchor'
```
**Action**: Always check TinyMCE migration docs when upgrading versions

## Missing CSS Classes Console Debugging

**Problem**: Using undefined CSS classes causes silent failures and missing styles
**Solution**: Replace all custom CSS classes with proper Mantine components
**Examples**:
```typescript
// ❌ WRONG - Undefined CSS classes
<button className="btn btn-primary">Add Session</button>
<button className="table-action-btn">Edit</button>

// ✅ CORRECT - Mantine components
<Button variant="filled" color="burgundy">Add Session</Button>
<Button size="compact-xs" variant="light" leftSection={<IconEdit />}>Edit</Button>
```
**Debug Method**: Use Playwright console error capture to find all undefined classes

## Wireframe Adherence

**Problem**: Adding sections not in wireframe (Staff Assignments) 
**Solution**: ALWAYS check wireframe before adding new sections
**Action**: Only implement what's explicitly shown in wireframes

## Tab Naming Precision

**Problem**: Using "Volunteers/Staff" when wireframe shows "Volunteers"
**Solution**: Use EXACT text from wireframes, don't add extra words

## Testing Requirements

**Problem**: Not testing visual changes before claiming completion
**Solution**: ALWAYS use Playwright to take screenshots and verify against wireframes

## Form Structure Following Wireframes

**Problem**: Missing volunteer position edit form at bottom of tab
**Solution**: Follow wireframe layout exactly, including all sections and forms

## Constant Page Reloading Issue - CRITICAL FIX

**Problem**: Pages with React components constantly reload and remount in infinite loops
**Root Cause**: Multiple conflicting Vite dev servers running on the same port causing resource conflicts and hot reload failures

**Solution Steps**:

1. **CRITICAL**: Kill all conflicting dev servers first
```bash
# Check for multiple dev servers
ps aux | grep "vite\|npm" | grep -v grep

# Kill conflicting processes (may require sudo for system processes)
pkill -f vite
# OR restart on different port
npm run dev -- --port 5174
```

2. **API Client 401 Interceptor Fix**: 
```typescript
if (response?.status === 401) {
  const isOnDemoPage = window.location.pathname.includes('/demo') || 
                      window.location.pathname.includes('/admin/events-management-api-demo')
  
  if (!window.location.pathname.includes('/login') && !isOnDemoPage) {
    // Only clear queries and redirect when not on demo pages
    localStorage.removeItem('auth_token')
    queryClient.clear()
    window.location.href = '/login'
  } else {
    // Don't clear queryClient on demo pages to prevent reload loops
    console.log('Skipping redirect and query clearing (on demo page)');
  }
}
```

3. **TanStack Query Optimization**:
```typescript
export function useLegacyEvents() {
  return useQuery({
    queryKey: legacyEventsKeys.eventsList(),
    queryFn: () => legacyEventsApiService.getEvents(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    refetchOnWindowFocus: false,
    refetchOnMount: false,
    refetchInterval: false, // Disable automatic refetching
    refetchOnReconnect: false,
    retry: (failureCount, error: any) => {
      // Don't retry on client errors (4xx)
      if (error?.response?.status >= 400 && error?.response?.status < 500) {
        return false;
      }
      return failureCount < 3;
    },
  });
}
```

4. **Mantine v7 Tabs Fix**:
```typescript
// ❌ WRONG (Mantine v6)
<Tabs value={activeTab} onTabChange={setActiveTab}>

// ✅ CORRECT (Mantine v7)  
<Tabs value={activeTab} onChange={setActiveTab}>
```

**Key Insight**: The primary cause was dev server conflicts, NOT TanStack Query configuration. Always check for multiple dev servers when debugging infinite reload issues.

## Navigation Component Reloading Fix - CRITICAL UPDATE

**Problem**: Navigation component causing constant page reloads despite previous fixes
**Root Cause**: Auth store `checkAuth()` being called repeatedly without proper guards, causing render loops

**Solution**:

1. **Auth Store Guards** - Added cooldown and concurrency protection:
```typescript
checkAuth: async () => {
  const currentState = get();
  
  // Prevent repeated auth checks within a short time period
  if (currentState.lastAuthCheck) {
    const timeSinceLastCheck = Date.now() - currentState.lastAuthCheck.getTime();
    if (timeSinceLastCheck < 5000) { // 5 seconds cooldown
      console.log('🔍 Auth check skipped - recent check within 5 seconds');
      return;
    }
  }
  
  // Skip auth check if already loading to prevent concurrent calls
  if (currentState.isLoading) {
    console.log('🔍 Auth check skipped - already loading');
    return;
  }
  
  // Rest of checkAuth logic...
}
```

2. **Initial Loading State Fix**:
```typescript
// Changed from isLoading: true to false to prevent initial render loops
isLoading: false, // Prevents constant re-renders waiting for auth
```

3. **App.tsx Ref Guard** - Prevent multiple checkAuth calls:
```typescript
const hasCheckedAuth = useRef(false);

useEffect(() => {
  if (!hasCheckedAuth.current) {
    console.log('🔍 App.tsx: Initial auth check starting...');
    hasCheckedAuth.current = true;
    checkAuth().catch((error) => {
      console.error('🔍 App.tsx: Initial auth check failed:', error);
    });
  }
}, []);
```

**Prevention Strategy**: Always add guards and logging to auth-related operations to prevent infinite loops and concurrent calls.

## useEffect Infinite Render Loop - CRITICAL FIX

**Problem**: `useEffect` without proper dependencies causing "Maximum update depth exceeded" errors
**Root Cause**: Using useEffect to update state without dependency array causes re-runs on every render

**Example Error**:
```typescript
// ❌ WRONG - Causes infinite loop
useEffect(() => {
  setRenderCount(renderCount + 1); // Updates state, triggers re-render, runs effect again
});
```

**Solution**: Always include proper dependency array for useEffect
```typescript
// ✅ CORRECT - Runs only once on mount
useEffect(() => {
  setRenderCount(prev => {
    const newCount = prev + 1;
    console.log('Component render count:', newCount);
    return newCount;
  });
}, []); // Empty dependency array for mount-only effect
```

**Key Learning**: When accessing variables in useEffect that should be in scope, use the setState callback pattern with proper dependency arrays to avoid infinite loops.

## Events System Phase 1 Implementation Pattern

**Implementation Pattern**:
```typescript
// 1. Add routes first
{
  path: "events",
  element: <PublicEventsPage />
},
{
  path: "events/:id", 
  element: <EventDetailsPage />
},
{
  path: "admin/events",
  element: <AdminEventsPage />,
  loader: authLoader
}

// 2. Create page components using existing API hooks
const { data: events, isLoading, error } = useEvents();

// 3. Follow established patterns for loading/error states
if (isLoading) return <Loader />;
if (error) return <Alert />;
```

**E2E Test Fix**: 
```typescript
// Fixed localStorage security error in Playwright
test.beforeEach(async ({ page }) => {
  await page.context().clearCookies();
  await page.goto('http://localhost:5174/'); // Navigate first
  
  try {
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
  } catch (error) {
    console.log('⚠️ Could not clear localStorage:', error);
  }
});
```

## EventForm Modal Integration Pattern

**Implementation**:
```tsx
// Modal state management
const [sessionModalOpen, setSessionModalOpen] = useState(false);
const [ticketModalOpen, setTicketModalOpen] = useState(false);
const [editingSession, setEditingSession] = useState<EventSession | null>(null);
const [editingTicketType, setEditingTicketType] = useState<EventTicketType | null>(null);

// Session handlers with modal integration
const handleAddSession = () => {
  setEditingSession(null);
  setSessionModalOpen(true);
};

const handleEditSession = (sessionId: string) => {
  const session = form.values.sessions.find(s => s.id === sessionId);
  if (session) {
    setEditingSession(session);
    setSessionModalOpen(true);
  }
};

const handleSessionSubmit = (sessionData: Omit<EventSession, 'id'>) => {
  if (editingSession) {
    // Update existing session
    const updatedSessions = form.values.sessions.map(session =>
      session.id === editingSession.id
        ? { ...sessionData, id: editingSession.id }
        : session
    );
    form.setFieldValue('sessions', updatedSessions);
  } else {
    // Add new session with crypto.randomUUID()
    const newSession: EventSession = {
      ...sessionData,
      id: crypto.randomUUID()
    };
    form.setFieldValue('sessions', [...form.values.sessions, newSession]);
  }
};
```

**Key Patterns**:
- ✅ Used crypto.randomUUID() for generating unique IDs
- ✅ Proper modal state management with editing vs adding modes
- ✅ Interface alignment between grid and modal components
- ✅ Form state updates using Mantine form.setFieldValue()
- ✅ Modal components rendered at end of form JSX

**Critical Learning**: Always verify interface compatibility between modal forms and display grids. Create conversion functions when interfaces don't align perfectly.

## CRITICAL: Events Management Business Rules Implementation

**Problem**: Implemented generic "registration" system ignoring documented business rules that Classes and Social Events work differently
**Root Cause**: Failed to research requirements before implementation, ignored existing documentation

**Business Rules**:
1. **Classes (eventType: 'class')**: 
   - ONLY ticket purchase (paid), NO RSVP
   - Show "Buy Tickets" button only
   - Display "Tickets Sold" table only

2. **Social Events (eventType: 'social')**: 
   - BOTH RSVP (free) AND optional ticket purchase
   - Show BOTH "RSVP" and "Buy Tickets" buttons
   - Display BOTH "RSVP" table AND "Tickets Sold" table
   - RSVP table shows members + whether they bought tickets

**Implementation Pattern**:
```typescript
// Event type detection
const eventType = event.eventType || 'class';
const isClass = eventType === 'class';
const isSocialEvent = eventType === 'social';

// Classes: Ticket purchase only
{isClass && (
  <Button onClick={handleOpenTicketPurchase} leftSection={<IconTicket />}>
    Buy Tickets
  </Button>
)}

// Social Events: Both RSVP and tickets
{isSocialEvent && (
  <>
    <Button onClick={handleOpenRSVP} leftSection={<IconUsers />}>
      RSVP (Free)
    </Button>
    <Button onClick={handleOpenTicketPurchase} leftSection={<IconTicket />}>
      Buy Tickets
    </Button>
  </>
)}
```

**Prevention Strategy**: 
1. **ALWAYS research requirements FIRST** - Never code without understanding business rules
2. **Check existing documentation** for business logic and terminology
3. **Use business-requirements agent** when in doubt about rules
4. **Test against business requirements** not just technical functionality
5. **Verify event type handling** in ALL components

## Test ID Consistency Critical Issue

**Problem**: E2E tests failing due to inconsistent data-testid attributes between tests and implementation
**Root Cause**: Manual implementation of test IDs without following established naming conventions from testing standards

**Implementation Pattern**:
```typescript
// Events List Container
<Box data-testid="events-list" /* ... */> 
  {events.map(event => (
    <Paper data-testid="event-card" /* ... */ />
  ))}
</Box>

// Controls
<SegmentedControl data-testid="button-view-toggle" /* ... */ />
<TextInput data-testid="input-search" /* ... */ />
<Select data-testid="select-category" /* ... */ />
```

**Prevention Strategy**:
1. **Check existing tests** for expected data-testid values before implementing
2. **Follow naming convention** from testing standards documentation
3. **Test immediately** after adding data-testid attributes  
4. **Batch test ID fixes** instead of addressing one-by-one

## CRITICAL: Events Table Field Mapping Fix

**Problem**: Admin events table showing "Date TBD" and "Time TBD" despite API returning valid dates
**Root Cause**: Component accessing `event.startDateTime` but API returns `startDate`, field mapping not consistently applied  

**Solution**: Enhanced EventsTableView with robust field handling
```typescript
// BEFORE: Assumes field mapping worked perfectly
{formatEventDate(event.startDateTime || '')}
{formatTimeRange(event.startDateTime || '', event.endDateTime || '')}

// AFTER: Handles both API and mapped field names
const formatEventDate = (event: EventDto): string => {
  const dateString = event.startDateTime || (event as any).startDate || '';
  if (!dateString) {
    console.warn('No date field found for event:', { 
      id: event.id, fields: Object.keys(event).filter(k => k.includes('date'))
    });
    return 'Date TBD';
  }
  // ... rest of validation
};

// Usage: Pass full event object instead of individual fields
{formatEventDate(event)}
{formatTimeRange(event)}
```

**Prevention Strategy**: 
- Always handle both mapped and original field names in components
- Add debug logging to identify field mapping issues early  
- Test components with direct API responses not just mapped data
- Use full event objects in formatting functions for maximum field access

## CRITICAL: Date Serialization in Zustand Persist

**Problem**: `TypeError: currentState.lastAuthCheck.getTime is not a function` preventing events from loading
**Root Cause**: Zustand persist middleware stores Date objects as strings in localStorage/sessionStorage, but code expects Date objects

**Solution**:
```typescript
// BEFORE (broken)
const timeSinceLastCheck = Date.now() - currentState.lastAuthCheck.getTime();

// AFTER (fixed)
const lastCheckTime = typeof currentState.lastAuthCheck === 'string' 
  ? new Date(currentState.lastAuthCheck).getTime()
  : currentState.lastAuthCheck.getTime();
const timeSinceLastCheck = Date.now() - lastCheckTime;
```

**Prevention Strategy**:
- Always handle Date objects in Zustand persist as potentially strings
- Use type guards when calling Date methods on persisted values
- Consider custom serialization/deserialization for Date objects in persist middleware
- Test localStorage behavior early in development

## CRITICAL: Test Infrastructure Port Configuration Issues

**Problem**: Critical test infrastructure failures due to hard-coded API ports and duplicate MSW handlers
**Root Causes**: 
1. Multiple duplicate MSW handlers with different hard-coded ports
2. MSW handlers returning raw events instead of API response format
3. Tests using hard-coded `localhost:5651/5653` instead of `VITE_API_BASE_URL`
4. Mixed endpoint casing (/api/auth vs /api/Auth) causing routing conflicts

**Solution**:

### 1. Environment-Based API URL Configuration
```typescript
// BEFORE: Hard-coded ports everywhere
http.post('http://localhost:5653/api/auth/login', ...)
http.post('http://localhost:5655/api/auth/login', ...)
http.post('http://localhost:5651/api/auth/login', ...)

// AFTER: Environment-based URLs
const getApiBaseUrl = () => {
  return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655'
}
const API_BASE_URL = getApiBaseUrl()
http.post(`${API_BASE_URL}/api/Auth/login`, ...)
```

### 2. Fixed MSW Response Format for Events
```typescript
// BEFORE: Raw events (broke useEvents query)
return HttpResponse.json(events)

// AFTER: Proper API response format
return HttpResponse.json({
  success: true,
  data: events
})
```

### 3. Centralized Test Configuration
```typescript
export const TEST_API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655'
```

**Prevention Strategy**:
1. Use centralized test configuration helper for all new tests
2. Verify MSW response format matches actual API structure
3. Use linting rules to prevent hard-coded localhost URLs
4. Document MSW handler patterns for consistency
5. Test events page immediately after API changes

## CRITICAL: DTO Field Mismatch - Events Not Displaying

**Problem**: Events API returns data correctly but React UI shows "No Events Currently Available" despite 10 events in API response
**Root Causes**: 
1. **Field Name Mismatch**: API returns `startDate` but generated EventDto expects `startDateTime`
2. **Missing Status Filter**: Frontend filters for `event.status === 'Published'` but API response has no `status` field
3. **Missing End Date**: API response lacks `endDate` field causing invalid Date objects

**Solution**:

### 1. DTO Field Alignment Fix
```typescript
// Handle both generated type and actual API response field names
const startDateString = (event.startDateTime || (event as any).startDate || '');
const endDateString = (event.endDateTime || (event as any).endDate || '');

const startDate = new Date(startDateString);
// Fallback for missing endDate: assume 2-hour duration
const endDate = endDateString ? new Date(endDateString) : new Date(startDate.getTime() + 2 * 60 * 60 * 1000);
```

### 2. Status Field Missing Fix
```typescript
// BEFORE: All events filtered out
const publishedEvents = events.filter(event => event.status === 'Published')

// AFTER: Treat API-returned events as published if no status field
const publishedEvents = events.filter(event => !event.status || event.status === 'Published')

// Action status fix
if (!event.status || event.status === 'Published') {
  actionStatus = eventType === 'class' ? 'Tickets Available' : 'RSVP & Tickets';
}
```

**Prevention Strategy**:
1. **Validate generated types against actual API responses** regularly
2. **Use field alignment helpers** for common DTO mismatches
3. **Test API integration immediately** after schema changes
4. **Document known field mapping issues** for future reference
5. **Consider creating field mapping utilities** for consistent handling

## CRITICAL: Authentication Token Persistence Issue

**Problem**: Users getting logged out when trying to save event changes because PUT requests return 401 Unauthorized
**Root Cause**: Multiple authentication system issues causing tokens not to be available for authenticated API calls

### Issues and Fixes:

#### 1. Auth Store Token Persistence Issue
**Problem**: Zustand auth store excluded tokens from persistence for security
**Solution**: Temporarily enabled token persistence in sessionStorage for event updates to work
```typescript
// BEFORE: Tokens excluded from persistence
// NEVER persist token or tokenExpiresAt for security

// AFTER: Tokens included for functionality (temporary fix)
token: state.token,
tokenExpiresAt: state.tokenExpiresAt
```

#### 2. API Client Token Access Issue
**Problem**: `/lib/api/client.ts` only checked localStorage for tokens, not the auth store
**Solution**: Enhanced request interceptor to check multiple token sources in priority order
```typescript
// Enhanced token resolution in priority order:
// 1. Auth store (window.__AUTH_STORE__)
// 2. AuthService (authService.getToken()) 
// 3. localStorage fallback
```

#### 3. Dual API Client Architecture
**Discovery**: Project has TWO separate axios instances:
- `/api/client.ts` - Used by auth mutations (login/logout) - Had proper auth integration
- `/lib/api/client.ts` - Used by useEvents hooks (event CRUD) - Had broken auth integration

#### 4. Improved 401 Error Handling
**Solution**: Enhanced error handling with context-aware responses
```typescript
// Differentiate between admin/demo pages and normal pages
// Provide return URL for admin pages
// Clear auth store properly on logout
```

**Critical Learning**: 
- **Multiple API clients require consistent auth patterns** - Don't assume one working client means all are working
- **Token persistence vs security trade-off** - Temporary functionality fix vs long-term security architecture 
- **Auth store access patterns** - Window exposure for cross-module access without circular dependencies
- **401 error handling context matters** - Different user flows need different error responses

## CRITICAL: Reuse Existing Form Components

**Problem**: User frustrated with recreating event details layout when EventForm already exists with complete design
**Solution**: Always reuse existing form components instead of building custom detail views from scratch

**Implementation Pattern**:
```typescript
// BEFORE: Custom detail layout with Cards, Grids, ThemeIcons (300+ lines)
<Grid>
  <Grid.Col span={{ base: 12, md: 8 }}>
    <Card withBorder shadow="sm" padding="lg">
      <Group justify="space-between" mb="md">
        <Title order={3} size="h4">Basic Information</Title>
        <ThemeIcon variant="light" color="wcr.7">
          <IconInfoCircle size={20} />
        </ThemeIcon>
      </Group>
      // ... complex custom layout
    </Card>
  </Grid.Col>
</Grid>

// AFTER: Reuse existing EventForm component (simple)
<EventForm
  initialData={convertEventToFormData(event)}
  onSubmit={handleFormSubmit}
  onCancel={handleFormCancel}
  isSubmitting={false}
/>
```

**Data Conversion Pattern**:
```typescript
// Convert API EventDto to form EventFormData
const convertEventToFormData = (event: EventDto): EventFormData => {
  return {
    eventType: 'class', // Default or derive from event data
    title: event.title,
    shortDescription: event.description.substring(0, 160),
    fullDescription: event.description,
    policies: '', // Handle missing fields gracefully
    venueId: event.location || '',
    teacherIds: [], // Map or default empty arrays
    sessions: [], // Will be populated from related data
    ticketTypes: [], // Will be populated from related data
  };
};
```

**Edit Mode Toggle Pattern**:
```typescript
const [isEditMode, setIsEditMode] = useState(false);

// Toggle between view and edit mode
const handleEdit = () => setIsEditMode(true);
const handleFormCancel = () => setIsEditMode(false);

// Page header adjusts based on mode
<Title>
  {isEditMode ? 'Edit Event' : 'Event Details'}
</Title>

{!isEditMode && (
  <Button onClick={handleEdit}>Edit Event</Button>
)}
```

**Prevention Strategy**:
1. **Survey existing components** before starting detail page implementation
2. **Prefer form reuse over custom layouts** for consistent UX
3. **Create conversion utilities** to bridge API and form data structures
4. **Use simple state toggles** for view/edit mode switching

## Admin Event Details Page Implementation Pattern

### 1. Page Structure Pattern
```typescript
// Page component with proper data fetching and error handling
export const AdminEventDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: event, isLoading, error } = useEvent(id!, !!id);

  // Proper error states and loading states
  if (!id) return <InvalidIdAlert />;
  if (isLoading) return <LoadingSkeleton />;
  if (error || !event) return <NotFoundAlert />;

  return (
    <Container size="xl" py="xl" data-testid="page-admin-event-details">
      {/* Implementation */}
    </Container>
  );
};
```

### 2. Navigation Pattern
```typescript
// EventsTableView.tsx - Updated to navigate to details
const handleRowClick = (eventId: string) => {
  navigate(`/admin/events/${eventId}`);
};
```

### 3. Data Display Patterns
```typescript
// Properly handle HTML description with XSS safety
<Text dangerouslySetInnerHTML={{ __html: event.description }} />

// Use existing utility functions
import { formatEventDate, formatEventTime, calculateEventDuration } from '../../utils/eventUtils';

// Reuse existing CapacityDisplay component
<CapacityDisplay current={event.currentAttendees} max={event.capacity} />
```

## CRITICAL: Hard-Coded Port Configuration Management

**Problem**: Hard-coded localhost ports throughout codebase causing test failures and inconsistent API connections

**Solutions**:

### 1. Centralized Environment Configuration
```typescript
export const config = loadEnvironmentConfig();
export const configHelpers = {
  getWebUrl: (path: string = '') => `${protocol}://localhost${port}${path}`,
  getApiUrl: (endpoint: string = '') => `${config.api.baseUrl}${endpoint}`,
  isFeatureEnabled: (feature) => config.features[feature],
};
```

### 2. Field Mapping Utility
```typescript
export function autoFixEventFieldNames<T extends any[]>(events: T): EventDto[] {
  return events.map((event): EventDto => {
    if (event.startDate) {
      return mapApiEventToDto(event); // API field names → TypeScript types
    }
    return event as EventDto;
  });
}
```

### 3. Updated API Queries
```typescript
export function useEvents() {
  return useQuery({
    queryFn: async () => {
      const response = await api.get('/api/events');
      const rawEvents = response.data?.data || [];
      return autoFixEventFieldNames(rawEvents); // Handles field mapping
    }
  });
}
```

**Prevention Strategy**:
1. **Always use centralized config** - Import from `/config/environment.ts` 
2. **Handle field mapping at query level** - Use autoFixEventFieldNames in API queries
3. **Relative URLs in tests** - Use Playwright baseURL, never hard-coded URLs
4. **Lint rules for compliance** - Prevent hard-coded localhost URLs

## CRITICAL: Test Runner Memory Management - System Crash Prevention

**Problem**: Test runners (Playwright and Vitest) launching unlimited worker processes causing COMPLETE SYSTEM CRASHES

### Root Causes
1. **Playwright**: `workers: process.env.CI ? 1 : undefined` = unlimited workers in development
2. **Vitest**: No worker thread limitations or memory management
3. **No Node.js memory limits** on test processes 
4. **No cleanup mechanisms** for orphaned processes

### Solution

#### 1. Playwright Worker Limits and Browser Args
```typescript
// playwright.config.ts
workers: process.env.CI ? 1 : 2, // CRITICAL: Limit to 2 workers max
maxFailures: 2, // Stop after 2 failures to prevent runaway tests
globalTeardown: './tests/playwright/global-teardown.ts',
launchOptions: {
  args: [
    '--max-old-space-size=1024', // Limit Node.js memory
    '--disable-dev-shm-usage', // Overcome limited resource problems
    '--disable-gpu', // Disable GPU hardware acceleration
    '--memory-pressure-off', // Disable memory pressure warnings
    '--disable-background-timer-throttling', // Prevent hanging tests
  ],
},
```

#### 2. Vitest Single-Thread Configuration
```typescript
// vitest.config.ts
test: {
  pool: 'threads',
  poolOptions: {
    threads: {
      singleThread: true, // CRITICAL: Use single thread only
    }
  },
  maxConcurrency: 1, // Limit concurrent tests
  teardownTimeout: 10000,
  testTimeout: 30000,
}
```

#### 3. Node.js Memory-Limited Test Scripts
```json
// package.json
{
  "test": "NODE_OPTIONS='--max-old-space-size=2048' vitest",
  "test:e2e": "NODE_OPTIONS='--max-old-space-size=2048' playwright test",
  "test:safe": "./scripts/monitor-test-memory.sh vitest",
  "test:e2e:safe": "./scripts/monitor-test-memory.sh playwright",
  "test:cleanup": "./scripts/cleanup-test-processes.sh"
}
```

### MANDATORY USAGE PATTERNS

#### For Development (ALWAYS USE):
```bash
# ✅ SAFE - Use memory-monitored versions
npm run test:safe          # Vitest with memory monitoring
npm run test:e2e:safe      # Playwright with memory monitoring

# ❌ DANGEROUS - Can crash system
npm run test               # Raw vitest (no monitoring)
npm run test:e2e          # Raw playwright (no monitoring)
```

### Prevention Strategy
1. **NEVER** use unlimited workers in test configurations
2. **ALWAYS** set Node.js memory limits on test commands
3. **MONITOR** memory usage during test development
4. **USE** safe test scripts (`test:safe`, `test:e2e:safe`) for development
5. **CLEANUP** immediately if tests crash system
6. **LIMIT** concurrent test execution to prevent resource exhaustion

## Admin Events Table Display Issues

### 1. Date/Time "Invalid Date" Display Fix
**Problem**: EventsTableView showing "Invalid Date" instead of proper dates and times
**Root Cause**: Component was using event.startDateTime but API returns startDate, plus no validation for missing/invalid dates
**Solution**: Enhanced formatEventDate() and formatTimeRange() functions with proper validation and fallbacks
```typescript
// BEFORE: No validation, assumed fields exist
const formatEventDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {...});
};

// AFTER: Validation and fallbacks
const formatEventDate = (dateString: string): string => {
  if (!dateString) return 'Date TBD';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return 'Invalid Date';
  return date.toLocaleDateString('en-US', {...});
};
```

### 2. Capacity "0/0" Display Fix
**Problem**: CapacityDisplay showing "0/0" because API doesn't return capacity/currentAttendees fields
**Solution**: Enhanced CapacityDisplay component with proper fallback handling
```typescript
// BEFORE: Always showed numbers even when undefined
current={event.currentAttendees || 0}
max={event.capacity || 0}

// AFTER: Proper fallback for missing data
if (max === 0 || (max === undefined && current === undefined)) {
  return <Text c="dimmed">Capacity TBD</Text>;
}
```

### 3. Copy Button Styling Improvement
```typescript
// BEFORE: Poor visibility
<Button size="sm" variant="light" color="blue">Copy</Button>

// AFTER: Proper WCR design with explicit sizing
<Button
  size="compact-sm"
  variant="filled"
  color="wcr.7"
  styles={{
    root: {
      minWidth: '60px', height: '32px', fontWeight: 600,
      display: 'flex', alignItems: 'center', justifyContent: 'center'
    }
  }}
>Copy</Button>
```

## Admin Events Dashboard Critical UI Fixes

### 1. Layout Consolidated to Single Line
```tsx
// BEFORE: Stack with multiple lines
<Stack gap="md" mb="lg">
  <Group>Filter controls</Group>
  <Group justify="space-between">Search + toggle</Group>
</Stack>

// AFTER: Single line layout
<Group mb="lg" justify="space-between" align="center" wrap="nowrap">
  <Group align="center" gap="md">
    Filter: [Social] [Class] [Show Past Events]
  </Group>
  <TextInput>Search</TextInput>
</Group>
```

### 2. Perfect Center Alignment
```tsx
// BEFORE: Standard center alignment
style={{ width: '150px', textAlign: 'center' }}

// AFTER: Perfect webkit center alignment 
style={{ width: '150px', textAlign: '-webkit-center' as any }}
```

**Key Patterns**:
- **`-webkit-center` alignment**: Essential for perfect button centering in table cells
- **Text size consistency**: All row text should use same size (`md` = 16px) for visual harmony
- **Button text optimization**: 14px provides good readability in compact buttons without breaking layout

## Admin Route Authentication Issue

**Problem**: User reports "admin events page is not loading data" despite API working correctly
**Root Cause**: Authentication-protected routes redirect to login page when accessed without authentication

**Critical Learning**: 
- **Authentication trumps API connectivity** - a working API doesn't guarantee page access
- **Browser screenshots are essential** for diagnosing route protection issues
- **Login redirects are correct behavior** for protected admin routes
- **Always verify authentication state** when debugging "data not loading" issues

**Prevention Strategy**:
1. **Check authentication first** when debugging admin page issues
2. **Use browser tests with login** to verify protected routes
3. **Distinguish between API issues and auth issues** in troubleshooting
4. **Document protected routes clearly** to avoid confusion

## Font Legibility Issue

**Problem**: User reported Bodoni Moda font in AdminEventDetailsPage title and SegmentedControl is not legible
**Solution**: Changed to Source Sans 3 font to match the filter chips font for consistency

**Key Learning**: 
- **User feedback on legibility takes priority** over design system consistency
- **Match fonts across related UI elements** for visual harmony
- **Default system fonts often more legible** than decorative serif fonts
- **Always check what fonts existing components use** before applying custom fonts

## Admin Event Update API Integration Pattern

### 1. Data Transformation Layer
```typescript
// Convert form data to API format with partial updates
export function convertEventFormDataToUpdateDto(
  eventId: string, 
  formData: EventFormData, 
  isPublished?: boolean
): UpdateEventDto

// Track only changed fields for efficient API calls
export function getChangedEventFields(
  eventId: string,
  current: EventFormData, 
  initial: EventFormData,
  isPublished?: boolean
): UpdateEventDto
```

### 2. Form Integration Pattern
```typescript
const updateEventMutation = useUpdateEvent();
const [initialFormData, setInitialFormData] = useState<EventFormData | null>(null);

const handleFormSubmit = async (data: EventFormData) => {
  // Get only changed fields for efficient partial updates
  const changedFields = initialFormData 
    ? getChangedEventFields(id, data, initialFormData)
    : convertEventFormDataToUpdateDto(id, data);
    
  await updateEventMutation.mutateAsync(changedFields);
};

// Handle publish/draft toggle separately
const confirmStatusChange = async () => {
  await updateEventMutation.mutateAsync({
    id,
    isPublished: pendingStatus === 'published'
  });
};
```

**Key Learning Patterns**:
1. **Data Transformation Layer**: Always create utility functions to convert between form data and API formats
2. **Partial Updates**: Track changed fields and only send modifications to the API
3. **Separate Publish Logic**: Handle publish/draft status changes as separate operations
4. **Comprehensive Error Handling**: Provide specific error messages and fallback handling
5. **Form State Management**: Track initial data to enable change detection and dirty state

## CRITICAL React Unit Test Fixes

### Problem: ProfilePage Tests Failing Due to Multiple "Profile" Elements
**Solution**: Use specific role-based selectors instead of generic text matching:
```typescript
// ❌ WRONG - finds multiple "Profile" elements
expect(screen.getByText('Profile')).toBeInTheDocument()

// ✅ CORRECT - targets specific heading role and level
expect(screen.getByRole('heading', { name: 'Profile', level: 1 })).toBeInTheDocument()
expect(screen.getByRole('heading', { name: 'Profile Information', level: 2 })).toBeInTheDocument()
```

### Problem: Mantine CSS Warnings Cluttering Test Output
**Solution**: Filter console warnings in test setup file:
```typescript
// In src/test/setup.ts - Filter out Mantine CSS warnings
const originalError = console.error
const originalWarn = console.warn

console.error = (...args) => {
  const message = args.join(' ')
  if (
    message.includes('Unsupported style property') ||
    message.includes('Did you mean') ||
    message.includes('mantine-') ||
    message.includes('@media')
  ) {
    return
  }
  originalError.apply(console, args)
}
```

### Problem: MSW Handlers Using Wrong API Endpoints
**Solution**: Match MSW handlers to actual API endpoints used by components:
```typescript
// ❌ WRONG - mocks wrong endpoint
http.get('http://localhost:5655/api/Protected/profile', () => {...})

// ✅ CORRECT - matches useCurrentUser hook endpoint
http.get('/api/auth/user', () => {
  return HttpResponse.json({
    success: true,
    data: { /* user data */ }
  })
})
```

### Problem: TanStack Query v5 cacheTime vs gcTime
**Solution**: Use correct property name for Query Client v5:
```typescript
// ❌ WRONG - v4 syntax
queries: { retry: false, cacheTime: 0 }

// ✅ CORRECT - v5 syntax  
queries: { retry: false, gcTime: 0 }
```

**Key Lessons**:
1. Always check what API endpoints React Query hooks are actually calling
2. Use role-based selectors for better test specificity and accessibility
3. Add explicit test IDs to components when accessibility roles aren't sufficient
4. Filter out framework warnings that aren't test failures to improve test output clarity
5. Disable query caching and retries in tests for predictable behavior
6. Use TanStack Query v5 API (gcTime not cacheTime)