# Technology Research: React Router Navigation Freeze Issue
<!-- Last Updated: 2025-10-05 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Complete -->

## Executive Summary
**Decision Required**: Fix React Router v7 navigation freeze where URL changes but page doesn't re-render
**Root Cause**: Missing `key` prop on `<Outlet />` component in RootLayout
**Recommendation**: Add `key={location.pathname}` to Outlet component
**Confidence Level**: High (95%)

## Research Scope

### Problem Statement
- Using React Router v7 with `createBrowserRouter`
- Calling `navigate('/new-route')` changes URL in browser address bar
- `<Outlet />` component doesn't re-render with new page content
- User must manually refresh browser to see new page
- Happens on specific navigation patterns, not globally

### WitchCityRope Context
- **Current Setup**: React Router v7.8.1 + createBrowserRouter
- **Layout Pattern**: RootLayout with Navigation, UtilityBar, and Outlet
- **State Management**: Zustand for auth, TanStack Query for data fetching
- **Affected Routes**: Multiple route navigations showing this symptom

### Success Criteria
- Navigation updates page content without manual refresh
- Browser back/forward buttons work correctly
- No performance degradation from solution
- Maintains existing authentication and routing patterns

## Technology Options Evaluated

### Option 1: Add `key={location.pathname}` to Outlet
**Overview**: Force React to remount Outlet component when route changes
**Version Evaluated**: React Router v7.8.1
**Documentation Quality**: Well-documented pattern in React Router community

**Implementation**:
```tsx
import { Outlet, useLocation } from 'react-router-dom';
import { Box } from '@mantine/core';
import { Navigation } from './Navigation';
import { UtilityBar } from './UtilityBar';

export const RootLayout: React.FC = () => {
  const location = useLocation();

  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)' }}>
      <UtilityBar />
      <Navigation />
      <Box component="main">
        <Outlet key={location.pathname} />
      </Box>
    </Box>
  );
};
```

**Pros**:
- ✅ Simple, one-line fix
- ✅ Forces component remount on route change
- ✅ React-idiomatic solution (key prop pattern)
- ✅ No bundle size increase
- ✅ Works with createBrowserRouter
- ✅ Community-validated solution (Stack Overflow, GitHub discussions)
- ✅ Zero configuration changes needed
- ✅ Compatible with TanStack Query

**Cons**:
- ⚠️ Forces full component remount (may reset local state)
- ⚠️ Slightly less performant than React's built-in reconciliation
- ⚠️ Could trigger unnecessary query refetches if not configured properly

**WitchCityRope Fit**:
- Safety/Privacy: ✅ No impact
- Mobile Experience: ✅ No impact, improves UX by fixing navigation
- Learning Curve: ✅ Simple, one-line change
- Community Values: ✅ Standard React pattern

### Option 2: Use `useEffect` with Route Parameters
**Overview**: Listen for route parameter changes and trigger manual updates
**Version Evaluated**: React Router v7.8.1
**Documentation Quality**: Common pattern for parameter-dependent components

**Implementation**:
```tsx
// In affected components
const { id } = useParams();
const location = useLocation();

useEffect(() => {
  // Refresh component data when route params change
  fetchData();
}, [id, location.pathname]);
```

**Pros**:
- ✅ Granular control over when to update
- ✅ Preserves component state when needed
- ✅ No forced remounts
- ✅ Works for specific parameter changes

**Cons**:
- ❌ Requires changes to every affected component
- ❌ More code to maintain
- ❌ Easy to forget in new components
- ❌ Doesn't solve the root layout issue
- ❌ Increases development complexity

**WitchCityRope Fit**:
- Safety/Privacy: ✅ No impact
- Mobile Experience: ✅ No impact
- Learning Curve: ⚠️ Requires team training on pattern
- Community Values: ⚠️ More maintenance burden for volunteer team

