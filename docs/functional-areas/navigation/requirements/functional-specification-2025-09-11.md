# Functional Specification: Navigation Updates for Logged-in Users
<!-- Last Updated: 2025-09-11 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview

This specification details the implementation of navigation updates for the WitchCityRope React application to improve authenticated user experience. The changes maintain all existing styling, animations, and functionality while restructuring the navigation to provide clearer visual hierarchy and role-based access.

**Core Changes:**
1. Move authentication status to utility bar (welcome message left, logout right)
2. Replace Login button with Dashboard CTA for authenticated users  
3. Add Admin link to main navigation for administrators
4. Preserve all existing animations and responsive design patterns

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + TypeScript): UI/Auth at http://localhost:5173
- **API Service** (.NET Minimal API): Business logic at http://localhost:5655
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Component Structure
```
/apps/web/src/components/layout/
├── Navigation.tsx         # Main navigation component (MODIFY)
├── UtilityBar.tsx        # Utility bar component (MODIFY)
└── RootLayout.tsx        # Root layout (NO CHANGES)

/apps/web/src/stores/
└── authStore.ts          # Authentication state (NO CHANGES)
```

### Service Architecture
- **Web Service**: UI components use Zustand auth store for state management
- **No API Changes**: All functionality uses existing auth endpoints
- **No Database Changes**: Uses existing user roles and authentication data

## Data Models

### Existing Auth Store Structure
```typescript
interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  lastAuthCheck: Date | string | null;
  token: string | null;
  tokenExpiresAt: Date | null;
}

interface UserDto {
  id?: string;
  email?: string;
  sceneName?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  roles?: string[];  // Array contains role strings like "Admin"
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  lastLoginAt?: string | null;
}
```

### Required State Checks
```typescript
// Authentication status check
const isAuthenticated = useIsAuthenticated();
const user = useUser();

// Role-based rendering check  
const isAdmin = user?.roles?.includes('Admin') || false;

// User greeting data
const sceneName = user?.sceneName || '';
```

## API Specifications

### No New API Endpoints Required
All functionality uses existing authentication system:
- Current auth store provides all required data
- Existing logout functionality through `useAuthActions().logout`
- Role checking uses existing `user.roles` array
- No backend API changes needed

### Authentication Flow (Existing)
| Method | Path | Description | Usage |
|--------|------|-------------|-------|
| GET | /api/auth/user | Get current user | Auth store `checkAuth()` |
| POST | /api/auth/logout | Logout user | Auth store `logout()` |

## Component Specifications

### Navigation Component Updates
- **Path**: `/apps/web/src/components/layout/Navigation.tsx`
- **Authorization**: Uses existing auth hooks
- **Render Mode**: Standard React functional component
- **Key Features**: Dashboard CTA, Admin link (conditional), preserved animations

#### Main Navigation Layout Structure
```typescript
// Navigation order (left to right):
// 1. Logo: "WITCH CITY ROPE" (unchanged)
// 2. Admin (NEW - only for administrators)  
// 3. Events & Classes (existing)
// 4. How to Join (existing)
// 5. Resources (existing)
// 6. Dashboard CTA (NEW - replaces login for authenticated) / Login (for guests)

// Conditional rendering logic:
{isAuthenticated && user ? (
  // Authenticated state: Show Dashboard + Admin (if admin)
  <>
    {/* Admin link - only for users with 'Admin' role */}
    {user?.roles?.includes('Admin') && (
      <AdminNavigationLink />
    )}
    
    {/* Dashboard CTA button */}
    <DashboardButton />
  </>
) : (
  // Guest state: Show Login button
  <LoginButton />
)}
```

#### Admin Navigation Link Component
```typescript
// NEW component to be added
<Box
  component={Link}
  to="/admin"
  data-testid="link-admin"
  style={{
    color: 'var(--color-charcoal)',
    textDecoration: 'none',
    fontFamily: 'var(--font-heading)',
    fontWeight: 500,
    fontSize: '15px',
    textTransform: 'uppercase',
    letterSpacing: '1px',
    transition: 'all 0.3s ease',
    position: 'relative',
  }}
  className="nav-underline-animation" // PRESERVE existing animation
>
  Admin
</Box>
```

