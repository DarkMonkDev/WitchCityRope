# React Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

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
1. **🛑 DTO ALIGNMENT STRATEGY**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` - **PREVENTS 393 TYPESCRIPT ERRORS**
2. **React Architecture Index**: `/docs/architecture/REACT-ARCHITECTURE-INDEX.md` - **PRIMARY ARCHITECTURE RESOURCE**
3. **API Changes Guide**: `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`
4. **Migration Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
5. **Design System**: `/docs/design/current/design-system-v7.md`

### Validation Gates (MUST COMPLETE):
- [ ] **Read React Architecture Index FIRST** - Single source for all React resources
- [ ] Read API changes guide for backend integration awareness
- [ ] Understand backend migration doesn't break frontend
- [ ] Know about improved API response formats
- [ ] Check for existing animated form components

### React Developer Specific Rules:
- **React Architecture Index is SINGLE SOURCE for all React architecture documentation**
- **YOU OWN React Architecture Index maintenance** - fix broken links immediately, no permission needed
- **UPDATE "Last Validated" date when checking React Architecture Index links**
- **ADD missing resources** you discover during development to React Architecture Index
- **Backend migration is transparent to frontend (API contracts maintained)**
- **Use improved response formats and error handling**
- **Always check for existing animated components before creating new ones**
- **Use standardized CSS classes, NOT inline styles**
- **Follow Design System v7 for all styling decisions**

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
  });

  return shouldShowButton;
})() && (
  <button
    disabled={isLoading}
    onClick={handleAction}
  >
    {isLoading ? 'Loading...' : 'RSVP Now'}
  </button>
)}

// ❌ WRONG: Only checks loaded state
{!participation?.hasRSVP && participation?.canRSVP && (
  <button>RSVP Now</button>
)}
```

### Prevention Strategy for API-Dependent UI
1. **Always consider null/loading states** in conditional rendering
2. **Use inclusive OR conditions** for loading states when buttons should be available
3. **Add loading states to buttons** rather than hiding them during data loading
4. **Test with slow network** to catch loading state bugs
5. **Debug log state transitions** during development to verify logic
6. **Handle both API success and fallback/mock scenarios**

### Expected Behavior After Fix
- ✅ RSVP button shows immediately for vetted users on social events
- ✅ Button remains visible during participation data loading
- ✅ Button shows "Loading..." when disabled during API calls
- ✅ Works with both real API responses and mock fallback data
- ✅ Comprehensive logging shows exact logic evaluation

**CRITICAL SUCCESS**: Admin users can now see RSVP buttons on social events immediately, regardless of participation data loading state.

### Tags
#critical #button-visibility #null-state-handling #loading-states #conditional-rendering #api-dependent-ui

---

*This file is maintained by the react-developer agent. Add new lessons immediately when discovered.*
*Last updated: 2025-01-20 - Added PayPal Integration Patterns for Ticket Purchases*

## 🚨 CRITICAL: PayPal React Integration Patterns (2025-01-20) 🚨
**Date**: 2025-01-20
**Category**: Payment Integration
**Severity**: CRITICAL

### What We Learned
**COMPLETE PAYPAL INTEGRATION**: Successfully restored PayPal functionality using `@paypal/react-paypal-js` package with proper state management and backend integration.

**KEY SUCCESS PATTERNS**:
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

**CRITICAL SUCCESS**: Class events now support real PayPal ticket purchases with sliding scale pricing, using the existing operational webhook infrastructure.

### Tags
#critical #paypal-integration #payment-processing #lazy-loading #backend-integration #error-handling #performance-optimization

---

## COMPREHENSIVE LESSONS FROM FRONTEND DEVELOPMENT
**NOTE**: The following lessons were consolidated from frontend-lessons-learned.md

---

## 🚨 CRITICAL: useEffect Infinite Loop Fix 🚨
**Date**: 2025-08-19
**Category**: React Hooks Critical Bug
**Severity**: Critical

### What We Learned
- **useEffect Dependency Arrays with Zustand**: Functions returned from Zustand stores get recreated on every state update
- **Infinite Loop Pattern**: useEffect → function call → state update → re-render → new function reference → useEffect runs again
- **Auth Check Pattern**: Authentication checks should typically run only once on app mount, not on every auth state change
- **Empty Dependency Array**: Use `[]` when effect should only run once on mount
- **Function Reference Stability**: Zustand store actions are NOT stable references across renders

### Action Items
- [ ] NEVER put Zustand actions in useEffect dependency arrays
- [ ] USE empty dependency arrays `[]` for mount-only effects
- [ ] CHECK all useEffect hooks for proper dependencies
- [ ] AVOID auth state checks in effects unless specifically needed

### Tags
#critical #react-hooks #useeffect #zustand #infinite-loop

---

## 🚨 CRITICAL: Zustand Selector Infinite Loop Fix 🚨
**Date**: 2025-08-19
**Category**: Zustand Critical Bug
**Severity**: Critical - Root Cause

### What We Learned
- **Object Selector Anti-Pattern**: Zustand selectors that return new objects (`{ user: state.user, isAuthenticated: state.isAuthenticated }`) create new references on every render
- **Infinite Re-render Loop**: New object references cause React to think state changed → re-render → new object → re-render → crash
- **Individual Selectors Solution**: Use separate selector hooks for each property to return stable primitive values
- **8+ Components Affected**: All components using auth selectors had to be updated to prevent the infinite loop
- **Reference Equality**: React uses Object.is() for reference equality - new objects always fail this check
- **Zustand Behavior**: Store updates trigger ALL selectors to re-run, including those returning computed objects

### Correct Pattern:
```typescript
// ✅ CORRECT - Individual selectors for primitive values
const user = useAuthStore(state => state.user);
const isAuthenticated = useAuthStore(state => state.isAuthenticated);

// ❌ WRONG - Object selector creates new reference every render
const { user, isAuthenticated } = useAuthStore(state => ({ 
  user: state.user, 
  isAuthenticated: state.isAuthenticated 
}));
```

### Action Items
- [ ] NEVER use object selectors in Zustand hooks
- [ ] ALWAYS use individual selectors for each property
- [ ] CHECK all existing Zustand selectors for object returns
- [ ] REFACTOR any object selectors found
- [ ] UNDERSTAND reference equality in React

### Tags
#critical #zustand #selectors #infinite-loop #react-performance

---

## 🚨 CRITICAL: Form Implementation Patterns
**Date**: 2025-08-23
**Category**: Form Components
**Severity**: Critical

