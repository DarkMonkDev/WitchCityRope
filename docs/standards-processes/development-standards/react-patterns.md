# React Patterns - WitchCityRope Development Standards

*Created: 2025-09-20*
*Last Updated: 2025-09-20*
*Maintained By: React Developer Agent*

## Overview

This document contains standardized React patterns for the WitchCityRope project. These patterns have been proven through development experience and prevent common bugs.

## 🚨 CRITICAL: API-Dependent Button Visibility Pattern

### Problem
Buttons that depend on API data disappear during loading when using naive conditional rendering.

### Bad Pattern
```typescript
// ❌ WRONG: Button disappears during API loading
{!data?.hasAction && data?.canPerformAction && (
  <button>Perform Action</button>
)}
```

### Good Pattern
```typescript
// ✅ CORRECT: Button remains visible during loading
{(() => {
  const hasNotPerformed = !data?.hasAction;
  const canPerformCondition = data?.canPerformAction || data === null || isLoading;
  const shouldShowButton = hasNotPerformed && canPerformCondition;

  return shouldShowButton;
})() && (
  <button
    disabled={isLoading}
    onClick={handleAction}
  >
    {isLoading ? 'Loading...' : 'Perform Action'}
  </button>
)}
```

### Key Principles
1. **Handle null/loading states explicitly** in button visibility logic
2. **Use inclusive OR conditions** for API-dependent buttons
3. **Show loading state** instead of hiding buttons during API calls
4. **Always consider the user experience** during data loading

## Form State Management Pattern

### Problem
Form components re-initializing on every prop change, losing user input.

### Bad Pattern
```typescript
// ❌ WRONG: Re-initializes form on every prop change
useEffect(() => {
  if (initialData) {
    form.setValues({ ...defaults, ...initialData });
  }
}, [initialData]);
```

### Good Pattern
```typescript
// ✅ CORRECT: Only initialize once on mount
const hasInitialized = useRef(false);
useEffect(() => {
  if (initialData && !hasInitialized.current) {
    form.setValues({ ...defaults, ...initialData });
    hasInitialized.current = true;
  }
}, [initialData]);
```

## User Role Checking Pattern

### Problem
Frontend assumes single role structure but backend provides multiple formats.

### Bad Pattern
```typescript
// ❌ WRONG: Only checks one role structure
const userRoles = user.roles || [];
const isVetted = userRoles.includes('Vetted');
```

### Good Pattern
```typescript
// ✅ CORRECT: Handle both legacy and new role structures
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
```

## Error Handling in Components

### Problem
Components crash on unexpected data formats.

### Bad Pattern
```typescript
// ❌ WRONG: No null safety
const options = sessions.map(s => ({
  value: s.sessionIdentifier,
  label: `${s.sessionIdentifier} - ${s.name}`
}));
```

### Good Pattern
```typescript
// ✅ CORRECT: Filter and validate before mapping
const options = sessions
  .filter(session => session?.sessionIdentifier && session?.name)
  .map(session => ({
    value: session.sessionIdentifier,
    label: `${session.sessionIdentifier} - ${session.name}`,
  }));
```

## Loading State Management

### Problem
Components show loading indicators inconsistently.

### Good Pattern
```typescript
// ✅ CORRECT: Comprehensive loading state handling
export function MyComponent({ data, isLoading, error }) {
  if (isLoading) {
    return <LoadingState />;
  }

  if (error) {
    return <ErrorState error={error} />;
  }

  if (!data) {
    return <EmptyState />;
  }

  return <DataView data={data} />;
}
```

## State Update Prevention

### Problem
useEffect infinite loops with Zustand actions.

### Bad Pattern
```typescript
// ❌ WRONG: Zustand actions in dependency array
const { checkAuth } = useAuthStore();
useEffect(() => {
  checkAuth();
}, [checkAuth]); // Infinite loop!
```

### Good Pattern
```typescript
// ✅ CORRECT: Empty dependency array for mount-only effects
const { checkAuth } = useAuthStore();
useEffect(() => {
  checkAuth();
}, []); // Only runs on mount
```

## Zustand Selector Pattern

### Problem
Object selectors cause infinite re-renders.

### Bad Pattern
```typescript
// ❌ WRONG: Object selector creates new reference every render
const { user, isAuthenticated } = useAuthStore(state => ({
  user: state.user,
  isAuthenticated: state.isAuthenticated
}));
```

### Good Pattern
```typescript
// ✅ CORRECT: Individual selectors for primitive values
const user = useAuthStore(state => state.user);
const isAuthenticated = useAuthStore(state => state.isAuthenticated);
```

## Environment-Aware Component Pattern

### Problem
Components break when environment variables are missing.

### Good Pattern
```typescript
// ✅ CORRECT: Environment-aware component with fallback
export function ApiDependentComponent({ data }) {
  const apiKey = import.meta.env.VITE_API_KEY;

  if (!apiKey) {
    return (
      <Alert color="blue" title="Development Mode">
        API key not configured. Using fallback behavior.
      </Alert>
    );
  }

  return <EnhancedComponent apiKey={apiKey} data={data} />;
}
```

---

## Standards Maintenance

**Updates Required**: When discovering new patterns or anti-patterns during development:
1. Add pattern to this document immediately
2. Include both bad and good examples
3. Explain the problem and solution
4. Update lessons learned files if critical

**Pattern Validation**: All patterns in this document are proven through:
- Production implementation
- Bug fixes from real issues
- Performance testing
- User experience validation

---

*This document is maintained by the React Developer Agent and updated as new patterns are discovered.*