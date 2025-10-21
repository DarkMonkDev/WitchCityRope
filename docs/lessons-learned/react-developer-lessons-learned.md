# React Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL ARCHITECTURE DOCUMENTS (MUST READ FIRST): 🚨
1. **🛑 DTO ALIGNMENT STRATEGY** - **PREVENTS 393 TYPESCRIPT ERRORS**
`/home/chad/repos/witchcityrope/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

2. **React Architecture Guide** - **CORE ARCHITECTURE DECISIONS**
`/home/chad/repos/witchcityrope/docs/architecture/react-migration/react-architecture.md`

3. **API Changes Guide**
`/home/chad/repos/witchcityrope/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`

4. **Project Architecture** - **TECH STACK AND STANDARDS**
`/ARCHITECTURE.md`

5. **Design System**
`/home/chad/repos/witchcityrope/docs/design/current/design-system-v7.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/home/chad/repos/witchcityrope/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs
- **React Patterns** - `/home/chad/repos/witchcityrope/docs/standards-processes/development-standards/react-patterns.md` - React standards
- **UI Standards** - `/home/chad/repos/witchcityrope/docs/standards-processes/ui-implementation-standards.md` - UI implementation rules

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Workflow Process** - `/home/chad/repos/witchcityrope/docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `/home/chad/repos/witchcityrope/docs/standards-processes/agent-boundaries.md` - What each agent does
- **Documentation Standards** - `/home/chad/repos/witchcityrope/docs/standards-processes/documentation-standards.md` - How to document

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

## 🛠️ AVAILABLE DEVELOPMENT TOOLS

### Chrome DevTools MCP (NEW - 2025-10-03)
**Purpose**: Debug React components, inspect state/props, and validate UI rendering

**Key Capabilities for React Developers**:
- **Component Inspection**: Inspect React component hierarchy, state, and props in real-time
- **DOM Inspection**: View rendered HTML and CSS styles
- **Console Debugging**: Monitor JavaScript errors, warnings, and console output
- **Network Monitoring**: Inspect API calls, view request/response data, validate loading states
- **Performance Profiling**: Analyze component render times and identify performance bottlenecks

**Use Cases for React Development**:
- Component debugging - Inspect component state and props during development
- UI validation - Verify components render correctly across different states
- API integration testing - Monitor API calls and validate response handling
- Performance optimization - Identify slow renders and unnecessary re-renders
- Responsive design testing - Test components at different viewport sizes

**Configuration**: Automatically available via MCP - see `/home/chad/repos/witchcityrope/docs/standards-processes/MCP/MCP_SERVERS.md`

**Best Practices**:
- Use React DevTools features to inspect component state and props
- Monitor console for warnings about missing keys, deprecated APIs, or performance issues
- Validate API responses match TypeScript interfaces from `@witchcityrope/shared-types`
- Take screenshots for visual regression testing
- Profile component performance before and after optimizations

---

## 🚨 IF THIS FILE EXCEEDS 1700 LINES, CREATE PART 2! BOTH FILES CAN BE UP TO 1700 LINES EACH 🚨

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 2 total
**Part 1**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned.md` (THIS FILE - STARTUP ONLY)
**Part 2**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-2.md` (MAIN LESSONS FILE)
**Read ALL**: Both Part 1 AND Part 2 are MANDATORY
**Write to**: Part 2 ONLY - **NEVER ADD NEW LESSONS TO THIS FILE (PART 1)**
**Maximum file size**: 1700 lines (to stay under token limits). Both Part 1 and Part 2 files can be up to 1700 lines each
**IF READ FAILS**: STOP and fix per documentation-standards.md

## 🚨 ULTRA CRITICAL: NEW LESSONS GO TO PART 2, NOT HERE! 🚨
**PART 1 PURPOSE**: Startup procedures and critical navigation ONLY
**ALL NEW LESSONS**: Must go to Part 2 - `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-2.md`
**IF YOU ADD LESSONS HERE**: You are violating the split pattern!

## 🚨 LATEST CRITICAL LESSON: REACT ROUTER NAVIGATION TIMING FIX 🚨

### ⚠️ PROBLEM: navigate() changes URL but component doesn't re-render
**DISCOVERED**: 2025-10-05 - Vetting application detail page navigation broken

**SYMPTOMS**:
- URL changes from `/admin/vetting` to `/admin/vetting/applications/123` ✅
- Loader runs for new route ✅
- BUT: New component never mounts ❌
- Old component (table) remains visible ❌
- Manual browser refresh required to see detail page ❌

### 🛑 ROOT CAUSE:
**React Router Outlet doesn't re-render when `navigate()` is called during event handler**

When `navigate()` is called synchronously during a click event handler:
1. URL updates immediately
2. Loader runs successfully
3. BUT React Router's internal state update gets "batched" with the current render cycle
4. The `<Outlet />` component doesn't recognize it needs to unmount/mount new component
5. Result: URL changes but UI doesn't update

### ✅ SOLUTION: Defer navigation to next tick
```typescript
// ❌ WRONG: Synchronous navigation during event handler
const handleRowClick = useCallback((applicationId: string) => {
  navigate(`/admin/vetting/applications/${applicationId}`);
}, [navigate]);