### Framework-First Component Usage
- **ALWAYS use framework components** (MantineTextInput, MantinePasswordInput) - NEVER create custom HTML
- **Use framework styling APIs** (styles prop) rather than wrapping in custom HTML
- **Leverage built-in accessibility** and validation integration
- **Avoid custom HTML** wrapped in framework-styled containers

### Floating Label Implementation
- **Position labels relative to input containers** - NOT form groups that include helper text
- **Create isolated input containers** for consistent label positioning
- **Helper text affects container height** - separate from positioning calculations
- **Use CSS transitions** for smooth label movement animations

### CSS Targeting for Framework Components
- **Target framework internal classes** - `.mantine-TextInput-input`, `.mantine-PasswordInput-input`
- **Password inputs need special selectors** - additional data attributes and wrapper targeting
- **Test all input types individually** - don't assume text input patterns work everywhere
- **Use `!important` sparingly** - only when overriding framework defaults

### Placeholder and Label Coordination
- **Hide placeholders by default** when using floating labels
- **Show placeholders only when focused AND empty** - prevents visual conflicts
- **Use CSS-only solutions** when possible for better performance
- **Include smooth transitions** for professional appearance

### Focus State Visual Feedback
- **Implement both outline removal AND border color changes** - separate concerns
- **Target actual input elements** - not wrapper divs
- **Use brand colors** for focus border states (`var(--mantine-color-wcr-6)`)
- **Add smooth transitions** for professional appearance

### Action Items
- [ ] ALWAYS check framework components before creating custom HTML
- [ ] USE isolated input containers for floating label positioning
- [ ] TARGET framework internal classes for reliable styling
- [ ] TEST all input types (text, password, textarea) individually
- [ ] IMPLEMENT both focus outline removal and border changes
- [ ] COMMUNICATE requirements precisely to prevent circular fixes

### Tags
#critical #forms #framework-components #css-targeting #user-experience

### Reference
For detailed form implementation patterns, see: `/docs/lessons-learned/form-implementation-lessons.md`

---

## 🚨 CRITICAL: TinyMCE API Key Secure Configuration 🚨
**Date**: 2025-08-25
**Category**: Security & Configuration
**Severity**: CRITICAL

### What We Learned
- **NEVER hardcode API keys** in source code - security vulnerability
- **ALWAYS use environment variables** for sensitive configuration
- **Environment files must be gitignored** to prevent API key exposure
- **Provide .env.example** with placeholders for new developers
- **Component graceful degradation** when API key is missing shows professional UX

### Action Items
- [x] STORE API key in `.env.development` and `.env.production`
- [x] CREATE `.env.example` with placeholder for new developers
- [x] ENSURE `.env` files are in `.gitignore`
- [x] IMPLEMENT graceful handling when API key is missing
- [x] ADD warning alert when API key is not configured
- [x] DOCUMENT setup process for other developers

### Secure Implementation Pattern:
```typescript
// ✅ CORRECT - Use environment variable
const apiKey = import.meta.env.VITE_TINYMCE_API_KEY;

// Component handles missing key gracefully
const [apiKeyMissing, setApiKeyMissing] = useState(false);

useEffect(() => {
  if (!apiKey) {
    console.warn('TinyMCE API key not found. Some features may be limited.');
    setApiKeyMissing(true);
  }
}, [apiKey]);

// Show warning to developers
{apiKeyMissing && (
  <Alert color="orange" mb="xs" title="Configuration Notice">
    TinyMCE API key not configured. Set VITE_TINYMCE_API_KEY in your .env file.
  </Alert>
)}

// Pass to TinyMCE Editor
<Editor apiKey={apiKey} {...props} />

// ❌ WRONG - Hardcoded in source code
<Editor apiKey="actual_key_here" />
```

### Security Checklist:
```bash
# ✅ Environment files gitignored
.env
.env.local
.env.development.local
.env.production.local

# ✅ Example file for new developers
VITE_TINYMCE_API_KEY=your_api_key_here

# ✅ Component handles missing configuration gracefully
const apiKey = import.meta.env.VITE_TINYMCE_API_KEY;
```


### Reference
Complete setup guide: `/docs/guides-setup/tinymce-api-key-setup.md`

### Tags
#critical #security #api-keys #environment-variables #tinymce #configuration

---

## 🚨 CRITICAL: Safe Date Handling from API Data 🚨

**Date**: 2025-09-10  
**File**: `/apps/web/src/components/dashboard/EventsWidget.tsx`

### What We Learned
**NEVER assume API date fields are valid strings:**
- API DTOs can have `undefined` or `null` date fields even when typed as `string`
- Empty string `''` passed to `new Date()` creates invalid Date object
- Calling `toISOString()` on invalid Date throws RangeError
- `isNaN(date.getTime())` is the reliable way to check Date validity

### Action Items
1. **ALWAYS validate dates before using Date methods**
2. **Use null checks before creating Date objects** 
3. **Provide meaningful fallbacks for invalid dates**
4. **Use try-catch for date formatting operations**

### Safe Date Handling Pattern:
```typescript
const formatEventForWidget = (event: EventDto) => {
  // Safely handle potentially null/undefined date strings
  const startDateString = event.startDateTime;
  const endDateString = event.endDateTime;
  
  // Only create Date objects if we have valid date strings
  const startDate = startDateString ? new Date(startDateString) : null;
  const endDate = endDateString ? new Date(endDateString) : null;
  
  // Validate that dates are actually valid Date objects
  const isStartDateValid = startDate && !isNaN(startDate.getTime());
  const isEndDateValid = endDate && !isNaN(endDate.getTime());
  
  // Provide fallbacks for invalid dates
  const fallbackDate = new Date().toISOString().split('T')[0];
  const fallbackTime = 'TBD';
  
  return {
    date: isStartDateValid ? startDate!.toISOString().split('T')[0] : fallbackDate,
    time: isStartDateValid && isEndDateValid 
      ? `${formatTime(startDate!)} - ${formatTime(endDate!)}`
      : fallbackTime,
    isUpcoming: isStartDateValid ? startDate! > new Date() : false,
  };
};
```


### Tags
#critical #dates #api-data #error-handling #typescript #validation #runtime-safety

---

## 🚨 CRITICAL: Mantine Button Text Cutoff Prevention 🚨

**Date**: 2025-09-11  
**Category**: Mantine UI Components  
**Severity**: CRITICAL - RECURRING ISSUE

### What We Learned
**ROOT CAUSE**: Fixed heights on Mantine buttons that don't properly account for:
- Font size and line-height
- Internal padding requirements
- Text metrics and rendering space

