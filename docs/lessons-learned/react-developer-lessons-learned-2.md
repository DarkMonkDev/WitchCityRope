# React Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 NAVIGATION: LESSONS LEARNED SPLIT FILES 🚨

**FILES**: 3 total
**Part 1**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned.md` (STARTUP + CRITICAL PATTERNS)
**Part 2**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-2.md` (THIS FILE - CONTINUED LESSONS)
**Part 3**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-3.md` (MORE LESSONS)
**Read ALL**: All three parts are MANDATORY
**Write to**: Part 3 for new lessons (NOT THIS FILE)
**Maximum file size**: 1700 lines (to stay under token limits). All three parts can be up to 1700 lines each

## 🚨 ULTRA CRITICAL: ADD NEW LESSONS TO PART 3, NOT PART 1 OR PART 2! 🚨

**PART 1 IS FOR STARTUP** - Keep Part 1 under 1700 lines for startup procedures
**PART 2 IS FOR CORE LESSONS** - This file is now frozen at 1730 lines
**ALL NEW LESSONS GO TO PART 3** - Do not add to Part 1 or Part 2!

---

**Skills Usage**: See `/.claude/skills/HOW-TO-USE-SKILLS.md` for complete guide on when/how to use skills

---

## 🚨🚨🚨 STOP! READ THIS FIRST - MOST COMMON VIOLATION 🚨🚨🚨

### ⛔ NEVER CREATE MANUAL TypeScript INTERFACES FOR API DATA ⛔

**This is THE most frequently violated rule. If you do NOTHING else, do NOT violate this!**

**WRONG** (causes hours of debugging):
```typescript
// ❌ CREATING INTERFACES FOR API DATA
interface UserDto { ... }
interface EventDto { ... }
interface SessionDto { ... }
```

**CORRECT** (always works):
```typescript
// ✅ IMPORT FROM AUTO-GENERATED TYPES
import type { components } from '@witchcityrope/shared-types';
export type UserDto = components['schemas']['UserDto'];
```

**Why this matters**:
- Manual interfaces cause 393+ TypeScript errors
- Backend changes break frontend silently
- You waste hours fixing type mismatches
- Violates core architecture (DTO Alignment Strategy)

**Full details**: See lesson "ULTRA CRITICAL: NEVER MANUALLY DEFINE API TYPES" below (line 408+)

**IF YOU SEE THIS PATTERN IN CODE**: Fix it immediately before doing ANY other work!

---

## 🚨 CRITICAL: REACT ROUTER SCROLLRESTORATION REVERSE NAVIGATION BUG 🚨
**Date**: 2025-11-13
**Category**: React Router v7 / ScrollRestoration / Navigation
**Severity**: CRITICAL - REVERSE NAVIGATION SCROLLS TO WRONG POSITION

### What We Learned
**SCROLLRESTORATION WITHOUT getKey BREAKS REVERSE NAVIGATION**: React Router's `<ScrollRestoration />` component without a `getKey` function fails to scroll to top on reverse navigation (Events → Homepage), landing at 614px instead of 0px.

**ROOT CAUSE**: React Router's ScrollRestoration component uses an internal algorithm to decide when to:
1. Scroll to top (new navigation)
2. Restore previous scroll position (browser back/forward)

Without a `getKey` function, the component can't reliably detect route changes during reverse navigation, causing it to:
- Incorrectly restore a scroll position that doesn't exist
- Skip the scroll-to-top behavior
- Land at a seemingly random offset (614px in this case)

**TEST EVIDENCE**:
```
Test: "scrolls to top when navigating from events to homepage - DESKTOP"
Expected: scrollY <= 10px
Actual: scrollY = 614px ❌

Forward navigation (Homepage → Events): 0px ✅ WORKS
Reverse navigation (Events → Homepage): 614px ❌ BROKEN
```

Adding waits made it WORSE (93px → 614px), confirming this is NOT a timing issue.

### 🛑 BROKEN PATTERN:

```typescript
// ❌ WRONG: ScrollRestoration without getKey
import { ScrollRestoration } from 'react-router-dom';

export const RootLayout: React.FC = () => {
  return (
    <Box>
      <ScrollRestoration />  {/* No getKey - reverse navigation broken! */}
      <Navigation />
      <Outlet />
      <Footer />
    </Box>
  );
};
```

**Why This Breaks**:
1. Forward navigation (Homepage → Events) works by chance
2. Reverse navigation (Events → Homepage) fails - scrolls to 614px
3. React Router can't reliably detect route changes
4. Scroll restoration behavior becomes unpredictable
5. Different routes may have different bugs

### ✅ CRITICAL SOLUTION: Add getKey Function

```typescript
// ✅ CORRECT: ScrollRestoration with getKey based on pathname
import { ScrollRestoration, useLocation } from 'react-router-dom';

export const RootLayout: React.FC = () => {
  const location = useLocation();

  return (
    <Box>
      {/* CRITICAL FIX: getKey function ensures scroll-to-top on ALL navigation
          Without this, reverse navigation (Events → Homepage) scrolls to 614px instead of 0px
          The key based on pathname forces React Router to recognize route change
          and scroll to top instead of trying to restore previous scroll position

          Bug fixed: Desktop reverse navigation now scrolls to 0px correctly
          See: /tests/playwright/scroll-restoration.spec.ts (line 225-275)
      */}
      <ScrollRestoration
        getKey={(location) => {
          // Use pathname as key to force scroll-to-top on route changes
          // This ensures both forward (Homepage → Events) and reverse (Events → Homepage)
          // navigation always scroll to top of page (Y = 0)
          return location.pathname;
        }}
      />
      <Navigation />
      <Outlet />
      <Footer />
    </Box>
  );
};
```

### 📋 MANDATORY PATTERN FOR SCROLLRESTORATION:

**Step 1: Always include getKey function**
```typescript
<ScrollRestoration
  getKey={(location) => location.pathname}
/>
```

**Step 2: Use pathname for standard scroll-to-top behavior**
```typescript
// For most apps: pathname is sufficient
getKey={(location) => location.pathname}
```

**Step 3: Add location key for query param changes**
```typescript
// If query params should trigger scroll-to-top:
getKey={(location) => location.pathname + location.search}
```

**Step 4: Custom keys for special cases**
```typescript
// For apps with complex scroll restoration needs:
getKey={(location, matches) => {
  // Use pathname for most routes
  const paths = ["/", "/events", "/about"];
  if (paths.includes(location.pathname)) {
    return location.pathname;
  }
  // Use full location key for dynamic routes
  return location.pathname + location.search + location.hash;
}}
```

### 🔧 WHEN THIS PATTERN APPLIES:

**ALWAYS use getKey with ScrollRestoration**:
- Every React Router v7 application
- Any app using ScrollRestoration component
- Both forward and reverse navigation scenarios
- Desktop and mobile viewports

**SYMPTOMS of missing getKey:**
- Forward navigation works, reverse navigation broken
- Scroll position lands at seemingly random offset
- Different scroll behavior for different routes
- Adding waits doesn't fix the issue (not timing-related)
- Playwright tests show exact scroll offset (614px, 93px, etc.)

### 💥 CONSEQUENCES OF IGNORING:

- Reverse navigation scrolls to wrong position (614px instead of 0px)
- Poor user experience - page doesn't start at top
- Inconsistent scroll behavior across different routes
- Users must manually scroll to top after navigation
- E2E tests fail with scroll position assertions

### 🎯 PREVENTION RULES:

1. NEVER use `<ScrollRestoration />` without `getKey` function
2. ALWAYS add `getKey={(location) => location.pathname}` as minimum
3. TEST both forward and reverse navigation routes
4. VERIFY scroll position is at top (Y = 0) after navigation
5. CHECK Playwright tests for scroll-related failures

### 📚 FILES AFFECTED:

- `/apps/web/src/components/layout/RootLayout.tsx` - Lines 38-45 (ScrollRestoration with getKey)
- `/tests/playwright/scroll-restoration.spec.ts` - Lines 225-275 (Desktop reverse navigation test)

### 📖 REACT ROUTER DOCUMENTATION REFERENCE:

**ScrollRestoration API**:
- `getKey`: Function that returns a key for scroll position storage
- Without `getKey`: Uses default algorithm (unreliable for some routes)
- With `getKey`: Forces explicit scroll behavior based on key

**Common getKey patterns**:
```typescript
// Scroll to top on any route change
getKey={(location) => location.pathname}

