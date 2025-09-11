# Business Requirements: Navigation Updates for Logged-in Users
<!-- Last Updated: 2025-09-11 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary

This enhancement improves the user experience for authenticated members by restructuring the navigation to provide clearer visual hierarchy, improved accessibility to key features, and role-based administrative access. The changes move authentication status information to the utility bar while promoting primary user actions (Dashboard) to the main navigation, creating a more intuitive and functional interface.

## Business Context

### Problem Statement
The current navigation structure places authentication information (welcome message, logout) in the primary navigation area, creating visual clutter and competing with core platform functions. Additionally, administrators lack direct navigation access to administrative features, requiring them to navigate through other pages to access admin functionality.

### Business Value
- **Improved User Experience**: Cleaner primary navigation focused on core platform features
- **Enhanced Administrative Efficiency**: Direct admin access reduces clicks and improves workflow
- **Better Visual Hierarchy**: Authentication status in utility bar provides clear information architecture
- **Increased Dashboard Usage**: Prominent Dashboard link encourages member engagement with personalized features
- **Reduced Cognitive Load**: Cleaner navigation reduces decision fatigue for users

### Success Metrics
- Increased Dashboard page visits by authenticated users (target: +40%)
- Reduced time-to-admin-features for administrators (target: -50%)
- Improved user satisfaction scores for navigation experience (target: +25%)
- Reduced support tickets related to navigation confusion (target: -30%)

## User Stories

### Story 1: Member Dashboard Access
**As a** logged-in member (any role)
**I want to** see a prominent "Dashboard" link in the main navigation
**So that** I can quickly access my personalized member area and account features

**Acceptance Criteria:**
- Given I am logged in as any user role
- When I view the main navigation
- Then I see a "Dashboard" call-to-action link where the Login button previously appeared
- And the Dashboard link is visually prominent (styled as primary CTA)
- And clicking the Dashboard link navigates me to `/dashboard`

### Story 2: Clean Primary Navigation
**As a** logged-in member
**I want to** see a clean primary navigation focused on platform features
**So that** I can easily find and access core platform functionality without distraction

**Acceptance Criteria:**
- Given I am logged in
- When I view the main navigation
- Then I do not see authentication status information (welcome message, logout button)
- And the primary navigation contains only: Logo, Admin (if admin), Events & Classes, How to Join, Resources, Dashboard
- And all navigation items are clearly visible and accessible

### Story 3: Authentication Status in Utility Bar
**As a** logged-in member
**I want to** see my authentication status in the utility bar
**So that** I know I'm logged in and can access logout functionality without cluttering primary navigation

**Acceptance Criteria:**
- Given I am logged in
- When I view the utility bar at the top of the page
- Then I see "Welcome, [sceneName]" on the left side of the utility bar
- And I see a "Logout" link on the right side of the utility bar (after Contact link)
- And the welcome message clearly identifies my logged-in status
- And clicking Logout properly logs me out and redirects to homepage

### Story 4: Administrator Quick Access
**As an** administrator
**I want to** see an "Admin" link in the main navigation
**So that** I can quickly access administrative features without extra navigation steps

**Acceptance Criteria:**
- Given I am logged in with Admin role
- When I view the main navigation
- Then I see an "Admin" link positioned to the left of "Events & Classes"
- And clicking the Admin link navigates me to `/admin`
- And the Admin link is styled consistently with other navigation items
- And non-admin users do not see the Admin link

### Story 5: Guest User Experience
**As a** non-logged-in visitor
**I want to** see the login option in the primary navigation
**So that** I can easily access the login functionality

**Acceptance Criteria:**
- Given I am not logged in
- When I view the main navigation
- Then I see a "Login" button where authenticated users see "Dashboard"
- And I do not see any authentication status information
- And the utility bar shows only the standard public links (Report an Incident, Private Lessons, Contact)

### Story 6: Responsive Mobile Experience
**As a** mobile user (authenticated or guest)
**I want to** access all navigation features on my mobile device
**So that** I have full functionality regardless of device

**Acceptance Criteria:**
- Given I access the site on a mobile device
- When I interact with navigation elements
- Then all functionality works correctly on mobile breakpoints
- And the utility bar remains accessible and functional
- And the mobile menu toggle continues to work properly
- And all new navigation elements are accessible via mobile menu

## Business Rules

### Authentication-Based Display Rules
1. **Unauthenticated Users**: Show Login button in main navigation, standard utility bar links only
2. **Authenticated Users**: Show Dashboard button in main navigation, add welcome message and logout to utility bar
3. **Admin Users**: Additionally show Admin link in main navigation, positioned before Events & Classes

