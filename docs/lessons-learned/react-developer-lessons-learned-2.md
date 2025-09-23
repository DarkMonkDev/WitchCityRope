# React Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## 📚 MULTI-FILE LESSONS LEARNED
**This is Part 2 of 2**
**Read Part 1 first**: react-developer-lessons-learned.md - **CONTAINS MANDATORY STARTUP PROCEDURE**
**Write to**: Part 2 ONLY
**Max size**: 2,000 lines per file (NOT 2,500)
**IF READ FAILS**: STOP and fix per documentation-standards.md

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using procedure in documentation-standards.md
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

---

## CONTINUED FROM PART 1

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

### ✅ PROVEN WORKING PATTERN (Applied 2025-09-22):
```typescript
// ✅ CORRECT: Button styling that prevents text cutoff
<Button
  style={{
    minHeight: TOUCH_TARGETS.BUTTON_HEIGHT, // Use minHeight, not height (56px)
    paddingTop: 12,                         // Explicit vertical padding
    paddingBottom: 12,                      // Prevents text cutoff
    paddingLeft: 24,                        // Horizontal spacing
    paddingRight: 24,
    fontSize: 16,
    fontWeight: 600,
    lineHeight: 1.4,                        // Proper line spacing for text rendering
  }}
>
  Login to Your Account
</Button>

// ❌ BROKEN: Fixed height without proper padding
<Button style={{ height: 56 }}>Text gets cut off</Button>
```

**KEY PRINCIPLES**:
- **minHeight + padding** instead of fixed height
- **Explicit vertical padding** (12px top/bottom works well)
- **lineHeight: 1.4** ensures proper text rendering space
- **Use TOUCH_TARGETS constants** for consistent sizing across app

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
  color="wcr"
  style={{
    minHeight: 32,
    padding: '6px 12px',
    fontSize: 13,
    fontWeight: 600,
    lineHeight: 1.3
  }}
>
  Copy Event ID
</Button>
```

**Button Size Standards**:
- **compact-sm**: Admin action buttons (Copy, Delete, Quick actions)
- **sm**: Secondary form buttons
- **md**: Primary form buttons, main navigation
- **lg**: Major CTAs, login/register buttons

**CRITICAL**: Always test button text with various lengths - action buttons often have varying text that can overflow.

### Icon Placement Patterns
```typescript
// ✅ CORRECT: Consistent icon sizing and placement
<Button leftSection={<IconCopy size={16} />} size="compact-sm">
  Copy ID
</Button>

// ❌ WRONG: Inconsistent icon patterns
<Button><IconCopy />Copy</Button>
```

**Icon Size Standards**:
- **size={14}**: compact-sm buttons
- **size={16}**: sm/md buttons
- **size={18}**: lg buttons

### Color Scheme Enforcement
```typescript
// ✅ CORRECT: WCR brand colors
<Button color="wcr" variant="filled">Primary Action</Button>
<Button color="gray" variant="outline">Secondary</Button>
<Button color="red" variant="light">Destructive</Button>

// ❌ WRONG: Generic Mantine colors
<Button color="blue">Action</Button>
```

### State Management Patterns
```typescript
// ✅ CORRECT: Comprehensive button states
<Button
  loading={isSubmitting}
  disabled={!isValid || isSubmitting}
  onClick={handleSubmit}
>
  {isSubmitting ? 'Saving...' : 'Save Changes'}
</Button>
```

### Responsive Button Groups
```typescript
// ✅ CORRECT: Mobile-responsive button layout
<Group justify="flex-end" gap="sm" style={{ flexWrap: 'wrap' }}>
  <Button variant="outline" order={{ base: 2, sm: 1 }}>Cancel</Button>
  <Button color="wcr" order={{ base: 1, sm: 2 }}>Save</Button>
</Group>
```

**Mobile Considerations**:
- Primary actions appear first on mobile (order={1})
- Secondary actions follow (order={2})
- Minimum touch target: 44px height
- Adequate spacing between buttons: 8px minimum

### Form Integration Patterns
```typescript
// ✅ CORRECT: Form-aware button behavior
<Button
  type="submit"
  disabled={!form.isValid() || form.isSubmitting()}
  loading={form.isSubmitting()}
  fullWidth={{ base: true, sm: false }}
