# Frontend Lessons Learned

## Overview

This document captures key technical learnings for React developers working on WitchCityRope. It contains actionable insights about React patterns, TypeScript integration, Mantine v7, TanStack Query, and frontend architecture decisions.

**IMPORTANT**: This file is for capturing technical lessons that help future development, NOT for documenting processes, progress tracking, or implementation details. For process documentation, see `/docs/guides-setup/` and `/docs/standards-processes/`.

## Lessons Learned

## TanStack Query v5 Integration Patterns

**Date**: 2025-08-19  
**Category**: Architecture  
**Severity**: High

### Context
Implemented comprehensive API integration proof-of-concept using TanStack Query v5 to validate critical patterns for React development.

### What We Learned
- QueryClientProvider must wrap entire app at root level, above MantineProvider for proper context access
- Axios interceptors integrate seamlessly with TanStack Query for auth and error handling
- Query key factories prevent cache inconsistencies and enable precise cache invalidation
- Optimistic updates require careful rollback handling with onMutate/onError/onSettled pattern
- Use `gcTime` instead of deprecated `cacheTime` in v5+
- Retry logic should be error-type aware (don't retry 401s)

### Action Items
- [ ] ALWAYS use query key factories for consistent cache management
- [ ] IMPLEMENT optimistic updates with proper rollback for all mutations
- [ ] USE gcTime instead of cacheTime in TanStack Query v5+
- [ ] CONFIGURE retry logic based on error types (don't retry 401s)
- [ ] WRAP entire app with QueryClientProvider above other providers
- [ ] INCLUDE React Query DevTools in development for debugging

### Impact
Establishes production-ready patterns for all React feature development with proven technology stack choices.

### Tags
#tanstack-query #react-query #api-integration #optimistic-updates #cache-management #architecture

---

## Mantine v7 CSS Specificity Requirements

**Date**: 2025-08-18  
**Category**: Performance  
**Severity**: High

### Context
Discovered critical patterns for working with Mantine v7's internal class structure during component styling implementation.

### What We Learned
- Must use Mantine's internal class names (e.g., `.mantine-TextInput-input`) with `!important` to override framework styles
- Placeholder visibility requires targeting multiple selectors (type, class, data attributes) for complete coverage
- Password inputs have different internal structure requiring special targeting beyond regular input types
- CSS-only solutions are preferred over React state for visual behaviors - more performant and maintainable

### Action Items
- [ ] ALWAYS use Mantine's internal class names with !important for style overrides
- [ ] TEST all input types including password fields when implementing CSS changes
- [ ] USE CSS-only solutions for visual enhancements when possible
- [ ] TARGET multiple selector variations for comprehensive coverage

### Impact
Enables reliable styling of Mantine v7 components without framework conflicts.

### Tags
#mantine #css-specificity #performance #styling

---

## Mantine v7 Form Component Critical Fixes

**Date**: 2025-08-18  
**Category**: Security  
**Severity**: Critical

### Context
Fixed multiple critical issues in Mantine v7 form implementation including focus outline bugs, helper text positioning, and missing border color changes.

### What We Learned
- Orange focus outline bug occurs when Mantine components inherit browser focus-visible styles
- Helper text positioning requires flex layout control at root level, not just CSS order
- Placeholder visibility logic must check both focus AND empty state, not just focus
- Floating label positioning needs conditional logic based on focus and value state
- Border color changes on focus require targeting the correct Mantine internal classes

### Action Items
- [ ] ALWAYS remove focus outlines from Mantine root elements with CSS rules
- [ ] USE flex layout control at component root level for element ordering
- [ ] IMPLEMENT proper focus AND empty state logic for placeholder visibility
- [ ] TARGET correct Mantine internal classes for border color changes
- [ ] TEST all form states: empty, filled, focused, error

### Impact
Ensures form components work correctly across all interaction states without visual bugs.

### Tags
#mantine #forms #focus-states #critical #styling

---

## Zustand Authentication Store Patterns

**Date**: 2025-08-19  
**Category**: Security  
**Severity**: Critical

### Context
Implemented secure authentication state management using Zustand with httpOnly cookies for token storage.

### What We Learned
- HttpOnly cookies provide XSS protection that localStorage cannot offer
- Zustand stores should be separated by domain (auth, UI, forms) for better organization
- Selector hooks improve performance by preventing unnecessary re-renders
- Authentication state should be validated server-side on route changes
- Store actions should handle both success and error scenarios

### Action Items
- [ ] NEVER store auth tokens in localStorage or sessionStorage
- [ ] ALWAYS use httpOnly cookies for authentication token storage
- [ ] IMPLEMENT selector hooks for performance optimization
- [ ] SEPARATE stores by domain for better organization
- [ ] VALIDATE authentication server-side on protected routes

### Impact
Ensures secure authentication implementation that prevents XSS attacks and maintains good performance.

### Tags
#zustand #authentication #security #httponly-cookies #performance

---

## React Router v7 Protected Route Patterns

**Date**: 2025-08-19  
**Category**: Security  
**Severity**: High

### Context
Implemented secure route protection using React Router v7 loaders for server-side authentication validation.

### What We Learned
- Route guards should be implemented via loaders, not components, for security
- Authentication validation must happen server-side to prevent client bypass
- Loaders provide better UX by handling redirects before component rendering
- Role-based access control should be implemented at the loader level
- Return URLs should be properly encoded for post-login redirects

### Action Items
- [ ] ALWAYS implement route guards via loaders, not components
- [ ] VALIDATE permissions server-side in loader functions
- [ ] USE type-safe route definitions for better developer experience
- [ ] IMPLEMENT proper return URL handling for login redirects
- [ ] NEVER rely on client-only route protection

### Impact
Ensures secure route protection that cannot be bypassed by client-side manipulation.

### Tags
#react-router #authentication #security #route-guards #loaders

---

## TypeScript Strict Mode Configuration

**Date**: 2025-08-17  
**Category**: Architecture  
**Severity**: Medium

### Context
Configured TypeScript strict mode for better type safety in React components and API integration.

### What We Learned
- Strict mode catches many runtime errors at compile time
- Proper type definitions improve developer experience and code quality
- Generic types for API responses enable type-safe data handling
- Union types for form states prevent invalid state combinations
- Interface inheritance reduces code duplication

### Action Items
- [ ] ALWAYS use TypeScript strict mode for new projects
- [ ] DEFINE proper interfaces for all API responses
- [ ] USE generic types for reusable components and hooks
- [ ] IMPLEMENT union types for state management
- [ ] LEVERAGE interface inheritance for related types

### Impact
Reduces runtime errors and improves developer productivity through better type safety.

### Tags
#typescript #type-safety #architecture #strict-mode

---

## React Component Performance Patterns

**Date**: 2025-08-16  
**Category**: Performance  
**Severity**: Medium

### Context
Optimized React component rendering performance using memoization and proper dependency management.

### What We Learned
- React.memo prevents unnecessary re-renders for pure components
- useCallback should be used for functions passed as props to prevent child re-renders
- useMemo should be used for expensive calculations, not simple object creation
- useEffect dependency arrays must include all dependencies to prevent stale closures
- Component composition is often better than complex conditional rendering

### Action Items
- [ ] USE React.memo for components that receive stable props
- [ ] IMPLEMENT useCallback for functions passed as props
- [ ] APPLY useMemo only for truly expensive calculations
- [ ] INCLUDE all dependencies in useEffect dependency arrays
- [ ] PREFER component composition over complex conditional rendering

### Impact
Improves application performance by reducing unnecessary re-renders and expensive calculations.

### Tags
#react #performance #memoization #hooks #rendering

---

## Responsive Design with Mantine Breakpoints

**Date**: 2025-08-16  
**Category**: Architecture  
**Severity**: Medium

### Context
Implemented responsive design patterns using Mantine's breakpoint system for consistent mobile experience.

### What We Learned
- Mantine provides consistent breakpoint values that should be used instead of custom ones
- Mobile-first approach works better with Mantine's responsive utilities
- Grid and Flex components handle most responsive layout needs
- Custom responsive logic should use Mantine's useMediaQuery hook
- Responsive spacing should use Mantine's responsive props

### Action Items
- [ ] USE Mantine's predefined breakpoints instead of custom values
- [ ] IMPLEMENT mobile-first responsive design approach
- [ ] LEVERAGE Grid and Flex components for responsive layouts
- [ ] USE useMediaQuery hook for custom responsive logic
- [ ] APPLY responsive spacing through Mantine's prop system

### Impact
Ensures consistent responsive behavior across all components and devices.

### Tags
#mantine #responsive-design #mobile-first #breakpoints #layout