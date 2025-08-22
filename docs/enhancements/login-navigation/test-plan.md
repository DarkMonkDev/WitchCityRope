# Login Navigation Test Plan

## Test Scope
This test plan covers the verification of post-login navigation functionality for different user roles in the WitchCityRope application.

## Test Accounts
| Email | Password | Role | Expected Menu Items |
|-------|----------|------|-------------------|
| admin@witchcityrope.com | Test123! | Administrator | My Profile, My Tickets, Settings, Admin Panel, Logout |
| staff@witchcityrope.com | Test123! | Moderator | My Profile, My Tickets, Settings, Logout |
| member@witchcityrope.com | Test123! | Member | My Profile, My Tickets, Settings, Logout |
| guest@witchcityrope.com | Test123! | Attendee | My Profile, My Tickets, Settings, Logout |
| organizer@witchcityrope.com | Test123! | Moderator | My Profile, My Tickets, Settings, Logout |

## Test Scenarios

### 1. Pre-Login Navigation Test
**Objective**: Verify navigation shows correct items for unauthenticated users
- [ ] Navigate to home page
- [ ] Verify "Login" button is visible
- [ ] Verify authenticated menu items are NOT visible
- [ ] Verify public menu items are accessible

### 2. Admin Login Navigation Test
**Objective**: Verify admin users see all navigation options including admin panel
- [ ] Login as admin@witchcityrope.com
- [ ] Verify redirect to /member/dashboard
- [ ] Verify user dropdown menu contains:
  - My Profile
  - My Tickets
  - Settings
  - Admin Panel (admin only)
  - Logout
- [ ] Click Admin Panel and verify navigation to /admin
- [ ] Verify admin dashboard loads correctly

### 3. Member Login Navigation Test
**Objective**: Verify regular members see appropriate navigation without admin options
- [ ] Login as member@witchcityrope.com
- [ ] Verify redirect to /member/dashboard
- [ ] Verify user dropdown menu contains standard items
- [ ] Verify Admin Panel link is NOT visible
- [ ] Test navigation to My Profile, My Tickets, Settings

### 4. Staff/Moderator Login Navigation Test
**Objective**: Verify moderators see appropriate navigation
- [ ] Login as staff@witchcityrope.com
- [ ] Verify navigation items match moderator role
- [ ] Verify access to any moderator-specific features

### 5. Guest/Attendee Login Navigation Test
**Objective**: Verify basic users see minimal navigation options
- [ ] Login as guest@witchcityrope.com
- [ ] Verify basic navigation items only
- [ ] Verify restricted access to member-only features

### 6. Mobile Navigation Test
**Objective**: Verify navigation works correctly on mobile devices
- [ ] Test with mobile viewport
- [ ] Verify hamburger menu functionality
- [ ] Test all navigation items in mobile menu
- [ ] Verify proper menu collapse/expand

### 7. Navigation Link Functionality Test
**Objective**: Verify all navigation links work correctly
- [ ] Test each navigation link for each role
- [ ] Verify correct page loads
- [ ] Verify no 404 or error pages
- [ ] Check for proper authorization redirects

### 8. Logout Navigation Test
**Objective**: Verify navigation reverts after logout
- [ ] Click Logout from user menu
- [ ] Verify redirect to home page
- [ ] Verify navigation shows Login button
- [ ] Verify authenticated items are hidden

### 9. Deep Link After Login Test
**Objective**: Verify returnUrl functionality
- [ ] Navigate to protected page while logged out
- [ ] Verify redirect to login with returnUrl
- [ ] Login and verify redirect to original page
- [ ] Verify navigation is properly updated

### 10. UI/Wireframe Compliance Test
**Objective**: Verify UI matches wireframe designs
- [ ] Compare navigation styling with wireframes
- [ ] Verify menu placement and layout
- [ ] Check responsive behavior
- [ ] Verify color scheme and styling

## Automated Test Implementation
All scenarios above will be implemented as automated integration tests in:
`/tests/WitchCityRope.IntegrationTests/LoginNavigationTests.cs`

## Success Criteria
- All test scenarios pass
- Navigation updates immediately after login
- Role-based menu items display correctly
- All links navigate to correct pages
- UI matches wireframe specifications
- No console errors or warnings
- Performance is acceptable (navigation updates < 100ms)