>
  Submit Application
</Button>
```

**Form Button Rules**:
- Submit buttons use `type="submit"`
- Disable during validation errors
- Show loading during submission
- Full-width on mobile, auto-width on desktop

---

## 🚨 CRITICAL: Admin Context API Parameter Pattern (2025-09-22) 🚨
**Date**: 2025-09-22
**Category**: API Integration
**Severity**: CRITICAL

### What We Learned
**ADMIN-SPECIFIC API PARAMETERS**: When backend adds role-restricted query parameters (like `includeUnpublished`), frontend hooks must be updated to support optional parameters while maintaining backward compatibility.

**PATTERN DISCOVERED**: Admin contexts need different data than public contexts, requiring conditional API parameter passing based on user role/context.

### Action Items
- [x] **ALWAYS parameterize useQuery hooks** to accept optional parameters for admin features
- [x] **UPDATE query keys** to include parameters for proper cache differentiation
- [x] **PASS admin parameters ONLY in admin contexts** - never in public/user-facing pages
- [x] **MAINTAIN backward compatibility** - existing calls without parameters must continue working
- [x] **TEST both contexts** - admin and public - to ensure correct data filtering

### Critical Implementation Pattern:
```typescript
// ✅ CORRECT: Parameterized hook with backward compatibility
export function useEvents(options: { includeUnpublished?: boolean } = {}) {
  return useQuery<EventDto[]>({
    queryKey: queryKeys.events(options),
    queryFn: async (): Promise<EventDto[]> => {
      const params: Record<string, any> = {}
      if (options.includeUnpublished) {
        params.includeUnpublished = true
      }

      const response = await api.get('/api/events', { params })
      const rawEvents = response.data?.data || []
      return autoFixEventFieldNames(rawEvents)
    },
    staleTime: 5 * 60 * 1000,
  })
}

// ✅ CORRECT: Admin context usage
const { data: events } = useEvents({ includeUnpublished: true });

// ✅ CORRECT: Public context usage (unchanged)
const { data: events } = useEvents();

// ✅ CORRECT: Query key with options support
events: (options?: any) => options ? ['events', options] as const : ['events'] as const,
```

### Backend Integration Requirements
1. **Role-based parameter validation** - backend must verify user has Admin role
2. **Graceful parameter handling** - invalid parameters should not break API
3. **Consistent response structure** - same format regardless of parameters
4. **Clear error messages** - if unauthorized, return meaningful 403 response

### Testing Strategy
1. **Test without authentication** - should return published events only
2. **Test with regular user** - should return published events only
3. **Test with admin user + parameter** - should return all events including drafts
4. **Test cache separation** - admin and public queries should cache separately

### Prevention Strategy
- **NEVER hardcode admin-only parameters** in public-facing components
- **ALWAYS check user role/context** before passing admin parameters
- **USE separate query keys** for different parameter combinations
- **COORDINATE with backend** on parameter naming and validation

**CRITICAL SUCCESS**: Admin events page now shows draft events while public pages continue showing only published events.

### Tags
#critical #admin-context #api-parameters #role-based-access #cache-management #backward-compatibility

---

## 🚨 CRITICAL: React App Failed to Mount - Missing Dependency Blocks Entire App

**Problem**: React app completely fails to mount if ANY component has a missing dependency, even if that component isn't used on the current page.

**Root Cause**: Vite's module resolution attempts to load ALL components during the build graph, so a single missing dependency in PayPalButton.tsx blocked the entire app from mounting.

**Symptoms**:
- Empty #root element, blank page
- main.tsx console.log statements never appear
- 500 error for the component with missing dependency
- No error boundary can catch this - app never starts

**Solution**:
1. Always check for missing dependencies FIRST when app won't mount
2. Temporarily replace problematic components with placeholders
3. Use dynamic imports for components with heavy dependencies

**Prevention**: Run `npm ls` to verify all dependencies before deployment

---

*This file is maintained by the react-developer agent. Add new lessons immediately when discovered.*