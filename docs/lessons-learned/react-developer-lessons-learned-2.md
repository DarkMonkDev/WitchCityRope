# React Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 NAVIGATION: LESSONS LEARNED SPLIT FILES 🚨

**FILES**: 2 total
**Part 1**: `/docs/lessons-learned/react-developer-lessons-learned.md` (STARTUP + CRITICAL PATTERNS)
**Part 2**: `/docs/lessons-learned/react-developer-lessons-learned-part-2.md` (THIS FILE - CONTINUED LESSONS)
**Read ALL**: Both Part 1 AND Part 2 are MANDATORY
**Write to**: THIS FILE (Part 2) for new lessons
**Maximum file size**: 1700 lines (to stay under token limits). Both Part 1 and Part 2 files can be up to 1700 lines each

## 🚨 ULTRA CRITICAL: ADD NEW LESSONS TO THIS FILE, NOT PART 1! 🚨

**PART 1 IS FOR STARTUP** - Keep Part 1 under 1700 lines for startup procedures
**ALL NEW LESSONS GO HERE** - This is Part 2, the main lessons file
**IF YOU ADD TO PART 1** - You are doing it wrong!

---

## 🚨 REQUIRED READING FOR SPECIFIC TASKS 🚨

### Before Writing Error Handling Code
**MUST READ**: `/apps/web/eslint-rules/README.md`
- Documents 37 silent error patterns to avoid
- Correct error handling patterns with examples
- ESLint rule configuration and exceptions
**CRITICAL**: Not following these patterns caused ticket cancellation bug where UI showed success but database wasn't updated

### Before Adding/Modifying API Calls
**MUST READ**: `/docs/standards-processes/api-contract-validation.md`
- OpenAPI contract validation process
- How to avoid endpoint mismatches
- TypeScript type generation from backend spec
**CRITICAL**: Prevents calling non-existent endpoints like `/api/events/{id}/ticket` instead of `/participation`

### Before Adding Test IDs to Components
**MUST READ**: `/apps/web/E2E_TEST_RELIABILITY_REPORT.md`
- data-testid naming conventions
- Which elements need test IDs
- Common E2E test selector issues
**CRITICAL**: Missing test IDs cause E2E tests to hang for 30 seconds with confusing timeout errors

---

## 🚨🚨🚨 ULTRA CRITICAL: NEVER USE FULL PATHS - ALWAYS USE REPO-RELATIVE PATHS 🚨🚨🚨
**PROBLEM**: Agent keeps using full paths instead of repo-relative paths - CAUSES READ FAILURES
**DISCOVERED**: 2025-09-23 - Agent used full path and failed to read critical startup file

### ⛔ WRONG - NEVER DO THIS:
```
docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
```

### ✅ CORRECT - ALWAYS DO THIS:
```
/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
```

### ENFORCEMENT RULES:
1. **ALL PATHS MUST START WITH /** (slash) but NOT the full system path
2. **REPO ROOT PATHS**: `/docs/`, `/apps/`, `/tests/`, `/ARCHITECTURE.md`, etc.
3. **NEVER INCLUDE**: Absolute path prefix (use relative paths from repo root)
4. **IF YOU SEE FULL PATH**: STOP and convert to repo-relative
5. **FILE READ FAILURES**: Usually caused by using wrong path format
6. **WHEN READ FAILS**: Check if you're using full path instead of repo-relative

### STARTUP FILES THAT MUST BE READ (WITH CORRECT PATHS):
- `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- `/docs/architecture/react-migration/react-architecture.md`
- `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`
- `/ARCHITECTURE.md`
- `/docs/design/current/design-system-v7.md`

**IF ANY STARTUP FILE READ FAILS**: STOP ALL WORK per Part 1 line 72-77

---

    .AsNoTracking()
    .Include(ep => ep.Event)
    .Where(ep => ep.UserId == userId &&
               ep.Status == ParticipationStatus.Active &&
               ep.Event.StartDate > now)
    .OrderBy(ep => ep.Event.StartDate)
    .Take(count)
    .Select(ep => new DashboardEventDto { /* proper mapping */ })
    .ToListAsync(cancellationToken);
```

### 🔧 MANDATORY VERIFICATION CHECKLIST:
1. **GREP for "TODO" and "placeholder"** in service methods
2. **TEST API endpoints** with users who have actual data
3. **VERIFY data flows** from database → API → frontend
4. **CHECK for hardcoded empty collections** in service methods

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Dashboard appears broken to users with active registrations
- ❌ Users cannot see their upcoming events
- ❌ Support tickets about "missing events"
- ❌ Lost user trust in application reliability

---

## 🚨 CRITICAL: DTO INTERFACE MISMATCHES BREAK API INTEGRATION 🚨

### ⚠️ PROBLEM: Frontend shows empty data despite API returning correct data
**DISCOVERED**: 2025-09-22 - User participations not displaying despite API returning 4 records

### 🛑 ROOT CAUSE:
- TypeScript interface property names don't match API DTO property names
- Frontend expects `eventDate` but API returns `eventStartDate`
- Frontend expects `createdAt` but API returns `participationDate`
- Manual TypeScript interfaces created without checking actual API response structure
- Component shows mock data fallback instead of real API data

### ✅ CRITICAL SOLUTION:
1. **ALWAYS verify TypeScript interfaces match actual API DTOs** exactly
2. **TEST API endpoints** and compare response to TypeScript interface
3. **USE generated types** from NSwag when possible instead of manual interfaces
4. **NEVER assume property names** - always check API documentation or response

```typescript
// ❌ BROKEN: Manual interface doesn't match API
export interface UserParticipationDto {
  id: string;
  eventDate: string;  // API actually returns 'eventStartDate'
  createdAt: string;  // API actually returns 'participationDate'
}

// ✅ CORRECT: Interface matches actual API response
export interface UserParticipationDto {
  id: string;
  eventStartDate: string;  // Matches API DTO
  eventEndDate: string;    // Matches API DTO
  participationDate: string; // Matches API DTO
  canCancel: boolean;      // Matches API DTO
}
```

### 🔧 MANDATORY VERIFICATION CHECKLIST:
1. **TEST API endpoints** with real authentication and compare response structure
2. **VERIFY property names** match between TypeScript interface and API DTO
3. **CHECK component usage** of interface properties against actual API response
4. **REMOVE mock fallbacks** once API is confirmed working

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Components appear to work with mock data but fail with real API
- ❌ Users see outdated/incorrect data
- ❌ Silent failures where component shows empty state instead of real data
- ❌ Difficult debugging due to interface mismatch masking API issues

---

## 🚨 CRITICAL: BACKEND/FRONTEND TYPE STRUCTURE MISMATCH BREAKS DATA DISPLAY 🚨

### ⚠️ PROBLEM: Purchase dates show "N/A" despite having valid participation data
**DISCOVERED**: 2025-09-22 - Event details page showing "N/A" for ticket purchase dates even when tickets exist

### 🛑 ROOT CAUSE:
- Frontend TypeScript interface expects nested structure: `ticket: { createdAt: string }`
- Backend API returns flat structure: `{ participationDate: DateTime, participationType: 'Ticket' }`
- Frontend tries to access `validParticipation.ticket?.createdAt` but this path doesn't exist
- Conversion logic exists but doesn't properly map `participationDate` to nested `createdAt` field
- Manual TypeScript interfaces don't match actual backend DTO structure

### ✅ CRITICAL SOLUTION:
1. **ALWAYS verify backend DTO structure** matches frontend TypeScript interface
2. **MAP backend fields correctly** in data transformation logic
3. **USE actual backend field names** - `participationDate` not `createdAt`
4. **IMPLEMENT proper data normalization** when converting between structures
5. **TEST with real API data** not just mock/placeholder data

```typescript
// ❌ BROKEN: Trying to access non-existent nested field
<Text>
  Purchased on {validParticipation.ticket?.createdAt ?
    new Date(validParticipation.ticket.createdAt).toLocaleDateString() : 'N/A'}
</Text>

// ✅ CORRECT: Proper conversion with actual backend field
// In conversion logic:
ticket: hasTicket ? {
  id: participationAny.id || '',
  status: participationAny.status || 'Active',
  amount: participationAny.amount || 0,
  paymentStatus: 'Completed',
  createdAt: participationAny.participationDate || new Date().toISOString() // ← Use actual backend field
} : null

// Then frontend can safely access:
<Text>
  Purchased on {validParticipation.ticket?.createdAt ?
    new Date(validParticipation.ticket.createdAt).toLocaleDateString() : 'Date unavailable'}