### Navigation Positioning Rules
1. **Utility Bar Left**: Welcome message ("Welcome, [sceneName]") for authenticated users only
2. **Utility Bar Right**: Standard links (Report an Incident, Private Lessons, Contact) + Logout for authenticated users
3. **Main Navigation**: Logo, [Admin], Events & Classes, How to Join, Resources, [Login/Dashboard]

### Role-Based Access Rules
1. **Admin Link Visibility**: Only visible to users with UserRole.Admin (role level 4)
2. **Dashboard Access**: Available to all authenticated users regardless of role
3. **Logout Functionality**: Available to all authenticated users

### Link Ordering Requirements
1. **Main Navigation Order**: Logo → Admin (if admin) → Events & Classes → How to Join → Resources → Dashboard/Login
2. **Utility Bar Order**: Welcome message (left) → Report an Incident → Private Lessons → Contact → Logout (if authenticated)

## Constraints & Assumptions

### Technical Constraints
- Must work within existing React + TypeScript architecture
- Must utilize existing Zustand auth store for authentication state
- Must maintain existing responsive design patterns
- Must preserve existing CSS styling variables and classes
- Cannot break existing navigation functionality

### Design Constraints
- Must maintain visual consistency with existing design system
- Must preserve accessibility standards (WCAG 2.1 compliance)
- Must work with existing color scheme (burgundy, charcoal, cream, etc.)
- Must maintain existing hover animations and transitions

### Business Constraints
- Changes must not confuse existing users during transition
- Must maintain all existing navigation functionality
- Cannot impact SEO or site performance negatively
- Must be implementable without backend API changes

### Assumptions
- Users will adapt quickly to authentication information in utility bar
- Dashboard functionality exists and is accessible at `/dashboard` route
- Admin functionality exists and is accessible at `/admin` route
- Current auth store provides reliable role-based information

## Security & Privacy Requirements

### Authentication State Security
- Authentication status must be determined from secure auth store only
- Role-based navigation must verify permissions before display
- Logout functionality must properly clear all authentication state
- Navigation should not expose sensitive user information beyond scene name

### Authorization Requirements
- Admin navigation link must only display for verified admin users
- Navigation should gracefully handle authorization failures
- Role checks must be performed client-side for UI display, server-side for actual access

### Data Privacy Considerations
- Scene name display in utility bar respects user privacy preferences
- No sensitive user information should be displayed in navigation
- Authentication state should not be logged or tracked unnecessarily

## Compliance Requirements

### Accessibility Standards
- All navigation elements must be keyboard accessible
- Screen reader compatibility must be maintained
- Color contrast requirements must be met for all text elements
- Focus indicators must be clearly visible

### Platform Policies
- Changes must align with community safety standards
- Navigation must support anonymous incident reporting functionality
- No impact on existing safety and consent workflow patterns

## User Impact Analysis

| User Type | Impact | Priority | Specific Changes |
|-----------|--------|----------|------------------|
| **Admin** | High Positive | High | Gains direct admin access, cleaner navigation |
| **Teacher** | Medium Positive | Medium | Cleaner navigation, prominent dashboard access |
| **Vetted Member** | Medium Positive | Medium | Improved user experience, clearer information hierarchy |
| **General Member** | Medium Positive | Medium | Better navigation clarity, dashboard promotion |
| **Guest/Public** | Low Neutral | Low | No functional changes, maintains existing experience |

## Data Structure Requirements

### User Authentication Data
Based on existing auth store structure:
- `user.sceneName`: string (required for welcome message)
- `user.role`: UserRole enum (required for admin link visibility)
- `isAuthenticated`: boolean (required for navigation state determination)

### Navigation State Requirements
- Current authentication status from auth store
- User role level for conditional admin link display
- Scene name for personalized welcome message
- Logout capability through existing auth actions

## Examples/Scenarios

### Scenario 1: Admin User Login Experience
1. Admin logs in successfully
2. Navigation updates to show: Logo → **Admin** → Events & Classes → How to Join → Resources → **Dashboard**
3. Utility bar shows: **Welcome, [sceneName]** (left) → Report an Incident → Private Lessons → Contact → **Logout** (right)
4. Admin clicks "Admin" link and navigates to administrative dashboard
5. Admin clicks "Logout" from utility bar and returns to public homepage

