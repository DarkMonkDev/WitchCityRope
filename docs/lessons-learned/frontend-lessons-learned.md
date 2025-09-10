# Frontend Lessons Learned - WitchCityRope React

## Data-TestId Implementation for E2E Testing - COMPLETED ✅

**Problem**: Playwright E2E tests need reliable element selectors to avoid brittleness
**Solution**: Add comprehensive data-testid attributes to all interactive React components
**Implementation**: Follow naming convention from PLAYWRIGHT_TESTING_STANDARDS.md

### Naming Convention Applied:
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

### Components Updated:
**Authentication**:
- `/apps/web/src/pages/LoginPage.tsx` - Added page, form, inputs, button, and link attributes

**Events**:
- `/apps/web/src/pages/events/EventsListPage.tsx` - Added page, search input, filter select, events list, and event cards
- `/apps/web/src/pages/events/EventDetailPage.tsx` - Added page, hero section, sessions list, and register button

**Dashboard**:
- `/apps/web/src/pages/dashboard/DashboardPage.tsx` - Added page and widget attributes
- `/apps/web/src/pages/dashboard/RegistrationsPage.tsx` - Added page, list, and cancel button
- `/apps/web/src/pages/dashboard/ProfilePage.tsx` - Added page and form attributes
- `/apps/web/src/components/profile/ProfileForm.tsx` - Added save button attribute

**Navigation**:
- `/apps/web/src/components/layout/Navigation.tsx` - Added nav, links, and logout button attributes

### Usage for Playwright Tests:
```typescript
// ✅ CORRECT - Reliable selectors
await page.locator('[data-testid="login-button"]').click();
await page.fill('[data-testid="email-input"]', 'test@example.com');
await expect(page.locator('[data-testid="page-dashboard"]')).toBeVisible();
```

**Action Items**:
- ✅ All major interactive components now have data-testid attributes
- ✅ Follows established naming convention from testing standards
- ✅ Ready for comprehensive E2E test execution
- 🔄 Future components should include data-testid attributes from start

## Critical Button Sizing Issue - NEVER REPEAT

**Problem**: Button text gets cut off when not properly sized
**Solution**: ALWAYS set explicit button dimensions
**Example**:
```tsx
// ❌ WRONG - Text gets cut off
<Button>Add Email Template</Button>

// ✅ CORRECT - Properly sized
<Button style={{
  minWidth: '160px',
  height: '48px', 
  fontWeight: 600,
}}>Add Email Template</Button>
```

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
**Debug Command**: `npx playwright test tests/playwright/tinymce-debug.spec.ts --headed`

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
**Command**: `npm run test:e2e -- --grep="Screenshots"`

## Form Structure Following Wireframes

**Problem**: Missing volunteer position edit form at bottom of tab
**Solution**: Follow wireframe layout exactly, including all sections and forms

## Constant Page Reloading Issue - CRITICAL FIX APPLIED ✅

**Problem**: Pages with React components constantly reload and remount in infinite loops
**Root Cause**: Multiple conflicting Vite dev servers running on the same port causing resource conflicts and hot reload failures

**Solution Steps Applied**:

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

**Verification Results**:
- ✅ Component mounts: 2 (normal for React.StrictMode)
- ✅ Component unmounts: 1 (normal)
- ✅ Page navigations: 2 (normal)
- ✅ Events loading: 3 fallback events displaying correctly
- ✅ API calls: Working properly to http://localhost:5655/api/events

**Key Insight**: The primary cause was dev server conflicts, NOT TanStack Query configuration. Always check for multiple dev servers when debugging infinite reload issues.

## Navigation Component Reloading Fix - CRITICAL UPDATE ✅

**Problem**: Navigation component causing constant page reloads despite previous fixes
**Root Cause**: Auth store `checkAuth()` being called repeatedly without proper guards, causing render loops

**Solution Applied**:

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

**Verification**: 
- ✅ Navigation component no longer causes reloads
- ✅ Auth state remains stable 
- ✅ API calls properly throttled
- ✅ Demo pages work without auth loops

**Prevention Strategy**: Always add guards and logging to auth-related operations to prevent infinite loops and concurrent calls.

## useEffect Infinite Render Loop - CRITICAL FIX ✅

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

**Files Fixed**:
- `/apps/web/src/pages/NavigationTestPage.tsx` - Fixed useEffect infinite loop
- `/apps/web/src/routes/router.tsx` - Fixed demo route pointing to correct component

## Events System Phase 1 Implementation - TDD SUCCESS ✅

**Problem**: Need to implement Phase 1 Events system routes and pages following TDD approach
**Solution**: Created routes and stub components with proper API integration

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

**Routes Implemented**:
- ✅ `/events` - PublicEventsPage with API integration
- ✅ `/events/:id` - EventDetailsPage with dynamic routing  
- ✅ `/admin/events` - AdminEventsPage with auth protection
- ✅ `/dashboard/events` - Already existed (DashboardEventsPage)

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

**Success Metrics**:
- ✅ First E2E test passes: "Step 1: Public Event Viewing"  
- ✅ All basic route tests pass (PublicEventsPage, AdminEventsPage, EventDetailsPage)
- ✅ API integration working with existing `useEvents()` hook
- ✅ Proper loading states and error handling
- ✅ Mantine UI components rendering correctly