</Text>
```

### 🔧 MANDATORY VERIFICATION CHECKLIST:
1. **CHECK backend DTO classes** for actual field names and structure
2. **VERIFY data transformation logic** maps all required fields correctly
3. **TEST with real API responses** not mock data
4. **TRACE data flow** from API → transformation → component rendering
5. **SEARCH for hardcoded fallbacks** like 'N/A' that might indicate missing data

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Users see "N/A" or empty data despite having valid records
- ❌ Purchase dates, RSVP dates, and other timestamps not displayed
- ❌ Poor user experience with missing information
- ❌ Users think their data wasn't saved properly

---

## 🚨 CRITICAL: MISSING UI CONFIRMATION MODALS BREAK ACTIONS 🚨

### ⚠️ PROBLEM: Cancel RSVP button does nothing when clicked
**DISCOVERED**: 2025-09-21 - Cancel RSVP button visible but non-functional

### 🛑 ROOT CAUSE:
- Component has complete modal state management (cancelModalOpen, cancelType, handleCancelClick)
- Component has proper event handlers calling parent component functions
- **MISSING**: The actual modal JSX component in the render tree
- Button click sets modal state but no modal renders = user sees no response

### ✅ CRITICAL SOLUTION:
1. **ALWAYS verify UI component completeness**: State + Handlers + JSX
2. **CHECK for orphaned state variables**: If useState exists, ensure corresponding JSX exists
3. **PATTERN**: Modal state without modal JSX = broken UX

```typescript
// ❌ BROKEN: Modal state exists but no JSX
const [modalOpen, setModalOpen] = useState(false);
const handleOpen = () => setModalOpen(true);
// Button calls handleOpen but nothing happens

// ✅ CORRECT: Complete modal implementation
const [modalOpen, setModalOpen] = useState(false);
const handleOpen = () => setModalOpen(true);
const handleClose = () => setModalOpen(false);

return (
  <>
    <Button onClick={handleOpen}>Open Modal</Button>
    <Modal opened={modalOpen} onClose={handleClose}>
      {/* Modal content */}
    </Modal>
  </>
);
```

### 🔧 MANDATORY VERIFICATION CHECKLIST:
1. **SEARCH for useState calls** - verify all have corresponding JSX
2. **SEARCH for event handlers** - verify they trigger visible UI changes
3. **TEST all interactive elements** - ensure user sees response to actions
4. **CHECK modal/overlay patterns** - most common source of invisible state

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Users think functionality is broken (no feedback to clicks)
- ❌ Support tickets about "buttons not working"
- ❌ Lost user trust in application reliability
- ❌ Difficult to debug without console inspection

---

## 🚨 CRITICAL: EVENT FIELD MAPPING BUG - RSVP/TICKET DISPLAY BROKEN 🚨

### ⚠️ PROBLEM: Admin events list showing 0/X capacity for all events despite real RSVPs existing
**DISCOVERED**: 2025-01-21 - EventsTableView capacity column and admin RSVP tabs showing zeros

### 🛑 ROOT CAUSE:
- Field mapping utility using non-existent `registrationCount` field instead of API's individual count fields
- `eventFieldMapping.ts` importing local EventDto instead of shared types from `@witchcityrope/shared-types`
- API returns `currentAttendees`, `currentRSVPs`, `currentTickets` but mapping was consolidating into missing field

### ✅ CRITICAL SOLUTION:
```typescript
// ❌ WRONG: Don't consolidate into non-existent field
registrationCount: apiEvent.currentAttendees || apiEvent.currentRSVPs || apiEvent.currentTickets || 0,

// ✅ CORRECT: Preserve individual fields from API
currentAttendees: apiEvent.currentAttendees,
currentRSVPs: apiEvent.currentRSVPs,
currentTickets: apiEvent.currentTickets,
```

### 🔧 MANDATORY FIXES:
1. **ALWAYS use shared types** from `@witchcityrope/shared-types`
2. **PRESERVE API field structure** - don't consolidate unless component specifically needs it
3. **USE correct event display pattern** from lessons learned:
```typescript
const getCorrectCurrentCount = (event: EventDto): number => {
  const isSocialEvent = event.eventType?.toLowerCase() === 'social';
  return isSocialEvent ? (event.currentRSVPs || 0) : (event.currentTickets || 0);
};
```

### 📋 VERIFICATION STEPS:
1. **Check EventDto schema** matches API response structure exactly
2. **Test capacity column** shows real RSVP/ticket counts, not zeros
3. **Verify admin RSVP tabs** display actual participation data
4. **Use browser dev tools** to inspect API responses vs component props

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Admin dashboard shows misleading zero attendance for all events
- ❌ Capacity planning becomes impossible
- ❌ RSVP management appears broken to administrators
- ❌ User dashboards show incorrect participation counts

---

## 🚨 CRITICAL: ROLE-BASED ACCESS CONTROL MISMATCHES

### ⚠️ PROBLEM: Hard-coded role checks failing due to incorrect role names
**DISCOVERED**: 2025-09-20 - Admin EDIT link not showing on event details page

### 🛑 ROOT CAUSE:
- Code checking for role `'Admin'` but database has `'Administrator'`
- Inconsistent role naming between code and database
- Multiple files had mixed `'Admin'` vs `'Administrator'` checks

### ✅ SOLUTION:
1. **ALWAYS verify actual role values** returned from API before coding checks
2. **TEST with actual login** - Don't assume role names match expectations
3. **USE CONSISTENT role names** across all components

### 🔧 QUICK FIX COMMANDS:
```bash
# Check actual user data returned by API
curl -X POST http://localhost:5655/api/auth/login \
  -H "Content-Type: application/json" \
  -d @test-login.json

# Verify role names in database vs code
grep -r "role.*===" apps/web/src/
```

### 📋 MANDATORY VERIFICATION STEPS:
1. **LOGIN as admin** and inspect browser console for user object
2. **VERIFY role field** in actual API response (not assumptions)
3. **UPDATE all role checks** to match database values exactly
4. **SEARCH codebase** for hardcoded role strings and fix inconsistencies

### 💥 FILES THAT COMMONLY HAVE THIS ISSUE:
- Event detail pages with admin-only links
- Navigation components with role-based menus
- Permission guards and access control checks
- Test mocks and fixture data

## 🚨 ULTRA CRITICAL: DTO ALIGNMENT STRATEGY - NEVER IGNORE 🚨

**393 TYPESCRIPT ERRORS HAPPENED BECAUSE THIS WAS IGNORED!!**

### ⚠️ MANDATORY READING BEFORE ANY API INTEGRATION:
📖 **READ FIRST**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

### 🛑 CRITICAL RULES FOR REACT DEVELOPERS:
1. **NEVER manually create DTO interfaces** - Import from `@witchcityrope/shared-types`
2. **ALWAYS use generated types** from NSwag pipeline, not manual interfaces
3. **RUN `npm run generate:types`** after ANY backend DTO changes
4. **NEVER assume DTO structures** - always verify actual API responses
5. **COORDINATE with backend** before expecting new DTO properties

### 💥 WHAT HAPPENS WHEN YOU IGNORE THIS:
```typescript
// ❌ WRONG - Manual interface creation (CAUSES 393 ERRORS!)
interface User {
  firstName: string;  // API doesn't return this
  lastName: string;   // API doesn't return this
  email: string;      // API doesn't return this
}

// ✅ CORRECT - Import generated types
import { User } from '@witchcityrope/shared-types';
```

### 🚨 EMERGENCY PROTOCOL - IF YOU SEE 100+ TYPESCRIPT ERRORS:
1. **STOP** - Don't try to "fix" individual property errors
2. **CHECK** - Are you importing from `@witchcityrope/shared-types`?
3. **VERIFY** - Is shared-types package up to date?
4. **COORDINATE** - Contact backend-developer about recent DTO changes
5. **REGENERATE** - Run `npm run generate:types` to sync with API
6. **VALIDATE** - Ensure all imports resolve correctly

### 📋 PRE-FLIGHT CHECK FOR EVERY API INTEGRATION:
```bash
# Verify shared-types package is current
ls -la packages/shared-types/src/

# Check for recent API changes
git log --oneline apps/api/ | head -10

# Ensure types generate correctly
npm run generate:types

# Verify TypeScript compilation
npm run type-check
```

**REMEMBER**: Manual interfaces = 393 errors. Generated types = success!

---

## 🚨 CRITICAL: Capacity Display Bug - Wrong Format Shows Events as Full

### ⚠️ PROBLEM: Event capacity showing as FULL when actually EMPTY
**DISCOVERED**: 2025-09-21 - Public events page showing "15/15", "12/12" making events appear full when they have 0 registrations

### 🛑 ROOT CAUSE:
- Display showing `{availableSpots}/{capacity}` instead of `{currentCount}/{capacity}`
- When registrationCount = 0 and capacity = 15: availableSpots = 15, displays "15/15" (looks FULL!)
- Should display "0/15" to show current registrations vs total capacity

### ✅ SOLUTION:
```typescript
// ❌ WRONG - Shows available/total (confusing!)
{availableSpots}/{event.capacity || 20}