#### Dashboard CTA Button Component
```typescript
// REPLACE existing login button with:
<Box
  component={Link}
  to="/dashboard"
  data-testid="link-dashboard"
  className="btn btn-primary" // PRESERVE existing button styling
>
  Dashboard
</Box>
```

### UtilityBar Component Updates  
- **Path**: `/apps/web/src/components/layout/UtilityBar.tsx`
- **Authorization**: Uses existing auth hooks
- **Key Features**: User greeting (left), logout link (right), preserved animations

#### Updated UtilityBar Layout Structure
```typescript
// Layout structure (space-between):
// LEFT: Welcome message (when authenticated)
// RIGHT: Existing links + Logout (when authenticated)

<Group justify="space-between" gap="var(--space-lg)">
  {/* LEFT: User greeting (NEW) */}
  {isAuthenticated && user ? (
    <Box 
      data-testid="user-greeting"
      style={{
        color: 'var(--color-taupe)',
        fontFamily: 'var(--font-heading)',
        fontSize: '13px',
        textTransform: 'uppercase',
        letterSpacing: '1px',
        fontWeight: 500,
      }}
    >
      Welcome, {user.sceneName}
    </Box>
  ) : (
    <Box /> // Empty spacer when logged out
  )}
  
  {/* RIGHT: Existing links + logout (MODIFIED) */}
  <Group gap="var(--space-lg)">
    {/* Existing links - PRESERVE EXACTLY */}
    <UtilityBarLink to="/incident-report">Report an Incident</UtilityBarLink>
    <UtilityBarLink to="/private-lessons">Private Lessons</UtilityBarLink>
    <UtilityBarLink to="/contact">Contact</UtilityBarLink>
    
    {/* NEW: Logout link */}
    {isAuthenticated && (
      <Box
        component="button"
        data-testid="button-logout"
        onClick={handleLogout}
        style={{
          background: 'none',
          border: 'none',
          color: 'var(--color-taupe)',
          textDecoration: 'none',
          transition: 'all 0.3s ease',
          fontFamily: 'var(--font-heading)',
          fontSize: '13px',
          textTransform: 'uppercase',
          letterSpacing: '1px',
          cursor: 'pointer',
        }}
        className="utility-bar-link" // PRESERVE existing animation
      >
        Logout
      </Box>
    )}
  </Group>
</Group>
```

#### Logout Handler Implementation
```typescript
const handleLogout = async () => {
  try {
    await logout(); // Uses existing auth store logout action
    navigate('/'); // Redirect to homepage
  } catch (error) {
    console.error('Logout failed:', error);
    // Error handling can use existing notification system
  }
};
```

## State Management

### Authentication State Integration
**Uses Existing Zustand Hooks:**
```typescript
// Import existing auth hooks (NO CHANGES to authStore.ts)
import { useUser, useIsAuthenticated, useAuthActions } from '../../stores/authStore';

// Component state access
const user = useUser();                    // Individual selector (prevents re-renders)
const isAuthenticated = useIsAuthenticated(); // Individual selector
const { logout } = useAuthActions();       // Actions
```

### Role-Based Rendering Logic
```typescript
// Admin link visibility check
const isAdmin = user?.roles?.includes('Admin') || false;

// Conditional rendering patterns:
{isAuthenticated && (
  <UserGreeting sceneName={user?.sceneName || ''} />
)}

{isAuthenticated && isAdmin && (
  <AdminNavLink />
)}
```

### Navigation State Transitions
```typescript
// Login state change (existing auth store handles this)
useEffect(() => {
  // Navigation automatically updates via auth store selectors
  // No additional state management needed
}, [isAuthenticated, user]);
```

## Integration Points

### Existing Authentication System
- **Auth Store**: Uses existing Zustand store with session storage persistence
- **Login Flow**: No changes to existing login/register pages
- **Logout Flow**: Uses existing auth store logout action
- **Route Protection**: Existing authLoader patterns continue to work