// Scroll to top on pathname or query param change
getKey={(location) => location.pathname + location.search}

// Never restore scroll (always top)
getKey={() => "top"}

// Restore scroll on back/forward, top on new navigation
getKey={(location, matches) => {
  // React Router default behavior - but explicit
  return matches.map(m => m.pathname).join("-");
}}
```

### Tags
#critical #react-router-v7 #scroll-restoration #navigation #reverse-navigation #getkey #scroll-to-top #e2e-testing

---

## 🚨 CRITICAL: MANTINE STACK INTERCEPTS POINTER EVENTS IN MOBILE MENU 🚨
**Date**: 2025-11-13
**Category**: Mobile Navigation / Mantine Components / Pointer Events
**Severity**: CRITICAL - BLOCKS ALL MOBILE MENU INTERACTIONS

### What We Learned
**MANTINE STACK BLOCKS MOBILE MENU CLICKS**: The Mantine Stack component in the mobile navigation menu was intercepting pointer events, preventing ALL links from being clickable on mobile devices.

**ROOT CAUSE**: Mantine's Stack component creates a layout container that can intercept pointer events. When Stack has padding/styling, it creates an event-capturing layer that blocks child elements from receiving clicks.

**PLAYWRIGHT TEST EVIDENCE**:
```
Error: <div class="m_6d731127 mantine-Stack-root">…</div> from
<div id="mobile-menu" role="navigation" class="mobile-menu open">…</div>
subtree intercepts pointer events

Attempted to click 53+ times - all failed
```

### 🛑 BROKEN PATTERN:

```typescript
// ❌ WRONG: Stack without pointer events configuration blocks clicks
<Stack gap="0" p="var(--space-lg)" pt="80px">
  <Box component={Link} to="/events" onClick={closeMobileMenu}>
    Events & Classes
  </Box>
  {/* Links are NOT clickable - Stack intercepts events */}
</Stack>
```

**Why This Breaks**:
1. Stack component creates event-capturing layer
2. Padding/styling on Stack strengthens the capture
3. Child links never receive click events
4. Playwright test shows "subtree intercepts pointer events"
5. Mobile users cannot navigate anywhere from menu

### ✅ CRITICAL SOLUTION: Disable Pointer Events on Stack, Re-enable on Children

```typescript
// ✅ CORRECT: Block events on Stack, allow on children
<Stack
  gap="0"
  p="var(--space-lg)"
  pt="80px"
  style={{
    /* CRITICAL FIX: Ensure pointer events pass through to child links
     * Without this, Mantine Stack intercepts pointer events and prevents
     * links from being clickable on mobile menu (Playwright test failure)
     */
    pointerEvents: 'none',
  }}
>
  {/* Button with explicit pointer events */}
  <Button
    component={Link}
    to="/dashboard"
    onClick={closeMobileMenu}
    styles={{
      root: {
        // ... other styles
        pointerEvents: 'auto', // Re-enable pointer events for clickability
      },
    }}
  >
    Dashboard
  </Button>

  {/* Box link with explicit pointer events */}
  <Box
    component={Link}
    to="/events"
    onClick={closeMobileMenu}
    style={{
      // ... other styles
      pointerEvents: 'auto', // Re-enable pointer events for clickability
    }}
  >
    Events & Classes
  </Box>
</Stack>
```

### 📋 MANDATORY PATTERN FOR MANTINE STACK WITH CLICKABLE CHILDREN:

**Step 1: Disable pointer events on Stack**
```typescript
<Stack style={{ pointerEvents: 'none' }}>
```

**Step 2: Re-enable on ALL clickable children**
```typescript
// For Mantine Buttons
<Button
  styles={{
    root: {
      pointerEvents: 'auto',
    }
  }}
/>

// For Box links
<Box
  component={Link}
  style={{
    pointerEvents: 'auto',
  }}
/>
```

### 🔧 WHEN THIS PATTERN APPLIES:

**USE this pattern when:**
- Stack contains clickable links or buttons
- Stack has padding, margins, or styling
- Mobile menu or navigation component
- ANY layout where Stack wraps interactive elements

**SYMPTOMS of this issue:**
- Playwright error: "subtree intercepts pointer events"
- Links visible but not clickable on mobile
- Clicks appear to work on desktop but fail on mobile viewport
- Test tries 50+ times to click but never succeeds

### 💥 CONSEQUENCES OF IGNORING:

- Mobile menu completely unusable
- Users cannot navigate on mobile devices
- Appears as catastrophic UX failure
- Support tickets about "broken mobile site"
- Lost mobile traffic

### 🎯 PREVENTION RULES:

1. ALWAYS add `pointerEvents: 'none'` to Stack components with clickable children
2. ALWAYS add `pointerEvents: 'auto'` to EVERY clickable child
3. TEST mobile menu clicks with Playwright after any Stack changes
4. CHECK for "subtree intercepts pointer events" error in test failures

### 📚 FILES AFFECTED:

- `/apps/web/src/components/layout/Navigation.tsx` - Lines 251-454 (Mobile menu Stack and all links)

### Tags
#critical #mobile-menu #mantine-stack #pointer-events #navigation #mobile-ux #playwright-testing

---

## 🛑🛑🛑 ULTRA CRITICAL - MOST VIOLATED RULE: NEVER MANUALLY DEFINE API TYPES - ALWAYS USE @witchcityrope/shared-types 🛑🛑🛑
### ⚠️ THIS VIOLATION HAPPENS CONSTANTLY - STOP DOING IT! ⚠️
**Date**: 2025-10-23
**Category**: TypeScript / DTO Alignment Strategy
**Severity**: ULTRA CRITICAL - PREVENTS 393+ TYPE ERRORS

### What We Learned
**MANUAL API TYPE DEFINITIONS CAUSE 393+ TYPESCRIPT ERRORS**: During React migration (August 2025), manually-created TypeScript interfaces in `/apps/web/src/types/api.types.ts` didn't match backend C# DTOs, causing **393 TypeScript compilation errors**.

**ROOT CAUSE**: Manual interface definitions drift from backend DTOs when:
1. Backend developer changes C# DTO structure
2. Frontend developer doesn't know about the change
3. No automatic synchronization between backend and frontend
4. Manual updates forgotten or incorrect

### 🛑 CRITICAL VIOLATION PATTERN:

```typescript
// ❌ WRONG: Manual interface definition
// /apps/web/src/types/api.types.ts
export interface UserDto {
  id?: string;
  email?: string;
  sceneName?: string | null;
  firstName?: string | null;  // Might not exist in backend!
  lastName?: string | null;   // Might not exist in backend!
  roles?: string[];           // Backend uses string 'role', not array 'roles'!
}
```

**Why This Breaks**:
- Backend adds/removes/renames fields → Frontend not updated
- TypeScript thinks interface is valid → Runtime failures
- 393 compilation errors during migration from manual interfaces
- Hours wasted debugging type mismatches

### ✅ CRITICAL SOLUTION: USE GENERATED TYPES FROM @witchcityrope/shared-types

```typescript
// ✅ CORRECT: Import from generated types package
// /apps/web/src/types/api.types.ts
import type { components } from '@witchcityrope/shared-types';