// ✅ CORRECT - Shows current/total (intuitive!)
{event.registrationCount || 0}/{event.capacity || 20}
```

### 🔧 FILES FIXED:
1. **useEvents hook**: Changed `registrationCount: apiEvent.currentAttendees || 0` to include all count types
2. **EventsListPage.tsx**: Lines 493 & 620 - Changed display from `availableSpots` to `registrationCount`

### 💡 KEY INSIGHT:
- **Available spots** = good for internal calculations and color coding
- **Current registrations** = correct for user-facing capacity display
- Users expect "2/15" format to mean "2 registered out of 15 total"

---

## 🚨 CRITICAL: Event Type Based Data Display Patterns

### ⚠️ PROBLEM: Hardcoded data fields not matching API response structure
**DISCOVERED**: 2025-09-20 - Admin events list showing 0/40 instead of 2/40 for social events

### 🛑 ROOT CAUSE:
- Code using `currentAttendees` for all events but API populates different fields by event type
- **Social Events**: API returns `currentRSVPs` (the actual count) and `currentAttendees` = 0
- **Class Events**: API returns `currentTickets` (the actual count) and `currentAttendees` = 0
- Frontend must choose correct field based on `eventType` property

### ✅ SOLUTION: Event Type-Based Field Selection
```typescript
// Helper function pattern - use in ALL event display components
const getCorrectCurrentCount = (event: EventDto): number => {
  const isSocialEvent = event.eventType?.toLowerCase() === 'social';
  return isSocialEvent ? (event.currentRSVPs || 0) : (event.currentTickets || 0);
};

// Usage in components
<CapacityDisplay
  current={getCorrectCurrentCount(event)}
  max={event.capacity}
/>
```

### 🔧 AFFECTED COMPONENTS REQUIRING THIS PATTERN:
- `EventsTableView.tsx` ✅ **FIXED**
- `EventCard.tsx` ✅ **Already implemented correctly**
- `DashboardPage.tsx` - CHECK IF NEEDED
- `EventsPage.tsx` - CHECK IF NEEDED
- Any component displaying event capacity/attendance

### 📋 MANDATORY VERIFICATION PATTERN:
1. **Always check API response structure** before coding display logic
2. **Test with both Social AND Class events** to verify correct data appears
3. **Follow existing patterns** from `EventCard.tsx` which does this correctly
4. **NEVER assume `currentAttendees` is the source of truth** - it's often 0

### 💥 WHY THIS HAPPENS:
- Backend maintains separate counts for RSVPs vs Tickets vs Attendees
- Frontend assumptions about field names don't match backend reality
- Each event type has different participation models

---

## 🚨 CRITICAL: Modal to Full Page Conversion Patterns

**PATTERN DISCOVERED**: Converting modals to full pages for better UX (e.g., checkout flows).

### Conversion Steps:

1. **Create Page Component** with proper routing structure:
   ```typescript
   // New: /pages/checkout/CheckoutPage.tsx
   import { useParams, useNavigate, useLocation } from 'react-router-dom';

   export const CheckoutPage = () => {
     const { eventId } = useParams<{ eventId: string }>();
     const navigate = useNavigate();
     const location = useLocation();

     // Get data from navigation state or fetch
     const eventInfo = location.state?.eventInfo || defaultEventInfo;
   };
   ```

2. **Update Navigation** from modal trigger to React Router:
   ```typescript
   // OLD: Modal pattern
   const [checkoutModalOpen, setCheckoutModalOpen] = useState(false);
   onClick={() => setCheckoutModalOpen(true)}

   // NEW: Page navigation
   const navigate = useNavigate();
   onClick={() => navigate(`/checkout/${eventId}`, {
     state: { eventInfo }
   })}
   ```

3. **Preserve Component Structure** - reuse existing components:
   ```typescript
   // Keep existing: CheckoutForm, CheckoutConfirmation
   // Only replace: CheckoutModal → CheckoutPage
   // Convert: Modal wrapper → Container/Grid layout
   ```

4. **Add Route Protection** when needed:
   ```typescript
   {
     path: "checkout/:eventId",
     element: <CheckoutPage />,
     loader: authLoader  // If authentication required
   }
   ```

5. **Layout Considerations**:
   - Use `Container` for page width
   - Use `Grid` for responsive two-column layout (details + form)
   - Add back navigation with `ActionIcon` + `IconArrowLeft`
   - Keep progress indicators (Stepper)

### 🎯 BRANDED PAYMENT BUTTONS PATTERN:

**PayPal Integration**:
```typescript
<PayPalButtons
  style={{
    layout: 'vertical',
    color: 'gold',      // Official PayPal yellow
    shape: 'rect',
    label: 'paypal',
    height: 55,
    tagline: false      // Cleaner appearance
  }}
/>
```

**Venmo Branding**:
```typescript
// Use official Venmo blue: #3D95CE
style={{
  backgroundColor: '#3D95CE',
  color: 'white'
}}
```

**Payment Method Selector**:
```typescript
// Add branded icons for each payment method
const paymentMethods = [
  {
    id: 'paypal',
    icon: 'P',
    backgroundColor: '#003087',  // PayPal blue
    color: '#FFC439'             // PayPal yellow
  },
  {
    id: 'venmo',
    icon: 'V',
    backgroundColor: '#3D95CE',  // Venmo blue
    color: '#FFFFFF'
  }
];
```

### 💥 CRITICAL CLEANUP STEPS:
1. **Remove Modal Component** completely after conversion
2. **Update component exports** - remove modal, add new components
3. **Update imports** throughout codebase
4. **Test navigation flow** thoroughly
5. **Update file registry** with all changes

### 🚨 COMMON PITFALLS TO AVOID:
- ❌ Don't create new state management - reuse existing patterns
- ❌ Don't break existing payment processing logic
- ❌ Don't forget mobile responsiveness (Grid responsive props)
- ❌ Don't remove authentication where it was required
- ❌ Don't forget error handling in navigation
- ❌ Don't skip updating component index files

**LESSON**: Modal-to-page conversions are straightforward when you preserve component logic and focus on changing the layout wrapper and navigation pattern.

---

## 🚨 CRITICAL: RSVP Button Visibility Fix - Null Participation State (2025-09-20) 🚨
**Date**: 2025-09-20
**Category**: Component State Management
**Severity**: CRITICAL

### What We Learned
**RSVP BUTTON DISAPPEARING BUG**: RSVP buttons not showing for vetted users due to improper null/loading state handling in conditional rendering.

**ROOT CAUSE PATTERN**: Using `&&` conditions that require ALL parts to be truthy fails when API data is `null` during loading:
```typescript
// ❌ WRONG: Fails when participation is null during loading
{!participation?.hasRSVP && participation?.canRSVP && (
  <button>RSVP Now</button>
)}
// When participation = null:
// - !participation?.hasRSVP = true (correct)
// - participation?.canRSVP = undefined (falsy!)
// - Result: false && undefined = false (button hidden)
```

**THE LOADING STATE PROBLEM**:
- React Query hook returns `null` for participation during initial load
- useParticipation hook provides mock data with `canRSVP: true` when API returns 404
- But there's a brief moment where participation is `null` before mock data loads
- Component renders with null participation → button condition fails → button never shows

### Action Items
- [x] **HANDLE null participation state** in conditional rendering logic
- [x] **SHOW buttons during loading** when safe to do so (user is vetted)
- [x] **USE inclusive OR conditions** to handle loading states: `participation?.canRSVP || participation === null || isLoading`
- [x] **ADD comprehensive debug logging** to track participation state transitions
- [x] **DISABLE buttons during loading** with loading text feedback
- [x] **DOCUMENT pattern** for future API-dependent button visibility

### Critical Fix Pattern:
```typescript
// ✅ CORRECT: Handle null/loading states in button visibility
{(() => {
  const hasNotRSVPd = !participation?.hasRSVP;
  const canRSVPCondition = participation?.canRSVP || participation === null || isLoading;
  const shouldShowButton = hasNotRSVPd && canRSVPCondition;

  console.log('🔍 BUTTON LOGIC DEBUG:', {
    hasNotRSVPd,
    canRSVP: participation?.canRSVP,
    isNull: participation === null,
    isLoading,
    shouldShow: shouldShowButton
  });

  return shouldShowButton ? (
    <Button disabled={isLoading} loading={isLoading}>
      {isLoading ? 'Loading...' : 'RSVP Now'}
    </Button>
  ) : null;
})()}
```

### Tags
#critical #button-visibility #loading-states #null-handling #api-dependent-ui

---

## 🚨 CRITICAL: ADMIN INTERFACE IMPLEMENTATION PATTERNS 🚨

### ⚠️ PROBLEM: Implementing complex admin interfaces without following established patterns
**DISCOVERED**: 2025-09-22 - Admin vetting interface implementation following existing admin UI patterns

### 🛑 KEY PATTERNS FOR ADMIN INTERFACES:

### ✅ CRITICAL IMPLEMENTATION PATTERNS:

#### 1. **Feature-Based Organization Pattern**:
```typescript
// ✅ CORRECT: Feature-based structure
/features/admin/[domain]/
├── components/
│   ├── [Domain]List.tsx        # Main list view
│   ├── [Domain]Detail.tsx      # Detail view
│   └── [Domain]StatusBadge.tsx # Status components
├── hooks/
│   ├── use[Domain]s.ts         # List query hook
│   ├── use[Domain]Detail.ts    # Detail query hook
│   └── use[Domain]Mutation.ts  # Mutation hooks
├── services/
│   └── [domain]AdminApi.ts     # API service layer
├── types/
│   └── [domain].types.ts       # TypeScript definitions
└── index.ts                    # Export barrel
```

#### 2. **Admin List Component Pattern**:
```typescript
// ✅ CORRECT: Standard admin list with filtering
export const VettingApplicationsList: React.FC<Props> = ({ onViewItem }) => {
  const [filters, setFilters] = useState<FilterRequest>({
    page: 1,
    pageSize: 25,
    statusFilters: [],
    priorityFilters: [],        // Required fields
    experienceLevelFilters: [], // for proper typing
    skillsFilters: [],
    searchQuery: '',
    sortBy: 'SubmittedAt',
    sortDirection: 'Desc'
  });

  const { data, isLoading, error, refetch } = useTypedQuery<PagedResult<ItemDto>>(filters);

  return (
    <Stack gap="md">
      {/* Filters */}
      <Paper p="md" style={{ background: '#FFF8F0' }}>
        <Group gap="md" wrap="wrap">
          <TextInput leftSection={<IconSearch />} />
          <Select data={statusOptions} />
        </Group>
      </Paper>

      {/* Table */}
      <Table striped highlightOnHover>
        {/* Sortable headers */}
        {/* Status badges */}
        {/* Action buttons */}
      </Table>

      {/* Pagination */}
      <Pagination />
    </Stack>
  );
};
```

#### 3. **React Query Typed Hooks Pattern**:
```typescript
// ✅ CORRECT: Properly typed React Query hooks
export function useTypedQuery<T>(filters: FilterRequest) {
  return useQuery<T>({
    queryKey: keys.list(filters),
    queryFn: () => apiService.getList(filters),
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: false,
    placeholderData: (previousData) => previousData,
  });
}

