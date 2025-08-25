# React Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 CRITICAL: WORKTREE COMPLIANCE - MANDATORY 🚨

### ALL WORK MUST BE IN THE SPECIFIED WORKTREE DIRECTORY

**VIOLATION = CATASTROPHIC FAILURE**

When given a Working Directory like:
`/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management`

**YOU MUST:**
- Write ALL files to paths within the worktree directory
- NEVER write to `/home/chad/repos/witchcityrope-react/` main repository
- ALWAYS use the full worktree path in file operations
- VERIFY you're in the correct directory before ANY file operation

**Example:**
- ✅ CORRECT: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/docs/...`
- ❌ WRONG: `/home/chad/repos/witchcityrope-react/docs/...`

**Why This Matters:**
- Worktrees isolate feature branches
- Writing to main repo pollutes other branches
- Can cause merge conflicts and lost work
- BREAKS the entire development workflow

## 🚨 CRITICAL: MANDATORY PLAYWRIGHT TESTING REQUIREMENTS 🚨

### ALL REACT IMPLEMENTATIONS REQUIRE VISUAL VERIFICATION

**NO EXCEPTIONS - IMPLEMENTATION IS NOT COMPLETE WITHOUT PLAYWRIGHT TESTS**

**MANDATORY TESTING CHECKLIST:**
- [ ] **Visual Verification**: Playwright test with screenshot confirmation required for ALL implementations
- [ ] **Functionality Testing**: All interactive elements must be tested (buttons, forms, navigation)
- [ ] **Cross-Browser Compatibility**: Test in Chromium (minimum) with Playwright
- [ ] **Responsive Design**: Test at multiple viewport sizes if applicable
- [ ] **Error State Testing**: Test error scenarios and edge cases
- [ ] **Performance Validation**: Ensure no obvious performance issues
- [ ] **Accessibility Check**: Basic accessibility validation through Playwright

**IMPLEMENTATION WORKFLOW:**
1. Build React component/feature
2. Write Playwright test with visual verification
3. Take screenshots of working implementation
4. Test all interactive functionality
5. Verify responsive behavior if applicable
6. ONLY THEN commit implementation

**Required Playwright Test Pattern:**
```typescript
// Example: verify-[feature-name]-implementation.spec.ts
import { test, expect } from '@playwright/test';

