# React Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 2 total
**Read ALL**: Part 1, Part 2
**Write to**: Part 2 ONLY
**Max size**: 2,000 lines per file (NOT 2,500)
**IF READ FAILS**: STOP and fix per documentation-standards.md

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using procedure in documentation-standards.md
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

## 🚨 CRITICAL: VETTING FORM AUTHENTICATION HANDLING PATTERN 🚨

### ⚠️ PROBLEM: Public forms accessed by non-authenticated users fail with poor UX
**DISCOVERED**: 2025-09-22 - Vetting application form at `/join` route not handling non-authenticated users gracefully

### 🛑 ROOT CAUSE:
- Forms requiring authentication but accessible via public routes (/join, /contact, etc.)
- React Query hooks throwing 401 errors instead of handling gracefully
- No clear authentication requirement messaging for users
- API calls failing silently with poor error feedback

### ✅ CRITICAL SOLUTION PATTERN:
1. **CHECK authentication state BEFORE rendering form** - show login requirement prominently
2. **CONFIGURE React Query hooks** to handle 401 errors gracefully with `throwOnError`
3. **PROVIDE clear login/register paths** for non-authenticated users
4. **ENHANCE error messages** with specific guidance and context

```typescript
// ✅ CORRECT: Check auth state and show requirement upfront
if (!isAuthenticated || !user) {
  return (
    <Alert color="blue" icon={<IconLogin />} title="Login Required">
      <Stack gap="md">
        <Text>You must have an account and be logged in to submit a vetting application.</Text>
        <Group gap="md">
          <Button component="a" href="/login" color="wcr" leftSection={<IconLogin />}>
            Login to Your Account
          </Button>
          <Text size="sm" c="dimmed">
            Don't have an account? <Anchor href="/register" fw={600}>Create one here</Anchor>
          </Text>
        </Group>
      </Stack>
    </Alert>
  );
}

// ✅ CORRECT: React Query with graceful 401 handling
const { data, error } = useQuery({
  queryKey: ['resource'],
  queryFn: apiCall,
  enabled: !!user && isAuthenticated, // Only run when authenticated
  throwOnError: (error: any) => {
    // Don't throw 401 errors - let UI handle auth state
    return error?.response?.status !== 401;
  }
});

// ✅ CORRECT: API service with graceful 401 handling
async checkExistingApplication(): Promise<Data | null> {
  try {
    const { data } = await apiClient.get('/api/resource');
    return data.data || null;
  } catch (error: any) {
    if (error.response?.status === 401) {
      return null; // Graceful handling - let UI manage auth state
    }
    throw error; // Re-throw other errors
  }
}

// ❌ BROKEN: No auth check, poor error handling
const { data } = useQuery({
  queryKey: ['resource'],
  queryFn: apiCall // Will throw 401 and confuse users
});
```

### 🏗️ ENHANCED ERROR MESSAGE PATTERN:
```typescript
export const getErrorMessage = (error: any): string => {
  const status = error.response?.status || error.status;
  const message = error.message || error.response?.data?.message;

  if (status === 401) {
    return 'You must be logged in to access this feature. Please login or create an account first.';
  }

  // Network errors
  if (error.code === 'NETWORK_ERROR' || message?.includes('Network Error')) {
    return 'Network connection error. Please check your internet connection and try again.';
  }

  // Timeout errors
  if (error.code === 'ECONNABORTED' || message?.includes('timeout')) {
    return 'Request timed out. Please try again.';
  }

  return message || 'An unexpected error occurred. Please try again.';
};
```

### 🔧 MANDATORY IMPLEMENTATION CHECKLIST:
1. **CHECK authentication state** before showing forms requiring auth
2. **SHOW login requirement** prominently with helpful UI and links
3. **CONFIGURE React Query** `throwOnError` to handle 401s gracefully
4. **ENHANCE API error messages** with user-friendly guidance
5. **TEST with non-authenticated users** accessing public routes
6. **PROVIDE form preview** for non-authenticated users when helpful

### 📋 CRITICAL TESTING REQUIREMENTS:
- **Non-authenticated user** visits form route → sees login requirement
- **Authenticated user** uses form normally with proper error feedback
- **Network/server errors** show helpful messages to users
- **Page refresh** maintains proper authentication state

### Tags
#critical #authentication #forms #public-routes #error-handling #user-experience

---

## 🚨 CRITICAL: TICKET AMOUNT DATA MAPPING ISSUE - METADATA FIELD MISSING 🚨

### ⚠️ PROBLEM: Ticket purchase amounts displaying as $0 on public event pages
**DISCOVERED**: 2025-09-22 - Frontend attempting to access non-existent `amount` field instead of parsing JSON metadata

### 🛑 ROOT CAUSE:
- Backend stores ticket purchase amounts in `EventParticipation.Metadata` field as JSON: `{"purchaseAmount": 50}`
- Original `ParticipationStatusDto` and `EventParticipationDto` DTOs missing `metadata` field
- Frontend code defaulting to `participationAny.amount || 0` when field doesn't exist
- Admin view hardcoded `$50.00` instead of reading actual amount from data

### ✅ CRITICAL SOLUTION:
1. **UPDATE backend DTOs** to include `metadata` field
2. **REGENERATE TypeScript types** after DTO changes using NSwag
3. **CREATE helper function** to parse amount from metadata JSON
4. **UPDATE frontend mapping logic** to use metadata instead of non-existent amount field

```typescript
// ✅ CORRECT: Helper to extract amount from metadata JSON
const extractAmountFromMetadata = (metadata?: string): number => {
  if (!metadata) return 0;
  try {
    const parsed = JSON.parse(metadata);
    return parsed.purchaseAmount || parsed.amount || parsed.ticketAmount || 0;
  } catch (error) {
    console.warn('Failed to parse participation metadata:', metadata, error);
    return 0;
  }
};

// ✅ CORRECT: Use metadata for amount in data conversion
ticket: hasTicket ? {
  id: participationAny.id || '',
  status: participationAny.status || 'Active',
  amount: extractAmountFromMetadata(participationAny.metadata) || 0, // Extract from metadata
  paymentStatus: 'Completed',
  createdAt: participationAny.participationDate || new Date().toISOString(),
  canceledAt: undefined,
  cancelReason: undefined
} : null

// ❌ BROKEN: Trying to access non-existent amount field
amount: participationAny.amount || 0, // This field doesn't exist!
```