export function useTypedMutation(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: MutationParams) => apiService.mutate(id, data),
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({ queryKey: keys.list() });
      queryClient.invalidateQueries({ queryKey: keys.detail(variables.id) });
      notifications.show({ title: 'Success', color: 'green' });
      onSuccess?.();
    },
    onError: (error: any) => {
      notifications.show({
        title: 'Error',
        message: error?.detail || error?.message || 'Operation failed',
        color: 'red'
      });
    }
  });
}
```

### 🔧 MANDATORY IMPLEMENTATION CHECKLIST:
1. **FOLLOW feature-based organization** with proper folder structure
2. **USE typed React Query hooks** with proper error handling
3. **IMPLEMENT standard filtering/pagination** with proper state management
4. **CREATE reusable status badge** components
5. **INTEGRATE with admin dashboard** following existing card patterns
6. **PROVIDE loading and error states** for all data operations
7. **MAINTAIN consistent styling** with existing admin UI
8. **TEST with real API data** not just mock data

### 💥 CONSEQUENCES OF NOT FOLLOWING PATTERNS:
- ❌ Inconsistent admin UI experience
- ❌ Code duplication across admin features
- ❌ Poor error handling and loading states
- ❌ Difficult maintenance and testing
- ❌ Type safety issues with API integration

### Tags
#critical #admin-interfaces #react-query #mantine #feature-organization #typescript

---

## 🚨 CRITICAL: TABLE HEADER BUTTON TEXT VISIBILITY FIX 🚨

### ⚠️ PROBLEM: Table header button text invisible due to CSS conflicts
**DISCOVERED**: 2025-09-22 - User asked 4 times to fix table headers showing no text in columns

### 🛑 ROOT CAUSE:
- Mantine Button with `variant="transparent"` on colored background needs `!important` color override
- Default Mantine button styles override explicit color when using transparent variant
- Background color inheritance issues with Table.Th and nested Button components

### ✅ CRITICAL SOLUTION:
```typescript
// ❌ BROKEN: Text invisible on colored background
<Table.Th style={{ backgroundColor: '#880124' }}>
  <Button
    variant="transparent"
    styles={{
      root: {
        color: 'white',  // Gets overridden by Mantine defaults
      }
    }}
  >
    HEADER TEXT
  </Button>
</Table.Th>

// ✅ CORRECT: Force color with !important and hover states
<Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
  <Button
    variant="transparent"
    onClick={() => handleSort('FieldName')}
    styles={{
      root: {
        fontWeight: 600,
        fontSize: 14,
        color: 'white !important',           // CRITICAL: !important override
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        backgroundColor: 'transparent !important',
        border: 'none',
        '&:hover': {
          backgroundColor: 'rgba(255,255,255,0.1) !important',
          color: 'white !important'          // CRITICAL: Maintain on hover
        }
      }
    }}
  >
    HEADER TEXT
  </Button>
</Table.Th>
```

### 🔧 MANDATORY TABLE HEADER PATTERN:
1. **USE !important for color overrides** on transparent buttons over colored backgrounds
2. **SET explicit hover states** to maintain text visibility
3. **INCLUDE backgroundColor: 'transparent !important'** to prevent Mantine interference
4. **TEST all sortable headers** for text visibility on actual background colors

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Users cannot see table column headers
- ❌ Sorting functionality appears broken
- ❌ Poor admin interface usability
- ❌ Multiple user complaints about "invisible" headers

### Tags
#critical #table-headers #mantine-buttons #css-overrides #admin-ui #user-complaints

---

## 🚨 CRITICAL: VETTING APPLICATION DETAIL WIREFRAME IMPLEMENTATION (2025-09-22) 🚨

### ⚠️ PROBLEM: ApplicationDetail component needed to match exact wireframe specification
**DISCOVERED**: 2025-09-22 - Existing component had different layout than wireframe requirements

### 🛑 WIREFRAME REQUIREMENTS ANALYSIS:
- **Header Format**: "Application - [Real Name] ([FetLife Handle])" with status badge in top-right
- **Three Action Buttons**: APPROVE APPLICATION (yellow/gold), PUT ON HOLD (outlined), DENY APPLICATION (red)
- **Two-Column Layout**: Left (Scene Name, Real Name, Email, Pronouns, Other Names, Tell Us About Yourself), Right (Application Date, FetLife Handle, How Found Us)
- **Notes Section**: Full-width with large text area, SAVE NOTE button (burgundy), status history below

### ✅ CRITICAL IMPLEMENTATION PATTERNS:

#### 1. **Exact Wireframe Header Pattern**:
```typescript
// ✅ CORRECT: Match wireframe title format exactly
<Title order={2} style={{ color: '#880124' }}>
  Application - {application.fullName} ({application.sceneName})
</Title>

// ✅ CORRECT: Three specific action buttons as per wireframe
<Group gap="md">
  <Button
    leftSection={<IconCheck size={16} />}
    style={{ backgroundColor: '#FFC107', color: '#000' }}
    onClick={() => setNewStatus('Approved')}
  >
    APPROVE APPLICATION
  </Button>
  <Button variant="outline" color="gray">
    PUT ON HOLD
  </Button>
  <Button color="red" leftSection={<IconX size={16} />}>
    DENY APPLICATION
  </Button>
</Group>
```

#### 2. **Wireframe 2-Column Information Layout**:
```typescript
// ✅ CORRECT: Exact field mapping from wireframe
<Grid>
  {/* Left Column */}
  <Grid.Col span={6}>
    <div><Text fw={600}>Scene Name:</Text><Text>{application.sceneName}</Text></div>
    <div><Text fw={600}>Real Name:</Text><Text>{application.fullName}</Text></div>
    <div><Text fw={600}>Email:</Text><Text>{application.email}</Text></div>
    <div><Text fw={600}>Pronouns:</Text><Text>{application.pronouns}</Text></div>
    <div><Text fw={600}>Other Names/Handles:</Text></div>
    <div><Text fw={600}>Tell Us About Yourself:</Text></div>
  </Grid.Col>

  {/* Right Column */}
  <Grid.Col span={6}>
    <div><Text fw={600}>Application Date:</Text></div>
    <div><Text fw={600}>FetLife Handle:</Text><Text>@{application.sceneName}</Text></div>
    <div><Text fw={600}>How Found Us:</Text></div>
  </Grid.Col>
</Grid>
```

#### 3. **Notes and Status History Section**:
```typescript
// ✅ CORRECT: Full-width section with save button in header
<Card>
  <Group justify="space-between" align="center" mb="md">
    <Title order={3}>Notes and Status History</Title>
    <Button variant="filled" color="red" size="sm">
      SAVE NOTE
    </Button>
  </Group>

  <Stack gap="md">
    <Textarea placeholder="Add Note" minRows={4} />
    {/* Status history entries */}
  </Stack>