/**
 * User Data Transfer Object
 * Source: C# UserDto via NSwag generation
 */
export type UserDto = components['schemas']['UserDto'];

/**
 * Event Data Transfer Object
 * Source: C# EventDto via NSwag generation
 */
export type EventDto = components['schemas']['EventDto'];
```

### 📋 MANDATORY PATTERN FOR ALL API TYPES:

**Step 1: Import from generated types**
```typescript
import type { components } from '@witchcityrope/shared-types';
```

**Step 2: Re-export with JSDoc**
```typescript
/**
 * [Type Name]
 * Source: C# [DTO Name] via NSwag generation
 */
export type [TypeName] = components['schemas']['[SchemaName]'];
```

**Step 3: Add comments for complex types**
```typescript
/**
 * API Response wrapper for list of EventDto
 * Source: C# ApiResponse<List<EventDto>> via NSwag generation
 */
export type ApiResponseOfListOfEventDto = components['schemas']['ApiResponseOfListOfEventDto'];
```

### 🔧 TYPE GENERATION WORKFLOW:

**When backend DTOs change:**

1. **Backend Developer**: Modify C# DTOs in `/apps/api/Features/*/Models/`
2. **Generate Types**: `cd packages/shared-types && npm run generate`
3. **Frontend Developer**: Types automatically updated (no manual work!)
4. **Test**: TypeScript compiler catches any breaking changes

### 🎯 WHAT TO RE-EXPORT VS KEEP MANUAL:

**RE-EXPORT from generated types** (API data contracts):
- ✅ `UserDto`, `EventDto`, `EventParticipationDto` - Backend DTOs
- ✅ `ApiResponseOfListOfEventDto` - Backend response wrappers
- ✅ `UpdateEventRequest`, `CreateUserRequest` - Backend request models
- ✅ `PagedResultOf*`, `UserListResponse` - Backend pagination types
- ✅ `ParticipationStatus`, `PaymentStatus` - Backend enums
- ✅ `ProblemDetails`, `ValidationProblemDetails` - Backend error types

**KEEP MANUAL** (frontend-only logic):
- ✅ `EventFilters` - Frontend filtering logic (not sent to API)
- ✅ `CreateEventData` - Frontend form structure (if different from backend)
- ✅ `PaginatedResponse<T>` - Generic convenience type (supplement to specific types)
- ✅ `ApiResponse<T>` - Generic convenience type (supplement to specific types)

### 🚨 REFERENCE IMPLEMENTATION - VETTING TYPES (GOLD STANDARD):

**File**: `/apps/web/src/features/admin/vetting/types/vetting.types.ts`

```typescript
// ✅ CORRECT: This is the pattern ALL API types should follow
import type { components } from '@witchcityrope/shared-types';

// Re-export API types for convenience
export type ApplicationSummaryDto = components['schemas']['ApplicationSummaryDto'];
export type ApplicationReferenceStatus = components['schemas']['ApplicationReferenceStatus'];
export type ApplicationDetailResponse = components['schemas']['ApplicationDetailResponse'];
export type ApplicationStatusResponse = components['schemas']['ApplicationStatusResponse'];
// ... more re-exports
```

This is **100% compliant** with DTO Alignment Strategy.

### 🚨 PRIORITY 1 VIOLATION - FIXED OCTOBER 2025:

**File**: `/apps/web/src/types/api.types.ts`
**Before**: 106 lines of manual interfaces (VIOLATION)
**After**: 313 lines of generated type re-exports (COMPLIANT)

**TODO Comment that triggered fix**:
```typescript
// TODO: Use generated types from @witchcityrope/shared-types when package is available
// Temporarily using inline types to fix import failures
```

This comment sat in the codebase for **2+ months** acknowledging the violation but not fixing it.

### 💥 CONSEQUENCES OF MANUAL API TYPES:

- ❌ **393 TypeScript errors** during React migration (August 2025)
- ❌ Hours wasted debugging type mismatches
- ❌ Runtime failures when types don't match API
- ❌ Duplicate maintenance effort (backend + frontend)
- ❌ High risk of inconsistency between systems
- ❌ Architecture violation (DTO Alignment Strategy)

### ✅ BENEFITS OF GENERATED TYPES:

- ✅ **Zero TypeScript errors** from type mismatches
- ✅ Automatic synchronization with backend
- ✅ Zero manual maintenance burden
- ✅ 100% type safety guarantee
- ✅ Single source of truth (C# DTOs)
- ✅ Architecture compliance

### 📋 MANDATORY CHECKLIST FOR NEW API TYPES:

When you need to use a new backend type:

1. **Check generated types** - `packages/shared-types/src/generated/api-types.ts`
2. **If type exists** - Re-export it from `components['schemas'][...]`
3. **If type missing** - Backend needs to add OpenAPI annotations
4. **Regenerate types** - `cd packages/shared-types && npm run generate`
5. **NEVER create manual interface** for API data

### 🛑 CODE REVIEW RED FLAGS:

**Watch for these patterns in PRs:**

```typescript
// ❌ RED FLAG: Manual interface for API data
export interface UserDto {
  // If this comes from API, it MUST be generated!
}

// ❌ RED FLAG: Manual DTO creation
export interface EventResponse {
  // Check if this exists in generated types first!
}

// ❌ RED FLAG: Duplicating backend enums
export type EventStatus = 'Draft' | 'Published' | 'Cancelled';
// Use generated enum instead!
```

**Correct patterns:**
```typescript
// ✅ GREEN FLAG: Import from generated types
import type { components } from '@witchcityrope/shared-types';

// ✅ GREEN FLAG: Re-export with documentation
export type UserDto = components['schemas']['UserDto'];

// ✅ GREEN FLAG: Frontend-only type (not API data)
export interface EventFilters {
  // This is frontend logic, not backend data
  search?: string;
  startDate?: string;
}
```

### 🚨 ENFORCEMENT - MANDATORY PRE-WORK CHECK:

**BEFORE writing ANY React code that uses API data:**

1. **ASK**: "Does this data come from the backend API?"
2. **IF YES**: Check `packages/shared-types/src/generated/api-types.ts`
3. **IF TYPE EXISTS**: Re-export it using `components['schemas'][...]`
4. **IF TYPE MISSING**: Backend needs to add it first (with OpenAPI annotations)
5. **NEVER PROCEED** with manual interface creation

**Hard stop rule**: If you catch yourself typing `interface [Name]Dto {`, STOP IMMEDIATELY. You are about to violate this rule.

**Detection pattern**:
- Any file with `interface` + `Dto`/`Response`/`Request` in same definition = RED FLAG
- Any file importing from backend that doesn't import from `@witchcityrope/shared-types` = RED FLAG
- Any manual definition of backend enum values = RED FLAG

**When in doubt**: ASK the user "Should I use generated types from @witchcityrope/shared-types for [TypeName]?"

### 📚 RELATED DOCUMENTATION:

**CRITICAL - MUST READ**:
- `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` - Core principles
- `/docs/architecture/react-migration/domain-layer-architecture.md` - NSwag implementation

**REFERENCE IMPLEMENTATION**:
- `/apps/web/src/features/admin/vetting/types/vetting.types.ts` - Gold standard
- `/apps/web/src/types/api.types.ts` - Complete example (fixed October 2025)

**IMPLEMENTATION SUMMARY**:
- `/session-work/2025-10-23/api-types-dto-alignment-migration-summary.md` - Detailed migration documentation

### Tags
#ultra-critical #dto-alignment #typescript #api-types #nswag #type-generation #architecture-compliance #393-errors-prevented

---

## 🚨🚨🚨 ULTRA CRITICAL: TIPTAP EDITOR KEY PROP CAUSES REMOUNTING AND FOCUS LOSS 🚨🚨🚨
**Date**: 2025-10-17
**Category**: TipTap Rich Text Editor / React State Management
**Severity**: ULTRA CRITICAL - MAKES FORM UNUSABLE

### What We Learned
**TIPTAP EDITORS BLANKING AFTER SAVE**: EventForm TipTap editors (shortDescription, policies, fullDescription) were blanking after save and losing focus on every keystroke.

**ROOT CAUSE**: Dynamic `key` props using content substring caused React to unmount and remount editors on every keystroke:

```typescript
// ❌ BROKEN: Key changes on EVERY keystroke, forcing remount
<MantineTiptapEditor
  key={`fullDescription-${form.values.fullDescription?.substring(0, 50) || 'empty'}`}
  value={form.values.fullDescription}
  onChange={(content) => form.setFieldValue('fullDescription', content)}
/>

<MantineTiptapEditor
  key={`policies-${form.values.policies?.substring(0, 50) || 'empty'}`}
  value={form.values.policies}
  onChange={(content) => form.setFieldValue('policies', content)}
/>
```

**Why This Completely Breaks**:
1. User types character in `policies` field
2. `onChange` fires → `form.setFieldValue('policies', content)` updates form state
3. `form.values.policies` changes → EventForm re-renders
4. **Key prop changes** (because substring of content changed) → React sees different key
5. **React unmounts old editor** → Focus lost, content appears to vanish
6. **React mounts new editor** with updated value prop → New editor appears
7. **User must click back in** to continue typing → UNUSABLE UX

**Previous "Fix" Made It WORSE**: Commit f92d6fc8 added these dynamic key props thinking it would help with remounting. It did the opposite - forced remounting on EVERY keystroke.

### ✅ CRITICAL SOLUTION:

#### Fix 1: Remove Dynamic Key Props
```typescript
// ✅ CORRECT: No key prop - let TipTap manage its own state
<MantineTiptapEditor
  value={form.values.fullDescription}
  onChange={(content) => form.setFieldValue('fullDescription', content)}
  minRows={10}
  placeholder="Enter detailed event description..."
/>

<MantineTiptapEditor
  value={form.values.policies}
  onChange={(content) => form.setFieldValue('policies', content)}
  minRows={5}
  placeholder="Enter policies and procedures..."
/>
```

#### Fix 2: Improve useEffect in MantineTiptapEditor
The `useEffect` that syncs props to editor content was comparing HTML strings naively, causing unnecessary updates and focus loss:

```typescript
// ❌ BROKEN: Simple string comparison causes issues
useEffect(() => {
  if (editor && value !== editor.getHTML()) {
    editor.commands.setContent(value)
  }
}, [value, editor])

// ✅ CORRECT: Normalized comparison + focus preservation
useEffect(() => {
  if (!editor) return

  const currentContent = editor.getHTML()

  // Normalize HTML for comparison (remove extra whitespace, normalize tags)
  const normalize = (html: string) => html?.trim().replace(/\s+/g, ' ') || ''
  const normalizedValue = normalize(value)
  const normalizedCurrent = normalize(currentContent)

  // Only update if content has actually changed
  if (normalizedValue !== normalizedCurrent) {
    // Prevent cursor jump by checking if editor is focused
    const isFocused = editor.isFocused

    editor.commands.setContent(value, false) // false = don't emit update event

    // Restore focus if editor was focused
    if (isFocused) {
      editor.commands.focus('end')
    }
  }
}, [value, editor])
```

### 🛑 NEVER USE DYNAMIC KEYS WITH CONTROLLED INPUTS

**CRITICAL RULE**: NEVER use dynamic key props with controlled form inputs (including TipTap editors):

```typescript
// ❌ WRONG: Dynamic keys cause remounting
<MantineTiptapEditor
  key={`field-${value}`}  // Changes on every update!
  key={`editor-${value?.substring(0, 10)}`}  // Changes on every keystroke!
  key={`${someState}-${otherState}`}  // Changes frequently!
/>

// ❌ WRONG: Even stable-looking keys can cause issues
<MantineTiptapEditor
  key={`editor-${initialValue}`}  // If initialValue changes, editor remounts!
/>

// ✅ CORRECT: No key prop for controlled inputs
<MantineTiptapEditor
  value={form.values.field}
  onChange={(content) => form.setFieldValue('field', content)}
/>

// ✅ CORRECT: Static key only when you have multiple editors for truly different data
<MantineTiptapEditor
  key="policies-editor"  // Static string that never changes
  value={form.values.policies}
  onChange={(content) => form.setFieldValue('policies', content)}
/>
```

### 📋 WHEN TO USE KEY PROPS WITH EDITORS:

**DO use key prop when:**
- Switching between completely different data sources (e.g., different emails templates)
- Need to force remount when switching between entirely different entities (e.g., editing event A vs event B)
- Key value comes from entity ID that changes infrequently

**DO NOT use key prop when:**
- Editor is controlled by form state
- Content changes frequently during typing
- Key would change on every keystroke or update
- Using with `onChange` handlers that update parent state

### 🔧 TIPTAP CONTROLLED COMPONENT PATTERN:

```typescript
// ✅ CORRECT: TipTap controlled component pattern
export const MyForm: React.FC = () => {
  const form = useForm({
    initialValues: {
      description: '',
      policies: ''
    }
  })

  return (
    <>
      {/* NO KEY PROP - Editor manages its own internal state */}
      <MantineTiptapEditor
        value={form.values.description}
        onChange={(content) => form.setFieldValue('description', content)}
      />

      <MantineTiptapEditor
        value={form.values.policies}
        onChange={(content) => form.setFieldValue('policies', content)}
      />
    </>
  )
}
```

### 🚨 SYMPTOMS OF KEY PROP REMOUNTING BUG:

1. **Focus jumps out** of editor on every keystroke
2. **Cursor position lost** after typing
3. **Content appears to blank** briefly then reappear
4. **Typing feels sluggish** or unresponsive
5. **Data doesn't persist** after form submission
6. **Console shows component mount/unmount** on every state change

### 💥 FILES AFFECTED:

- `/apps/web/src/components/events/EventForm.tsx` - Lines 559, 582 (Removed dynamic key props)
- `/apps/web/src/components/forms/MantineTiptapEditor.tsx` - Lines 210-234 (Improved useEffect)

### VERIFICATION CHECKLIST:

1. **Type in fullDescription editor** - Focus should NOT jump, cursor should stay in place
2. **Type in policies editor** - No focus loss, smooth typing experience
3. **Type rapidly** - Editor should keep up without lag or remounting
4. **Save form** - All editor content should persist correctly
5. **Edit existing event** - Editors should load with content and allow editing

### 💥 CONSEQUENCES OF IGNORING:

- ❌ Forms completely unusable - users cannot type more than one character at a time
- ❌ Users lose content they typed
- ❌ Extremely poor UX - appears as if application is broken
- ❌ Support tickets about "form not working"
- ❌ Lost user trust in application

### Tags
#ultra-critical #tiptap #rich-text-editor #key-props #remounting #focus-loss #unusable-form #mantine-tiptap #react-controlled-components

---

---

## 🚨 CRITICAL: MANTINE RESPONSIVE PROPS DON'T WORK WITH PLAYWRIGHT 🚨
**Date**: 2025-10-17
**Category**: Playwright Testing / Mantine Responsive Design
**Severity**: CRITICAL - BREAKS MOBILE TESTING

### What We Learned
**MANTINE RESPONSIVE PROPS FAIL WITH PLAYWRIGHT**: Using `hiddenFrom`, `visibleFrom`, or `useMediaQuery` hook does NOT work when Playwright changes viewport size with `page.setViewportSize()`.

**ROOT CAUSE**: These props rely on CSS media queries that Playwright doesn't trigger properly during programmatic viewport changes.

**TEST EVIDENCE**:
```
✅ Viewport set to mobile (375×667)
✅ Edit button visible on mobile
Button position: relative  ← WRONG! Should be "fixed" for FAB
❌ FAB not rendering - desktop button showing on mobile
```

The test showed the desktop button (with `position: relative`) was rendering on mobile instead of the FAB (with `position: fixed`), proving Mantine's responsive props didn't work.

### ❌ BROKEN PATTERNS

```typescript
// ❌ WRONG: Mantine responsive props don't work with Playwright
<ActionIcon
  onClick={onClick}
  size={56}
  hiddenFrom="md"  // Doesn't work with Playwright!
  data-testid="cms-edit-fab"