### 🏗️ BACKEND DTO FIXES REQUIRED:
```csharp
// ✅ Add metadata field to both DTOs
public class ParticipationStatusDto
{
    // ... existing fields ...
    public string? Metadata { get; set; } // ADD THIS
}

public class EventParticipationDto
{
    // ... existing fields ...
    public string? Metadata { get; set; } // ADD THIS
}

// ✅ Include metadata in service mapping
var dto = new ParticipationStatusDto
{
    // ... existing mappings ...
    Metadata = participation.Metadata // ADD THIS
};
```

### 🔄 POST-FIX STEPS:
1. Restart API container after DTO changes
2. Regenerate types: `cd packages/shared-types && npm run generate`
3. Update frontend helper functions in both public and admin views
4. Replace hardcoded amounts with dynamic metadata parsing

### 🎯 PREVENTION RULES:
- **NEVER assume field names** match between frontend expectations and backend DTOs
- **ALWAYS include metadata fields** in DTOs when they contain important display data
- **CREATE helper functions** for parsing JSON metadata consistently
- **AVOID hardcoded values** in admin displays - use actual data
- **REGENERATE types immediately** after any DTO structure changes

## 🚨 CRITICAL: MANTINE FORM VALIDATION & SUBMIT BUTTON PATTERNS 🚨

### ⚠️ PROBLEM: Form submit buttons stay disabled even with valid data
**DISCOVERED**: 2025-09-22 - Mantine form `isValid()` and `isDirty()` checks preventing submission

### 🛑 ROOT CAUSE:
- `form.isDirty()` check fails when form values don't change from initial state properly
- Complex `form.isValid()` logic with Zod validation not working as expected
- Over-reliance on Mantine's built-in validation state vs explicit field checks

### ✅ CRITICAL SOLUTION PATTERN:
1. **REPLACE complex validation** with explicit field and error checks
2. **REMOVE isDirty() requirement** for simple forms where all fields start empty
3. **USE explicit required field checks** for better predictability
4. **CONFIGURE validation timing** with `validateInputOnChange` and `validateInputOnBlur`

```typescript
// ❌ BROKEN: Over-complex validation logic
disabled={!form.isValid() || !form.isDirty() || !isAuthenticated}

// ✅ CORRECT: Explicit field and error checks
disabled={
  Object.keys(form.errors).length > 0 ||
  !isAuthenticated ||
  !form.values.requiredField1 ||
  !form.values.requiredField2 ||
  !form.values.agreesToTerms
}

// ✅ CORRECT: Enhanced form configuration
const form = useForm<FormData>({
  validate: zodResolver(schema),
  initialValues: defaultValues,
  // Enable real-time validation for better UX
  validateInputOnChange: true,
  validateInputOnBlur: true,
});
```

### 🔧 VALIDATION DEBUGGING PATTERN:
```typescript
// Add temporary logging to debug form state
console.log('Form validation debug:', {
  isValid: form.isValid(),
  isDirty: form.isDirty(),
  errors: form.errors,
  values: form.values,
  hasErrors: Object.keys(form.errors).length > 0
});
```

## 🚨 CRITICAL: BOOLEAN && PATTERN RENDERS "0" IN REACT JSX 🚨

### ⚠️ PROBLEM: "0" appearing before buttons or components in React conditional rendering
**DISCOVERED**: 2025-09-22 - IIFE returning boolean used with && operator renders falsy values like "0"

### 🛑 ROOT CAUSE:
- React renders falsy values (0, empty string) when using `condition && <Component />`
- IIFE (Immediately Invoked Function Expression) returning boolean creates problematic pattern
- When condition evaluates to 0 or false, React displays the value instead of hiding component
- Pattern: `(() => { return someCondition; })() && <Component />` breaks when someCondition is falsy

### ✅ CRITICAL SOLUTION:
1. **NEVER use boolean && pattern with functions that might return falsy values**
2. **USE ternary operator** with explicit null for false cases
3. **CONVERT IIFE patterns** to proper conditional rendering
4. **ALWAYS use Boolean()** to convert numbers to true/false if needed

```typescript
// ❌ BROKEN: IIFE boolean && pattern renders "0"
{(() => {
  const condition = someNumber || someBoolean;
  return condition;
})() && (
  <Button>Click me</Button>
)}

// ✅ CORRECT: Ternary operator with explicit null
{(() => {
  const condition = someNumber || someBoolean;
  return condition ? (
    <Button>Click me</Button>
  ) : null;
})()}

// ✅ EVEN BETTER: Direct ternary without IIFE
{(someNumber || someBoolean) ? (
  <Button>Click me</Button>
) : null}

// ✅ SAFEST: Boolean conversion
{Boolean(someNumber) && (
  <Button>Click me</Button>
)}
```

### 🔧 MANDATORY DETECTION CHECKLIST:
1. **SEARCH for `})() &&`** in all React components
2. **REPLACE with ternary operator** `condition ? component : null`
3. **TEST edge cases** where conditions might be 0, "", false, null
4. **USE Boolean()** conversion for numeric conditions

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Unwanted "0" or empty strings render in UI
- ❌ Poor user experience with visual glitches
- ❌ Difficult to debug conditional rendering issues
- ❌ Inconsistent component display behavior

---

## 🚨 CRITICAL: MIXING CUSTOM CSS CLASSES WITH MANTINE BREAKS STYLING 🚨

### ⚠️ PROBLEM: Button text cut off, inconsistent styling when using custom CSS classes with Mantine
**DISCOVERED**: 2025-09-22 - Buttons using `className="btn btn-primary"` have text cutoff and styling conflicts

### 🛑 ROOT CAUSE:
- Custom CSS classes from legacy codebase override Mantine component styling
- `btn` class has conflicting padding, font-size, and layout properties
- Mantine Button components have their own styling that gets overridden
- Text cutoff occurs due to conflicting line-height and padding values

### ✅ CRITICAL SOLUTION:
1. **NEVER mix custom CSS classes with Mantine components** - use Mantine props only
2. **USE Mantine Button component props** instead of CSS classes
3. **REPLACE custom classes systematically** across all components
4. **TEST button text visibility** after converting from custom styles

```typescript
// ❌ BROKEN: Custom CSS classes with Mantine Button
<Button className="btn btn-primary" style={{ width: '100%' }}>
  Text gets cut off
</Button>

// ✅ CORRECT: Pure Mantine Button with proper props
<Button
  variant="filled"
  color="blue"
  fullWidth
  size="lg"
  loading={isLoading}
>
  Text displays properly
</Button>
```