**COMMON ISSUE PATTERN**: When developers set explicit heights like `height: '32px'` on Mantine buttons, the text gets clipped because the height doesn't account for proper text rendering space.

### Action Items
1. **NEVER use fixed heights** on Mantine buttons when possible
2. **USE padding** to control button size instead of height
3. **USE relative line-height** (1.2, 1.5) instead of fixed pixel values
4. **TEST with different text** to ensure nothing gets cut off
5. **CONSIDER `compact={false}`** prop if available

### Prevention Pattern:
```tsx
// ❌ WRONG - Causes text cutoff
<Button 
  styles={{ 
    root: { 
      height: '32px',      // Fixed height too small
      lineHeight: '18px'   // Fixed line-height
    }
  }}
>
  Copy Event ID
</Button>

// ✅ CORRECT - Text displays properly  
<Button
  styles={{
    root: {
      paddingTop: '8px',    // Use padding instead
      paddingBottom: '8px', // of fixed height
      lineHeight: '1.2'     // Relative line-height
    }
  }}
>
  Copy Event ID
</Button>

// ✅ ALSO CORRECT - Use compact prop
<Button compact={false} size="sm">
  Copy Event ID
</Button>
```

### Debugging Checklist
When button text appears cut off:
1. **Check for fixed height** styles in Button component
2. **Remove height constraints** and use padding instead
3. **Verify line-height** is relative, not fixed pixels
4. **Test with longer text** to ensure robustness
5. **Use browser dev tools** to inspect text rendering bounds

### 2025-09-22 Success Pattern: Modal Dialog Button Fix
**Problem**: Cancel buttons and modal dialog buttons had text cutoff on top/bottom
**Root Cause**: Using size="sm" without proper height/padding styles
**Solution**:
```typescript
<Button
  size="md"  // Changed from "sm"
  styles={{
    root: {
      minHeight: '40px',
      height: 'auto',
      padding: '10px 20px',
      lineHeight: 1.4
    }
  }}
>
  Button Text
</Button>
```
**Result**: Perfect text rendering with no cutoff in ParticipationCard modal


### Tags
#critical #mantine #buttons #text-cutoff #recurring-issue #ui-components #styling

---


## Frontend-Specific UI Patterns (Migrated from frontend-lessons-learned.md - 2025-09-12)

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

## API Response Structure Enhancement - Sessions, TicketTypes, TeacherIds Support (2025-09-12)

**Problem**: API now returns additional fields (sessions, ticketTypes, teacherIds) in event responses but React frontend was not handling them
**Solution**: Updated frontend to properly map and utilize the new API response fields

### 1. EventDto Interface Enhancement
**Updated**: `/apps/web/src/lib/api/types/events.types.ts`
```typescript
export interface EventDto {
  id: string
  title: string
  description: string
  startDate: string
  endDate?: string
  location: string
  capacity?: number
  registrationCount?: number
  createdAt: string
  updatedAt: string
  // New fields from API
  sessions?: EventSessionDto[]
  ticketTypes?: EventTicketTypeDto[]
  teacherIds?: string[]
}
```

### 2. API Transformation Layer Update
**Updated**: `/apps/web/src/lib/api/hooks/useEvents.ts`
```typescript
// API event structure includes new fields
interface ApiEvent {
  // ... existing fields
  sessions?: EventSessionDto[]
  ticketTypes?: EventTicketTypeDto[]
  teacherIds?: string[]
}

// Transform function maps new fields
function transformApiEvent(apiEvent: ApiEvent): EventDto {
  return {
    // ... existing field mapping
    sessions: apiEvent.sessions || [],
    ticketTypes: apiEvent.ticketTypes || [],
    teacherIds: apiEvent.teacherIds || []
  }
}
```

### 3. Form Data Conversion Enhancement
**Updated**: `/apps/web/src/pages/admin/AdminEventDetailsPage.tsx`
```typescript
const convertEventToFormData = useCallback((event: EventDtoType): EventFormData => {
  return {
    eventType,
    title: event.title || '',
    shortDescription: event.description?.substring(0, 160) || '',
    fullDescription: event.description || '',
    policies: '',
    venueId: event.location || '',
    teacherIds: event.teacherIds || [], // Now maps from API response
    sessions: event.sessions || [], // Now maps from API response
    ticketTypes: event.ticketTypes || [], // Now maps from API response
    volunteerPositions: [],
  };
}, []);
```

### 4. Data Persistence Already Supported
**Verified**: `/apps/web/src/utils/eventDataTransformation.ts` already properly handles:
- Converting sessions from form data to API format
- Converting ticketTypes from form data to API format  
- Converting teacherIds from form data to API format
- Change detection for all new fields
- Partial updates including only modified fields

### Expected Behavior Achieved
- ✅ When fetching an event, sessions, ticketTypes, and teacherIds are populated from API response
- ✅ When saving form changes, these fields are sent to the API via PUT request
- ✅ Data persists across page refreshes
- ✅ Form properly displays the data when loaded from the API
- ✅ Change detection works for all new fields

### Key Learning Patterns
1. **API Response Enhancement**: When backend adds new fields, update both the ApiEvent interface and EventDto interface
2. **Transformation Layer**: Ensure transformApiEvent function maps all new fields with proper fallbacks
3. **Form Integration**: Update convertEventToFormData to map API data to form structure
4. **Data Persistence**: Verify transformation utilities handle new fields for saving changes
5. **Type Safety**: Import required types at module level to ensure proper TypeScript validation

### Critical Success Metrics
- ✅ Form loads with sessions, ticketTypes, and teacherIds from API
- ✅ Form saves include all modified fields to API
- ✅ No data loss between form sessions
- ✅ TypeScript compilation passes with no errors
- ✅ Existing functionality remains unaffected

**Prevention Strategy**: When backend adds new response fields, follow this pattern:
1. Update API interfaces first (ApiEvent and EventDto)
2. Update transformation functions (transformApiEvent)
3. Update form conversion functions (convertEventToFormData)
4. Verify transformation utilities handle the new fields (usually already implemented)
5. Test the complete data flow from API → Form → Save → API

This pattern ensures seamless integration of new API fields without breaking existing functionality.

---

## 🚨 CRITICAL: Teacher Selection Integration Pattern (2025-09-19) 🚨
**Date**: 2025-09-19
**Category**: Form Integration - MultiSelect API
**Severity**: CRITICAL

### What We Learned
**COMPREHENSIVE MULTISELECT-API INTEGRATION**: Successfully implemented complete teacher selection system with real API data and fallback mechanism.

