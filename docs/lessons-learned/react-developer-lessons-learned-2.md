# React Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 NAVIGATION: LESSONS LEARNED SPLIT FILES 🚨

**FILES**: 2 total
**Part 1**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned.md` (STARTUP + CRITICAL PATTERNS)
**Part 2**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-part-2.md` (THIS FILE - CONTINUED LESSONS)
**Read ALL**: Both Part 1 AND Part 2 are MANDATORY
**Write to**: THIS FILE (Part 2) for new lessons
**Maximum file size**: 1700 lines (to stay under token limits). Both Part 1 and Part 2 files can be up to 1700 lines each

## 🚨 ULTRA CRITICAL: ADD NEW LESSONS TO THIS FILE, NOT PART 1! 🚨

**PART 1 IS FOR STARTUP** - Keep Part 1 under 1700 lines for startup procedures
**ALL NEW LESSONS GO HERE** - This is Part 2, the main lessons file
**IF YOU ADD TO PART 1** - You are doing it wrong!

---

## 🚨🚨🚨 ULTRA CRITICAL: NEVER MANUALLY DEFINE API TYPES - USE GENERATED TYPES FROM @witchcityrope/shared-types 🚨🚨🚨
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

## 🚨 NOTE: THIS FILE IS TOO LARGE 🚨
**Current size**: Over 2167 lines
**Maximum recommended**: 1700 lines
**Action needed**: Split into Part 3 or archive old lessons

---