>
  <IconEdit />
</ActionIcon>

<Button
  onClick={onClick}
  visibleFrom="md"  // Doesn't work with Playwright!
  data-testid="cms-edit-button"
>
  Edit
</Button>

// ❌ WRONG: useMediaQuery hook doesn't update with Playwright
const isMobile = useMediaQuery('(max-width: 768px)') // Stays at initial value!

if (isMobile) {
  return <MobileFAB />
}
```

### ✅ CORRECT SOLUTION: Prop-Based Conditional Rendering

```typescript
// ✅ CORRECT: Parent component detects viewport and passes as prop
import { useViewportSize } from '@mantine/hooks'

export const ParentComponent: React.FC = () => {
  const { width: viewportWidth } = useViewportSize()

  return <CmsEditButton onClick={handleEdit} viewportWidth={viewportWidth} />
}

// ✅ CORRECT: Child component uses prop for conditional rendering
interface CmsEditButtonProps {
  onClick: () => void
  viewportWidth?: number
}

export const CmsEditButton: React.FC<CmsEditButtonProps> = ({
  onClick,
  viewportWidth = typeof window !== 'undefined' ? window.innerWidth : 1024
}) => {
  const isMobile = viewportWidth < 768

  // CRITICAL: Explicit conditional rendering, NOT CSS media queries
  if (isMobile) {
    // Mobile FAB
    return (
      <ActionIcon
        onClick={onClick}
        size={56}
        style={{
          position: 'fixed',  // MUST be fixed for FAB
          bottom: 24,
          right: 24,
          zIndex: 999999
        }}
        data-testid="cms-edit-fab"
      >
        <IconEdit size={24} />
      </ActionIcon>
    )
  }

  // Desktop button
  return (
    <Button
      onClick={onClick}
      style={{ position: 'sticky', top: 16 }}
      data-testid="cms-edit-button"
    >
      Edit
    </Button>
  )
}
```

### 🛑 NEVER USE WITH PLAYWRIGHT TESTS

**Avoid these Mantine features for components tested with Playwright:**

1. ❌ `hiddenFrom` prop
2. ❌ `visibleFrom` prop
3. ❌ `useMediaQuery` hook (for responsive logic)
4. ❌ CSS media queries for show/hide behavior
5. ❌ `@media` queries in inline styles

### ✅ ALWAYS USE FOR PLAYWRIGHT TESTS

**Use these patterns instead:**

1. ✅ `useViewportSize()` hook from `@mantine/hooks` in **parent component**
2. ✅ Pass viewport width as **prop** to child components
3. ✅ **Explicit conditional rendering** (`if/else` or ternary)
4. ✅ **JavaScript-based logic** for responsive behavior
5. ✅ Verify computed styles in tests (`position: fixed` vs `relative`)

### 🔧 DEBUGGING CHECKLIST

When responsive components don't work in Playwright tests:

1. **Check test output** - What's the computed style? (`position`, `display`)
2. **Verify button identity** - Is correct button rendering? (Check `data-testid`)
3. **Remove Mantine responsive props** - Replace with prop-based rendering
4. **Pass viewport width** from parent using `useViewportSize()` hook
5. **Use explicit conditionals** - `if (isMobile)` instead of CSS
6. **Test position style** - Verify FAB has `position: fixed`, not `relative`

### 📋 FILES AFFECTED

- `/apps/web/src/features/cms/components/CmsEditButton.tsx` - Fixed with prop-based rendering
- `/apps/web/src/features/cms/components/CmsPage.tsx` - Added useViewportSize hook

### 💥 CONSEQUENCES OF IGNORING

- ❌ Mobile UI tests fail in Playwright
- ❌ Wrong components render on mobile viewports
- ❌ Hours wasted debugging CSS media queries
- ❌ False confidence in desktop-only testing
- ❌ Mobile UI bugs ship to production

### 🎯 REUSABLE PATTERN

**Use this pattern for ANY responsive component tested with Playwright:**

```typescript
// Step 1: Parent gets viewport width
const { width } = useViewportSize()