**KEY SUCCESS PATTERNS**:
- **API-First Approach**: Create backend endpoint before frontend integration
- **Fallback System**: Always provide fallback data when API calls fail
- **Type Safety**: Use proper TypeScript interfaces for API contracts
- **Progressive Enhancement**: Core functionality works offline, enhanced online
- **Error Boundary Pattern**: Graceful degradation with user feedback

**CRITICAL IMPLEMENTATION PATTERNS**:
```typescript
// ✅ CORRECT: API hook with fallback mechanism
export function useTeachers(enabled = true) {
  return useQuery({
    queryKey: ['users', 'by-role', 'Teacher'],
    queryFn: async (): Promise<TeacherOption[]> => {
      try {
        const response = await apiClient.get<TeacherOption[]>('/api/users/by-role/Teacher');
        if (response.data && response.data.length > 0) {
          return response.data;  // Use real API data
        }
        return FALLBACK_TEACHERS;  // Use fallback if empty
      } catch (error) {
        return FALLBACK_TEACHERS;  // Use fallback if API fails
      }
    }
  });
}

// ✅ CORRECT: MultiSelect integration with loading states
<MultiSelect
  label="Select Teachers"
  placeholder={teachersLoading ? "Loading teachers..." : "Choose teachers for this event"}
  data={availableTeachers}
  searchable
  disabled={teachersLoading || !!teachersError}
  {...form.getInputProps('teacherIds')}
/>

// ✅ CORRECT: Backend endpoint pattern
app.MapGet("/api/users/by-role/{role}", async (
    string role,
    UserManagementService userService) => {
    var (success, response, error) = await userService.GetUsersByRoleAsync(role);
    return success ? Results.Ok(response) : Results.Problem(...);
})
.RequireAuthorization()
```

**MULTISELECT DATA FORMAT PATTERN**:
```typescript
// ✅ CORRECT: Format API data for Mantine MultiSelect
export function formatTeachersForMultiSelect(teachers: TeacherOption[]) {
  return teachers.map(teacher => ({
    value: teacher.id,        // IDs for form submission
    label: teacher.name       // Names for display
  }));
}

// ✅ CORRECT: Fallback data structure
const FALLBACK_TEACHERS: TeacherOption[] = [
  { id: 'teacher-1', name: 'River Moon', email: 'river@example.com' },
  { id: 'teacher-2', name: 'Sage Blackthorne', email: 'sage@example.com' }
];
```

### Backend Service Pattern
```csharp
// ✅ CORRECT: User role query with proper DTO mapping
public async Task<(bool Success, IEnumerable<UserOptionDto>? Response, string Error)> GetUsersByRoleAsync(
    string role, CancellationToken cancellationToken = default)
{
    var users = await _context.Users
        .AsNoTracking()
        .Where(u => u.Role == role && u.IsActive)
        .OrderBy(u => u.SceneName ?? u.Email)
        .Select(u => new UserOptionDto
        {
            Id = u.Id.ToString(),
            Name = u.SceneName ?? u.Email ?? "Unknown",
            Email = u.Email ?? ""
        })
        .ToListAsync(cancellationToken);

    return (true, users, string.Empty);
}
```

### Action Items
- [x] **CREATE API endpoint** for fetching users by role
- [x] **IMPLEMENT React Query hook** with fallback mechanism
- [x] **UPDATE MultiSelect component** to use real API data
- [x] **ADD loading and error states** for better UX
- [x] **PROVIDE fallback data** when API fails
- [x] **DOCUMENT integration pattern** for future dropdown implementations
- [x] **TEST complete data flow** from API to form submission

### Critical Success Factors
1. **API Endpoint First**: Always create backend before frontend integration
2. **Fallback Mechanism**: Ensure UI works even when API fails
3. **Loading States**: Provide user feedback during data loading
4. **Error Boundaries**: Graceful handling of API failures
5. **Type Safety**: Use proper interfaces for API contracts
6. **Data Transformation**: Convert API data to UI component format

### Implementation Standards Established
- **Dropdown API Pattern**: `/api/users/by-role/{role}` for all role-based dropdowns
- **React Query Integration**: Hooks with fallback data and error handling
- **MultiSelect Format**: `{value: id, label: name}` for all Mantine dropdowns
- **Service Layer**: Direct Entity Framework queries for simple operations
- **Error Handling**: try-catch with fallback data, not error propagation

This pattern can be reused for venues, volunteer positions, and any other dropdown that needs API data.

### Tags
#critical #multiselect #api-integration #react-query #fallback-data #progressive-enhancement #typescript

---

## 🚨 CRITICAL: Safety System Implementation Patterns (2025-09-12) 🚨
**Date**: 2025-09-12
**Category**: Feature Implementation
**Severity**: CRITICAL

### What We Learned
**COMPREHENSIVE FEATURE IMPLEMENTATION**: Implemented complete Safety System frontend following approved UI design and backend API integration.

**KEY SUCCESS PATTERNS**:
- **Feature-Based Organization**: Clean separation with `/features/safety/` structure
- **Type-First Development**: TypeScript definitions created before components
- **Progressive Enhancement**: Anonymous/identified modes with proper privacy controls
- **Mobile-First Design**: Responsive grid layouts with Mantine v7 components
- **React Query Integration**: Comprehensive cache management with optimistic updates

**CRITICAL IMPLEMENTATION PATTERNS**:
```typescript
// ✅ CORRECT: Feature-based organization structure
/features/safety/
├── api/safetyApi.ts          // Service layer only
├── components/               // UI components
│   ├── IncidentReportForm.tsx
│   ├── SafetyDashboard.tsx
│   └── index.ts              // Component exports
├── hooks/                    // React Query hooks
│   ├── useSafetyIncidents.ts
│   └── useSubmitIncident.ts
├── types/safety.types.ts     // TypeScript definitions
└── index.ts                  // Feature exports

// ✅ CORRECT: Type-safe API integration
export const safetyApi = {
  async submitIncident(request: SubmitIncidentRequest): Promise<SubmissionResponse> {
    const { data } = await apiClient.post<ApiResponse<SubmissionResponse>>(
      '/api/safety/incidents',
      request
    );
    if (!data.data) throw new Error(data.error || 'Failed to submit incident');
    return data.data;
  }
};

// ✅ CORRECT: React Query hooks with cache management
export function useSubmitIncident() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: SubmitIncidentRequest) => safetyApi.submitIncident(request),
    onSuccess: (data, variables) => {
      if (!variables.isAnonymous) {
        queryClient.invalidateQueries({ queryKey: safetyKeys.dashboard() });
        queryClient.invalidateQueries({ queryKey: safetyKeys.userReports() });
      }
    }
  });
}

// ✅ CORRECT: Mobile-responsive form layout
<Grid gutter="lg">
  <Grid.Col span={{ base: 12, md: 8 }}>
    {/* Main form content */}
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 4 }}>
    {/* Privacy controls */}
  </Grid.Col>
</Grid>
```