test.describe('[Feature Name] Implementation Verification', () => {
  test('should display and function correctly', async ({ page }) => {
    await page.goto('/path/to/feature');
    
    // Visual verification - MANDATORY
    await expect(page).toHaveScreenshot('[feature-name]-implementation.png');
    
    // Functionality testing - MANDATORY
    await page.click('[data-testid="interactive-element"]');
    await expect(page.locator('[data-testid="result"]')).toBeVisible();
    
    // Error testing - REQUIRED
    await page.fill('[data-testid="input"]', 'invalid-data');
    await expect(page.locator('[data-testid="error-message"]')).toBeVisible();
  });
  
  test('should be responsive', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 }); // Mobile
    await page.goto('/path/to/feature');
    await expect(page).toHaveScreenshot('[feature-name]-mobile.png');
  });
});
```

**TEST FILES LOCATION:**
- All Playwright tests: `/tests/playwright/[feature-name].spec.ts`
- Screenshots automatically saved to: `/tests/playwright/[feature-name]-test-results/`

**VIOLATION CONSEQUENCES:**
- Implementation commits WITHOUT Playwright tests will be considered incomplete
- No deployment without visual verification
- Code review rejection for missing test coverage

### Action Items
- [ ] **NEVER commit React implementations without Playwright visual verification**
- [ ] **ALWAYS include screenshot comparison tests**
- [ ] **TEST all interactive elements and edge cases**
- [ ] **VERIFY responsive behavior when applicable**
- [ ] **VALIDATE error states and loading states**
- [ ] **CHECK accessibility basics through Playwright tools**

### Tags
#critical #mandatory-testing #playwright #visual-verification #implementation-standards

---

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### Critical Architecture Documents (MUST READ BEFORE ANY WORK):
1. **React Architecture Index**: `/docs/architecture/REACT-ARCHITECTURE-INDEX.md` - **PRIMARY ARCHITECTURE RESOURCE**
2. **API Changes Guide**: `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`
3. **Migration Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
4. **DTO Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
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

## 🚨 CRITICAL: WORKTREE WORKFLOW MANDATORY 🚨

**All development MUST happen in git worktrees, NOT main repository**
- Working directory MUST be: `/home/chad/repos/witchcityrope-worktrees/[feature-name]`
- NEVER work in: `/home/chad/repos/witchcityrope-react`
- Verify worktree context before ANY operations

### Worktree Verification Checklist
- [ ] Run `pwd` to confirm in worktree directory
- [ ] Check for .env file presence
- [ ] Verify node_modules exists
- [ ] Test `npm run dev` starts successfully

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

### Context
WitchCityRope backend has migrated to Simple Vertical Slice Architecture with minimal impact on frontend integration. API contracts are maintained for backward compatibility, but developers must understand the improvements.

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

### Performance Benefits for Frontend
- **Faster API responses**: Backend overhead elimination improves response times
- **Better caching opportunities**: Consistent response formats enable better caching
- **Reduced bundle sizes**: More focused DTO models reduce generated type sizes
- **Improved developer experience**: Better type safety and error messages

### Impact
API architecture improvements provide better developer experience, improved performance, and more consistent error handling while maintaining complete backward compatibility for existing frontend code.

### Tags
#critical #api-integration #backend-changes #improved-patterns #error-handling #type-generation

---

## 🚨 CRITICAL: Use Animated Form Components for ALL Forms 🚨
**Date**: 2025-08-22
**Category**: Form Components
**Severity**: CRITICAL

### Context
Security page was using plain Mantine TextInput components instead of the approved animated form components that provide beautiful tapered underline animations and floating labels.

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

### Impact
Using animated form components ensures:
- Consistent beautiful animations across all forms
- Better user experience with floating labels
- Design System v7 color compliance
- Professional appearance matching the design standards

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

### Context
Discovered multiple instances of inline styling on buttons and components when standardized CSS classes already exist in the project.

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

### Impact
Using standardized CSS classes ensures:
- Consistent UI across the entire application
- No text cutoff or rendering issues
- Easier maintenance and updates
- Proper hover states and transitions
- Accessibility compliance

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

*This file is maintained by the react-developer agent. Add new lessons immediately when discovered.*
*Last updated: 2025-08-25 - Added mandatory Playwright testing requirements*

## COMPREHENSIVE LESSONS FROM FRONTEND DEVELOPMENT
**NOTE**: The following lessons were consolidated from frontend-lessons-learned.md

---

## 🚨 CRITICAL: useEffect Infinite Loop Fix 🚨
**Date**: 2025-08-19
**Category**: React Hooks Critical Bug
**Severity**: Critical

### Context
Fixed "Maximum update depth exceeded" error that occurred when visiting any test page. The error was caused by an infinite loop in the App.tsx useEffect dependency array.

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

### Context
Fixed the ACTUAL root cause of "Maximum update depth exceeded" error. The infinite loop was caused by Zustand selectors that return new objects on every render, causing all components using those selectors to re-render infinitely.

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

### Context
Critical patterns learned from complex form implementation work requiring precise component usage, CSS targeting, and user experience optimization.

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

## 🚨 CRITICAL: Vite Hot Reload Port Configuration Fix 🚨
**Date**: 2025-08-25
**Category**: Development Environment
**Severity**: Critical

### Context
Event Session Matrix demo page was constantly reloading every 2-4 seconds due to Vite WebSocket connection failures. The issue was caused by port mismatch between dev server and HMR configuration.

### What We Learned
- **Root Cause**: Vite config specified port 5173 but dev server ran on port 5174
- **WebSocket Error**: `WebSocket connection to 'ws://0.0.0.0:24678/?token=...' failed: Error during WebSocket handshake: Unexpected response code: 400`
- **Reload Behavior**: Failed WebSocket connection triggered `[vite] server connection lost. Polling for restart...`
- **Port Alignment Critical**: HMR port must be consistent with dev server configuration

### Action Items
- [ ] ALWAYS align Vite config port with command-line port parameter
- [ ] USE different HMR port (24679) to avoid conflicts with existing services
- [ ] SET `strictPort: false` to allow port flexibility during development
- [ ] TEST WebSocket connection with browser dev tools before deployment

### Impact
Proper port configuration eliminates constant page reloading, enabling normal development workflow and form testing.

### Required Vite Config Pattern:
```typescript
// vite.config.ts
server: {
  host: '0.0.0.0',
  port: 5174, // Match command line port
  strictPort: false, // Allow flexibility
  
  hmr: {
    port: 24679, // Unique HMR port
    host: '0.0.0.0',
    clientPort: 24679, // Match HMR port
  },
}
```

### Debugging Commands:
```bash
# Check WebSocket connection in browser dev tools
# Look for: [vite] connecting... followed by [vite] connected.
# Error patterns: WebSocket handshake failures, connection lost messages
```

### Tags
#critical #vite #hmr #websocket #development-environment

---

## 🚨 CRITICAL: TinyMCE vs TipTap Implementation Standards 🚨
**Date**: 2025-08-25
**Category**: Rich Text Editor
**Severity**: Critical

### Context
EventForm originally used TipTap but was migrated to TinyMCE for UI standards compliance and better user experience. TinyMCE provides professional-grade rich text editing with comprehensive toolbar and features.

### What We Learned
- **UI Standards Compliance**: TinyMCE chosen over TipTap for better professional appearance
- **Package Requirements**: `@tinymce/tinymce-react` package required for React integration
- **Configuration Complexity**: TinyMCE requires proper toolbar, plugins, and content styling configuration
- **Form Integration**: Rich text editors need manual form value synchronization with onChange handlers
- **Content Styling**: TinyMCE content needs CSS styling for proper appearance

### Action Items
- [ ] ALWAYS use TinyMCE (@tinymce/tinymce-react) for rich text editing, NOT TipTap
- [ ] INSTALL TinyMCE React package: `npm install @tinymce/tinymce-react`
- [ ] CONFIGURE comprehensive toolbar with essential formatting options
- [ ] SYNC editor content with form values using onChange callback
- [ ] STYLE TinyMCE content with proper CSS classes for professional appearance

### Impact
TinyMCE provides professional rich text editing experience with:
- Comprehensive formatting toolbar
- Professional UI appearance
- Better user experience than TipTap
- Robust content handling and validation
- Industry-standard rich text editing capabilities

### Required TinyMCE Implementation Pattern:
```typescript
import { Editor } from '@tinymce/tinymce-react';