### Navigation Routing
- **Dashboard Route**: `/dashboard` (must exist and be accessible)
- **Admin Route**: `/admin` (must exist and be role-protected)
- **Existing Routes**: All current routes remain unchanged

### CSS Styling Integration
- **Preserve All Animations**: nav-underline-animation, utility-bar-link hover effects
- **Maintain Color System**: Design System v7 colors via CSS variables
- **Responsive Design**: Existing mobile breakpoint patterns preserved

## Security Requirements

### Authentication Validation
- Admin link visibility based on `user.roles` array check
- Logout functionality uses secure auth store action (clears all auth state)
- No sensitive data displayed beyond scene name in utility bar
- Role checks performed client-side for UI display only

### Authorization Rules
```typescript
// Admin link display rule
const canSeeAdmin = user?.roles?.includes('Admin') || false;

// Logout access rule  
const canLogout = isAuthenticated && user !== null;

// User greeting display rule
const showGreeting = isAuthenticated && user?.sceneName;
```

### Data Privacy
- Only scene name displayed in navigation (respects privacy)
- No role information exposed in UI text
- Authentication state handled by existing secure patterns

## Performance Requirements

### Component Re-render Optimization
- **Individual Selectors**: Uses existing optimized auth store selectors
- **Conditional Rendering**: Minimal DOM updates via React conditional patterns
- **Animation Performance**: Existing CSS transform/opacity animations preserved

### Response Time Targets
- **Navigation Updates**: < 100ms when auth state changes
- **Logout Action**: < 500ms for complete logout process
- **Role Check Rendering**: Instantaneous (no API calls needed)

### Memory Management
- No new memory allocations beyond existing auth store
- Component cleanup handled by React lifecycle
- No memory leaks introduced by event listeners

## Testing Requirements

### Unit Test Coverage (Target: 90%+)
```typescript
describe('Navigation Component', () => {
  it('shows Dashboard button for authenticated users', () => {
    // Test authenticated state rendering
  });
  
  it('shows Login button for guest users', () => {
    // Test unauthenticated state rendering
  });
  
  it('shows Admin link only for administrators', () => {
    // Test role-based rendering
  });
  
  it('preserves existing navigation animations', () => {
    // Test CSS animation classes are applied
  });
});

describe('UtilityBar Component', () => {
  it('displays user greeting for authenticated users', () => {
    // Test welcome message rendering
  });
  
  it('shows logout link for authenticated users', () => {
    // Test logout button presence
  });
  
  it('handles logout action correctly', () => {
    // Test logout functionality
  });
  
  it('preserves existing utility bar styling', () => {
    // Test CSS classes and animations
  });
});
```

### E2E Test Scenarios
```typescript
describe('Navigation E2E Tests', () => {
  it('should update navigation after login', async () => {
    // 1. Visit homepage as guest
    // 2. Click Login button  
    // 3. Complete login form
    // 4. Verify Dashboard button appears
    // 5. Verify welcome message in utility bar
    // 6. Verify logout link in utility bar
  });
  
  it('should show admin link for administrators', async () => {
    // 1. Login as admin user
    // 2. Verify Admin link appears in navigation
    // 3. Click Admin link
    // 4. Verify navigation to /admin route
  });
  
  it('should handle logout flow correctly', async () => {
    // 1. Login as any user
    // 2. Click logout in utility bar
    // 3. Verify redirect to homepage
    // 4. Verify Login button appears (Dashboard hidden)
    // 5. Verify user greeting removed from utility bar
  });
});
```

### Visual Regression Testing
- Screenshot comparisons for all authentication states
- Animation behavior verification
- Mobile responsive layout testing
- Color contrast validation for accessibility

## Acceptance Criteria

### Dashboard CTA Implementation
- [ ] **Login button replaced**: Dashboard CTA appears for authenticated users
- [ ] **Styling preserved**: Uses existing `btn btn-primary` classes
- [ ] **Navigation works**: Clicking Dashboard navigates to `/dashboard` route
- [ ] **Guest experience**: Login button remains for unauthenticated users