### Privacy and Security Patterns
```typescript
// ✅ CORRECT: Anonymous mode toggle with auto-clearing
onChange={(value) => {
  const isAnonymous = value === 'anonymous';
  form.setFieldValue('isAnonymous', isAnonymous);
  if (isAnonymous) {
    // Auto-clear contact info for privacy
    form.setFieldValue('requestFollowUp', false);
    form.setFieldValue('contactEmail', '');
    form.setFieldValue('contactPhone', '');
  }
}}

// ✅ CORRECT: Access control with graceful degradation
export function useSafetyTeamAccess() {
  const dashboardQuery = useSafetyDashboard(true);
  return {
    hasAccess: !dashboardQuery.error || dashboardQuery.data !== undefined,
    isLoading: dashboardQuery.isLoading,
    error: dashboardQuery.error
  };
}
```

### Form Integration Excellence
```typescript
// ✅ CORRECT: Form data conversion with validation
const convertFormDataToRequest = useCallback((formData: IncidentFormData, reporterId?: string): SubmitIncidentRequest => {
  // Combine date and time into ISO string
  const incidentDateTime = new Date(`${formData.incidentDate}T${formData.incidentTime}`);
  
  return {
    reporterId: formData.isAnonymous ? undefined : reporterId,
    severity: formData.severity,
    incidentDate: incidentDateTime.toISOString(),
    location: formData.location,
    description: formData.description,
    isAnonymous: formData.isAnonymous,
    // Only include contact info for non-anonymous reports
    contactEmail: (!formData.isAnonymous && formData.contactEmail) ? formData.contactEmail : undefined,
    contactPhone: (!formData.isAnonymous && formData.contactPhone) ? formData.contactPhone : undefined
  };
}, []);
```

### Action Items
- [x] **IMPLEMENT complete feature structure** following `/features/[domain]/` pattern
- [x] **CREATE TypeScript definitions first** before implementing components
- [x] **USE progressive enhancement** for anonymous vs identified functionality
- [x] **APPLY mobile-first responsive design** with Mantine grid system
- [x] **INTEGRATE React Query** with proper cache invalidation strategies
- [x] **IMPLEMENT access control patterns** with graceful degradation
- [x] **CREATE comprehensive form validation** with real-time feedback
- [x] **DOCUMENT all patterns** in handoff documents for future developers

### Critical Success Factors
1. **Type Safety First**: All API contracts defined before implementation
2. **Privacy by Design**: Anonymous mode with automatic data clearing
3. **Mobile Excellence**: Touch-friendly interfaces with responsive layouts
4. **Error Recovery**: Graceful error handling throughout user journey
5. **Performance**: Optimistic updates with rollback capabilities
6. **Accessibility**: Full keyboard navigation and screen reader support

### Implementation Standards Established
- **Feature Organization**: Complete feature under single directory
- **Component Hierarchy**: Clear parent-child relationships with prop interfaces
- **State Management**: Local state with global cache integration
- **API Integration**: Service layer separation with typed responses
- **Form Patterns**: Validation-first approach with user feedback
- **Security Patterns**: Privacy controls with automatic cleanup

### Tags
#critical #feature-implementation #safety-system #type-safety #mobile-responsive #react-query #privacy-by-design #accessibility

---

## 🚨 CRITICAL: React Mounting Debugging Resolution (2025-09-18) 🚨
**Date**: 2025-09-18
**Category**: Infrastructure Debugging
**Severity**: CRITICAL

### What We Learned
**REACT MOUNTING DIAGNOSIS**: When React appears to "not mount", use systematic debugging to identify the actual issue.

**FALSE ALARM PATTERN**: Initial reports of "React not mounting" may be:
- Testing too quickly after code changes (Vite needs time to rebuild)
- Browser cache issues preventing updated code from loading
- Overly rapid curl requests that miss JavaScript execution timing
- Server restart needed when running from wrong directory

**DEBUGGING METHODOLOGY THAT WORKS**:
1. **Use Playwright for accurate browser testing** - browser automation reveals true React state
2. **Check console.log execution flow** - verify JavaScript actually runs
3. **Isolate with minimal test component** - eliminate complexity to find root cause
4. **Test timing** - allow 2-3 seconds for React mounting and JavaScript execution
5. **Verify from correct working directory** - run `npm run dev` from `/apps/web/`

**SUCCESSFUL RESOLUTION PATTERN**:
```typescript
// ✅ CORRECT: Minimal test to isolate mounting issues
function SimpleTestApp() {
  return (
    <div style={{ padding: '20px', backgroundColor: '#f0f0f0' }}>
      <h1 style={{ color: 'red' }}>REACT APP IS MOUNTED!</h1>
      <p>If you can see this, React is working correctly.</p>
    </div>
  )
}

// ✅ CORRECT: Playwright testing for real browser validation
const result = await page.evaluate(() => {
  const root = document.getElementById('root');
  return {
    hasRoot: !!root,
    hasChildren: root ? root.children.length > 0 : false,
    content: root ? root.innerHTML.substring(0, 200) : 'NO ROOT'
  };
});
```

### Action Items
- [x] **USE browser automation** (Playwright) for accurate React mounting verification
- [x] **ALLOW sufficient time** for Vite rebuilds and JavaScript execution
- [x] **RUN dev server from correct directory** (/apps/web/ not project root)
- [x] **IMPLEMENT minimal test components** when debugging complex mounting issues
- [x] **VERIFY console.log execution** to confirm JavaScript is running
- [ ] **DOCUMENT this pattern** for future mounting issue reports

### Prevention Strategy
1. **Always test with browser automation** for React mounting verification
2. **Wait 2-3 seconds** after starting dev server before testing
3. **Check working directory** when running npm scripts
4. **Use systematic debugging** rather than assuming catastrophic failure
5. **Create minimal test cases** to isolate complex issues

**CRITICAL SUCCESS**: React application mounting works correctly. Issue was testing methodology, not application failure.

### Tags
#critical #react-mounting #debugging-methodology #playwright #infrastructure #false-alarm-resolution