// Step 2: Pass to child as prop
<ResponsiveComponent viewportWidth={width} />

// Step 3: Child uses explicit conditional
const isMobile = viewportWidth < 768
if (isMobile) return <MobileVersion />
return <DesktopVersion />
```

### Tags
#critical #playwright #mantine #responsive-design #testing #mobile-testing #viewport #media-queries #fab-button

---

## 🚨 CRITICAL: REQUIRED ATTRIBUTE ON HIDDEN FORM FIELDS BLOCKS SUBMISSION 🚨
**Date**: 2025-10-26
**Category**: HTML5 Form Validation / Mantine Forms
**Severity**: CRITICAL - MAKES FORMS COMPLETELY UNUSABLE

### What We Learned
**HIDDEN REQUIRED FIELDS BLOCK FORM SUBMISSION**: Form fields with `required` attribute inside collapsed/hidden sections prevent form submission with cryptic browser errors.

**ERROR SYMPTOMS**:
```
An invalid form control with name='' is not focusable.
<input ... required aria-invalid="false" value ...>
```

**ROOT CAUSE**: Browser's native HTML5 validation tries to validate ALL `required` fields on the entire page, even those hidden in collapsed sections or modal dialogs. When validation fails, browser tries to focus the invalid field but can't because it's hidden, blocking submission silently.

### 🛑 PROBLEM PATTERN

**Scenario**: EventForm with volunteer positions in a collapsed `Collapse` component:

```typescript
// Main EventForm (always visible)
<form onSubmit={handleSubmit}>
  <TextInput label="Event Title" required {...form.getInputProps('title')} />
  {/* More main form fields */}

  <Button type="submit">Save Event</Button>
</form>

// Volunteer position form (hidden in collapsed section)
<Collapse in={isEditAreaOpen}>
  <TextInput
    label="Position Title"
    required  // ❌ BLOCKS MAIN FORM SUBMISSION!
    {...form.getInputProps('title')}
  />
  <Textarea
    label="Description"
    required  // ❌ BLOCKS MAIN FORM SUBMISSION!
    {...form.getInputProps('description')}
  />
</Collapse>
```

**What Happens**:
1. User fills out main EventForm fields
2. User clicks "Save" button on main form
3. Browser's HTML5 validation runs on ALL `required` fields on page
4. Finds empty required fields in collapsed volunteer section
5. Tries to focus on them to show validation message
6. **FAILS** because fields are hidden (`display: none` in collapsed section)
7. Console error: "An invalid form control with name='' is not focusable"
8. **Form submission SILENTLY BLOCKED** - no success, no error notification

### ✅ CRITICAL SOLUTION: REMOVE REQUIRED FROM HIDDEN FIELDS

```typescript
// ❌ WRONG: Required attribute on fields in collapsible sections
<Collapse in={isEditAreaOpen}>
  <TextInput
    label="Position Title"
    required  // Will block main form if collapsed!
    {...form.getInputProps('title')}
  />
</Collapse>

// ✅ CORRECT: No required attribute, rely on Mantine form validation
<Collapse in={isEditAreaOpen}>
  <TextInput
    label="Position Title"
    {...form.getInputProps('title')}  // Mantine validates via form.validate
  />