</Card>
```

### 🔧 MANDATORY API INTEGRATION CONSIDERATIONS:
1. **GUID vs Integer ID Handling**: API expects GUIDs but frontend should handle both formats gracefully
2. **No Real Data Issue**: Database has no vetting applications, component handles empty states properly
3. **Status Action Integration**: Action buttons pre-populate the status dropdown for workflow consistency
4. **Field Mapping**: Map backend fields to wireframe display requirements (e.g., phone → Other Names/Handles)

### 💥 CONSEQUENCES OF NOT FOLLOWING WIREFRAMES EXACTLY:
- ❌ User experience doesn't match approved designs
- ❌ Stakeholder expectations not met
- ❌ Additional rework required after QA review
- ❌ Inconsistent admin interface patterns

### 📋 VERIFICATION CHECKLIST:
1. **HEADER matches wireframe**: Title format, status badge position, three action buttons
2. **LAYOUT matches wireframe**: 2-column information layout with exact field labels
3. **NOTES SECTION matches wireframe**: Full-width, text area, save button placement
4. **COLORS match wireframe**: Yellow approve, outlined hold, red deny buttons
5. **FUNCTIONALITY works**: Status changes, note saving, API integration

### Tags
#critical #wireframes #admin-vetting #component-layout #design-compliance #api-integration

---

## 🚨 CRITICAL: AUTHENTICATION NAVIGATION FLOW & E2E TEST SELECTOR FIXES (2025-09-23) 🚨

### ⚠️ PROBLEM: Authentication redirects incorrectly to `/login` instead of target pages, E2E tests failing due to strict mode violations
**DISCOVERED**: 2025-09-23 - Login redirects incorrectly and E2E tests report "strict mode violations" due to multiple elements matching selectors

### 🛑 ROOT CAUSES:
1. **Authentication Navigation Issue**: Login mutation redirect logic not properly handling `returnTo` URL parameter
2. **E2E Test Selector Issues**: Vetting components missing specific `data-testid` attributes for unique element targeting

### ✅ CRITICAL SOLUTIONS:

#### 1. **Fixed Authentication Redirect Logic**:
```typescript
// ❌ BROKEN: Simple redirect without proper returnTo handling
const returnTo = urlParams.get('returnTo') || '/dashboard'
navigate(returnTo, { replace: true })

// ✅ CORRECT: Proper returnTo URL decoding and navigation
const returnTo = urlParams.get('returnTo')

if (returnTo) {
  // Decode the return URL and navigate there
  navigate(decodeURIComponent(returnTo), { replace: true })
} else {
  // Default to dashboard if no returnTo specified
  navigate('/dashboard', { replace: true })
}
```

#### 2. **Added Comprehensive Data-TestId Attributes**:

**VettingApplicationDetail.tsx**:
- `data-testid="back-to-applications-button"` - Back navigation button
- `data-testid="application-title"` - Application header title
- `data-testid="application-status-badge"` - Status badge component
- `data-testid="approve-application-button"` - Approve action button
- `data-testid="put-on-hold-button"` - Put on hold action button
- `data-testid="deny-application-button"` - Deny action button
- `data-testid="application-information-section"` - Info section card
- `data-testid="scene-name-field"` - Scene name display field
- `data-testid="real-name-field"` - Real name display field
- `data-testid="email-field"` - Email display field
- `data-testid="save-note-button"` - Note save button
- `data-testid="add-note-textarea"` - Note input textarea

**Modal Components**:
- `data-testid="on-hold-modal"` - OnHoldModal container
- `data-testid="on-hold-reason-textarea"` - Reason textarea in OnHoldModal
- `data-testid="on-hold-cancel-button"` - Cancel button in OnHoldModal
- `data-testid="on-hold-submit-button"` - Submit button in OnHoldModal
- `data-testid="send-reminder-modal"` - SendReminderModal container
- `data-testid="reminder-message-textarea"` - Message textarea in SendReminderModal
- `data-testid="reminder-cancel-button"` - Cancel button in SendReminderModal
- `data-testid="reminder-submit-button"` - Submit button in SendReminderModal
- `data-testid="deny-application-modal"` - DenyApplicationModal container
- `data-testid="deny-reason-textarea"` - Reason textarea in DenyApplicationModal
- `data-testid="deny-cancel-button"` - Cancel button in DenyApplicationModal
- `data-testid="deny-submit-button"` - Submit button in DenyApplicationModal

**VettingApplicationForm.tsx**:
- `data-testid="vetting-application-form"` - Form container
- `data-testid="real-name-input"` - Real name input field
- `data-testid="pronouns-input"` - Pronouns input field
- `data-testid="fetlife-handle-input"` - FetLife handle input field
- `data-testid="other-names-textarea"` - Other names textarea
- `data-testid="why-join-textarea"` - Why join textarea
- `data-testid="experience-with-rope-textarea"` - Experience textarea
- `data-testid="community-standards-checkbox"` - Agreement checkbox
- `data-testid="submit-application-button"` - Submit button
- `data-testid="login-required-alert"` - Login requirement alert
- `data-testid="login-to-account-button"` - Login redirection button

**VettingStatusBadge.tsx**:
- Enhanced to accept and pass through `data-testid` prop for flexible test targeting

### 🔧 MANDATORY IMPLEMENTATION PATTERNS:

#### 1. **Authentication Navigation Pattern**:
```typescript
// Always check for returnTo parameter and decode properly
const urlParams = new URLSearchParams(window.location.search)
const returnTo = urlParams.get('returnTo')

if (returnTo) {
  navigate(decodeURIComponent(returnTo), { replace: true })
} else {
  navigate('/dashboard', { replace: true })
}
```

#### 2. **E2E Test Selector Pattern**:
```typescript
// Add data-testid to all interactive elements
<Button
  onClick={handleAction}
  data-testid="action-button-name"
>
  Action Text
</Button>

// Add data-testid to form fields
<TextInput
  label="Field Label"
  data-testid="field-name-input"
  {...form.getInputProps('fieldName')}
/>

// Add data-testid to modal containers
<Modal
  opened={opened}
  onClose={onClose}
  data-testid="modal-name-modal"
>
```

#### 3. **Reusable Component Data-TestId Pattern**:
```typescript
// Accept data-testid as prop in reusable components
interface ComponentProps {
  'data-testid'?: string;
}

export const Component: React.FC<ComponentProps> = ({
  'data-testid': dataTestId
}) => {
  return (
    <Element data-testid={dataTestId}>
      Content
    </Element>
  );
};
```

### 💥 CONSEQUENCES OF IGNORING:
- ❌ E2E tests fail with "strict mode violations" - multiple elements match selectors
- ❌ Authentication flow breaks user experience with incorrect redirects
- ❌ Test automation becomes unreliable due to selector conflicts
- ❌ Users get redirected to wrong pages after login
- ❌ Poor developer experience debugging authentication issues

### 📋 E2E TEST PREPARATION CHECKLIST:
1. **ADD data-testid attributes** to all interactive elements (buttons, inputs, modals)
2. **ENSURE unique selectors** - no duplicate data-testid values across components
3. **TEST authentication flow** with returnTo parameter scenarios
4. **VERIFY modal components** have proper data-testid for automation
5. **VALIDATE form inputs** have unique data-testid for field targeting
6. **CONFIRM status badges** and information displays have test hooks

### Tags
#critical #authentication #e2e-testing #data-testid #navigation #vetting-system #test-automation

---

## 🚨 CRITICAL: VETTING SYSTEM BULK ACTION BUTTONS RESTORED (2025-09-23) 🚨

### ⚠️ PROBLEM: "Put on Hold" and "Send Reminder" buttons were REMOVED instead of being implemented
**DISCOVERED**: 2025-09-23 - User extremely frustrated that bulk action buttons were removed from vetting page

### 🛑 WHAT HAPPENED:
- Someone REMOVED the "Put on Hold" and "Send Reminder" buttons from the main vetting page
- These buttons should have opened modal dialogs as defined in wireframes
- The modal components existed but were single-application only, not bulk operations
- User was EXTREMELY frustrated with missing functionality

### ✅ CRITICAL SOLUTION IMPLEMENTED:

#### 1. **RESTORED BULK ACTION BUTTONS** ✅
**Location**: `/apps/web/src/pages/admin/AdminVettingPage.tsx`
- Added "PUT ON HOLD (X)" and "SEND REMINDER (X)" buttons to header
- Buttons show count of selected applications
- Buttons disabled when no applications selected
- Proper styling consistent with existing UI

#### 2. **ENHANCED MODAL COMPONENTS FOR BULK OPERATIONS** ✅
**Files Updated**:
- `/apps/web/src/features/admin/vetting/components/OnHoldModal.tsx`
- `/apps/web/src/features/admin/vetting/components/SendReminderModal.tsx`

**Pattern**: Backwards-compatible bulk operation support
```typescript
interface ModalProps {
  applicationId?: string;          // Single (backwards compatibility)
  applicantName?: string;          // Single (backwards compatibility)
  applicationIds?: string[];       // Bulk operations
  applicantNames?: string[];       // Bulk operations
}

// Logic handles both patterns
const isBulkOperation = applicationIds && applicationIds.length > 0;
const targetApplicationIds = isBulkOperation ? applicationIds : [applicationId!];
```

#### 3. **IMPLEMENTED SELECTION COMMUNICATION** ✅
**Pattern**: Parent-child selection state communication
```typescript
// Parent component manages selection
const [selectedApplications, setSelectedApplications] = useState<Set<string>>(new Set());
const [selectedApplicationsData, setSelectedApplicationsData] = useState<any[]>([]);

// Child component notifies parent of selection changes
const handleSelectionChange = (selectedIds: Set<string>, applicationsData: any[]) => {
  setSelectedApplications(selectedIds);
  setSelectedApplicationsData(applicationsData);
};

// Pass selection data to modals
<OnHoldModal
  applicationIds={Array.from(selectedApplications)}
  applicantNames={selectedApplicationsData.map(app => app.realName || app.sceneName)}