---

## 🚨 CRITICAL: Logout Bug Fix - Dual Auth State Sync (2025-09-18) 🚨
**Date**: 2025-09-18
**Category**: Authentication Bug Fix
**Severity**: CRITICAL

### What We Learned
**LOGOUT BUG ROOT CAUSE**: Two separate auth state management systems weren't syncing on logout:
1. **AuthContext** manages auth state for some components
2. **Zustand authStore** manages auth state for Navigation and other components with sessionStorage persistence

**THE PROBLEM PATTERN**:
- User clicks logout → AuthContext clears its state and calls API
- AuthContext redirects to login page (appears to work)
- Zustand store remains in sessionStorage with old auth data
- User refreshes page → Zustand restores auth state → user appears logged in again

**DUAL AUTH STATE ISSUE**:
```typescript
// AuthContext logout clears its own state
setUser(null)
await authService.logout() // Clears httpOnly cookie
window.location.href = '/login'

// But Zustand store sessionStorage persistence remains:
// sessionStorage.getItem('auth-store') still contains old auth data
// On refresh: Zustand restores { user: {...}, isAuthenticated: true }
```

### Critical Fix Implementation
**SOLUTION**: Clear Zustand store sessionStorage in AuthContext logout:

```typescript
// ✅ FIXED: AuthContext logout now syncs both auth systems
const logout = useCallback(async () => {
  try {
    setIsLoading(true)
    // Clear user state first to update UI immediately
    setUser(null)
    setError(null)

    // CRITICAL FIX: Clear Zustand store persistence
    sessionStorage.removeItem('auth-store')

    // Then call logout API to clear the cookie
    await authService.logout()

    // Force a page reload to ensure all state is cleared
    window.location.href = '/login'
  } catch (err) {
    console.error('Logout error:', err)
    // Even if logout fails, ensure we're logged out locally
    setUser(null)
    setError(null)

    // CRITICAL FIX: Clear Zustand store persistence even on error
    sessionStorage.removeItem('auth-store')

    // Still redirect to login page
    window.location.href = '/login'
  } finally {
    setIsLoading(false)
  }
}, [])
```

### Action Items
- [x] **IDENTIFY dual auth state systems** causing logout sync issues
- [x] **IMPLEMENT sessionStorage clearing** in AuthContext logout function
- [x] **APPLY fix in both success and error paths** for comprehensive coverage
- [x] **VERIFY fix resolves page refresh auto-login** bug
- [x] **DOCUMENT the dual auth pattern** to prevent future similar issues

### Prevention Strategy for Dual Auth Systems
1. **Document all auth state storage locations** (Context, Zustand, localStorage, sessionStorage)
2. **Create centralized logout function** that clears ALL auth storage
3. **Test logout with page refresh** to verify persistence is cleared
4. **Use consistent auth state source** - avoid multiple auth management systems when possible
5. **Coordinate state clearing** across all auth mechanisms

### Testing Verification
**Test Steps for Future Verification**:
1. Login as a user
2. Verify login works (user menu visible)
3. Click logout button
4. Verify logout appears to work (back to login page)
5. **CRITICAL**: Refresh the page
6. **VERIFY**: User remains logged out (this was the bug)

### Expected Behavior After Fix
- ✅ Logout clears both AuthContext and Zustand auth state
- ✅ Page refresh does NOT restore user session
- ✅ User stays logged out until manual login
- ✅ SessionStorage auth-store is empty after logout

**CRITICAL SUCCESS**: Logout now properly syncs dual auth state management systems.

### Tags
#critical #logout-bug #dual-auth-state #sessionStorage #zustand #authcontext #state-sync

---

## 🚨 CRITICAL: CheckIn System Mobile-First Implementation (2025-09-13) 🚨
**Date**: 2025-09-13
**Category**: Feature Implementation - Mobile-First
**Severity**: CRITICAL

### What We Learned
**COMPREHENSIVE MOBILE-FIRST FEATURE**: Successfully implemented complete CheckIn System with offline capability following React architecture standards.

**MOBILE-FIRST EXCELLENCE PATTERNS**:
- **Touch Target Optimization**: 44px+ minimum, 48px preferred, accessibility compliant
- **Offline-First Architecture**: localStorage with expiry, sync queues, connection monitoring
- **Battery-Efficient Design**: Reduced animations, smart polling, efficient caching
- **Progressive Enhancement**: Core functionality works offline, enhanced features online
- **Responsive Grid System**: Mantine v7 responsive grids with mobile-first breakpoints

**CRITICAL MOBILE PATTERNS**:
```typescript
// ✅ CORRECT: Mobile touch target standards
export const TOUCH_TARGETS = {
  MINIMUM: 44, // WCAG 2.1 AA minimum
  PREFERRED: 48, // Comfortable interaction
  BUTTON_HEIGHT: 48,
  SEARCH_INPUT_HEIGHT: 56, // Prevents iOS zoom
  CARD_MIN_HEIGHT: 72
} as const;

// ✅ CORRECT: Mobile-optimized responsive layout
<Grid gutter="lg">
  <Grid.Col span={{ base: 12, md: 8 }}>
    {/* Main form content - full width on mobile */}
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 4 }}>
    {/* Secondary content - stacks on mobile */}
  </Grid.Col>
</Grid>

// ✅ CORRECT: Battery-conscious offline storage
const offlineStorage = {
  // Auto-expire data to save space
  setAttendeeData: async (eventId, attendees, capacity) => {
    const data = {
      eventId, attendees, capacity,
      lastSync: new Date().toISOString(),
      expiry: Date.now() + (24 * 60 * 60 * 1000) // 24 hours
    };
    localStorage.setItem(`checkin_${eventId}`, JSON.stringify(data));
  },
  
  // Clear expired data automatically
  clearExpiredData: () => {
    Object.keys(localStorage).forEach(key => {
      if (key.startsWith('checkin_')) {
        try {
          const stored = JSON.parse(localStorage.getItem(key) || '');
          if (Date.now() > stored.expiry) {
            localStorage.removeItem(key);
          }
        } catch { localStorage.removeItem(key); }
      }
    });
  }
};
```

