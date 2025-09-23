# React Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL ARCHITECTURE DOCUMENTS (MUST READ FIRST): 🚨
1. **🛑 DTO ALIGNMENT STRATEGY** - **PREVENTS 393 TYPESCRIPT ERRORS**
`/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

2. **React Architecture Guide** - **CORE ARCHITECTURE DECISIONS**
`/docs/architecture/react-migration/react-architecture.md`

3. **API Changes Guide**
`/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`

4. **Project Architecture** - **TECH STACK AND STANDARDS**
`/ARCHITECTURE.md`

5. **Design System**
`/docs/design/current/design-system-v7.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs
- **React Patterns** - `/docs/standards-processes/development-standards/react-patterns.md` - React standards
- **UI Standards** - `/docs/standards-processes/ui-implementation-standards.md` - UI implementation rules

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Workflow Process** - `/docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `/docs/standards-processes/agent-boundaries.md` - What each agent does
- **Documentation Standards** - `/docs/standards-processes/documentation-standards.md` - How to document

### Validation Gates (MUST COMPLETE WHEN STARTING A NEW SESSION):
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

---

## 🚨 IF THIS FILE EXCEEDS 500 LINES, YOU ARE DOING IT WRONG! ADD TO PART 2! 🚨

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 2 total
**Part 1**: `/docs/lessons-learned/react-developer-lessons-learned.md` (THIS FILE - STARTUP ONLY)
**Part 2**: `/docs/lessons-learned/react-developer-lessons-learned-part-2.md` (MAIN LESSONS FILE)
**Read ALL**: Both Part 1 AND Part 2 are MANDATORY
**Write to**: Part 2 ONLY - **NEVER ADD NEW LESSONS TO THIS FILE (PART 1)**
**Max size**: Part 1 = 500 lines (startup only), Part 2 = 2,000 lines
**IF READ FAILS**: STOP and fix per documentation-standards.md

## 🚨 ULTRA CRITICAL: NEW LESSONS GO TO PART 2, NOT HERE! 🚨
**PART 1 PURPOSE**: Startup procedures and critical navigation ONLY
**ALL NEW LESSONS**: Must go to Part 2 - `/docs/lessons-learned/react-developer-lessons-learned-part-2.md`
**IF YOU ADD LESSONS HERE**: You are violating the split pattern!

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using procedure in documentation-standards.md
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

## 🚨 CRITICAL: WHEN CREATING NEW COMPONENTS - FOLLOW WIREFRAMES EXACTLY - NO UNAUTHORIZED FEATURES 🚨
**PROBLEM**: Adding features/columns/buttons not shown in approved wireframes
**PRINCIPLE**: Wireframes are contracts - implement ONLY what's shown
**PREVENTION**:
1. ALWAYS read approved wireframes before implementation
2. NEVER add "helpful" features without explicit approval
3. NEVER add columns/fields for "completeness"
4. NEVER assume missing features are oversights
5. When in doubt, ASK before adding anything

**WHY THIS MATTERS**: Extra features create technical debt, confuse users, and require removal

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
- **CRITICAL**: Even style props can cause text cutoff if height/padding not properly set

### ✅ CRITICAL SOLUTION:
1. **NEVER mix custom CSS classes with Mantine components** - use Mantine props only
2. **USE Mantine Button component props** instead of CSS classes
3. **REPLACE custom classes systematically** across all components
4. **TEST button text visibility** after converting from custom styles
5. **USE styles object with proper height/padding** for admin buttons

```typescript
// ❌ BROKEN: Custom CSS classes with Mantine Button
<Button className="btn btn-primary" style={{ width: '100%' }}>
  Text gets cut off
</Button>

// ❌ BROKEN: Style props without proper height
<Button
  variant="filled"
  color="blue"
  style={{ borderColor: '#880124', color: '#880124' }}
>
  Text still cuts off
</Button>

// ✅ CORRECT: Pure Mantine Button with styles object
<Button
  variant="filled"
  color="blue"
  fullWidth
  size="lg"
  loading={isLoading}
  styles={{
    root: {
      fontWeight: 600,
      height: '44px',           // CRITICAL: Set explicit height
      paddingTop: '12px',       // CRITICAL: Set explicit padding
      paddingBottom: '12px',    // CRITICAL: Set explicit padding
      fontSize: '14px',
      lineHeight: '1.2'         // CRITICAL: Set line height
    }
  }}
>
  Text displays properly
</Button>

// ✅ CORRECT: Admin dashboard button pattern
<Button
  leftSection={<IconPlus size={16} />}
  variant="light"
  onClick={handleAction}
  styles={{
    root: {
      borderColor: '#880124',
      color: '#880124',
      fontWeight: 600,
      height: '44px',
      paddingTop: '12px',
      paddingBottom: '12px',
      fontSize: '14px',
      lineHeight: '1.2'
    }
  }}
>
  Create New Event
</Button>
```

### 🔧 MANDATORY CONVERSION CHECKLIST:
1. **SEARCH for `className="btn`** in all React components
2. **REPLACE with appropriate Mantine Button variant/color**
3. **USE fullWidth instead of style={{ width: '100%' }}**
4. **USE leftSection/rightSection for icons** instead of inline elements
5. **APPLY admin button pattern** with height: '44px' and explicit padding
6. **TEST all button states** (normal, hover, loading, disabled)

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

```

---

## 🚨 NAVIGATION TO PART 2 - MORE LESSONS LEARNED 🚨

**PART 1 COMPLETE** - You have read the startup procedures and critical patterns.

**CONTINUE READING**: `/docs/lessons-learned/react-developer-lessons-learned-part-2.md`
**CONTAINS**: Additional lessons learned, more patterns, and detailed solutions
**MANDATORY**: You MUST read Part 2 for complete React developer knowledge
**SIZE**: Part 2 contains 1900+ additional lines of critical lessons

**REMEMBER**: All new lessons go to Part 2, not this file!