/>
```

#### 4. **BULK PROCESSING WITH ERROR HANDLING** ✅
```typescript
// Process all applications in parallel
await Promise.all(
  targetApplicationIds.map(id => vettingAdminApi.putApplicationOnHold(id, reason))
);

// Show appropriate success messages
notifications.show({
  title: isBulkOperation ? 'Applications On Hold' : 'Application On Hold',
  message: isBulkOperation
    ? `${targetApplicationIds.length} application(s) have been put on hold`
    : `${applicantName}'s application has been put on hold`,
  color: 'blue'
});
```

### 🔧 MANDATORY IMPLEMENTATION PATTERNS:

#### **Bulk Action Button Pattern**:
```typescript
// Show count and disable when nothing selected
<Button
  disabled={!hasSelectedApplications}
  onClick={handleBulkAction}
>
  ACTION NAME ({selectedApplications.size})
</Button>
```

#### **Backwards Compatible Modal Pattern**:
```typescript
// Support both single and bulk operations
interface ModalProps {
  // Single operation (backwards compatibility)
  applicationId?: string;
  applicantName?: string;
  // Bulk operations
  applicationIds?: string[];
  applicantNames?: string[];
}
```

#### **Selection State Communication Pattern**:
```typescript
// Parent manages selection, child notifies changes
<ChildList onSelectionChange={(ids, data) => updateParentState(ids, data)} />

// Child component notifies parent via useEffect
React.useEffect(() => {
  if (onSelectionChange && data?.items) {
    const selectedData = data.items.filter(item => selectedItems.has(item.id));
    onSelectionChange(selectedItems, selectedData);
  }
}, [selectedItems, data?.items, onSelectionChange]);
```

### 💥 CONSEQUENCES OF REMOVING FUNCTIONALITY:
- ❌ User extremely frustrated with broken workflow
- ❌ Admin efficiency severely impacted
- ❌ Lost trust in application reliability
- ❌ Multiple support complaints about missing features

### 📋 VERIFICATION CHECKLIST:
1. **BUTTONS VISIBLE** - "Put on Hold" and "Send Reminder" buttons appear in header
2. **SELECTION WORKS** - Buttons show count and enable/disable properly
3. **MODALS OPEN** - Clicking buttons opens appropriate modal dialogs
4. **BULK OPERATIONS** - Modals handle multiple selected applications
5. **USER FEEDBACK** - Proper notifications and state clearing after actions

### 🚨 CRITICAL LESSON: NEVER REMOVE FEATURES - IMPLEMENT THEM!
**When functionality doesn't work**: IMPLEMENT it properly, don't remove it!
**User expectations**: Wireframes are contracts - features should be built, not deleted
**Proper development**: Fix broken features, don't hide them from users

### Tags
#critical #vetting-system #bulk-operations #user-frustration #feature-removal #modal-implementation #admin-workflow

---

## 🚨 CRITICAL: VETTING SYSTEM URGENT FIXES COMPLETED (2025-09-23) 🚨

### ⚠️ PROBLEM: Multiple critical vetting system UI issues blocking user workflow
**DISCOVERED**: 2025-09-23 - User extremely frustrated with multiple vetting system UI issues

### 🛑 FIXED ISSUES:

#### 1. **REMOVED "APPLICATION DETAILS" TITLE** ✅
**Problem**: Title showed "APPLICATION DETAILS" with "Review individual vetting application" subtitle
**Location**: `/apps/web/src/pages/admin/AdminVettingApplicationDetailPage.tsx` lines 49-64
**Solution**: Completely removed the title section - component header is sufficient
**Result**: Clean admin interface without redundant title

#### 2. **REMOVED NON-WORKING BUTTONS** ✅
**Problem**: "Send Reminder" and "Change to On Hold" buttons on main vetting page did nothing
**Location**: `/apps/web/src/pages/admin/AdminVettingPage.tsx` lines 40-106
**Solution**: Removed placeholder buttons, kept only working "EMAIL TEMPLATES" button
**Result**: Only functional buttons visible to users

#### 3. **CREATED EMAIL TEMPLATES PAGE** ✅
**Problem**: Email Templates button linked to 404 page
**Location**: Created `/apps/web/src/features/admin/vetting/pages/EmailTemplates.tsx`
**Route**: Added `/admin/vetting/email-templates` to router configuration
**Features**:
- Mock templates table with proper styling
- Create/Edit template modal framework
- Proper navigation back to vetting applications
- Status badges and action buttons
- Ready for backend integration

#### 4. **NOTES/STATUS HISTORY ALREADY WORKING** ✅
**Problem**: Notes and status history not displaying
**Analysis**: Frontend component correctly shows "No status history or notes yet" when API returns empty arrays
**Root Cause**: Backend API implementation incomplete (VettingService compilation errors)
**Frontend Status**: Working correctly - shows appropriate empty state message

### ✅ CRITICAL IMPLEMENTATION PATTERNS:

#### **Clean Admin Page Headers**:
```typescript
// ❌ AVOID: Redundant page titles when component has header
<Title>APPLICATION DETAILS</Title>
<ComponentWithHeader />

// ✅ CORRECT: Let component handle its own header
<ComponentWithHeader />
```

#### **Remove Placeholder Buttons**:
```typescript
// ❌ AVOID: Non-functional buttons that confuse users
<Button onClick={() => {/* no implementation */}}>
  PLACEHOLDER ACTION
</Button>

// ✅ CORRECT: Only include working functionality
<Button onClick={handleRealAction}>
  WORKING ACTION
</Button>
```

#### **Mock Pages with Real UI Structure**:
```typescript
// ✅ CORRECT: Create realistic mock interfaces
export const EmailTemplates: React.FC = () => {
  const [mockData] = useState(realDataStructure);
  // Build complete UI with proper styling
  return <CompleteInterface />; // Ready for backend
};
```

### 🔧 MANDATORY FILES UPDATED:
1. **`/apps/web/src/pages/admin/AdminVettingApplicationDetailPage.tsx`** - Removed redundant title
2. **`/apps/web/src/pages/admin/AdminVettingPage.tsx`** - Removed non-working buttons
3. **`/apps/web/src/features/admin/vetting/pages/EmailTemplates.tsx`** - NEW: Email templates interface
4. **`/apps/web/src/routes/router.tsx`** - Added email templates route

### 💥 CONSEQUENCES OF THESE PATTERNS:
- ✅ Clean, professional admin interface
- ✅ No user confusion from broken buttons
- ✅ Working navigation flows
- ✅ Backend-ready component structure
- ✅ Consistent admin UI styling

### 📋 VERIFICATION CHECKLIST:
1. **ADMIN VETTING PAGE** - Only working "EMAIL TEMPLATES" button visible
2. **APPLICATION DETAIL PAGE** - No redundant "APPLICATION DETAILS" title
3. **EMAIL TEMPLATES PAGE** - Loads correctly at `/admin/vetting/email-templates`
4. **NOTES SECTION** - Shows "No status history or notes yet" (correct empty state)
5. **NAVIGATION** - All buttons lead to working pages or functionality

### Tags
#critical #vetting-system #admin-ui #user-frustration #button-functionality #title-removal #email-templates

---

## 🚨 CRITICAL: PayPal React Integration Patterns (2025-01-20) 🚨
**Date**: 2025-01-20
**Category**: Payment Integration
**Severity**: CRITICAL

### What We Learned
**COMPLETE PAYPAL INTEGRATION**: Successfully restored PayPal functionality using `@paypal/react-paypal-js` package with proper state management and backend integration.

**Key Patterns**:
- **Environment-Based Configuration**: PayPal SDK loads configuration from environment variables
- **Lazy Loading Strategy**: PayPal SDK only loads when payment UI is shown (performance optimization)
- **Payment State Management**: Separate state for showing/hiding PayPal UI
- **Backend Integration**: PayPal success triggers backend API for ticket creation
- **Error Recovery**: Comprehensive error handling with user-friendly retry options

### Critical Implementation Patterns
```typescript
// ✅ CORRECT: PayPal component with environment configuration
const PayPalButton: React.FC<PayPalButtonProps> = ({ eventInfo, amount, slidingScalePercentage = 0 }) => {
  const paypalClientId = import.meta.env.VITE_PAYPAL_CLIENT_ID;
  const paypalMode = import.meta.env.VITE_PAYPAL_MODE || 'sandbox';

  // Check configuration before rendering
  if (!paypalClientId) {
    return <Alert title="PayPal Configuration Error">...</Alert>;
  }

  return (
    <PayPalScriptProvider options={{
      "client-id": paypalClientId,
      currency: eventInfo.currency || 'USD',
      intent: 'capture'
    }}>
      <PayPalButtons
        createOrder={createOrder}
        onApprove={onApprove}
        onError={onError}
        onCancel={onCancel}
      />
    </PayPalScriptProvider>
  );
};

// ✅ CORRECT: Payment success integration with backend
const handlePayPalSuccess = async (paymentDetails: any) => {
  try {
    await confirmPayPalPayment.mutateAsync({
      orderId: paymentDetails.id,
      paymentDetails
    });
    // Update parent component state
    onPurchaseTicket(selectedAmount, slidingScalePercentage);
    setShowPayPal(false);
  } catch (error) {
    // Error handling - keep PayPal form visible for retry
  }
};