### Utility Bar Updates  
- [ ] **User greeting**: "Welcome, [sceneName]" appears on LEFT for authenticated users
- [ ] **Logout placement**: Logout link appears on RIGHT after Contact link
- [ ] **Styling consistent**: Uses existing utility-bar-link animations
- [ ] **Logout functionality**: Clicking logout properly logs out and redirects

### Admin Navigation Access
- [ ] **Role-based display**: Admin link appears only for users with "Admin" role  
- [ ] **Positioning correct**: Admin link positioned left of "Events & Classes"
- [ ] **Styling consistent**: Uses existing nav-underline-animation
- [ ] **Navigation works**: Clicking Admin navigates to `/admin` route

### Animation & Style Preservation
- [ ] **All animations work**: nav-underline-animation, utility-bar-link hover effects
- [ ] **Color system intact**: Design System v7 colors via CSS variables
- [ ] **Responsive design**: Mobile breakpoints work correctly
- [ ] **Focus indicators**: Keyboard navigation accessibility preserved

### Authentication Integration
- [ ] **State synchronization**: UI updates immediately when auth state changes
- [ ] **Role checking**: Admin link visibility updates with role changes  
- [ ] **Logout behavior**: Auth state cleared, user redirected correctly
- [ ] **Session handling**: Navigation state persists across page reloads

### Browser Compatibility
- [ ] **Chrome/Edge**: All functionality works in Chromium browsers
- [ ] **Firefox**: Cross-browser compatibility verified
- [ ] **Safari**: WebKit compatibility confirmed
- [ ] **Mobile browsers**: Touch interactions work correctly

## Migration Requirements

### Component Changes Only
- **Navigation.tsx**: Add admin link and dashboard CTA logic
- **UtilityBar.tsx**: Add user greeting and logout functionality
- **No Database Changes**: Uses existing user roles and authentication
- **No API Changes**: Uses existing auth store and endpoints

### CSS Updates (Minimal)
- **No new CSS classes**: Uses existing button and animation classes
- **Preserve all styling**: Existing color system and animations maintained
- **Mobile responsiveness**: Existing breakpoint behavior preserved

### Testing Migration
- **Update existing tests**: Modify auth-related navigation tests
- **Add new test cases**: Cover new admin link and utility bar functionality
- **E2E test updates**: Update test selectors and expectations

## Dependencies

### Technical Dependencies
- **Existing Auth Store**: Zustand store with user role data (`useUser`, `useIsAuthenticated`)  
- **React Router**: Navigation to `/dashboard` and `/admin` routes
- **Mantine UI**: Uses existing `Box`, `Group` components and styling patterns
- **CSS System**: Design System v7 color variables and animation classes

### Route Dependencies
```typescript
// These routes must exist and be accessible:
const requiredRoutes = [
  '/dashboard',  // Dashboard page for all authenticated users
  '/admin',      // Admin page with role-based protection  
  '/login',      // Existing login page (unchanged)
  '/',           // Homepage for logout redirect
];
```

### User Role Dependencies
```typescript
// Required role data structure (existing):
interface UserDto {
  roles?: string[];  // Must contain "Admin" for admin users
  sceneName?: string; // Required for welcome message
  // ... other existing fields
}
```

## Risk Assessment & Mitigation

### High Risk: User Confusion During Transition
**Risk**: Existing users may be confused by authentication UI moving to utility bar
**Likelihood**: Medium
**Impact**: High  
**Mitigation**: 
- Maintain clear visual hierarchy with existing animations
- Use recognizable patterns (welcome message, prominent logout link)
- Consider brief user communication about navigation improvements

### Medium Risk: Role-Based Rendering Issues
**Risk**: Admin link may not display correctly for all administrator users
**Likelihood**: Low
**Impact**: Medium
**Mitigation**:
- Comprehensive role checking logic with fallback to `false`
- Thorough testing with different user role combinations
- Logging for debugging role-based rendering issues