</Collapse>

// Mantine form validation still works:
const form = useForm({
  validate: {
    title: (value) => (!value ? 'Position title is required' : null),
  }
})
```

### 🛑 WHEN TO REMOVE `required` ATTRIBUTE

**REMOVE `required` from HTML inputs when**:
1. Field is in a collapsible section (`Collapse`, `Accordion`, etc.)
2. Field is in a modal/drawer that might be closed
3. Field is conditionally rendered (`{condition && <Input required />}`)
4. Field is in a tab that might not be active
5. Using Mantine's `useForm` with validation rules (handles validation)

**KEEP `required` attribute when**:
1. Field is always visible on the page
2. Field is part of main form that's never hidden
3. NOT using Mantine form validation (relying on native HTML5 validation)

### 🔧 DEBUGGING CHECKLIST

When form submission fails silently (no success, no error):

1. **Check browser console** - Look for "invalid form control" errors
2. **Inspect collapsed sections** - Are there required fields hidden?
3. **Check all tabs** - Required fields in inactive tabs cause this
4. **Check modals** - Required fields in closed modals block forms
5. **Remove all `required` attributes** from hidden areas
6. **Use Mantine form validation** instead of HTML5 validation

### 📋 BEST PRACTICES FOR MANTINE FORMS

**Pattern: Separate forms for separate concerns**

```typescript
// ✅ CORRECT: Main form and sub-form are separate
// Main EventForm
<form onSubmit={handleMainFormSubmit}>
  <TextInput required {...mainForm.getInputProps('title')} />
  <Button type="submit">Save Event</Button>
</form>

// Volunteer Position Form (separate, in collapsed section)
<Collapse in={isOpen}>
  <form onSubmit={handleVolunteerFormSubmit}>
    {/* NO required attributes on inputs */}
    <TextInput {...volunteerForm.getInputProps('title')} />
    <Button type="submit">Save Position</Button>
  </form>
</Collapse>

// Use Mantine validation for both forms
const mainForm = useForm({
  validate: { title: (v) => !v ? 'Required' : null }
})

const volunteerForm = useForm({
  validate: { title: (v) => !v ? 'Required' : null }
})
```

### 💥 FILES AFFECTED

- `/apps/web/src/components/events/VolunteerPositionInlineForm.tsx` - Removed 5 `required` attributes
  - Line 137: `<TextInput>` position title
  - Line 150: `<Textarea>` position description
  - Line 167: `<Select>` sessions
  - Line 183: `<NumberInput>` slots needed
  - Line 199: `<TimeInput>` start time
  - Line 211: `<TimeInput>` end time

### 🎯 VERIFICATION STEPS

After fix:
1. **Open admin event details page**
2. **DO NOT expand volunteer positions section** (keep it collapsed)
3. **Edit any main form field** (title, description, etc.)
4. **Click "Save" button**
5. **Verify**: Success notification appears ✅
6. **Verify**: No console errors ✅
7. **Verify**: Changes persist after page refresh ✅

### 💥 CONSEQUENCES OF IGNORING

- ❌ Form appears broken - Save button does nothing
- ❌ No user feedback - Silent failure is confusing
- ❌ Users lose work - They think it saved but it didn't
- ❌ Support tickets - "Save button not working"
- ❌ Developer confusion - Error message is cryptic
- ❌ Hours wasted debugging - Not obvious the issue is hidden fields

### 🚨 RELATED PATTERNS

**Similar issues occur with**:
- Mantine `<Modal>` with required fields
- Mantine `<Tabs>` with required fields in inactive tabs
- Conditional rendering: `{show && <Input required />}`
- CSS `display: none` or `visibility: hidden` on required fields

### Tags
#critical #forms #html5-validation #required-attribute #collapse #hidden-fields #form-submission #mantine-forms #silent-failure #user-experience

---

## 🚨 CRITICAL: MANTINE FORM OBJECT IN USEEFFECT DEPENDENCIES CAUSES INFINITE NOTIFICATION LOOP 🚨
**Date**: 2025-11-02
**Category**: React Hooks / Mantine Forms / Infinite Loops
**Severity**: CRITICAL - UNUSABLE UX WITH INFINITE POPUPS

### What We Learned
**FORM OBJECT IN USEEFFECT DEPENDENCIES CREATES INFINITE LOOP**: Including the Mantine `form` object (from `useForm()`) in a `useEffect` dependency array causes infinite re-renders and notification spam when `form.reset()` is called.

**USER SYMPTOMS**:
- Click "Change Password" button
- 5 success notifications appear immediately
- 3 seconds later, 5 more success notifications appear
- Pattern repeats infinitely until page refresh

**ROOT CAUSE**: Mantine's `useForm()` creates a NEW form object on every render. When `form` is in the dependency array AND `form.reset()` is called inside the effect:

1. User submits form successfully
2. `useEffect` runs because `isSuccess` changed
3. Notification shows ✅
4. `form.reset()` is called
5. **Mantine creates NEW form object** (new reference)
6. `useEffect` sees `form` reference changed → runs again
7. Shows notification again → infinite loop!

### 🛑 BROKEN PATTERN

```typescript
// ❌ WRONG: form object in dependency array causes infinite loop
const ChangePasswordForm: React.FC = () => {
  const changePasswordMutation = useChangePassword()

  const form = useForm<ChangePasswordDto>({
    initialValues: { currentPassword: '', newPassword: '', confirmPassword: '' }
  })

  const handleSubmit = (values: ChangePasswordDto) => {
    changePasswordMutation.mutate(values)
  }

  // ❌ CRITICAL BUG: form in dependency array
  React.useEffect(() => {
    if (changePasswordMutation.isSuccess) {
      notifications.show({
        title: 'Success',
        message: 'Password changed successfully',
        color: 'green',
      })
      form.reset()  // Creates NEW form object → triggers useEffect again!
    }
    if (changePasswordMutation.isError) {
      notifications.show({
        title: 'Error',
        message: 'Failed to change password',
        color: 'red',
      })
    }
  }, [
    changePasswordMutation.isSuccess,
    changePasswordMutation.isError,
    changePasswordMutation.error,
    form,  // ❌ CAUSES INFINITE LOOP!
  ])

  return <form onSubmit={form.onSubmit(handleSubmit)}>{/* ... */}</form>
}
```

**Why This Breaks**:
1. `form` object is created by `useForm()` - creates new reference on every component render
2. `form` is in dependency array - effect runs when reference changes
3. `form.reset()` triggers re-render - Mantine updates internal state
4. New `form` reference created → effect runs again
5. **Infinite loop**: notifications spam user, form becomes unusable

### ✅ CRITICAL SOLUTION: SPLIT USEEFFECTS, REMOVE FORM DEPENDENCY

```typescript
// ✅ CORRECT: Split into two useEffects, remove form from dependencies
const ChangePasswordForm: React.FC = () => {
  const changePasswordMutation = useChangePassword()

  const form = useForm<ChangePasswordDto>({
    initialValues: { currentPassword: '', newPassword: '', confirmPassword: '' }
  })

  const handleSubmit = (values: ChangePasswordDto) => {
    changePasswordMutation.mutate(values)
  }

  // ✅ CORRECT: Success effect with ONLY isSuccess dependency
  React.useEffect(() => {
    if (changePasswordMutation.isSuccess) {
      notifications.show({
        title: 'Success',
        message: 'Password changed successfully',
        color: 'green',
        icon: <IconCheck />,
      })
      form.reset()  // Safe because form NOT in dependency array
    }
  }, [changePasswordMutation.isSuccess])  // ✅ No 'form' dependency

  // ✅ CORRECT: Error effect separate
  React.useEffect(() => {
    if (changePasswordMutation.isError) {
      notifications.show({
        title: 'Error',
        message:
          changePasswordMutation.error instanceof Error
            ? changePasswordMutation.error.message
            : 'Failed to change password',
        color: 'red',
        icon: <IconAlertCircle />,
      })
    }
  }, [changePasswordMutation.isError, changePasswordMutation.error])

  return <form onSubmit={form.onSubmit(handleSubmit)}>{/* ... */}</form>
}
```

### 🛑 NEVER INCLUDE MANTINE FORM IN DEPENDENCIES

**CRITICAL RULE**: NEVER include Mantine `form` object in `useEffect` dependency arrays:

```typescript
// ❌ WRONG: Any of these patterns cause infinite loops
useEffect(() => {
  // ... code that calls form.reset() or form.setValues()
}, [form])  // WRONG!