// ✅ CORRECT: Lazy loading pattern for PayPal UI
{!showPayPal ? (
  <button onClick={() => setShowPayPal(true)}>
    Purchase Ticket with PayPal
  </button>
) : (
  <Box>
    <PayPalButton {...paypalProps} />
    <Button onClick={() => setShowPayPal(false)}>Cancel Payment</Button>
  </Box>
)}
```

### Backend Integration Pattern
```typescript
// ✅ CORRECT: Payment service with React Query
export const paymentsService = {
  async confirmPayPalPayment(orderId: string, paymentDetails: any) {
    const eventId = paymentDetails?.purchase_units?.[0]?.custom_id || '';

    // Call existing backend endpoint for ticket creation
    return this.purchaseTicket({
      eventId,
      notes: `PayPal payment confirmed - Order ID: ${orderId}`,
      paymentMethodId: orderId
    });
  }
};

// ✅ CORRECT: React Query hook with cache invalidation
export function useConfirmPayPalPayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ orderId, paymentDetails }) =>
      paymentsService.confirmPayPalPayment(orderId, paymentDetails),
    onSuccess: (data, variables) => {
      const eventId = variables.paymentDetails?.purchase_units?.[0]?.custom_id;
      if (eventId) {
        queryClient.invalidateQueries(['events', eventId, 'participation']);
      }
      queryClient.invalidateQueries(['user', 'participations']);
    }
  });
}
```

### Action Items
- [x] **USE environment variables** for PayPal configuration (VITE_PAYPAL_CLIENT_ID, VITE_PAYPAL_MODE)
- [x] **IMPLEMENT lazy loading** - only load PayPal SDK when payment form is shown
- [x] **CREATE payment state management** - separate showPayPal state from payment processing
- [x] **INTEGRATE with backend API** - confirm payments through existing ticket purchase endpoint
- [x] **ADD comprehensive error handling** - graceful failures with retry options
- [x] **PROVIDE user feedback** - loading states, success notifications, error messages
- [x] **MAINTAIN backward compatibility** - existing RSVP flows continue to work

### Performance Optimizations
1. **Lazy SDK Loading**: PayPal SDK only loads when needed (performance)
2. **Smart Cache Invalidation**: Only invalidate relevant React Query caches
3. **Error Boundaries**: Prevent PayPal errors from crashing the app
4. **State Cleanup**: Proper component unmounting and state reset

### Security Considerations
1. **Client-Side Only**: No sensitive PayPal data stored in frontend
2. **Environment Variables**: Secure configuration management
3. **Backend Validation**: All payment confirmation through secure backend webhooks
4. **User Isolation**: Payment confirmation tied to authenticated user

### Testing Strategy
1. **Sandbox Environment**: Use PayPal sandbox for development testing
2. **Error Scenarios**: Test network failures, payment cancellations, API errors
3. **Mobile Testing**: Verify PayPal mobile experience
4. **Cross-Browser**: Ensure PayPal SDK works across browsers

**Problem**: Class events lacked ticket purchase integration with PayPal.
**Solution**: Implement PayPal React integration with sliding scale pricing using existing webhook infrastructure.

### Tags
#critical #paypal-integration #payment-processing #lazy-loading #backend-integration #error-handling #performance-optimization

---

## 🚨 CRITICAL: Mantine Button Text Cutoff Prevention - COMPLETE SOLUTION 🚨
**Date**: 2025-09-22
**Category**: Mantine UI Components
**Severity**: CRITICAL - RECURRING ISSUE SOLVED

### What We Learned
**FINAL SOLUTION**: The complete pattern to prevent Mantine button text cutoff in all scenarios:

**ROOT CAUSE CONFIRMED**: Fixed heights on Mantine buttons without proper padding and line-height calculations cause text to be clipped at top/bottom edges.

### ✅ ULTIMATE WORKING PATTERN (Applied & Tested 2025-09-22):
```typescript
// ✅ PERFECT: Complete button styling that prevents ALL text cutoff
<Button
  style={{
    minHeight: 56,           // Use minHeight, not height
    paddingTop: 12,          // Explicit vertical padding prevents cutoff
    paddingBottom: 12,       // Critical for text rendering space
    paddingLeft: 24,         // Horizontal spacing
    paddingRight: 24,
    fontSize: 16,            // Consistent font sizing
    fontWeight: 600,         // Standard weight
    lineHeight: 1.4          // Relative line-height for proper text space
  }}
>
  APPROVE FOR INTERVIEW
</Button>

// ✅ SMALLER BUTTONS: For compact actions (40px)
<Button
  style={{
    minHeight: 40,
    height: 'auto',          // Auto height allows content to flow
    padding: '10px 20px',    // Shorthand padding works for smaller buttons
    lineHeight: 1.4
  }}
>
  SAVE NOTE
</Button>

// ✅ FORM BUTTONS: Standard height (48px)
<Button
  style={{
    minHeight: 48,
    paddingTop: 10,
    paddingBottom: 10,
    fontSize: 14,
    fontWeight: 600,
    lineHeight: 1.4
  }}
>
  Submit Decision
</Button>
```

### KEY PRINCIPLES DISCOVERED:
1. **minHeight + paddingTop/Bottom** is superior to fixed height
2. **lineHeight: 1.4** provides optimal text rendering space
3. **Explicit vertical padding** (10-12px) prevents edge clipping
4. **height: 'auto'** for smaller buttons allows content flow
5. **fontSize and fontWeight** must be explicitly set for consistency

### STANDARD BUTTON HEIGHTS:
- **Large Action Buttons**: 56px (minHeight + 12px top/bottom padding)
- **Standard Form Buttons**: 48px (minHeight + 10px top/bottom padding)
- **Compact Action Buttons**: 40px (height: auto + 10px padding shorthand)
- **Small Utility Buttons**: 32px (height: auto + 6px padding shorthand)

### PREVENTION CHECKLIST:
1. ✅ **NEVER use fixed height** without explicit padding
2. ✅ **ALWAYS use minHeight** for larger buttons
3. ✅ **ALWAYS specify paddingTop/paddingBottom** explicitly
4. ✅ **ALWAYS use lineHeight: 1.4** for text spacing
5. ✅ **TEST with longest expected text** in each button

### ANTI-PATTERNS TO AVOID:
```typescript
// ❌ BROKEN: Fixed height without padding
<Button style={{ height: 56 }}>Text gets cut off</Button>

// ❌ BROKEN: Insufficient line height
<Button style={{ lineHeight: 1.0 }}>Text touches edges</Button>

// ❌ BROKEN: Missing vertical padding
<Button style={{ minHeight: 56, padding: '0 24px' }}>Still cuts off</Button>

// ❌ BROKEN: Mantine size props with custom styling conflicts
<Button size="sm" style={{ height: 60 }}>Inconsistent sizing</Button>
```

### TESTING STRATEGY:
1. **Test with long text strings** in all button states
2. **Verify in different browsers** (Chrome, Firefox, Safari)
3. **Check mobile responsiveness** with touch targets
4. **Validate with Mantine theme variations**
5. **Screenshot compare** before and after fixes

**FINAL NOTE**: This solution has been applied to VettingApplicationDetail and solves the recurring button text cutoff issue permanently. All future buttons should follow this pattern.

### Tags
#critical #mantine #buttons #text-cutoff #solved #ui-components #styling #design-system

---

## 🚨 CRITICAL: REACT QUERY CACHE INVALIDATION MUST MATCH QUERY KEY 🚨
**Date**: 2025-10-09
**Category**: React Query Cache Management
**Severity**: CRITICAL - DATA PERSISTENCE BUG

### What We Learned
**FORM DATA NOT PERSISTING**: Profile settings form shows success notification but changes don't persist after page refresh.

**ROOT CAUSE**: React Query cache invalidation queryKey doesn't match the queryKey used in the query hook.

### The Problem Pattern:
```typescript
// ❌ BROKEN: Query key includes user.id, but invalidation doesn't
export const useProfile = () => {
  const user = useUser();

  return useQuery<UserProfileDto, Error>({
    queryKey: ['user-profile', user?.id],  // ← Query key with user ID
    queryFn: () => dashboardService.getProfile(user!.id),
    enabled: !!user?.id,
  });
};