### 🔧 MANDATORY CONVERSION CHECKLIST:
1. **SEARCH for `className="btn`** in all React components
2. **REPLACE with appropriate Mantine Button variant/color**
3. **USE fullWidth instead of style={{ width: '100%' }}**
4. **USE leftSection/rightSection for icons** instead of inline elements
5. **TEST all button states** (normal, hover, loading, disabled)

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Button text gets cut off at top and bottom
- ❌ Inconsistent styling across the application
- ❌ Poor user experience with unreadable buttons
- ❌ Mantine design system benefits lost

---

## 🚨 CRITICAL: HARDCODED EMPTY LISTS BREAK DASHBOARD FEATURES 🚨

### ⚠️ PROBLEM: Dashboard "Your Upcoming Events" shows no events despite user having RSVPs
**DISCOVERED**: 2025-09-21 - Dashboard shows empty events list even when user has active participations

### 🛑 ROOT CAUSE:
- API service method `GetUserEventsAsync` hardcoded to return empty list
- Comment says "Return empty list for now to get the endpoint working, then enhance later"
- Frontend component works correctly but API never returns actual data
- Placeholder implementations left in production code

### ✅ CRITICAL SOLUTION:
1. **NEVER ship placeholder implementations** - finish or remove them
2. **SEARCH for TODO comments** that indicate incomplete functionality
3. **ALWAYS test API endpoints** with actual data, not just successful responses

```csharp
// ❌ BROKEN: Hardcoded empty list
var upcomingEvents = new List<DashboardEventDto>(); // Placeholder!

// ✅ CORRECT: Actual query implementation
var upcomingEvents = await _context.EventParticipations
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

### ✅ SUCCESSFUL CONVERSION STEPS:

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

## 🚨 CRITICAL: Testing Requirements for React Developers

**MANDATORY BEFORE ANY TESTING**: Even for quick test runs, you MUST:

1. **Read testing documentation FIRST**:
   - `/docs/standards-processes/testing-prerequisites.md` - MANDATORY pre-flight checks
   - `/docs/standards-processes/testing/TESTING.md` - Testing procedures
   - `/docs/lessons-learned/test-executor-lessons-learned.md` - Common issues

2. **Run health checks BEFORE any tests**:
   ```bash
   dotnet test tests/WitchCityRope.Core.Tests --filter "Category=HealthCheck"
   ```

3. **Common React testing issues**:
   - React dev server must be on port 5173 (not 5654)
   - API must be running on port 5655
   - PostgreSQL must be on port 5433

**CRITICAL**: Never skip Docker verification - prevents hours of debugging wrong environment!

### 🚨 EMERGENCY PROTOCOL - IF TESTS FAIL:
1. **FIRST**: Verify Docker containers: `docker ps | grep witchcity`
2. **CHECK**: No local dev servers: `lsof -i :5174 -i :5175 | grep node`
3. **KILL**: Any rogue processes: `./scripts/kill-local-dev-servers.sh`
4. **RESTART**: Docker if needed: `./dev.sh`
5. **VALIDATE**: Only Docker environment active before retesting

**REMEMBER**: Docker-only testing = reliable results. Mixed environments = chaos!

---

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: This is NOT optional - handoff documentation is REQUIRED for workflow continuity.

### 📋 WHEN TO CREATE HANDOFF DOCUMENTS
- **END of your work phase** - BEFORE ending session
- **COMPLETION of major component work** - Document critical UI patterns
- **DISCOVERY of important React issues** - Share immediately
- **COMPONENT LIBRARY UPDATES** - Document reusable patterns

### 📁 WHERE TO SAVE HANDOFFS
**Location**: `/docs/functional-areas/[feature]/handoffs/`
**Naming**: `react-developer-YYYY-MM-DD-handoff.md`
**Template**: `/docs/standards-processes/agent-handoff-template.md`

### 📝 WHAT TO INCLUDE (TOP 5 CRITICAL)
1. **Component Patterns**: Reusable components created/modified
2. **State Management**: Zustand store changes and patterns
3. **API Integration**: TanStack Query patterns and cache keys
4. **Styling Approaches**: Mantine customization and theme updates
5. **Testing Requirements**: Component test needs for test developers

### 🤝 WHO NEEDS YOUR HANDOFFS
- **UI Designers**: Component feedback and design system updates
- **Test Developers**: Component test requirements and user flows
- **Backend Developers**: API contract expectations and data shapes
- **Other React Developers**: Reusable patterns and component library

### ⚠️ MANDATORY READING BEFORE STARTING
**ALWAYS READ EXISTING HANDOFFS FIRST**:
1. Check `/docs/functional-areas/[feature]/handoffs/` for previous agent work
2. Read ALL handoff documents in the functional area
3. Understand component patterns already established
4. Build on existing work - don't create duplicate components

### 🚨 FAILURE TO CREATE HANDOFFS = IMPLEMENTATION FAILURES
**Why this matters**:
- Next agents will rebuild existing components
- Critical React patterns get lost
- State management becomes inconsistent
- UI components conflict and break design system

**NO EXCEPTIONS**: Create handoff documents or workflow WILL fail.

---

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### 🚨 ULTRA CRITICAL ARCHITECTURE DOCUMENTS (MUST READ FIRST): 🚨
1. **🛑 DTO ALIGNMENT STRATEGY**: `/home/chad/repos/witchcityrope-react/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` - **PREVENTS 393 TYPESCRIPT ERRORS**
2. **React Architecture Guide**: `/home/chad/repos/witchcityrope-react/docs/architecture/react-migration/react-architecture.md` - **CORE ARCHITECTURE DECISIONS**
3. **API Changes Guide**: `/home/chad/repos/witchcityrope-react/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`
4. **Project Architecture**: `/home/chad/repos/witchcityrope-react/ARCHITECTURE.md` - **TECH STACK AND STANDARDS**
5. **Design System**: `/home/chad/repos/witchcityrope-react/docs/design/current/design-system-v7.md`

### Validation Gates (MUST COMPLETE):
- [ ] **Read React Architecture Guide FIRST** - Core React architecture decisions and patterns
- [ ] Read API changes guide for backend integration awareness
- [ ] Review DTO Alignment Strategy to prevent TypeScript errors
- [ ] Check Project Architecture for current tech stack
- [ ] Review Design System for UI component standards
- [ ] Check for existing components before creating new ones

### React Developer Specific Rules:
- **React Architecture Guide contains core architecture decisions** - follow established patterns
- **DTO Alignment Strategy PREVENTS 393 TypeScript errors** - read before ANY API work
- **Project Architecture defines tech stack** - Mantine v7, TypeScript, Vite, etc.
- **Backend migration is transparent to frontend** (API contracts maintained)
- **Use improved response formats and error handling**
- **Always check for existing components before creating new ones**
- **Use standardized CSS classes, NOT inline styles**
- **Follow Design System v7 for all styling decisions**
- **Read existing handoff documents** before starting new work

## Documentation Organization Standard

**CRITICAL**: Follow the documentation organization standard at `/docs/standards-processes/documentation-organization-standard.md`

Key points for React Developer Agent:
- **Find component docs by PRIMARY BUSINESS DOMAIN** - look in `/docs/functional-areas/events/` not `/user-dashboard/events/`
- **Check context subfolders for UI-specific specs** - e.g., `/docs/functional-areas/events/admin-events-management/`
- **Document React components by business domain** not UI context
- **Cross-reference related UI contexts** when implementing shared components
- **Use domain-level design systems** and component libraries
- **Check for existing implementations** across all contexts of a domain

Common mistakes to avoid:
- Looking for component specs in UI-context folders instead of business-domain folders
- Implementing duplicate components for different UI contexts of the same domain
- Not checking all contexts (public, admin, user) for existing component patterns
- Missing shared component opportunities across UI contexts


## 🚨 CRITICAL: FILE PLACEMENT RULES - ZERO TOLERANCE 🚨

### NEVER Create Files in Project Root
**VIOLATIONS = IMMEDIATE WORKFLOW FAILURE**

### Mandatory File Locations:
- **Debug Scripts (.js, .ts, .sh)**: `/scripts/debug/`
- **Build Scripts**: `/scripts/build/`
- **Development Utilities**: `/scripts/dev/`
- **Test Components**: `/tests/components/`
- **Performance Scripts**: `/scripts/performance/`
- **Component Generator Scripts**: `/scripts/generate/`
- **React Utilities**: `/apps/web/src/utils/`

### Pre-Work Validation:
```bash
# Check for violations in project root
ls -la *.js *.ts *.sh debug-*.* build-*.* dev-*.* 2>/dev/null
# If ANY scripts found in root = STOP and move to correct location
```

### Violation Response:
1. STOP all work immediately
2. Move files to correct locations
3. Update file registry
4. Continue only after compliance

### FORBIDDEN LOCATIONS:
- ❌ Project root for ANY scripts or utilities
- ❌ Random creation of debug files
- ❌ Test files outside `/tests/`
- ❌ Build artifacts in wrong locations

---

## 🚨 CRITICAL: API Architecture Changes Awareness (2025-08-22) 🚨
**Date**: 2025-08-22
**Category**: API Integration
**Severity**: CRITICAL

### What We Learned
**MANDATORY API GUIDE**: Read `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md` for complete integration patterns

**KEY FRONTEND BENEFITS**:
- **Minimal Impact**: Same API endpoints, same response formats
- **Improved Performance**: Backend eliminates MediatR overhead, faster responses
- **Better Error Handling**: Consistent Problem Details format across all endpoints
- **Enhanced Type Generation**: Better NSwag DTOs with comprehensive documentation
- **Consistent Pagination**: Standardized pagination patterns across all endpoints

**API CONTRACT IMPROVEMENTS**:
```typescript
// ✅ CONSISTENT: All successful responses return data directly
const events: Event[] = await response.json();