useEffect(() => {
  // ... notification + form.reset()
}, [isSuccess, form])  // WRONG!

useEffect(() => {
  // ... any form method call
}, [someState, form])  // WRONG!

// ✅ CORRECT: Never include form in dependencies
useEffect(() => {
  if (isSuccess) {
    form.reset()  // Safe because form not in dependency array
  }
}, [isSuccess])  // ✅ Correct!

useEffect(() => {
  if (someCondition) {
    form.setValues({ field: 'value' })  // Safe
  }
}, [someCondition])  // ✅ Correct!
```

### 📋 WHY FORM REFERENCE CHANGES

**Mantine form object changes reference when**:
1. `form.reset()` is called - Updates internal state
2. `form.setValues()` is called - Updates internal state
3. `form.setFieldValue()` is called - Updates internal state
4. Any validation runs - May update internal state
5. **Component re-renders** - Mantine may create new reference

**Key insight**: Even though `form` object looks stable, Mantine's internal implementation may create new references on state updates.

### 🔧 DEBUGGING CHECKLIST

When you see infinite notifications or infinite re-renders:

1. **Check useEffect dependencies** - Is `form` object included?
2. **Check if form methods called in effect** - `reset()`, `setValues()`, etc.?
3. **Remove form from dependencies** - Trust that closure will access current form
4. **Split into multiple useEffects** - Separate success/error handling
5. **Use mutation callbacks instead** - Consider `onSuccess`/`onError` on mutation

### 📋 ALTERNATIVE PATTERN: MUTATION CALLBACKS

**Instead of useEffect, use mutation callbacks**:

```typescript
// ✅ ALTERNATIVE: Use onSuccess/onError callbacks
const changePasswordMutation = useMutation({
  mutationFn: (data: ChangePasswordDto) =>
    dashboardService.changePassword(user!.id, data),
  onSuccess: () => {
    notifications.show({
      title: 'Success',
      message: 'Password changed successfully',
      color: 'green',
      icon: <IconCheck />,
    })
    form.reset()  // Safe here, no dependency issues
  },
  onError: (error) => {
    notifications.show({
      title: 'Error',
      message: error instanceof Error
        ? error.message
        : 'Failed to change password',
      color: 'red',
      icon: <IconAlertCircle />,
    })
  }
})

// No useEffect needed!
```

**When to use each pattern**:
- **Mutation callbacks**: Best for simple success/error notifications
- **useEffect**: When you need access to component state or multiple effects

### 💥 FILES AFFECTED

- `/apps/web/src/pages/dashboard/ProfileSettingsPage.tsx` - Lines 432-456
  - **Before**: Single useEffect with `form` in dependencies (BROKEN)
  - **After**: Two separate useEffects without `form` dependency (FIXED)

### 🎯 VERIFICATION STEPS

After fix:
1. **Navigate to Profile Settings page** → Change Password tab
2. **Fill out password form** with valid data
3. **Click "Change Password"**
4. **Verify**: Only ONE success notification appears ✅
5. **Verify**: Form clears after success ✅
6. **Verify**: No repeating notifications ✅
7. **Check browser console**: No infinite render warnings ✅

### 💥 CONSEQUENCES OF IGNORING

- ❌ Infinite notification spam (5 popups every 3 seconds)
- ❌ Form completely unusable after first submission
- ❌ Browser performance degrades (infinite re-renders)
- ❌ Notification queue fills up (memory leak potential)
- ❌ User confusion and frustration
- ❌ Production incidents and support tickets

### 🚨 RELATED PATTERNS TO WATCH FOR

**Similar issues occur with**:
- Any object created by custom hook in dependency array
- Date objects, function objects, array/object literals in dependencies
- Zustand store objects (if entire store is passed)
- Context values that change reference frequently

**General rule**: Only include **primitive values** or **stable references** in dependency arrays.

### Tags
#critical #infinite-loop #useEffect #mantine-forms #form-reset #notifications #dependency-array #react-hooks #user-experience #performance

---

## 🚨 CRITICAL: REACT HOOKS & RE-RENDER LOOPS - USECALLBACK FOR PROPS 🚨
**Date**: 2025-10-06
**Category**: React Hooks / Re-render Prevention
**Severity**: CRITICAL - BREAKS ALL NAVIGATION

### What We Learned
**CALLBACK PROPS WITHOUT USECALLBACK CAUSE INFINITE RE-RENDER LOOPS**: Passing callback functions as props to components that use them in `useEffect` dependency arrays WITHOUT wrapping them in `useCallback` causes infinite re-renders that break ALL React Router navigation.

**USER SYMPTOMS**:
- Navigation completely broken on admin vetting page
- URL changes but page doesn't re-render
- ALL navigation blocked (table rows, nav menu, links)
- Manual browser refresh required to see new pages

**ROOT CAUSE**: Callback function passed as prop was NOT wrapped in `useCallback`, creating a new function reference on every parent render:

```typescript
// ❌ WRONG: Creates new function reference on every render
const handleSelectionChange = (selectedIds: Set<string>, applicationsData: any[]) => {
  setSelectedApplications(selectedIds);
  setSelectedApplicationsData(applicationsData);
};