// ✅ CORRECT: Defer navigation using setTimeout
const handleRowClick = useCallback((applicationId: string) => {
  // Use setTimeout to ensure navigation happens AFTER React finishes current render cycle
  // This allows Outlet to properly unmount old component and mount new one
  setTimeout(() => {
    navigate(`/admin/vetting/applications/${applicationId}`);
  }, 0);
}, [navigate]);
```

### 📋 WHEN TO USE THIS PATTERN:
**USE `setTimeout` wrapper when:**
- Navigating from click event handlers
- Navigating from table row clicks
- Navigating from button click handlers
- ANY programmatic navigation triggered by user events

**DON'T NEED `setTimeout` when:**
- Using `<Link>` components (React Router handles timing)
- Navigating from useEffect (not in event handler context)
- Navigating from async functions (already deferred)

### 🔧 DEBUGGING CHECKLIST:
If navigation appears broken:
1. **Check URL** - Does it change? (If yes, routing config is correct)
2. **Check loader** - Does it run? (If yes, route exists and is matched)
3. **Check component mount** - Do component logs appear? (If no, timing issue)
4. **Add setTimeout wrapper** to navigation call
5. **Test** - Component should now mount correctly

**FILE**: `apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx`

---

## 🚨 ROUTING DEBUGGING PATTERN 🚨

### ⚠️ PROBLEM: React Router pages appear "broken" with poor error feedback
**DISCOVERED**: 2025-09-23 - Vetting application detail route navigation appears broken to users

### 🛑 ROOT CAUSE:
- Poor error handling in route components when API calls fail
- No debugging information to identify authentication vs data vs network issues
- Insufficient user feedback about what specifically went wrong

### ✅ CRITICAL SOLUTION PATTERN:
1. **ADD comprehensive console logging** to trace route rendering and API calls
2. **ENHANCE error handling** with specific error types (auth, network, not found)
3. **PROVIDE helpful user feedback** with specific guidance for each error type
4. **VALIDATE route parameters** and provide clear validation error messages

```typescript
// ✅ CORRECT: Enhanced route component with debugging and error handling
export const DetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();

  // Debug logging for route rendering
  console.log('DetailPage rendered:', {
    id,
    pathname: location.pathname,
    timestamp: new Date().toISOString()
  });

  // Parameter validation with helpful error messages
  if (!id) {
    return (
      <Alert icon={<IconAlertCircle />} color="red" title="Invalid ID">
        <Text>No ID provided in URL.</Text>
        <Text size="sm" c="dimmed">Current path: {location.pathname}</Text>
      </Alert>
    );
  }

  // API service with enhanced error handling
  const { data, isLoading, error } = useQuery({
    queryKey: ['detail', id],
    queryFn: () => apiService.getDetail(id)
  });

  // Enhanced error display with specific guidance
  if (error) {
    const errorMessage = error?.message || 'Unknown error';
    const isAuthError = errorMessage.includes('401') || errorMessage.includes('Authentication');
    const isNotFound = errorMessage.includes('404') || errorMessage.includes('not found');

    return (
      <Alert
        icon={<IconAlertCircle />}
        color={isAuthError ? "orange" : "red"}
        title={isAuthError ? "Authentication Required" : isNotFound ? "Not Found" : "Error"}
      >
        <Text>{errorMessage}</Text>
        {isAuthError && <Text size="sm" c="dimmed">Please log in again.</Text>}
        {isNotFound && <Text size="sm" c="dimmed">The requested item may have been deleted.</Text>}
      </Alert>
    );
  }
};