### Low Risk: Mobile Usability Impact
**Risk**: Utility bar functionality may be less accessible on mobile
**Likelihood**: Low  
**Impact**: Low
**Mitigation**:
- Preserve existing responsive design patterns
- Test thoroughly on mobile devices and various screen sizes
- Maintain existing mobile menu functionality

### Low Risk: Animation/Styling Regressions
**Risk**: Existing animations or styling may break during implementation
**Likelihood**: Low
**Impact**: Medium
**Mitigation**:
- Use existing CSS classes and animation patterns exactly
- Visual regression testing to catch styling changes
- Preserve all existing CSS variable usage

## Implementation Strategy

### Phase 1: Navigation Component Updates (High Priority)
1. **Dashboard CTA**: Replace login button with Dashboard link for authenticated users
2. **Admin Link**: Add conditional Admin navigation link for administrators  
3. **Preserve Styling**: Maintain all existing button and animation classes
4. **Test Integration**: Update existing navigation tests

### Phase 2: UtilityBar Component Updates (High Priority)  
1. **User Greeting**: Add welcome message to LEFT side of utility bar
2. **Logout Link**: Add logout button to RIGHT side after Contact link
3. **Event Handling**: Implement logout action using existing auth store
4. **Animation Preservation**: Maintain existing utility-bar-link hover effects

### Phase 3: Testing & Validation (Medium Priority)
1. **Unit Tests**: Add comprehensive component tests for new functionality
2. **E2E Tests**: Update and create navigation flow tests
3. **Visual Testing**: Screenshot comparison testing for all auth states
4. **Accessibility**: Keyboard navigation and screen reader testing

### Phase 4: Documentation & Monitoring (Low Priority)
1. **User Documentation**: Brief communication about navigation updates  
2. **Analytics**: Track Dashboard usage and admin feature access
3. **Performance Monitoring**: Ensure no regression in navigation performance
4. **Success Metrics**: Measure user engagement improvements

## Quality Gate Checklist (95% Required)

### Functional Requirements ✓
- [ ] Dashboard CTA replaces login button for authenticated users
- [ ] Admin link appears only for users with "Admin" role
- [ ] User greeting displays in utility bar LEFT for authenticated users  
- [ ] Logout link appears in utility bar RIGHT for authenticated users
- [ ] All existing navigation functionality preserved

### Technical Implementation ✓
- [ ] Uses existing Zustand auth store hooks (`useUser`, `useIsAuthenticated`)
- [ ] Role checking implemented with `user?.roles?.includes('Admin')`
- [ ] Logout handler uses existing `logout()` auth action
- [ ] Navigation routing works for `/dashboard` and `/admin` routes
- [ ] Component re-rendering optimized with individual selectors

### Styling & Animation Preservation ✓  
- [ ] All existing CSS classes preserved (`btn btn-primary`, `nav-underline-animation`)
- [ ] Design System v7 colors used via CSS variables
- [ ] Utility bar link hover animations maintained
- [ ] Mobile responsive design patterns preserved
- [ ] Keyboard navigation accessibility preserved

### Security & Authorization ✓
- [ ] Admin link visibility based on secure role checking
- [ ] Logout functionality properly clears all authentication state
- [ ] No sensitive user information exposed beyond scene name
- [ ] Client-side role checks for UI display only (server-side protection exists)

### Testing Coverage ✓
- [ ] Unit tests cover all new conditional rendering logic
- [ ] E2E tests verify complete authentication state transitions
- [ ] Visual regression tests ensure styling preservation
- [ ] Accessibility testing confirms keyboard navigation works
- [ ] Cross-browser compatibility verified

### Performance & Reliability ✓
- [ ] Navigation updates occur within 100ms of auth state changes
- [ ] No memory leaks introduced by new event handlers
- [ ] Component re-renders optimized via individual Zustand selectors
- [ ] Error handling for logout failures implemented

This functional specification provides comprehensive implementation guidance for the navigation updates while ensuring all existing functionality, styling, and performance characteristics are preserved exactly as they currently exist.