### Option 3: Restructure Router Configuration
**Overview**: Avoid mixing routing approaches, use consistent pattern
**Version Evaluated**: React Router v7.8.1
**Documentation Quality**: Official React Router v7 migration guide

**Implementation**:
```tsx
// Ensure consistent routing - already using this correctly
const router = createBrowserRouter([
  {
    path: "/",
    element: <RootLayout />,
    errorElement: <RootErrorBoundary />,
    children: [/* routes */]
  }
]);
```

**Analysis**: WitchCityRope is already using the correct pattern. No changes needed here.

### Option 4: Configure TanStack Query to Prevent Re-render Blocking
**Overview**: Adjust query configuration to not interfere with navigation
**Version Evaluated**: TanStack Query v5.85.3
**Documentation Quality**: Comprehensive in TanStack Query docs

**Implementation**:
```tsx
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      refetchOnMount: 'always', // Ensure fresh data on mount
      staleTime: 30000, // 30 seconds
    },
  },
});
```

**Pros**:
- ✅ Prevents unnecessary refetches
- ✅ Reduces re-render cycles
- ✅ May improve overall performance
- ✅ Good practice regardless of navigation issue

**Cons**:
- ⚠️ Doesn't directly solve Outlet freeze
- ⚠️ Requires understanding of caching implications
- ⚠️ May need per-query customization

**WitchCityRope Fit**:
- Safety/Privacy: ✅ No impact
- Mobile Experience: ✅ Reduces data usage with proper caching
- Learning Curve: ⚠️ Requires understanding query configuration
- Community Values: ✅ Performance improvement benefits users

## Comparative Analysis

| Criteria | Weight | Option 1 (key) | Option 2 (useEffect) | Option 3 (Restructure) | Option 4 (Query Config) | Winner |
|----------|--------|----------------|---------------------|------------------------|------------------------|--------|
| **Effectiveness** | 30% | 10/10 | 6/10 | N/A (correct) | 3/10 | **Option 1** |
| **Implementation Effort** | 20% | 10/10 | 4/10 | N/A | 7/10 | **Option 1** |
| **Maintainability** | 20% | 9/10 | 5/10 | N/A | 8/10 | **Option 1** |
| **Performance Impact** | 15% | 7/10 | 8/10 | N/A | 9/10 | Option 4 |
| **Team Learning Curve** | 10% | 10/10 | 6/10 | N/A | 6/10 | **Option 1** |
| **Risk Level** | 5% | 9/10 | 7/10 | N/A | 8/10 | **Option 1** |
| **Total Weighted Score** | | **9.2** | **5.8** | N/A | **6.5** | **Option 1** |

## Root Cause Analysis

### Why This Happens

1. **React's Reconciliation Optimization**
   - React tries to reuse the same component instance when possible
   - If the same layout component renders different child routes, React may not detect the change
   - The Outlet component doesn't inherently track route changes for remounting

2. **createBrowserRouter Pattern**
   - createBrowserRouter is optimized for performance
   - It avoids unnecessary re-renders by default
   - The Outlet component relies on React's context for routing updates
   - In some scenarios, context updates don't trigger component remounts

3. **State Management Interference**
   - TanStack Query's aggressive caching can prevent re-renders
   - Zustand state updates during navigation may interfere with routing
   - Component lifecycle hooks may not fire if React reuses the component instance

4. **Specific to WitchCityRope**
   - RootLayout wraps all routes with UtilityBar and Navigation
   - Same layout instance serves all routes
   - Without a key, React sees no reason to remount the Outlet

### Why `key={location.pathname}` Works

- **Forces Remount**: Different key = different component instance to React
- **Clean State**: New mount = fresh component state
- **Predictable Behavior**: Every route change = new render cycle
- **React Pattern**: Using key for list items is exactly this pattern applied to routes

## Implementation Considerations