// ✅ CORRECT: API service with comprehensive error handling
async getDetail(id: string): Promise<DetailResponse> {
  console.log('API.getDetail called with ID:', id);

  try {
    const response = await apiClient.get(`/api/items/${id}`);
    console.log('API response:', { status: response.status, hasData: !!response.data });
    return response.data;
  } catch (error: any) {
    console.error('API.getDetail error:', {
      id,
      status: error.response?.status,
      message: error.message
    });

    // Provide specific error messages based on HTTP status
    if (error.response?.status === 401) {
      throw new Error('Authentication required. Please log in.');
    } else if (error.response?.status === 404) {
      throw new Error(`Item with ID "${id}" was not found.`);
    } else if (error.response?.status >= 500) {
      throw new Error('Server error. Please try again later.');
    }

    throw error;
  }
}
```

### 🔧 MANDATORY DEBUGGING CHECKLIST:
1. **ADD console.log statements** to trace route component rendering and API calls
2. **VALIDATE route parameters** before using them
3. **ENHANCE error messages** with specific guidance for different error types
4. **PROVIDE fallback UI** for loading and error states
5. **TEST with invalid IDs and authentication issues**

### Tags
#critical #routing #debugging #error-handling #user-experience

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

## 🚨 CRITICAL: FALLBACK/MOCK DATA IN PRODUCTION MASKS API FAILURES 🚨

### ⚠️ PROBLEM: Silent API failures due to fallback data hiding real issues
**DISCOVERED**: 2025-09-23 - Vetting applications showing mock data with wrong IDs (1, 2, 3...) instead of GUIDs
**ROOT CAUSE**: React Query hooks silently falling back to sample data when API fails

### 🛑 CRITICAL ISSUE:
- `useVettingApplications` hook contained 140+ lines of mock data with sequential IDs (1-8)
- Both empty responses AND API failures triggered fallback to sample data
- Users clicking rows got URLs like `/admin/vetting/applications/2` instead of GUID URLs
- **MASKING REAL PROBLEMS**: API/database issues were hidden from users and developers

### ✅ CRITICAL SOLUTION PATTERN:
1. **REMOVE ALL fallback/mock data** from production hooks and services
2. **LET ERRORS SURFACE** - use React Query error handling instead of masking
3. **ADD proper logging** to track API call patterns and failures
4. **ENHANCE error messages** with specific user guidance

```typescript
// ❌ BROKEN: Fallback data masks API problems
export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery<PagedResult<ApplicationSummaryDto>>({
    queryKey: vettingKeys.applicationsList(filters),
    queryFn: async () => {
      try {
        const result = await vettingAdminApi.getApplicationsForReview(filters);
        // If API returns no data, provide sample data for demo purposes
        if (!result || !result.items || result.items.length === 0) {
          return { items: sampleApplications, ... }; // MASKS PROBLEMS!
        }
        return result;
      } catch (error) {
        // Fallback to sample data if API fails
        return { items: sampleApplications, ... }; // HIDES ERRORS!
      }
    }
  });
}