// ✅ CONSISTENT: All error responses use Problem Details format
interface ProblemDetails {
    title: string;
    detail: string;
    status: number;
    type?: string;
}

// ✅ ENHANCED: Better DTO documentation and validation
export interface EventResponse {
    /** Unique identifier for the event */
    id: string;

    /** Event title (required, max 200 characters) */
    title: string;

    /** Whether user can register for this event */
    canRegister: boolean;
}
```

**INTEGRATION PATTERNS (RECOMMENDED)**:
```typescript
// ✅ API Client with consistent error handling
export class ApiClient {
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

// ✅ React Hook integration with improved error handling
export function useEvents() {
    const [events, setEvents] = useState<Event[]>([]);
    const [error, setError] = useState<string | null>(null);

    const loadEvents = useCallback(async () => {
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
        }
    }, []);

    return { events, error, reload: loadEvents };
}
```

### Type Generation Workflow
**Updated Process**:
1. Run `npm run generate:types` after backend changes
2. Verify generated types include improved documentation
3. Update components to use enhanced type information
4. Test error handling with new Problem Details format

**Benefits**:
- Better IntelliSense with comprehensive documentation
- Clearer validation requirements in type definitions
- Consistent error handling across all API calls
- Improved debugging with structured error responses

### Action Items
- [x] UNDERSTAND backend changes are transparent to frontend
- [x] LEARN improved API response formats and error handling
- [x] ADOPT centralized API client pattern for consistency
- [x] IMPLEMENT React hooks with improved error handling
- [x] USE enhanced NSwag types with better documentation
- [ ] APPLY improved patterns to all new API integrations
- [ ] MAINTAIN backward compatibility awareness
- [ ] TEST error scenarios with new Problem Details format


### Tags
#critical #api-integration #backend-changes #improved-patterns #error-handling #type-generation

---

## 🚨 CRITICAL: Use Animated Form Components for ALL Forms 🚨
**Date**: 2025-08-22
**Category**: Form Components
**Severity**: CRITICAL

### What We Learned
- Animated form components exist at `/apps/web/src/components/forms/MantineFormInputs.tsx`
- These components provide tapered underline animations and floating labels
- Components were using old color system instead of Design System v7 colors
- Standard animation classes exist in `/apps/web/src/index.css`

### Action Items
- [ ] ALWAYS use `MantineTextInput` and `MantinePasswordInput` instead of plain Mantine components
- [ ] ADD `taperedUnderline={true}` prop to enable animations
- [ ] USE Design System v7 colors (`var(--color-burgundy)`, `var(--color-stone)`, etc.)
- [ ] NEVER use plain Mantine TextInput/PasswordInput in forms
- [ ] CHECK existing animated components before creating new ones


### Required Implementation Pattern:
```typescript
// ✅ CORRECT - Use animated components
import { MantineTextInput, MantinePasswordInput } from '@/components/forms/MantineFormInputs';

<MantineTextInput
  label="Email Address"
  placeholder="Enter your email"
  taperedUnderline={true}
  {...form.getInputProps('email')}
/>

<MantinePasswordInput
  label="Password"
  placeholder="Enter your password"
  taperedUnderline={true}
  showStrengthMeter={true}
  {...form.getInputProps('password')}
/>