// TinyMCE configuration
<Editor
  value={form.values.fieldName}
  onEditorChange={(content) => {
    form.setFieldValue('fieldName', content);
  }}
  init={{
    height: 300,
    menubar: false,
    toolbar: 'undo redo | blocks | bold italic underline strikethrough | link | bullist numlist | indent outdent | removeformat',
    plugins: 'advlist autolink lists link charmap preview anchor textcolor colorpicker',
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
```

### Package Installation:
```bash
npm install @tinymce/tinymce-react
```

### Tags
#critical #rich-text-editor #tinymce #ui-standards #form-integration

---

## 🚨 CRITICAL: Button Text Cutoff Fix - Use Standard CSS Classes 🚨
**Date**: 2025-08-25
**Category**: UI Button Styling
**Severity**: Critical

### Context
Event Session Matrix demo page had critical button text cutoff issues affecting all buttons. Users could not read button text properly due to top/bottom text being cut off.

### Root Cause
Using Mantine Button components with inline styles instead of standardized CSS button classes caused text positioning and height calculation issues.

### What We Learned
- **Mantine Button + Inline Styles = Text Cutoff**: Combining Mantine Button components with custom inline styles breaks text positioning
- **Standard CSS Classes Work Perfectly**: Using `.btn`, `.btn-primary`, `.btn-secondary` classes from `/apps/web/src/index.css` eliminates all text cutoff issues
- **Animation Classes Work**: Standard button classes include proper corner morphing and hover animations
- **Table Action Buttons**: Use `.table-action-btn` class for small buttons in data tables

### Fixed Implementation Pattern:
```typescript
// ✅ CORRECT - Use standardized CSS button classes  
<button type="button" className="btn btn-primary">
  Primary Action
</button>

<button type="button" className="btn btn-secondary">
  Secondary Action  
</button>

<button type="button" className="table-action-btn">
  <IconEdit size={14} style={{ marginRight: '4px' }} />
  Edit
</button>

// ❌ WRONG - Mantine Button with inline styles causes text cutoff
<Button 
  style={{
    backgroundColor: 'var(--mantine-color-amber-6)',
    color: 'var(--mantine-color-gray-9)',
  }}
>
  Text Gets Cut Off
</Button>
```

### Action Items
- [ ] ALWAYS use standard CSS button classes instead of Mantine Button with inline styles
- [ ] CHECK `/apps/web/src/index.css` for available button classes before creating buttons
- [ ] USE `.btn .btn-primary` for primary CTA buttons with amber gradient and animations
- [ ] USE `.btn .btn-secondary` for secondary buttons with burgundy outline and hover fill
- [ ] USE `.table-action-btn` for small buttons in data tables
- [ ] NEVER mix Mantine Button components with custom inline styling for text-bearing buttons

### Impact
Standard CSS button classes provide:
- No text cutoff issues (proper height and line-height)
- Professional corner morphing animations on hover
- Consistent styling across entire application  
- Better accessibility and focus states
- Proper gradient backgrounds and transitions

### Verification
Playwright tests confirm:
- ✅ Button text is fully visible (no cutoff)
- ✅ Hover animations work correctly
- ✅ All button styling is consistent

### Tags
#critical #button-styling #text-cutoff #mantine-vs-css #standardized-classes

---