// Child component has this callback in useEffect dependency array
React.useEffect(() => {
  if (onSelectionChange && data?.items) {
    onSelectionChange(selectedApplications, selectedData);
  }
}, [selectedApplications, data?.items, onSelectionChange]); // onSelectionChange changes every render!
```

**Result**: Infinite re-render loop that blocked ALL React Router navigation.

### ✅ CRITICAL SOLUTION

```typescript
// ✅ CORRECT: Wrap callback in useCallback with empty dependency array
const handleSelectionChange = useCallback((selectedIds: Set<string>, applicationsData: any[]) => {
  setSelectedApplications(selectedIds);
  setSelectedApplicationsData(applicationsData);
}, []); // Empty array = stable function reference
```

### 🛑 MANDATORY RULES FOR CALLBACK PROPS

**ALWAYS use `useCallback` when:**
1. Function is passed as prop to child component
2. **ESPECIALLY when child has that prop in useEffect dependency array**
3. Function doesn't need to access component state (can use empty dependencies)

**WHY this matters:**
- Without `useCallback`, new function reference created on every parent render
- Child's `useEffect` sees new reference → runs effect again
- Effect updates state → parent re-renders → new function → infinite loop
- **React Router navigation breaks** because component never finishes rendering

### 🔧 DEBUGGING CHECKLIST

When navigation breaks (URL changes but page doesn't re-render):

1. **Check for re-render loops** - Comment out ALL child components
2. **Add components back one at a time** to isolate problematic component
3. **Search for useEffect dependency arrays** in child components
4. **Look for function props** in those dependency arrays
5. **Wrap parent functions with useCallback**
6. **Enable ESLint `react-hooks/exhaustive-deps` rule** for warnings

### 💥 CONSEQUENCES OF IGNORING

- ❌ Complete navigation failure (URL changes, page doesn't)
- ❌ Infinite re-render loops crash browser tab
- ❌ Users completely blocked from using application
- ❌ Hours wasted debugging symptoms instead of root cause
- ❌ Production incidents requiring emergency fixes

### 🎯 WHY DEBUGGING WAS SO HARD

**Mistakes made during debugging:**
1. Tried fixing symptoms (route paths, key props, setTimeout workarounds) instead of root cause
2. Didn't test fixes before asking user to test
3. Should have immediately commented out components to isolate the problem
4. Should have searched for useEffect dependency arrays in child components

### Tags
#critical #react-hooks #useCallback #re-render-loops #navigation #useEffect #dependency-array #infinite-loop

---

## 🚨 CRITICAL: VITEST PARALLEL EXECUTION CONFIGURATION 🚨
**Date**: 2025-10-03
**Category**: Vitest Testing / Performance
**Severity**: CRITICAL - PREVENTS TDD WORKFLOW

### What We Learned
**SERIAL TEST EXECUTION MAKES TESTING UNUSABLE**: Vitest configured for serial execution (`singleThread: true`, `maxConcurrency: 1`) caused 84 test files to timeout (180+ seconds), making TDD workflow impossible.

**ROOT CAUSE**: Vitest defaults with single-threaded execution:
- `singleThread: true` - Only 1 test file at a time
- `maxConcurrency: 1` - Only 1 test within a file at a time
- Result: ~42 minutes to run all tests serially (84 files × 30s timeout)

### ✅ CRITICAL SOLUTION: PARALLEL EXECUTION WITH SAFETY LIMITS

```typescript
// vitest.config.ts
export default defineConfig({
  test: {
    pool: 'forks',  // Better isolation than threads for React tests
    poolOptions: {
      forks: {
        singleFork: false,  // Allow parallel test files
        maxForks: 4,        // Run 4 test files concurrently
        minForks: 1,
      }
    },
    isolate: true,        // Better cleanup between tests
    maxConcurrency: 5,    // Allow 5 concurrent tests per file
  }
})
```

**Results**:
- Tests complete in ~54-58 seconds (was timing out at 180+)
- All 84 test files execute successfully
- 3-4x performance improvement
- TDD workflow restored

### 🛑 KEY CONFIGURATION PRINCIPLES

1. **Use `forks` not `threads`** for React component tests - better process isolation
2. **Balance parallelism with memory** - 4 concurrent files is sweet spot
3. **Always test single file first** - if one file runs fast but all files hang, it's a config issue
4. **Serial execution doesn't scale** - 84 files × 30s timeout = 42 minutes minimum

### 💥 CONSEQUENCES OF SERIAL EXECUTION

- ❌ Test runs timeout (180+ seconds)
- ❌ TDD workflow impossible (can't wait 42+ minutes)
- ❌ CI/CD pipeline failures
- ❌ Developers skip running tests
- ❌ Test coverage decreases over time

### Tags
#critical #vitest #parallel-execution #performance #tdd #testing #configuration

---

## 🚨 CRITICAL: MODAL STATE RESET WITH USEEFFECT ON OPENED PROP 🚨
**Date**: 2025-11-09
**Category**: Mantine Modals / React State Management
**Severity**: CRITICAL - POOR UX WITHOUT STATE RESET

### What We Learned
**MODAL CHECKBOXES DON'T RESET AUTOMATICALLY**: Mantine Modal components don't automatically reset internal state when `opened` prop changes. Confirmation checkboxes remain checked after modal closes and reopens, leading to confusing UX.

**SYMPTOMS**:
- User confirms action → Modal closes → User reopens modal
- Confirmation checkbox still checked from previous interaction
- User can accidentally submit without re-confirming
- Poor UX - appears as if previous state is "stuck"

**ROOT CAUSE**: React state persists between modal open/close cycles. The `opened` prop controls visibility, not state lifecycle.

### ✅ CRITICAL SOLUTION: USEEFFECT TO RESET STATE

```typescript
// ❌ WRONG: State persists across modal open/close
export const RemoveRsvpModal: React.FC<Props> = ({ opened, onClose, onConfirm }) => {
  const [confirmed, setConfirmed] = useState(false);
  // No reset logic - checkbox stays checked after close!

  return (
    <Modal opened={opened} onClose={onClose}>
      <Checkbox checked={confirmed} onChange={(e) => setConfirmed(e.currentTarget.checked)} />
    </Modal>
  );
};

// ✅ CORRECT: Reset state when modal closes
import { useEffect } from 'react';

export const RemoveRsvpModal: React.FC<Props> = ({ opened, onClose, onConfirm }) => {
  const [confirmed, setConfirmed] = useState(false);

  // Reset state when modal closes
  useEffect(() => {
    if (!opened) {
      setConfirmed(false);
    }
  }, [opened]);

  return (
    <Modal opened={opened} onClose={onClose}>
      <Checkbox checked={confirmed} onChange={(e) => setConfirmed(e.currentTarget.checked)} />
    </Modal>
  );
};

// ✅ CORRECT: Multiple state values to reset
export const RefundTicketModal: React.FC<Props> = ({ opened, onClose, onConfirm }) => {
  const [confirmed, setConfirmed] = useState(false);
  const [alsoRemoveRsvp, setAlsoRemoveRsvp] = useState(true);

  // Reset ALL state when modal closes
  useEffect(() => {
    if (!opened) {
      setConfirmed(false);
      setAlsoRemoveRsvp(true); // Reset to default
    }
  }, [opened]);

  return (
    <Modal opened={opened} onClose={onClose}>
      <Checkbox checked={confirmed} onChange={(e) => setConfirmed(e.currentTarget.checked)} />
      <Checkbox checked={alsoRemoveRsvp} onChange={(e) => setAlsoRemoveRsvp(e.currentTarget.checked)} />
    </Modal>
  );
};
```

### 🛑 MANDATORY PATTERN FOR ALL MODALS

**ALWAYS add useEffect to reset state when:**
- Modal has form inputs (checkboxes, text fields, selects)
- Modal has confirmation checkboxes
- Modal has multi-step state
- Modal has toggle options that affect behavior
- ANY internal state that should not persist between opens

**WHY this matters:**
- `opened` prop controls **visibility**, not **lifecycle**
- React component instance persists - state doesn't auto-reset
- Manual reset required for clean UX

### 📋 IMPLEMENTATION CHECKLIST

When creating modal components:
1. **Identify all state variables** that need reset
2. **Add useEffect** with `opened` in dependency array
3. **Check `if (!opened)`** - reset when modal closes
4. **Reset to initial/default values** - don't assume false/empty
5. **Test**: Open modal → interact → close → reopen → verify clean state

### 🔧 TESTING PATTERN

```typescript
it('resets confirmation checkbox when modal closes', async () => {
  const { rerender } = renderWithProviders();

  // Check the checkbox
  await user.click(screen.getByTestId('confirmation-checkbox'));
  expect(screen.getByTestId('confirmation-checkbox')).toBeChecked();

  // Close modal
  rerender(<Modal opened={false} />);

  // Reopen modal
  rerender(<Modal opened={true} />);

  // Checkbox should be unchecked
  expect(screen.getByTestId('confirmation-checkbox')).not.toBeChecked();
});
```

### 💥 CONSEQUENCES OF IGNORING

- ❌ Checkboxes remain checked after close
- ❌ Users can accidentally confirm actions without realizing
- ❌ Form inputs retain old values
- ❌ Confusing UX - "Why is this already filled out?"
- ❌ Potential data integrity issues (wrong values submitted)

### 🚨 RELATED PATTERNS

**Similar issues with:**
- Mantine Drawer components
- Custom modal implementations
- Any component controlled by visibility prop

**General Rule**: If component visibility is controlled by prop, state lifecycle must be managed manually.

### Tags
#critical #mantine-modal #state-reset #useEffect #ux #modal-state #checkbox-reset #form-state

---