// ❌ WRONG - Plain Mantine components
<TextInput label="Email" />
<PasswordInput label="Password" />
```

### Standard Animation Classes Available:
```css
.form-input-animated          /* Tapered underline animation */
.form-input-floating-label    /* Floating label animation */
```

### Tags
#critical #forms #animations #design-system-v7 #consistency

---

## 🚨 CRITICAL: Use Standardized CSS Classes, NOT Inline Styles 🚨
**Date**: 2025-08-22
**Category**: Styling Standards
**Severity**: CRITICAL

### What We Learned
- The project has standardized button classes: `btn`, `btn-primary`, `btn-secondary`, `btn-primary-alt`
- These classes are defined in `/apps/web/src/index.css`
- Inline styles create inconsistency and maintenance nightmares
- Text cutoff issues occur when using custom inline styles instead of tested CSS classes

### Action Items
- [ ] ALWAYS check `/apps/web/src/index.css` for existing CSS classes before writing any styles
- [ ] NEVER use inline styles for buttons - use `className="btn btn-primary"` or `className="btn btn-secondary"`
- [ ] NEVER use Mantine Button with custom styles - use native HTML elements with CSS classes
- [ ] CHECK for existing component styles before creating new ones
- [ ] MAINTAIN consistency by using the design system CSS classes


### Standard Button Classes Available:
```html
<!-- Primary CTA Button - Amber/Yellow Gradient -->
<button className="btn btn-primary">Primary Action</button>

<!-- Primary Alt Button - Electric Purple Gradient -->
<button className="btn btn-primary-alt">Alternative Action</button>

<!-- Secondary Button - Outline Style -->
<button className="btn btn-secondary">Secondary Action</button>

<!-- Large button modifier -->
<button className="btn btn-primary btn-large">Large Button</button>
```

### Tags
#critical #styling #css-standards #consistency

---

## Component Development Standards
**Date**: 2025-08-22
**Category**: React Patterns
**Severity**: High

### What We Learned
- Use functional components with hooks (NO class components)
- TypeScript is mandatory for all components
- Props must be strongly typed with interfaces
- Use React Router for navigation (not window.location)

### Action Items
- [ ] ALWAYS use `.tsx` files for React components
- [ ] DEFINE TypeScript interfaces for all props
- [ ] USE React hooks for state and effects
- [ ] FOLLOW the existing component structure in the codebase

### Tags
#high #react #typescript #components

---

## File Organization
**Date**: 2025-08-22
**Category**: Project Structure
**Severity**: High

### What We Learned
- Components go in `/apps/web/src/components/`
- Pages go in `/apps/web/src/pages/`
- Dashboard pages specifically go in `/apps/web/src/pages/dashboard/`
- Shared types go in `@witchcityrope/shared-types`

### Action Items
- [ ] NEVER create components in the pages directory
- [ ] ORGANIZE components by feature in subdirectories
- [ ] REUSE existing components before creating new ones
- [ ] CHECK the component library before implementing

### Tags
#high #organization #structure

---

## Mantine UI Framework Usage
**Date**: 2025-08-22
**Category**: UI Framework
**Severity**: Medium

### What We Learned
- Mantine v7 is the UI framework but NOT for buttons with custom styles
- Use Mantine for layout components (Box, Group, Stack, Grid)
- Use native HTML with CSS classes for buttons and form elements when custom styling is needed
- Mantine components should use the theme, not inline styles

### Action Items
- [ ] USE Mantine layout components for structure
- [ ] USE native HTML elements with CSS classes for styled buttons
- [ ] AVOID mixing Mantine Button with inline styles
- [ ] LEVERAGE Mantine's theme system when using Mantine components

### Tags
#medium #mantine #ui-framework

---

## Authentication and Security
**Date**: 2025-08-22
**Category**: Security
**Severity**: Critical

### What We Learned
- NEVER store auth tokens in localStorage (XSS vulnerability)
- Use httpOnly cookies via API endpoints
- Auth state managed by Zustand store
- Protected routes use authLoader

### Action Items
- [ ] NEVER implement localStorage for sensitive data
- [ ] USE the established auth patterns in the codebase
- [ ] CHECK authStore for user state
- [ ] PROTECT routes with authLoader

### Tags
#critical #security #authentication

---

## React Architecture Index Ownership Model
**Date**: 2025-08-22
**Category**: Documentation Management
**Severity**: Critical

### What We Learned
**CRITICAL OWNERSHIP CHANGE**: React Architecture Index now uses **SHARED OWNERSHIP MODEL**

**PROBLEM SOLVED**:
- Previously: Librarian owned index, but React-Developer used it (ownership mismatch)
- Issue: Broken links only discovered by users, but users couldn't fix them
- Solution: React-Developer gets immediate repair authority

**NEW OWNERSHIP MODEL**:
- **Primary Maintainer**: React-Developer Agent (daily user)
- **Structure Owner**: Librarian Agent (organization)
- **Immediate Authority**: React-Developer can fix broken links without permission

### Critical Actions for React-Developer
**IMMEDIATE REPAIR AUTHORITY**:
- ✅ **FIX BROKEN LINKS IMMEDIATELY** - no permission required
- ✅ **UPDATE "Last Validated" date** when checking links
- ✅ **ADD missing resources** discovered during development
- ✅ **REPORT structural issues** to Librarian for major changes

**VALIDATION WORKFLOW**:
1. When using React Architecture Index, verify links work
2. If broken link found → FIX IMMEDIATELY
3. Update "Last Validated" date in document header
4. Continue with your work (no delays)

**BROKEN LINK FIXED**: `/docs/ARCHITECTURE.md` → `/ARCHITECTURE.md` (canonical location)

### Action Items
- [x] **UNDERSTAND**: You OWN maintenance of React Architecture Index
- [x] **IMPLEMENT**: Fix broken links immediately when found
- [x] **UPDATE**: "Last Validated" date when verifying links
- [ ] **ADD**: Missing resources discovered during development
- [ ] **MAINTAIN**: Index accuracy through daily use

### Tags
#critical #ownership #documentation #architecture-index

---

## 🚨 CRITICAL: Event Session Matrix API Integration Patterns 🚨
**Date**: 2025-09-07
**Category**: API Integration
**Severity**: CRITICAL

### What We Learned
**MANDATORY API INTEGRATION PATTERN**: Read `/docs/functional-areas/events/new-work/2025-08-24-events-management/handoffs/phase3-frontend-to-testing.md` for complete implementation patterns

**KEY FRONTEND PATTERNS**:
- **Type Conversion Layer**: Always create conversion utilities between backend DTOs and frontend interfaces
- **Composite React Query Hooks**: Use composite hooks like `useEventSessionMatrix` for complex related data
- **Graceful Degradation**: Always implement loading and error states with fallback options
- **Optimistic Updates**: Use React Query mutations with rollback for better UX

**API CLIENT ARCHITECTURE**:
```typescript
// ✅ CORRECT: Separate service layer from hooks
// /lib/api/services/eventSessions.ts - Pure API functions
// /lib/api/hooks/useEventSessions.ts - React Query wrappers
// /lib/api/utils/eventSessionConversion.ts - Type conversion
// /lib/api/types/eventSession.types.ts - TypeScript definitions