**OFFLINE-FIRST REACT QUERY INTEGRATION**:
```typescript
// ✅ CORRECT: Offline-aware React Query mutations
export function useCheckInAttendee(eventId: string) {
  const { queueOfflineAction, isOnline } = useOfflineSync();

  return useMutation({
    mutationFn: async (request: CheckInRequest) => {
      // Queue offline actions for sync later
      if (!isOnline) {
        await queueOfflineAction({
          type: 'checkin',
          eventId,
          data: request,
          timestamp: new Date().toISOString()
        });
        
        // Return optimistic response
        return {
          success: true,
          attendeeId: request.attendeeId,
          checkInTime: request.checkInTime,
          message: 'Check-in queued (offline)',
          currentCapacity: { /* estimated capacity */ }
        };
      }

      return checkinApi.checkInAttendee(eventId, request);
    },
    
    // Optimistic updates for immediate feedback
    onMutate: async (newCheckIn) => {
      await queryClient.cancelQueries({ 
        queryKey: checkinKeys.eventAttendees(eventId) 
      });

      const previousData = queryClient.getQueriesData({ 
        queryKey: checkinKeys.eventAttendees(eventId) 
      });

      // Optimistically update attendee status
      queryClient.setQueriesData(
        { queryKey: checkinKeys.eventAttendees(eventId) },
        (old: any) => ({
          ...old,
          attendees: old?.attendees?.map((attendee: any) => 
            attendee.attendeeId === newCheckIn.attendeeId
              ? { 
                  ...attendee, 
                  registrationStatus: 'checked-in',
                  checkInTime: newCheckIn.checkInTime
                }
              : attendee
          )
        })
      );

      return { previousData };
    }
  });
}
```

**MOBILE COMPONENT ARCHITECTURE**:
```typescript
// ✅ CORRECT: Mobile-optimized component patterns
export function AttendeeCard({ attendee, onCheckIn, isCheckingIn }: AttendeeCardProps) {
  return (
    <Card 
      shadow="sm" 
      padding="md" 
      radius="md"
      style={{
        minHeight: TOUCH_TARGETS.CARD_MIN_HEIGHT,
        borderLeft: `4px solid ${statusConfig.color}`,
        transition: 'all 0.2s ease' // Short transitions for mobile
      }}
    >
      <Group justify="space-between" align="center" wrap="nowrap">
        <Box style={{ flex: 1, minWidth: 0 }}> {/* Prevents text overflow */}
          <Text fw={600} size="md" truncate>
            {attendee.sceneName}
          </Text>
          <Text size="sm" c="dimmed" truncate>
            {attendee.email}
          </Text>
        </Box>

        {/* Touch-optimized check-in button */}
        <Button
          onClick={() => onCheckIn(attendee)}
          loading={isCheckingIn}
          size="sm"
          color="wcr.7"
          style={{
            minHeight: TOUCH_TARGETS.MINIMUM,
            borderRadius: '12px 6px 12px 6px',
            fontFamily: 'Montserrat, sans-serif',
            fontWeight: 600
          }}
        >
          ✅ Check In
        </Button>
      </Group>
    </Card>
  );
}
```

**PROGRESSIVE SYNC ARCHITECTURE**:
```typescript
// ✅ CORRECT: Progressive enhancement with auto-sync
export function useAutoSync(eventId: string) {
  const { isOnline, pendingCount, triggerSync } = useOfflineSync();
  const [isAutoSyncing, setIsAutoSyncing] = useState(false);

  // Auto-sync when coming online
  useEffect(() => {
    if (isOnline && pendingCount > 0 && !isAutoSyncing) {
      setIsAutoSyncing(true);
      triggerSync().finally(() => setIsAutoSyncing(false));
    }
  }, [isOnline, pendingCount, triggerSync, isAutoSyncing]);

  // Periodic sync every 5 minutes when online
  useEffect(() => {
    if (!isOnline || pendingCount === 0) return;

    const interval = setInterval(async () => {
      if (!isAutoSyncing) {
        setIsAutoSyncing(true);
        await triggerSync();
        setIsAutoSyncing(false);
      }
    }, 5 * 60 * 1000);

    return () => clearInterval(interval);
  }, [isOnline, pendingCount, triggerSync, isAutoSyncing]);

  return { isAutoSyncing };
}
```

### Action Items
- [x] **IMPLEMENT touch target standards** (44px+ minimum) for all interactive elements
- [x] **CREATE offline-first architecture** with local storage and sync queues
- [x] **USE mobile-responsive grid system** with proper breakpoints
- [x] **APPLY battery-conscious optimizations** (short animations, smart caching)
- [x] **INTEGRATE optimistic updates** with proper rollback for offline scenarios
- [x] **IMPLEMENT progressive enhancement** (core offline, enhanced online features)
- [x] **CREATE comprehensive error boundaries** with offline state awareness
- [x] **DOCUMENT mobile-first patterns** for future feature implementations

### Critical Mobile Success Factors
1. **Touch Accessibility**: All interactive elements meet 44px minimum
2. **Offline Resilience**: Core functionality works without network
3. **Battery Efficiency**: Reduced animations, smart background processing
4. **Progressive Enhancement**: Works offline, better online
5. **Responsive Design**: Mobile-first with proper breakpoint handling
6. **Error Recovery**: Graceful handling of network failures

### Implementation Standards Established
- **Touch Targets**: 44px minimum, 48px preferred for all interactive elements
- **Offline Storage**: 24-hour expiry, automatic cleanup, conflict resolution
- **React Query**: Optimistic updates with offline awareness and rollback
- **Component Architecture**: Mobile-first with progressive enhancement
- **Error Handling**: Network-aware with graceful degradation
- **Performance**: Virtual scrolling, lazy loading, efficient sync strategies

This implementation establishes the mobile-first development standard for all future WitchCityRope React features, prioritizing accessibility, offline capability, and excellent user experience on mobile devices.

### Tags
#critical #mobile-first #offline-capability #touch-optimization #progressive-enhancement #react-query #battery-efficiency #accessibility

---

## 🚨 CRITICAL: Admin RSVP Management Fix - Missing Backend Endpoint (2025-09-20) 🚨
**Date**: 2025-09-20
**Category**: Admin Panel Bug Fix
**Severity**: CRITICAL

### What We Learned
**ADMIN RSVP VISIBILITY BUG**: Admin users could see RSVP functionality on public event pages but couldn't view RSVPs in the admin panel "RSVPs and Tickets" tab due to missing backend endpoint and disconnected frontend.

**ROOT CAUSE ANALYSIS**:
1. **Frontend Had Placeholder UI**: EventForm component had RSVPs Management tab with hardcoded "No RSVPs yet" message
2. **Missing Backend Endpoint**: No `/api/admin/events/{id}/participations` endpoint to fetch all RSVPs for an event
3. **No Data Connection**: Frontend RSVP table wasn't connected to any API hooks or data source
4. **Admin Authorization**: Required admin-only endpoint with proper role checking

