# React Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

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

## 🚨 CRITICAL: Event Session Matrix API Integration Patterns 🚨
**Date**: 2025-09-07
**Category**: API Integration
**Severity**: CRITICAL

### Context
Successfully implemented Phase 3 frontend integration for Event Session Matrix, connecting existing demo UI to 8 new backend API endpoints. Discovered critical patterns for React Query + TypeScript integration.

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

### Impact
This pattern enables rapid development of complex API integrations while maintaining type safety and excellent user experience. The Event Session Matrix demo went from mock data to full backend integration in a single implementation session.

### Tags
#critical #api-integration #react-query #typescript #event-session-matrix #backend-integration

---

---

## 🚨 CRITICAL: Frontend API Response Structure Fix 🚨
**Date**: 2025-01-09
**Category**: API Integration
**Severity**: CRITICAL

### Context
Fixed critical authentication and events system issues where API response handling was expecting wrapped responses but the actual API returns flat data structures.

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

### Impact
This fix enables:
- ✅ **Working Authentication**: Login/register/logout flow operational
- ✅ **Real Events Data**: Events list and detail pages using live API
- ✅ **Proper Error Handling**: No more hidden failures due to mock fallbacks
- ✅ **Type Safety**: Response handlers match actual API contracts
- ✅ **Performance**: Direct data access without unnecessary wrapping

### Tags
#critical #api-integration #authentication #events-system #response-handling #mock-data-removal

---

## 🚨 CRITICAL: Events List API Mismatch Fix 🚨
**Date**: 2025-01-10
**Category**: API Integration
**Severity**: CRITICAL

### Context  
Fixed events page showing no events despite API returning data correctly. The issue was a mismatch between expected response structure in React Query hook and actual API response.

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

### Impact
Events page now displays all 6 events from the API correctly, fixing a complete feature breakdown.

### Tags
#critical #api-integration #react-query #events-system #response-structure

---

*This file is maintained by the react-developer agent. Add new lessons immediately when discovered.*
*Last updated: 2025-01-10 - Added critical Events List API Mismatch Fix*

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

## 🚨 CRITICAL: TinyMCE API Key Secure Configuration 🚨
**Date**: 2025-08-25
**Category**: Security & Configuration
**Severity**: CRITICAL

### Context
Implemented secure TinyMCE API key configuration following security best practices to enable premium features while maintaining security standards.

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

### Impact
Secure API key configuration enables TinyMCE premium features while:
- Protecting sensitive configuration from source control
- Providing clear setup instructions for new developers  
- Gracefully handling missing configuration
- Following industry security best practices

### Reference
Complete setup guide: `/docs/guides-setup/tinymce-api-key-setup.md`

### Tags
#critical #security #api-keys #environment-variables #tinymce #configuration

---

## 🚨 CRITICAL: Safe Date Handling from API Data 🚨

**Date**: 2025-09-10  
**File**: `/apps/web/src/components/dashboard/EventsWidget.tsx`

### Context
EventsWidget component was throwing `RangeError: Invalid time value` when trying to format dates from API responses. The error occurred when calling `Date.toISOString()` on invalid Date objects created from null/undefined/empty string date values.

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

### Impact
This pattern prevents runtime crashes when API returns incomplete date data and provides graceful degradation with meaningful fallback values. Essential for any component displaying API date/time data.

### Tags
#critical #dates #api-data #error-handling #typescript #validation #runtime-safety

---