### Scenario 2: General Member Navigation
1. General member is logged in
2. Navigation shows: Logo → Events & Classes → How to Join → Resources → **Dashboard** (no Admin link)
3. Utility bar shows: **Welcome, [sceneName]** (left) → Report an Incident → Private Lessons → Contact → **Logout** (right)
4. Member clicks "Dashboard" for personalized experience
5. Member finds logout easily in utility bar when session ends

### Scenario 3: Guest User Experience
1. Visitor accesses site without logging in
2. Navigation shows: Logo → Events & Classes → How to Join → Resources → **Login**
3. Utility bar shows: Report an Incident → Private Lessons → Contact (no welcome or logout)
4. Visitor clicks "Login" to access authentication
5. Navigation remains clean and uncluttered for discovery experience

### Scenario 4: Mobile User Interaction
1. Authenticated user accesses site on mobile device
2. Utility bar remains visible and accessible at top
3. Main navigation collapses into mobile menu as expected
4. All new functionality (Dashboard, Admin if applicable) available in mobile menu
5. Logout remains accessible through utility bar on mobile

## Technical Implementation Considerations

### Component Updates Required
1. **Navigation.tsx**: Update authentication logic and conditional rendering
2. **UtilityBar.tsx**: Add welcome message and logout functionality
3. **Auth Store**: Ensure role-based data is properly exposed
4. **CSS/Styling**: Update responsive behavior and styling

### State Management Integration
- Utilize existing `useUser()`, `useIsAuthenticated()`, `useAuthActions()` hooks
- Implement role checking logic using existing user role data
- Maintain existing logout functionality through auth store actions

### Routing Considerations
- Ensure `/dashboard` route exists and is accessible
- Ensure `/admin` route exists and is properly protected
- Maintain existing route protection and authentication requirements

## Questions for Product Manager

- [ ] Is the Dashboard page fully functional and ready for increased traffic?
- [ ] Are there specific admin features that should be prioritized in the Admin section?
- [ ] Should we implement any onboarding or notification for users about the navigation changes?
- [ ] Are there analytics we should track to measure the success of these changes?
- [ ] Should we implement any gradual rollout or A/B testing for the navigation changes?
- [ ] Are there any specific accessibility requirements beyond WCAG 2.1 compliance?

## Quality Gate Checklist (95% Required)

- [ ] All user roles (Guest, Member, VettedMember, Teacher, Admin) addressed
- [ ] Clear acceptance criteria defined for each navigation change
- [ ] Business value quantified with measurable success metrics
- [ ] Authentication and authorization security requirements documented
- [ ] Mobile responsive experience requirements specified
- [ ] Accessibility requirements clearly defined
- [ ] Role-based conditional rendering logic documented
- [ ] Impact on existing functionality assessed
- [ ] Technical constraints and assumptions documented
- [ ] User experience scenarios provide clear examples
- [ ] Data structure requirements aligned with existing auth store
- [ ] Navigation positioning and ordering rules clearly specified
- [ ] Privacy and security implications considered
- [ ] Success metrics and measurement strategy defined
- [ ] Edge cases and error handling considerations included

## Dependencies

### Technical Dependencies
- Existing React Router configuration for `/dashboard` and `/admin` routes
- Zustand auth store functionality and user role data
- Current CSS styling variables and responsive breakpoints
- Existing authentication state management patterns

### Business Dependencies
- Dashboard page functionality and user acceptance
- Admin section completeness and usability
- User communication strategy for navigation changes
- Analytics tracking setup for success measurement

### Design Dependencies
- Approval of visual hierarchy changes
- Confirmation of styling consistency requirements
- Mobile experience validation and testing
- Accessibility audit completion

## Risk Assessment & Mitigation

### High Risk: User Confusion
**Risk**: Existing users may be confused by authentication information moving to utility bar
**Mitigation**: Implement clear visual design, consider brief onboarding notification, maintain logout accessibility

### Medium Risk: Mobile Usability
**Risk**: Utility bar functionality may be less accessible on mobile devices
**Mitigation**: Ensure responsive design maintains accessibility, test thoroughly on mobile devices

### Low Risk: Performance Impact
**Risk**: Additional conditional rendering may impact page load performance
**Mitigation**: Optimize component rendering, utilize existing auth store efficiently

### Low Risk: Role Management
**Risk**: Role-based admin link display may not work correctly
**Mitigation**: Implement proper role checking logic, include comprehensive testing for all user roles

This business requirements document provides a comprehensive foundation for implementing the navigation updates while ensuring all user needs, technical constraints, and business objectives are properly addressed.