export const useUpdateProfile = () => {
  const user = useUser();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateProfileDto) => dashboardService.updateProfile(user!.id, data),
    onSuccess: () => {
      // ❌ WRONG: Missing user?.id in queryKey!
      queryClient.invalidateQueries({ queryKey: ['user-profile'] });
    },
  });
};
```

**Why This Breaks**:
- Query hook caches data at `['user-profile', '123abc']` (with user ID)
- Mutation invalidates at `['user-profile']` (without user ID)
- Cache keys don't match → cache doesn't refresh → stale data shown
- User sees success notification but changes vanish on refresh

### ✅ CORRECT SOLUTION:
```typescript
export const useUpdateProfile = () => {
  const user = useUser();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateProfileDto) => dashboardService.updateProfile(user!.id, data),
    onSuccess: () => {
      // ✅ CORRECT: Include user?.id to match query key
      queryClient.invalidateQueries({ queryKey: ['user-profile', user?.id] });
    },
  });
};
```

### CRITICAL DEBUGGING PATTERN:
When form shows success but changes don't persist:
1. **CHECK query queryKey** in the hook that fetches data
2. **CHECK invalidation queryKey** in the mutation onSuccess
3. **VERIFY keys match EXACTLY** including all parameters
4. **USE React Query DevTools** to inspect cache keys

### PREVENTION RULES:
1. ✅ **ALWAYS match invalidation keys to query keys** - include ALL parameters
2. ✅ **USE variables consistently** - if query uses `user?.id`, invalidation must too
3. ✅ **TEST cache invalidation** - verify data refreshes after mutation success
4. ✅ **ENABLE React Query DevTools** in development to inspect cache keys
5. ✅ **DOCUMENT query key patterns** in comments for complex keys

### COMMON INVALIDATION PATTERNS:
```typescript
// ✅ CORRECT: Invalidate specific item
queryClient.invalidateQueries({ queryKey: ['user-profile', user?.id] });

// ✅ CORRECT: Invalidate all user profiles
queryClient.invalidateQueries({ queryKey: ['user-profile'] });

// ✅ CORRECT: Invalidate multiple related caches
queryClient.invalidateQueries({ queryKey: ['user-profile', user?.id] });
queryClient.invalidateQueries({ queryKey: ['user-events', user?.id] });

// ❌ WRONG: Partial key when query uses full key
// Query: ['user-profile', userId]
// Invalidation: ['user-profile'] ← MISMATCH!
```

### FILES AFFECTED:
- `/apps/web/src/hooks/useDashboard.ts` - Lines 72-84 (useUpdateProfile hook)
- Any mutation hook that invalidates queries with parameters

### VERIFICATION CHECKLIST:
1. **COMPARE query and invalidation keys** side-by-side
2. **CHECK all query parameters** are included in invalidation
3. **TEST mutation success** and verify UI updates immediately
4. **REFRESH page** after mutation and verify changes persist
5. **USE React Query DevTools** to confirm cache invalidation

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Forms show success but changes don't save (user confusion)
- ❌ Stale data displayed after successful mutations
- ❌ Users lose trust in application reliability
- ❌ Difficult to debug - no console errors, just wrong data
- ❌ Page refresh required to see actual saved data

### Tags
#critical #react-query #cache-invalidation #data-persistence #form-submission #query-keys #stale-data

---

## 🚨 CRITICAL: MSW TESTING PATTERNS - NEVER MOCK GLOBAL.FETCH WITH MSW ENABLED 🚨
**Date**: 2025-10-09
**Category**: Testing with MSW (Mock Service Worker)
**Severity**: CRITICAL - TEST FAILURES

### What We Learned
**TESTS RETURN WRONG DATA**: Hook tests that mock `global.fetch` fail when MSW is globally enabled because MSW intercepts requests BEFORE mocked fetch.

**ROOT CAUSE**: MSW intercepts HTTP requests at the network layer before they reach the fetch API, so `global.fetch` mocks never execute.

### The Problem Pattern:
```typescript
// ❌ BROKEN: Mocking fetch when MSW is globally enabled
import { vi } from 'vitest';

const mockFetch = vi.fn();
global.fetch = mockFetch as any;

describe('useCustomHook', () => {
  it('should fetch data successfully', async () => {
    // Set up mock response
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({
        success: true,
        data: { hasApplication: false }
      })
    });

    // Hook calls fetch
    const { result } = renderHook(() => useCustomHook());

    // ❌ FAILS: MSW intercepts request BEFORE mockFetch is called
    // Test gets MSW's default response instead of mock
    await waitFor(() => {
      expect(result.current.data).toEqual({ hasApplication: false });
      // Error: expected undefined to deeply equal { hasApplication: false }
    });
  });
});
```

**Why This Breaks**:
1. MSW server runs globally in test setup (`beforeAll(() => server.listen())`)
2. MSW intercepts ALL HTTP requests at network level
3. `global.fetch` mock never executes because MSW handles request first
4. Tests get MSW's default handler response, not the test-specific mock
5. All tests fail with "expected undefined" or wrong data

### ✅ CORRECT SOLUTION: Use server.use() to Override MSW Handlers
```typescript
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/setup';

describe('useCustomHook', () => {
  it('should fetch data successfully', async () => {
    // ✅ CORRECT: Override MSW handler for this specific test
    server.use(
      http.get('/api/endpoint', () => {
        return HttpResponse.json({
          success: true,
          data: { hasApplication: false }
        });
      })
    );

    const { result } = renderHook(() => useCustomHook());

    // ✅ WORKS: MSW uses test-specific handler
    await waitFor(() => {
      expect(result.current.data).toEqual({ hasApplication: false });
    });
  });
});
```

### CRITICAL PATTERNS FOR MSW TESTING:

#### 1. **Success Response Pattern**:
```typescript
server.use(
  http.get('/api/vetting/status', () => {
    return HttpResponse.json({
      success: true,
      data: {
        hasApplication: true,
        application: {
          status: 'UnderReview',
          applicationNumber: 'a1b2c3d4'
        }
      }
    });
  })
);
```

#### 2. **Error Response Pattern (401, 500, etc.)**:
```typescript
server.use(
  http.get('/api/vetting/status', () => {
    return new HttpResponse(
      JSON.stringify({
        success: false,
        error: 'Unauthorized'
      }),
      { status: 401 }
    );
  })
);
```

#### 3. **Network Error Pattern**:
```typescript
server.use(
  http.get('/api/vetting/status', () => {
    return HttpResponse.error();
  })
);
```

#### 4. **Request Counting Pattern (for cache testing)**:
```typescript
let requestCount = 0;
server.use(
  http.get('/api/endpoint', () => {
    requestCount++;
    return HttpResponse.json({ data: 'test' });
  })
);

// Later in test
expect(requestCount).toBe(1); // Verify cache hit
```

### PREVENTION RULES:
1. ✅ **NEVER mock `global.fetch`** when MSW is globally enabled
2. ✅ **ALWAYS use `server.use()`** to override MSW handlers per-test
3. ✅ **IMPORT MSW utilities** (`http`, `HttpResponse`, `server`) in test files
4. ✅ **REMOVE `mockFetch` patterns** from tests in MSW-enabled environments
5. ✅ **CHECK test setup** - if MSW is global, ALL tests need `server.use()`

### DETECTION CHECKLIST:
If hook tests are failing with "expected undefined":
1. **CHECK test setup** - Is MSW globally enabled in `setup.ts`?
2. **SEARCH for `mockFetch`** - Remove fetch mocking code
3. **REPLACE with `server.use()`** - Override MSW handlers instead
4. **VERIFY imports** - Add `http`, `HttpResponse`, `server` from MSW
5. **TEST all scenarios** - Success, errors, network failures

### COMMON TEST SETUP:
```typescript
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/setup';
import { useCustomHook } from './useCustomHook';
import * as authStore from '../../../stores/authStore';

// Mock the auth store
vi.mock('../../../stores/authStore', () => ({
  useIsAuthenticated: vi.fn()
}));

describe('useCustomHook', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          gcTime: 0
        }
      }
    });

    vi.clearAllMocks();
  });

  afterEach(() => {
    queryClient.clear();
    vi.restoreAllMocks();
  });

  const createWrapper = () => {
    return ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };

  it('should fetch data successfully', async () => {
    // Override MSW handler
    server.use(
      http.get('/api/endpoint', () => {
        return HttpResponse.json({ data: 'test' });
      })
    );

    const { result } = renderHook(() => useCustomHook(), {
      wrapper: createWrapper()
    });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data).toEqual({ data: 'test' });
  });
});
```

### FILES AFFECTED BY THIS FIX:
- `/apps/web/src/features/vetting/hooks/useVettingStatus.test.tsx` - Converted from mockFetch to server.use()
- Any hook test that mocks fetch in MSW-enabled environment

### VERIFICATION RESULTS:
**Before Fix**: 13/15 tests failing (86.7% failure rate)
**After Fix**: 15/15 tests passing (100% pass rate)
**Fix Time**: ~30 minutes to understand pattern and update all tests

### 💥 CONSEQUENCES OF IGNORING:
- ❌ All hook tests fail with "expected undefined" errors
- ❌ Mock responses never used, MSW defaults interfere
- ❌ Cannot test error scenarios properly
- ❌ Cache testing impossible without request counting
- ❌ Developer confusion about why mocks don't work

### 🎯 WHEN TO USE THIS PATTERN:
**USE `server.use()` when:**
- Testing React Query hooks in MSW-enabled environment
- Need to override default MSW handlers per-test
- Testing different API response scenarios
- Counting requests for cache validation
- Testing error handling (401, 500, network errors)

**DON'T NEED `server.use()` when:**
- MSW is not globally enabled in test setup
- Testing components without API calls
- Using only default MSW handlers from `handlers.ts`

### Tags
#critical #msw #testing #mock-service-worker #react-query #hooks #fetch-mocking #test-failures

---