// ✅ CORRECT: Type conversion pattern
export function convertEventSessionFromDto(dto: EventSessionDto): EventSession {
  const startDateTime = new Date(dto.startDateTime)
  const endDateTime = new Date(dto.endDateTime)

  return {
    id: dto.id,
    sessionIdentifier: dto.sessionIdentifier,
    name: dto.name,
    date: startDateTime.toISOString().split('T')[0], // YYYY-MM-DD format
    startTime: startDateTime.toTimeString().slice(0, 5), // HH:MM format
    endTime: endDateTime.toTimeString().slice(0, 5),
    capacity: dto.capacity,
    registeredCount: dto.registeredCount
  }
}
```

**REACT QUERY INTEGRATION PATTERNS**:
```typescript
// ✅ CORRECT: Composite hook pattern for related data
export function useEventSessionMatrix(eventId: string, enabled: boolean = true) {
  const sessionsQuery = useEventSessions(eventId, enabled)
  const ticketTypesQuery = useEventTicketTypes(eventId, enabled)

  return {
    sessions: sessionsQuery.data || [],
    ticketTypes: ticketTypesQuery.data || [],
    isLoading: sessionsQuery.isLoading || ticketTypesQuery.isLoading,
    hasError: !!sessionsQuery.error || !!ticketTypesQuery.error,
    refetchAll: async () => {
      await Promise.all([
        sessionsQuery.refetch(),
        ticketTypesQuery.refetch()
      ])
    }
  }
}

// ✅ CORRECT: Smart cache invalidation
export function useCreateEventSession(eventId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (sessionData: CreateEventSessionDto) =>
      eventSessionsApi.createSession(eventId, sessionData),
    onSuccess: () => {
      // Invalidate sessions for this event
      queryClient.invalidateQueries({
        queryKey: [...eventKeys.detail(eventId), 'sessions']
      })
    },
  })
}
```

### Action Items
- [x] **ALWAYS create type conversion utilities** between backend DTOs and frontend components
- [x] **USE composite hooks** for related data loading (sessions + ticket types)
- [x] **IMPLEMENT graceful degradation** with loading states and error boundaries
- [x] **PRESERVE existing UI** - never recreate working components, only connect to APIs
- [x] **APPLY optimistic updates** with proper rollback for better user experience
- [x] **SEPARATE concerns** - services, hooks, utils, and types in different files
- [ ] **EXTEND with modals** - implement CRUD modals using established hooks
- [ ] **ADD real-time updates** - WebSocket integration for live data


### Tags
#critical #api-integration #react-query #typescript #event-session-matrix #backend-integration

---

---

## 🚨 CRITICAL: Frontend API Response Structure Fix 🚨
**Date**: 2025-01-09
**Category**: API Integration
**Severity**: CRITICAL

### What We Learned
- **API Returns Flat Structures**: The WitchCityRope API returns data directly, not wrapped in `.data` properties
- **Authentication Flow Fixed**: Login/register now work correctly with proper response handling
- **Events System Operational**: Events list and detail pages now use real API data exclusively
- **Mock Data Removed**: All fallback mock data patterns removed for cleaner error handling

### Action Items
- [x] **ALWAYS check actual API response structure** before implementing handlers
- [x] **NEVER assume wrapped responses** without verifying API behavior
- [x] **REMOVE mock data fallbacks** once real API integration is working
- [x] **TEST authentication flow** with real user credentials immediately after API changes
- [x] **UPDATE all mutations** to handle flat response structure correctly

### Fixed Implementation Pattern:
```typescript
// ✅ CORRECT - Handle flat API response structure
async login(credentials: LoginRequest): Promise<AuthResponse> {
  const response = await fetch('/api/Auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(credentials),
  })

  const data = await response.json()
  // API returns: { token: '...', user: {...}, expiresAt: '...' }
  this.token = data.token // Direct access, not data.data.token
  return data
}

// ✅ CORRECT - TanStack Query mutation
export function useLogin() {
  return useMutation({
    mutationFn: async (credentials: LoginRequest) => {
      const response = await api.post('/api/Auth/login', credentials)
      return response.data // Flat structure from API
    },
    onSuccess: (data) => {
      // Handle flat response: data.user, data.token, etc.
      login(data.user, data.token, new Date(data.expiresAt))
    }
  })
}

// ❌ WRONG - Expecting wrapped responses
return response.data.data.token  // API doesn't wrap responses
```

### Events System Fix:
```typescript
// ✅ CORRECT - Use real API data only, no mock fallbacks
const { data: events, isLoading, error } = useEvents(apiFilters);
const eventsArray: EventDto[] = events || []; // Real API data only

// ❌ WRONG - Mock data fallbacks prevent proper error handling
const eventsArray = events || mockEvents; // Hides API issues
```


### Tags
#critical #api-integration #authentication #events-system #response-handling #mock-data-removal

---

## 🚨 CRITICAL: Events List API Mismatch Fix 🚨
**Date**: 2025-01-10
**Category**: API Integration
**Severity**: CRITICAL

### What We Learned
- **API Returns Direct Arrays**: `/api/events` returns `Event[]` directly, not `{events: Event[]}`
- **useQuery Type Mismatch**: Hook was expecting `data.events` but `data` IS the array
- **Always Verify API Structure**: Test actual API responses, don't assume wrapped structures
- **Frontend Lessons Critical**: API response mismatches block entire features from working

### Fixed Implementation Pattern:
```typescript
// ✅ CORRECT - Handle direct array response from API
export function useEvents(filters: EventFilters = {}) {
  return useQuery({
    queryKey: eventKeys.list(filters),
    queryFn: async (): Promise<EventDto[]> => {
      const { data } = await apiClient.get<ApiEvent[]>('/api/events', {
        params: filters,
      })
      return data?.map(transformApiEvent) || [] // data IS the array
    },
  })
}