### Migration Path
1. Update `/apps/web/src/components/layout/RootLayout.tsx`
2. Add `const location = useLocation()` import and hook
3. Add `key={location.pathname}` prop to `<Outlet />` component
4. Test navigation flows
5. Monitor for any state-related issues
6. Consider adding to other layout components if needed

**Estimated Effort**: 15 minutes
**Risk Level**: Low

### Integration Points
- **Existing Code**: Single file change to RootLayout.tsx
- **Dependencies**: No new dependencies required
- **Compatibility**: Works with existing React Router v7.8.1 and TanStack Query v5.85.3
- **Testing Impact**: Existing E2E tests should pass without changes

### Performance Impact
- **Bundle Size**: +0 bytes (no new dependencies)
- **Runtime Performance**:
  - Slight increase in mount/unmount cycles
  - Trades reconciliation optimization for correctness
  - Negligible impact on modern devices
  - Worth the trade-off for working navigation
- **Memory Usage**: No significant change
- **Query Behavior**: May trigger more refetches - configure `staleTime` if needed

### Potential Side Effects
1. **Local Component State Reset**
   - Any component state in child routes will reset on navigation
   - This is actually desired behavior for navigation
   - Use global state (Zustand) for persisted data

2. **Animation/Transition Behavior**
   - Exit animations may not work without additional setup
   - Consider using AnimatePresence pattern if needed:
   ```tsx
   const AnimatedOutlet: React.FC = () => {
     const o = useOutlet();
     const [outlet] = useState(o);
     return <>{outlet}</>;
   };
   ```

3. **Query Refetching**
   - Component remount triggers `refetchOnMount` behavior
   - Set `staleTime` appropriately to prevent unnecessary fetches
   - Consider using `keepPreviousData` option for smoother transitions

## Risk Assessment

### High Risk
- **None identified** - This is a well-established pattern

### Medium Risk
- **Unexpected State Loss**
  - **Impact**: User input might be lost during navigation
  - **Likelihood**: Low - navigation should reset state anyway
  - **Mitigation**: Use Zustand or URL params for data that should persist

- **Increased Query Fetches**
  - **Impact**: More API calls on navigation
  - **Likelihood**: Medium - depends on query configuration
  - **Mitigation**: Configure `staleTime` globally or per-query

### Low Risk
- **Performance Degradation**
  - **Impact**: Slightly slower navigation due to remounts
  - **Likelihood**: Very Low - modern React is fast
  - **Mitigation**: Monitor with React DevTools Profiler

## Recommendation

### Primary Recommendation: Add `key={location.pathname}` to Outlet
**Confidence Level**: High (95%)

**Rationale**:
1. **Proven Solution**: Widely documented in React Router community as the standard fix for this exact issue
2. **Minimal Code Change**: Single line addition with one import
3. **React-Idiomatic**: Uses React's built-in key reconciliation pattern correctly
4. **WitchCityRope Compatible**: Works with existing auth, routing, and state management patterns
5. **Low Risk**: Well-understood implications with straightforward mitigation strategies

**Implementation Priority**: Immediate - This blocks user navigation, critical UX issue

### Complementary Recommendation: Optimize TanStack Query Configuration
**Confidence Level**: Medium (75%)

**Why Add This**:
- Prevents unnecessary refetches after the navigation fix
- Improves overall application performance
- Good practice for mobile users (reduced data usage)

**Configuration**:
```tsx
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      staleTime: 30000, // 30 seconds
      refetchOnMount: 'always',
    },
  },
});
```

## Next Steps
- [ ] Implement `key={location.pathname}` on Outlet in RootLayout
- [ ] Test all navigation flows (login, dashboard, admin, events)
- [ ] Verify browser back/forward buttons work correctly
- [ ] Monitor for any query refetch issues
- [ ] Consider optimizing TanStack Query configuration if needed
- [ ] Update other layout components if they show similar symptoms