**Files Created**:
- `/apps/web/src/pages/events/PublicEventsPage.tsx` - Public events listing
- `/apps/web/src/pages/events/EventDetailsPage.tsx` - Individual event details
- `/apps/web/src/pages/admin/AdminEventsPage.tsx` - Admin event management

## EventForm Modal Integration - Phase 3 Sessions & Tickets ✅

**Problem**: EventForm handlers for sessions and ticket types were placeholder console.log statements
**Solution**: Integrated SessionFormModal and TicketTypeFormModal components with proper CRUD operations

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

**Key Patterns Applied**:
- ✅ Used crypto.randomUUID() for generating unique IDs
- ✅ Proper modal state management with editing vs adding modes
- ✅ Interface alignment between grid and modal components
- ✅ Form state updates using Mantine form.setFieldValue()
- ✅ Modal components rendered at end of form JSX

**Interface Mismatch Resolution**:
- Created separate EventTicketType interface in TicketTypeFormModal.tsx
- Conversion functions between grid format and modal format
- Grid format: sessionIdentifiers, minPrice, maxPrice
- Modal format: sessionsIncluded, price, description, etc.

**Files Modified**:
- `/apps/web/src/components/events/EventForm.tsx` - Added modal integration
- `/apps/web/src/components/events/TicketTypeFormModal.tsx` - Fixed interface definition
- `/apps/web/src/components/events/SessionFormModal.tsx` - Removed unsupported 'creatable' prop

**Critical Learning**: Always verify interface compatibility between modal forms and display grids. Create conversion functions when interfaces don't align perfectly.

## CRITICAL FIX: Events Management Phase 4 - Wrong Business Rules Implementation ✅

**Problem**: Implemented generic "registration" system ignoring documented business rules that Classes and Social Events work differently
**Root Cause**: Failed to research requirements before implementation, ignored existing documentation
**Solution**: Fixed to implement proper event-type-aware system

**Business Rules Applied**:
1. **Classes (eventType: 'class')**: 
   - ONLY ticket purchase (paid), NO RSVP
   - Show "Buy Tickets" button only
   - Display "Tickets Sold" table only

2. **Social Events (eventType: 'social')**: 
   - BOTH RSVP (free) AND optional ticket purchase
   - Show BOTH "RSVP" and "Buy Tickets" buttons
   - Display BOTH "RSVP" table AND "Tickets Sold" table
   - RSVP table shows members + whether they bought tickets

**Implementation Fixed**:
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

**Files Fixed**:
- ✅ `/apps/web/src/pages/events/EventDetailsPage.tsx` - Removed EventRegistrationModal import, added event-type logic
- ✅ `/apps/web/src/features/events/api/mutations.ts` - Renamed useRegisterForEvent → usePurchaseTicket, added useRSVPForEvent
- ✅ `/apps/web/src/pages/events/PublicEventsPage.tsx` - Fixed "Registration Open" → event-type-aware status
- ✅ `/apps/web/src/types/api.types.ts` - Added eventType field to Event interface

**Components Created**:
- ✅ `/apps/web/src/components/events/EventTicketPurchaseModal.tsx` - For Classes AND Social Events ticket purchase
- ✅ `/apps/web/src/components/events/EventRSVPModal.tsx` - For Social Events free RSVP only

**Terminology Fixed**: 
- ❌ ~~"Registration"~~ (wrong/generic)
- ✅ **"Ticket Purchase"** for Classes
- ✅ **"RSVP"** for Social Events (free)
- ✅ **"Buy Tickets"** for Social Events (optional upgrade)

**Prevention Strategy**: 
1. **ALWAYS research requirements FIRST** - Never code without understanding business rules
2. **Check existing documentation** for business logic and terminology
3. **Use business-requirements agent** when in doubt about rules
4. **Test against business requirements** not just technical functionality
5. **Verify event type handling** in ALL components

**Key Learning**: Business requirements research is MANDATORY before any implementation. Generic solutions often violate specific business rules.

## Test ID Consistency Critical Issue - FIXED ✅

**Problem**: E2E tests failing due to inconsistent data-testid attributes between tests and implementation
**Root Cause**: Manual implementation of test IDs without following established naming conventions from testing standards

**Missing Test IDs Fixed**:
1. **Events List Container**: Changed `data-testid="list-events"` → `data-testid="events-list"`
2. **Individual Event Cards**: Changed `data-testid="card-event"` → `data-testid="event-card"`  
3. **View Toggle Control**: Added missing `data-testid="button-view-toggle"`

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

**Critical Learning**: 
- **ALWAYS verify test ID naming against testing standards** before implementation
- **Use established naming convention**: `{element-type}-{purpose}` 
- **Test early**: Check data-testid attributes match E2E test expectations
- **Document naming**: Reference `/docs/standards-processes/PLAYWRIGHT_TESTING_STANDARDS.md`

**Prevention Strategy**:
1. **Check existing tests** for expected data-testid values before implementing
2. **Follow naming convention** from testing standards documentation
3. **Test immediately** after adding data-testid attributes  
4. **Batch test ID fixes** instead of addressing one-by-one

**Files Fixed**: `/apps/web/src/pages/events/EventsListPage.tsx` - All missing test IDs added

**Impact**: Improved E2E test pass rate from 38.5% to projected 80%+ for events tests