// ✅ CORRECT: Let errors surface with proper handling
export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery<PagedResult<ApplicationSummaryDto>>({
    queryKey: vettingKeys.applicationsList(filters),
    queryFn: async () => {
      console.log('useVettingApplications: Fetching applications with filters:', filters);

      try {
        const result = await vettingAdminApi.getApplicationsForReview(filters);

        console.log('useVettingApplications: API response received:', {
          hasResult: !!result,
          hasItems: !!result?.items,
          itemCount: result?.items?.length || 0,
          totalCount: result?.totalCount || 0
        });

        // Return actual API result or proper empty result
        if (!result) {
          console.warn('useVettingApplications: API returned null/undefined result');
          return {
            items: [],
            totalCount: 0,
            pageSize: filters.pageSize,
            pageNumber: filters.page,
            totalPages: 0
          };
        }

        return result;
      } catch (error: any) {
        console.error('useVettingApplications: API call failed:', {
          error: error.message || error,
          status: error.response?.status,
          filters
        });

        // Enhance error message based on HTTP status
        if (error.response?.status === 401) {
          throw new Error('Authentication required. Please log in to view vetting applications.');
        } else if (error.response?.status === 403) {
          throw new Error('Access denied. You do not have permission to view vetting applications.');
        }
        // ... more specific error handling

        // Re-throw the error to let React Query handle it properly
        throw error;
      }
    },
    // Ensure errors are thrown instead of silently returning fallback data
    throwOnError: true,
  });
}
```

### 🏗️ COMPONENT ERROR HANDLING PATTERN:
**Components should handle errors explicitly** instead of relying on fallback data:

```typescript
// ✅ CORRECT: Component handles errors with user-friendly messages
const { data, isLoading, error, refetch } = useVettingApplications(filters);

if (error) {
  return (
    <Paper p="xl" radius="md">
      <Text c="red" ta="center">
        Error loading applications: {error.message}
      </Text>
      <Group justify="center" mt="md">
        <Button onClick={() => refetch()} leftSection={<IconRefresh size={16} />}>
          Try Again
        </Button>
      </Group>
    </Paper>
  );
}