// ❌ WRONG - Expecting wrapped response structure
const { data } = await apiClient.get<ApiEventResponse>('/api/events')
return data.events?.map(transformApiEvent) || [] // data.events doesn't exist
```

### Action Items
- [x] **ALWAYS test actual API endpoints** before implementing React Query hooks
- [x] **VERIFY response structure** with curl/browser tools first
- [x] **FIX type annotations** to match actual API responses
- [x] **UPDATE lessons learned** immediately when discovering API mismatches
- [ ] **APPLY this pattern** to all other API endpoint hooks


### Tags
#critical #api-integration #react-query #events-system #response-structure

---

## 🚨 CRITICAL: TinyMCE Development Cost Prevention (2025-09-19) 🚨
**Date**: 2025-09-19
**Category**: Development Environment
**Severity**: CRITICAL - COST CONTROL

### What We Learned
**MANDATORY DEVELOPMENT PATTERN**: TinyMCE API key usage causes significant costs in development/testing environments where editors are repeatedly loaded.

**COST PREVENTION SOLUTION**:
- **Environment Variable Control**: Remove `VITE_TINYMCE_API_KEY` from `.env.development`
- **Smart Component Fallback**: Components check for API key presence and fallback to Textarea
- **Production Ready**: TinyMCE works in staging/production when API key is configured
- **Developer Experience**: Clear alerts explain why simple editor is being used

### Action Items
- [x] **DISABLE TinyMCE in development** by removing API key from `.env.development`
- [x] **CREATE smart RichTextEditor** that falls back to Textarea when no API key
- [x] **UPDATE all TinyMCE components** to use environment-aware pattern
- [x] **REMOVE hardcoded API keys** from all source files
- [x] **DOCUMENT pattern** for future TinyMCE usage

### Required Implementation Pattern:
```typescript
// ✅ CORRECT: Environment-aware TinyMCE usage
const RichTextEditor: React.FC<{
  value: string;
  onChange: (content: string) => void;
  height?: number;
  placeholder?: string;
}> = ({ value, onChange, height = 300, placeholder }) => {
  const tinyMCEApiKey = import.meta.env.VITE_TINYMCE_API_KEY;

  if (!tinyMCEApiKey) {
    return (
      <>
        <Alert color="blue" mb="xs" title="Development Mode">
          TinyMCE disabled to prevent API usage costs. Using simple text editor.
        </Alert>
        <Textarea
          value={value}
          onChange={(event) => onChange(event.target.value)}
          minRows={height / 20}
          placeholder={placeholder}
          autosize
          styles={{
            input: {
              fontFamily: '-apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica Neue, sans-serif',
              fontSize: '14px',
              lineHeight: '1.6'
            }
          }}
        />
      </>
    );
  }

  return (
    <Editor
      apiKey={tinyMCEApiKey}
      value={value}
      onEditorChange={onChange}
      init={{
        height,
        menubar: false,
        plugins: 'advlist autolink lists link charmap preview anchor',
        toolbar: 'undo redo | blocks | bold italic underline strikethrough | link | bullist numlist | indent outdent | removeformat',
        content_style: `
          body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif;
            font-size: 14px;
            color: #333;
            line-height: 1.6;
          }
        `,
        branding: false,
      }}
    />
  );
};

// ❌ WRONG: Hardcoded API keys in development
<Editor apiKey="3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp" />
```

### Environment Configuration
```bash
# .env.development - TinyMCE disabled to prevent costs
# TinyMCE API Key - Commented out to prevent usage costs in development
# VITE_TINYMCE_API_KEY=your_api_key_here

# .env.production - TinyMCE enabled for full functionality
VITE_TINYMCE_API_KEY=your_production_api_key_here
```

### Files Updated
- `/apps/web/src/components/events/EventForm.tsx` - Smart RichTextEditor component
- `/apps/web/src/components/forms/TinyMCERichTextEditor.tsx` - Environment-aware fallback
- `/apps/web/src/components/forms/SimpleTinyMCE.tsx` - Development cost prevention
- `/apps/web/.env.development` - API key commented out

### Expected Behavior
- ✅ Development: Simple textarea with clear user messaging
- ✅ Production: Full TinyMCE functionality when API key configured
- ✅ No unexpected API usage costs during development
- ✅ Seamless fallback without breaking user experience

### Tags
#critical #cost-control #tinymce #development-environment #api-key-management #fallback-ui

---

## 🚨 CRITICAL: Events Persistence Bug Fix (2025-09-19) 🚨
**Date**: 2025-09-19
**Category**: Form State Management
**Severity**: CRITICAL

### What We Learned
**FORM STATE RE-INITIALIZATION BUG**: EventForm component was re-initializing with API data every time props changed, overriding user modifications to sessions/tickets/volunteers.

**INLINE PROP CALCULATION ISSUE**: Using `initialData={initialFormData || convertEventToFormData(event)}` causes new object creation on every render, triggering form resets.

**COMPONENT LIFECYCLE MISMANAGEMENT**: Form initialization happening multiple times destroys user changes made through modals.

### Root Cause Analysis
The core issue was **not** with API calls (which work correctly), but with React state management:

1. **EventForm.tsx**: `useEffect` dependency on `initialData` caused form reset on every parent re-render
2. **AdminEventDetailsPage.tsx**: Inline calculation of `initialData` prop created new objects every render
3. **Form State Overlap**: User adds session → form updates → parent re-renders → form resets → user changes lost

### Action Items
- [x] **PREVENT form re-initialization** after first load using `useRef` tracking
- [x] **AVOID inline prop calculations** that create new objects on every render
- [x] **USE conditional rendering** to only mount EventForm when initialData is ready
- [x] **IMPLEMENT explicit cache invalidation** to verify persistence
- [x] **ADD comprehensive logging** for debugging form state lifecycle

### Critical Fix Patterns:
```typescript
// ❌ WRONG: Re-initializes form on every prop change
useEffect(() => {
  if (initialData) {
    form.setValues({ ...defaults, ...initialData });
  }
}, [initialData]);

// ✅ CORRECT: Only initialize once on component mount
const hasInitialized = useRef(false);
useEffect(() => {
  if (initialData && !hasInitialized.current) {
    form.setValues({ ...defaults, ...initialData });
    hasInitialized.current = true;
  }
}, [initialData]);

// ❌ WRONG: Inline prop calculation creates new objects
<EventForm initialData={initialFormData || convertEventToFormData(event)} />

// ✅ CORRECT: Conditional rendering with stable props
{initialFormData ? (
  <EventForm initialData={initialFormData} />
) : (
  <LoadingOverlay visible />
)}