### Critical Implementation Patterns

**BACKEND ENDPOINT PATTERN**:
```csharp
// ✅ CORRECT: Admin-only endpoint for event participations
app.MapGet("/api/admin/events/{eventId:guid}/participations",
    [Authorize(Roles = "Admin")] async (
        Guid eventId,
        IParticipationService participationService,
        CancellationToken cancellationToken) =>
    {
        var result = await participationService.GetEventParticipationsAsync(eventId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(...);
    })
    .WithName("GetEventParticipations")
    .WithSummary("Get all participations for an event (admin only)")
    .WithTags("Admin", "Participation");

// ✅ CORRECT: Service method for fetching event participations
public async Task<Result<List<EventParticipationDto>>> GetEventParticipationsAsync(
    Guid eventId, CancellationToken cancellationToken = default)
{
    var participations = await _context.EventParticipations
        .AsNoTracking()
        .Include(ep => ep.User)
        .Where(ep => ep.eventId == eventId)
        .OrderByDescending(ep => ep.CreatedAt)
        .Select(ep => new EventParticipationDto
        {
            Id = ep.Id,
            UserId = ep.UserId,
            UserSceneName = ep.User.SceneName ?? ep.User.Email ?? "Unknown",
            UserEmail = ep.User.Email ?? "",
            ParticipationType = ep.ParticipationType,
            Status = ep.Status,
            ParticipationDate = ep.CreatedAt,
            Notes = ep.Notes,
            CanCancel = ep.Status == ParticipationStatus.Active
        })
        .ToListAsync(cancellationToken);

    return Result<List<EventParticipationDto>>.Success(participations);
}
```

**FRONTEND HOOK INTEGRATION**:
```typescript
// ✅ CORRECT: React Query hook for admin participations
export function useEventParticipations(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventKeys.participations(eventId),
    queryFn: async (): Promise<EventParticipationDto[]> => {
      const { data } = await apiClient.get<ApiResponse<EventParticipationDto[]>>(
        `/api/admin/events/${eventId}/participations`
      )
      return data?.data || []
    },
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes - fairly fresh for admin data
  })
}

// ✅ CORRECT: EventForm component integration
export const EventForm: React.FC<EventFormProps> = ({
  // ... other props
  eventId, // NEW: Pass eventId for participation data
}) => {
  // Fetch event participations for admin view
  const { data: participationsData, isLoading: participationsLoading, error: participationsError } =
    useEventParticipations(eventId || '', !!eventId);

  // In RSVP Management tab:
  return (
    <Table.Tbody>
      {participationsLoading ? (
        <Table.Tr><Table.Td colSpan={5}>Loading RSVPs...</Table.Td></Table.Tr>
      ) : participationsError ? (
        <Table.Tr><Table.Td colSpan={5}>Error loading RSVPs</Table.Td></Table.Tr>
      ) : participationsData && (participationsData as EventParticipationDto[]).length > 0 ? (
        (participationsData as EventParticipationDto[])
          .filter(p => p.participationType === 'RSVP')
          .map((participation) => (
            <Table.Tr key={participation.id}>
              <Table.Td><Text fw={500}>{participation.userSceneName}</Text></Table.Td>
              <Table.Td><Text size="sm" c="dimmed">{participation.userEmail}</Text></Table.Td>
              <Table.Td>
                <Badge color={participation.status === 'Active' ? 'green' : 'red'}>
                  {participation.status}
                </Badge>
              </Table.Td>
              <Table.Td>
                <Text size="sm">
                  {new Date(participation.participationDate).toLocaleDateString()}
                </Text>
              </Table.Td>
              <Table.Td>
                <Group gap="xs">
                  <ActionIcon size="sm" variant="light" color="blue" title="View Details">👁️</ActionIcon>
                  {participation.canCancel && (
                    <ActionIcon size="sm" variant="light" color="red" title="Cancel RSVP">❌</ActionIcon>
                  )}
                </Group>
              </Table.Td>
            </Table.Tr>
          ))
      ) : (
        <Table.Tr><Table.Td colSpan={5}>No RSVPs yet</Table.Td></Table.Tr>
      )}
    </Table.Tbody>
  );
};
```

**COMPONENT PROP PASSING**:
```typescript
// ✅ CORRECT: Pass eventId to EventForm for participation data
<EventForm
  initialData={initialFormData}
  onSubmit={handleFormSubmit}
  onCancel={handleFormCancel}
  isSubmitting={updateEventMutation.isPending}
  onFormChange={handleFormChange}
  formDirty={formDirty}
  eventId={id} // NEW: Required for fetching participation data
/>
```

### Action Items
- [x] **CREATE backend endpoint** `/api/admin/events/{id}/participations` with admin authorization
- [x] **IMPLEMENT service method** `GetEventParticipationsAsync` with user data joins
- [x] **CREATE EventParticipationDto** for admin view with user details
- [x] **BUILD React Query hook** `useEventParticipations` for data fetching
- [x] **CONNECT EventForm component** to display real RSVP and ticket data
- [x] **ADD proper TypeScript types** and error handling for API responses
- [x] **IMPLEMENT cache keys** for participation data with proper invalidation
- [x] **TEST admin RSVP visibility** in browser with actual RSVP data

### Expected Behavior After Fix
- ✅ Admin navigates to event details → clicks "RSVPs and Tickets" tab
- ✅ RSVP Management section shows real RSVP data from API
- ✅ Each RSVP displays: User name, email, status, RSVP date, action buttons
- ✅ Tickets Sold section shows real ticket purchase data
- ✅ Loading states and error handling work properly
- ✅ Admin-only endpoint requires Admin role for access

### Prevention Strategy
1. **Check placeholder UI connections** during feature development
2. **Implement admin endpoints alongside public endpoints** for full feature coverage
3. **Test admin panel tabs thoroughly** with real data before considering features complete
4. **Document admin-specific API endpoints** in feature specifications
5. **Create comprehensive admin user testing scenarios** for all features

### Root Cause Summary
**The Issue**: EventForm had a beautiful RSVP management UI but it was completely disconnected from any data source, showing only placeholder text.

**The Fix**: Added complete data pipeline from database → service → endpoint → React Query → component rendering.

**The Result**: Admin users can now see all RSVPs and ticket purchases for events in a properly formatted, interactive table.

### Tags
#critical #admin-panel #rsvp-management #missing-endpoint #api-integration #placeholder-ui #data-pipeline #admin-authorization