// Empty state handling
{data?.items.length === 0 && !isLoading && (
  <Box p="xl" ta="center">
    <Text c="dimmed" size="lg">
      {filters.searchQuery || filters.statusFilters.length > 0
        ? 'No applications match your filters'
        : 'No vetting applications found'
      }
    </Text>
  </Box>
)}
```

### 🔧 MANDATORY PREVENTION CHECKLIST:
1. **SEARCH for sample/mock data** in all production hooks and services
2. **REMOVE fallback data patterns** that mask API failures
3. **ADD comprehensive logging** for API calls and responses
4. **ENHANCE error messages** with specific guidance for users
5. **TEST error scenarios** to ensure proper error surfacing
6. **USE throwOnError: true** in React Query to prevent silent failures

### 🎯 WHEN MOCK DATA IS ACCEPTABLE:
- **Test files only** (`.test.tsx`, `.spec.tsx`)
- **Storybook stories** for component development
- **Demo environments** with clear labeling
- **NEVER in production hooks/services**

### 💥 CONSEQUENCES OF IGNORING:
- ❌ API/database problems go undetected
- ❌ Wrong data types (sequential IDs vs GUIDs) break routing
- ❌ Users experience confusing behavior without clear error feedback
- ❌ Debugging becomes nearly impossible when real issues are masked
- ❌ Production incidents are harder to detect and resolve

### Tags
#critical #api-integration #error-handling #data-integrity #fallback-data #mock-data #production-issues

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
**RECURRING**: 2025-10-05 - **SAME ISSUE** in dashboard vetting button - THIS IS A SYSTEMIC PROBLEM

### 🛑 ROOT CAUSE:
- Custom CSS classes from legacy codebase override Mantine component styling
- `btn` class has conflicting padding, font-size, and layout properties
- Mantine Button components have their own styling that gets overridden
- Text cutoff occurs due to conflicting line-height and padding values
- **CRITICAL**: Even style props can cause text cutoff if height/padding not properly set
- **CRITICAL**: Using `size="sm"` without explicit height/padding causes text cutoff
- **SYSTEMIC ISSUE**: Every new button created without proper styling repeats this problem

### ✅ CRITICAL SOLUTION:
1. **NEVER mix custom CSS classes with Mantine components** - use Mantine props only
2. **NEVER use size prop alone** - ALWAYS include explicit height/padding in styles object
3. **USE Mantine Button component props** instead of CSS classes
4. **REPLACE custom classes systematically** across all components
5. **TEST button text visibility** after converting from custom styles
6. **USE styles object with proper height/padding** for ALL buttons

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

// ❌ BROKEN: Using size prop without explicit height/padding (Dashboard bug 2025-10-05)
<Button
  component="a"
  href="/join"
  color="blue"
  size="sm"
>
  Submit Vetting Application  {/* Text gets cut off! */}
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

// ✅ CORRECT: Dashboard vetting button with proper styling (Fixed 2025-10-05)
<Button
  component="a"
  href="/join"
  color="blue"
  styles={{
    root: {
      fontWeight: 600,
      height: '44px',
      paddingTop: '12px',
      paddingBottom: '12px',
      fontSize: '14px',
      lineHeight: '1.2'
    }
  }}
>
  Submit Vetting Application  {/* Text displays properly! */}
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

### 🚨 CRITICAL PREVENTION FOR NEW BUTTONS:
**THIS IS A RECURRING ISSUE - ENFORCE THESE RULES FOR EVERY NEW BUTTON:**

1. **NEVER create a Mantine Button without the styles object pattern**
2. **ALWAYS include these five properties in styles.root:**
   - `height: '44px'` (explicit height)
   - `paddingTop: '12px'` (explicit top padding)
   - `paddingBottom: '12px'` (explicit bottom padding)
   - `fontSize: '14px'` (consistent font size)
   - `lineHeight: '1.2'` (prevent text cutoff)
3. **DO NOT rely on size prop alone** (`size="sm"`, `size="md"`, etc.)
4. **COPY the correct pattern from this lessons learned file**
5. **TEST button text visibility immediately after creation**

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Button text gets cut off at top and bottom (RECURRING ISSUE)
- ❌ Inconsistent styling across the application
- ❌ Poor user experience with unreadable buttons
- ❌ Mantine design system benefits lost
- ❌ Same bug reported multiple times by users
- ❌ Developer time wasted fixing the same issue repeatedly

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

## 🚨 CRITICAL: MANTINE TIPTAP SETUP REQUIRES TWO FIXES 🚨

### ⚠️ PROBLEM: Tiptap editors not rendering on page - blank spaces where editors should be
**DISCOVERED**: 2025-10-08 - Admin events details page showing no rich text editors after TinyMCE migration

### 🛑 ROOT CAUSES (TWO CRITICAL ISSUES):

#### Issue 1: Incorrect Import for Link Extension
**File**: `MantineTiptapEditor.tsx`
**Problem**: Importing `Link` from wrong package

```typescript
// ❌ BROKEN: Link is not exported from @mantine/tiptap
import { RichTextEditor, Link } from '@mantine/tiptap'
```

**Why This Breaks**:
- `@mantine/tiptap` only exports React components (`RichTextEditor`, `Link` as a toolbar control)
- Tiptap extensions come from `@tiptap/extension-*` packages
- Import error prevents component from loading entirely

#### Issue 2: Missing Mantine Tiptap CSS
**File**: `main.tsx`
**Problem**: Tiptap styles not imported in main entry point

```typescript
// ❌ BROKEN: Missing Tiptap CSS import
import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'
// Missing: @mantine/tiptap/styles.css
import './index.css'
```

**Why This Breaks**:
- Without CSS, editor renders but is invisible or improperly styled
- Toolbar buttons don't display correctly
- Content area has no visual styling

### ✅ CRITICAL SOLUTION (BOTH FIXES REQUIRED):

#### Fix 1: Correct Import Statement
```typescript
// ✅ CORRECT: Import Link extension from correct package
import { RichTextEditor } from '@mantine/tiptap'
import Link from '@tiptap/extension-link'
import { useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Underline from '@tiptap/extension-underline'
import TextAlign from '@tiptap/extension-text-align'
// ... other extensions
```

#### Fix 2: Add Tiptap CSS Import
```typescript
// ✅ CORRECT: Import all required Mantine CSS files
import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'
import '@mantine/tiptap/styles.css'  // ← CRITICAL: Must add this
import './index.css'
```

### 🏗️ COMPLETE MANTINE TIPTAP COMPONENT PATTERN:

```typescript
// MantineTiptapEditor.tsx - Complete working pattern
import React, { useEffect, useImperativeHandle, forwardRef } from 'react'
import { RichTextEditor } from '@mantine/tiptap'
import { useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Underline from '@tiptap/extension-underline'
import Link from '@tiptap/extension-link'
import TextAlign from '@tiptap/extension-text-align'
import Superscript from '@tiptap/extension-superscript'
import Subscript from '@tiptap/extension-subscript'
import Highlight from '@tiptap/extension-highlight'

export interface MantineTiptapEditorProps {
  value?: string
  onChange?: (content: string) => void
  placeholder?: string
  minRows?: number
}

export const MantineTiptapEditor = forwardRef<any, MantineTiptapEditorProps>(
  ({ value = '', onChange, placeholder = 'Enter text...', minRows = 4 }, ref) => {
    const editor = useEditor({
      extensions: [
        StarterKit,
        Underline,
        Link,
        Superscript,
        Subscript,
        Highlight,
        TextAlign.configure({ types: ['heading', 'paragraph'] }),
      ],
      content: value,
      onUpdate: ({ editor }) => {
        const html = editor.getHTML()
        onChange?.(html)
      },
    })

    // Sync external value changes to editor
    useEffect(() => {
      if (editor && value !== editor.getHTML()) {
        editor.commands.setContent(value)
      }
    }, [value, editor])

    if (!editor) {
      return null
    }

    return (
      <RichTextEditor editor={editor}>
        <RichTextEditor.Toolbar sticky stickyOffset={60}>
          <RichTextEditor.ControlsGroup>
            <RichTextEditor.Bold />
            <RichTextEditor.Italic />
            <RichTextEditor.Underline />
            <RichTextEditor.Strikethrough />
          </RichTextEditor.ControlsGroup>

          <RichTextEditor.ControlsGroup>
            <RichTextEditor.H1 />
            <RichTextEditor.H2 />
            <RichTextEditor.H3 />
            <RichTextEditor.H4 />
          </RichTextEditor.ControlsGroup>

          <RichTextEditor.ControlsGroup>
            <RichTextEditor.Link />
            <RichTextEditor.Unlink />
          </RichTextEditor.ControlsGroup>
        </RichTextEditor.Toolbar>

        <RichTextEditor.Content />
      </RichTextEditor>
    )
  }
)
```

### 🔧 MANDATORY SETUP CHECKLIST FOR TIPTAP:

1. **INSTALL packages** in package.json:
   ```json
   "@mantine/tiptap": "^7.17.8",
   "@tiptap/react": "^3.6.5",
   "@tiptap/starter-kit": "^3.3.0",
   "@tiptap/extension-link": "^3.3.0",
   "@tiptap/extension-underline": "^3.3.0",
   "@tiptap/extension-text-align": "^3.3.0",
   "tippy.js": "^6.3.7"
   ```

2. **IMPORT CSS** in main.tsx:
   ```typescript
   import '@mantine/tiptap/styles.css'
   ```

3. **CORRECT imports** in editor component:
   - Import `RichTextEditor` from `@mantine/tiptap`
   - Import extensions from `@tiptap/extension-*` packages
   - Import `useEditor` from `@tiptap/react`

4. **VERIFY build** succeeds after changes

5. **CHECK browser console** for import/CSS errors

### 💥 SYMPTOMS WHEN TIPTAP SETUP IS WRONG:

#### Missing Link Import Fix:
- Console error: "Link is not exported from @mantine/tiptap"
- Component fails to render entirely
- Blank space where editor should be
- Build may succeed but runtime fails

#### Missing CSS Import:
- No console errors
- Editor HTML elements render but are invisible
- Toolbar buttons don't display
- Content area has no styling
- White background missing
- Placeholder text doesn't show

### 🎯 PREVENTION RULES:

1. **ALWAYS import Mantine Tiptap CSS** in main entry point
2. **NEVER import Tiptap extensions from @mantine/tiptap** - use @tiptap/extension-* packages
3. **VERIFY all required packages installed** before using editor
4. **TEST editor rendering** after setup changes
5. **CHECK browser DevTools** for missing CSS or import errors

### 📋 DEBUGGING CHECKLIST:

If Tiptap editor doesn't render:
1. **Check main.tsx** - Is `@mantine/tiptap/styles.css` imported?
2. **Check component imports** - Are extensions imported from correct packages?
3. **Check browser console** - Any import or module errors?
4. **Check Network tab** - Is tiptap CSS file loading?
5. **Check React DevTools** - Is MantineTiptapEditor component mounting?
6. **Verify build** - Run `npm run build` and check for errors

### Tags
#critical #tiptap #mantine #rich-text-editor #css #imports #migration #tinymce

---

## 🚨 CRITICAL: MISSING FIELDS IN API TRANSFORM LAYER BREAK DATA PERSISTENCE 🚨

### ⚠️ PROBLEM: Form fields don't load or persist despite database having data
**DISCOVERED**: 2025-10-17 - Event edit form shortDescription and policies fields immediately blank out after save

### 🔍 SYMPTOMS TO RECOGNIZE THIS ISSUE:
- Input fields/text boxes appear empty even though database has data
- Fields briefly show data then immediately clear/blank out after save
- Rich text editors (TipTap) display empty content despite data in database
- Other fields on the same form work correctly, but specific fields always blank
- Manual page refresh doesn't fix the issue
- Console shows `undefined` or `null` for field values when debugging
- Database queries show data exists, but frontend never displays it

### 🛑 ROOT CAUSE:
- **Manual API response transformation** in frontend stripping out fields
- `transformApiEvent` function in `/apps/web/src/lib/api/hooks/useEvents.ts` had TWO critical issues:
  1. `ApiEvent` interface missing `shortDescription` and `policies` fields
  2. Transform function return object missing these field mappings
- **Result**: Even though database → backend API → network all had the data, the frontend transform layer stripped it out before reaching React components

### 🔍 DEBUGGING SYMPTOM PROGRESSION:
1. User reports: "Fields blank out after save"
2. Check: Database has data ✓
3. Check: Backend API returns data (verified with curl) ✓
4. Check: TypeScript types include fields ✓
5. **Check: Frontend transform layer** ✗ **STRIPS FIELDS OUT**

### ✅ CRITICAL SOLUTION:
**ALWAYS check the manual transform layer when fields are "missing"**

```typescript
// ❌ BROKEN: Transform layer missing fields
interface ApiEvent {
  id: string
  title: string
  description: string
  // Missing: shortDescription
  // Missing: policies
  startDate: string
  location: string
}

function transformApiEvent(apiEvent: ApiEvent): EventDto {
  return {
    id: apiEvent.id,
    title: apiEvent.title,
    description: apiEvent.description,
    // Missing: shortDescription mapping
    // Missing: policies mapping
    startDate: apiEvent.startDate,
    location: apiEvent.location,
  }
}

// ✅ CORRECT: Complete transform with all fields
interface ApiEvent {
  id: string
  title: string
  shortDescription?: string | null  // ADD MISSING FIELD
  description: string
  policies?: string | null  // ADD MISSING FIELD
  startDate: string
  location: string
}

function transformApiEvent(apiEvent: ApiEvent): EventDto {
  return {
    id: apiEvent.id,
    title: apiEvent.title,
    shortDescription: apiEvent.shortDescription || null,  // ADD MAPPING
    description: apiEvent.description,
    policies: apiEvent.policies || null,  // ADD MAPPING
    startDate: apiEvent.startDate,
    location: apiEvent.location,
  }
}
```

### 🔧 MANDATORY DEBUGGING CHECKLIST FOR "MISSING FIELDS":

When fields don't load or persist:
1. **Check database** - Does it have the data? (SQL query)
2. **Check backend API** - Does it return the data? (`curl` the endpoint)
3. **Check generated TypeScript types** - Do they include the fields? (Check `@witchcityrope/shared-types`)
4. **Check frontend transform layer** - Is it mapping the fields? ← **MOST COMMON ISSUE**
5. **Check component code** - Is it using the fields correctly?

### 📍 FILE LOCATIONS TO CHECK:
- **Frontend transform functions**: `/apps/web/src/lib/api/hooks/useEvents.ts` (or similar hooks)
- **API interface definitions**: Look for `interface Api[Entity]` in hooks files
- **Transform function**: Look for `function transformApi[Entity]` in hooks files

### 🎯 PREVENTION RULES:

1. **WHEN ADDING NEW FIELDS TO BACKEND DTOs:**
   - Regenerate TypeScript types immediately
   - Search for transform functions that map this entity
   - Add field to BOTH interface definition AND transform return object
   - Test that field appears in frontend immediately

2. **WHEN FIELDS DON'T PERSIST:**
   - **Don't create workarounds** (like skipNextSync flags)
   - **Trace data flow systematically** from database → API → transform → component
   - **Add console.log** at each layer to find where data is lost
   - **Check the transform layer FIRST** - it's the most common culprit

3. **WHEN CREATING MANUAL TRANSFORM FUNCTIONS:**
   - Document why manual transform is needed (vs generated types)
   - Keep transform interfaces in sync with backend DTOs
   - Add comments for optional fields
   - Test with actual API data, not mocks

### 💥 CONSEQUENCES OF IGNORING:
- ❌ Fields appear to "not save" even though database is updated
- ❌ Hours wasted debugging database, API, and form code
- ❌ Workarounds pollute codebase instead of fixing root cause
- ❌ Same bug reoccurs when new fields are added
- ❌ Users lose data they entered

### 🚨 CRITICAL LESSON:
**"Missing from a list"** means checking the manual transform layer!
When user says "fields are missing from a list somewhere", check:
1. API interface definitions (the input "list")
2. Transform function return objects (the output "list")

Don't spend hours checking database, API endpoints, or generated types first.

### Tags
#critical #data-persistence #api-transform #form-fields #debugging #data-flow

---

## 🚨 NAVIGATION TO PART 2 - MORE LESSONS LEARNED 🚨

**PART 1 COMPLETE** - You have read the startup procedures and critical patterns.

**CONTINUE READING**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-2.md`
**CONTAINS**: Additional lessons learned, more patterns, and detailed solutions
**MANDATORY**: You MUST read Part 2 for complete React developer knowledge
**SIZE**: Part 2 contains up to 1700 additional lines of critical lessons

**REMEMBER**: Make sure this file does not exceed 1500 lines. If it does, then write new lessons to Part 2, not this file!