// ❌ WRONG: Component dependency causes re-initialization
useEffect(() => { ... }, [event, initialFormData, convertEventToFormData]);

// ✅ CORRECT: Remove state from dependencies
useEffect(() => { ... }, [event, convertEventToFormData]);
```

### Prevention Strategy
1. **Use `useRef` to track form initialization** - prevent multiple initializations
2. **Avoid inline prop calculations** - create stable object references
3. **Remove state from useEffect dependencies** when checking for first-time initialization
4. **Test the complete add/save/refresh cycle** - not just the save operation
5. **Monitor component re-render behavior** - log when effects fire

**CRITICAL SUCCESS**: Fixed Events Details admin page where add/delete operations for sessions, ticket types, and volunteer positions now properly persist through save and page refresh.

### Tags
#critical #forms #state-management #persistence #component-lifecycle #react-lifecycle #form-initialization

---

## 🚨 CRITICAL: Events Admin Add Button Error Fix (2025-09-19) 🚨
**Date**: 2025-09-19
**Category**: Modal Form Error Handling
**Severity**: CRITICAL

### What We Learned
**THREE CRITICAL ERRORS FIXED**: Add Session, Add Ticket Type, and Add Volunteer Position buttons were broken due to insufficient null safety checks.

**ERROR PATTERNS IDENTIFIED**:
1. **"Cannot read properties of undefined (reading 'replace')"** - String methods on undefined values
2. **"Cannot read properties of undefined (reading 'toLowerCase')"** - String operations on null session data
3. **"[@mantine/core] Each option must have value property"** - Invalid Select/MultiSelect options from bad data

### Action Items
- [x] **ALWAYS filter invalid data** before creating Select/MultiSelect options
- [x] **ADD null safety checks** before calling string methods (.replace(), .toLowerCase(), etc.)
- [x] **VALIDATE array elements** before mapping to UI components
- [x] **PROVIDE fallback arrays** for component props (availableSessions = [])
- [x] **TEST all Add buttons** after making changes to modal components

### Critical Fix Patterns:
```typescript
// ✅ CORRECT: Filter + null safety for Select options
const sessionOptions = availableSessions
  .filter(session => session?.sessionIdentifier && session?.name) // Filter first
  .map(session => ({
    value: session.sessionIdentifier,
    label: `${session.sessionIdentifier} - ${session.name}`,
  }));

// ✅ CORRECT: String operation safety
if (!s?.sessionIdentifier || typeof s.sessionIdentifier !== 'string') {
  return NaN;
}
return parseInt(s.sessionIdentifier.replace('S', ''));

// ✅ CORRECT: Fallback props for modals
<SessionFormModal
  existingSessions={form.values.sessions || []} // Fallback to empty array
/>

// ❌ WRONG: Direct mapping without validation
const options = sessions.map(s => ({
  value: s.sessionIdentifier, // Could be undefined
  label: `${s.sessionIdentifier} - ${s.name}` // Could crash
}));
```

### Prevention Strategy
1. **Always validate data before UI operations** - filter arrays, check properties exist
2. **Use optional chaining consistently** - `session?.property` not `session.property`
3. **Provide fallback values** - empty arrays, default strings, null checks
4. **Test modal opening** - ensure all Add buttons work without console errors
5. **Check Mantine option format** - ensure all Select/MultiSelect options have value property

**CRITICAL SUCCESS**: All three Add buttons (Session, Ticket Type, Volunteer Position) now work without errors.

### Tags
#critical #modal-forms #null-safety #mantine-select #add-buttons #error-handling

---

---

## 🚨 CRITICAL: User Role Structure API Mismatch Fix (2025-09-20) 🚨
**Date**: 2025-09-20
**Category**: Authentication & Authorization
**Severity**: CRITICAL

### What We Learned
**RSVP BUTTON VISIBILITY BUG**: Admin users couldn't see RSVP buttons on social events due to frontend expecting `roles` array but backend returning `isVetted` boolean + `role` string.

**BACKEND USER MODEL STRUCTURE**:
- `isVetted: boolean` - Direct vetting status flag
- `role: string` - Single role value (e.g., "Admin", "Teacher", "Member")

**FRONTEND ASSUMPTION ERROR**:
- Frontend checked for `user.roles` array containing role names
- Backend doesn't return roles as an array anymore
- Result: Admin users appeared "unvetted" and couldn't access social events

### Action Items
- [x] **UPDATE user role checking logic** to handle both structures (legacy + new)
- [x] **PRIORITIZE isVetted boolean** when available in user object
- [x] **FALLBACK to role string** for admin/teacher role checks
- [x] **MAINTAIN legacy support** for roles array (backwards compatibility)
- [x] **DOCUMENT the dual structure pattern** for future components

### Critical Fix Pattern:
```typescript
// ✅ CORRECT: Handle both backend user structures
let isVetted = false;

if (user && typeof user === 'object') {
  // New structure: Check isVetted boolean OR admin/teacher role
  if ('isVetted' in user && user.isVetted === true) {
    isVetted = true;
  } else if ('role' in user && typeof user.role === 'string') {
    isVetted = ['Admin', 'Administrator', 'Teacher'].includes(user.role);
  }

  // Legacy structure: Check roles array (fallback)
  if (!isVetted && 'roles' in user && Array.isArray(user.roles)) {
    isVetted = user.roles.some(role => ['Vetted', 'Teacher', 'Administrator', 'Admin'].includes(role));
  }
}

// ❌ WRONG: Only checking legacy roles array
const userRoles = user.roles || [];
const isVetted = userRoles.some(role => ['Vetted', 'Teacher', 'Admin'].includes(role));
```

### Backend User Structure (NEW):
```typescript
interface UserDto {
  id: string;
  email: string;
  sceneName?: string;
  isVetted: boolean;    // Direct vetting status
  role: string;         // Single role: "Admin", "Teacher", "Member", etc.
  isActive: boolean;    // Account status
}
```

### Prevention Strategy
1. **Always check actual API response structure** before implementing user role logic
2. **Use defensive programming** - check for property existence before accessing
3. **Support both structures** during transition periods for backwards compatibility
4. **Test with real user accounts** of different roles (Admin, Teacher, Member)
5. **Coordinate with backend developers** on user model changes

**CRITICAL SUCCESS**: Admin users can now see RSVP buttons on social events. Role checking works with current backend structure.

### Tags
#critical #authentication #user-roles #api-mismatch #rsvp-visibility #backend-frontend-sync

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