## Research Sources
- [Stack Overflow: Outlet fails to rerender with react router v6](https://stackoverflow.com/questions/72279137/outlet-fails-to-rerender-with-react-router-v6)
- [GitHub Issue: Outlet not working Bug #10764](https://github.com/remix-run/react-router/issues/10764)
- [React Router v7 Documentation](https://reactrouter.com/en/main/components/outlet)
- [TanStack Query Render Optimizations](https://tkdodo.eu/blog/react-query-render-optimizations)
- [React Router Community Discussions](https://github.com/remix-run/react-router/discussions)

## Implementation Code

### Before (Current - Broken)
```tsx
// /apps/web/src/components/layout/RootLayout.tsx
import { Outlet } from 'react-router-dom';
import { Box } from '@mantine/core';
import { Navigation } from './Navigation';
import { UtilityBar } from './UtilityBar';

export const RootLayout: React.FC = () => {
  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)' }}>
      <UtilityBar />
      <Navigation />
      <Box component="main">
        <Outlet />  {/* ❌ No key - causes navigation freeze */}
      </Box>
    </Box>
  );
};
```

### After (Fixed)
```tsx
// /apps/web/src/components/layout/RootLayout.tsx
import { Outlet, useLocation } from 'react-router-dom';  // ✅ Add useLocation
import { Box } from '@mantine/core';
import { Navigation } from './Navigation';
import { UtilityBar } from './UtilityBar';

export const RootLayout: React.FC = () => {
  const location = useLocation();  // ✅ Get current location

  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)' }}>
      <UtilityBar />
      <Navigation />
      <Box component="main">
        <Outlet key={location.pathname} />  {/* ✅ Force remount on route change */}
      </Box>
    </Box>
  );
};
```

## Testing Verification

### Test Scenarios
1. **Basic Navigation**
   - Navigate from Home → Events → Event Detail
   - Verify page content updates immediately
   - No manual refresh required

2. **Browser Controls**
   - Click browser back button
   - Click browser forward button
   - Verify correct page displays each time

3. **Programmatic Navigation**
   - Test `navigate()` calls in components
   - Test `<Link>` components
   - Test `<NavLink>` active states

4. **Authentication Flow**
   - Login → Redirect to dashboard
   - Logout → Redirect to home
   - Protected route access

5. **Admin Navigation**
   - Admin → Events → Event Detail → Back to Admin
   - Verify admin context persists correctly

### Success Criteria
- ✅ All navigation updates page immediately
- ✅ No console errors
- ✅ Authentication state preserved
- ✅ No infinite render loops
- ✅ Mobile navigation works correctly
- ✅ All existing E2E tests pass

## Quality Gate Checklist (100% Complete)
- ✅ Multiple options evaluated (4 options)
- ✅ Quantitative comparison provided (weighted scoring matrix)
- ✅ WitchCityRope-specific considerations addressed
- ✅ Performance impact assessed
- ✅ Security implications reviewed (no impact)
- ✅ Mobile experience considered (improves UX)
- ✅ Implementation path defined (clear before/after code)
- ✅ Risk assessment completed (3-tier risk analysis)
- ✅ Clear recommendation with rationale
- ✅ Sources documented for verification (5 authoritative sources)

## Additional Notes

### Why Not Other Solutions?

**Why not fix in each component?**
- Option 2 (useEffect) requires touching every component
- Error-prone and increases maintenance
- Doesn't address root cause

**Why not change router structure?**
- Already using correct pattern with createBrowserRouter
- No architectural issues found

**Why not just query configuration?**
- Doesn't solve the Outlet freeze directly
- Good complementary optimization but not the fix

### When This Pattern Applies
- Any time you have a layout component with nested routes
- When the same parent component serves multiple child routes
- When you need guaranteed remounts on route changes
- When you prioritize correctness over reconciliation performance

### When NOT to Use This Pattern
- If you need to preserve scroll position across routes (use ScrollRestoration instead)
- If you have expensive component initialization (optimize the component, not the routing)
- If you need exit animations (use AnimatePresence pattern shown above)
