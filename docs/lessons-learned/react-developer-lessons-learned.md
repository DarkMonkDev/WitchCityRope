# React Developer Lessons Learned

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
*Last updated: 2025-08-22 - Added critical CSS standards lesson*