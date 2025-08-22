# Business Requirements: User Dashboard Redesign (SIMPLIFIED)
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 3.0 - CRITICAL SIMPLIFICATION UPDATE -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Simplified -->

## 🚨 CRITICAL SIMPLIFICATION REQUIREMENTS 🚨

Based on stakeholder feedback, this is a **simple website with simple functionality**. The previous requirements were over-engineered for our needs.

### Key Simplification Principles:
- **REMOVE**: Multiple layers, floating boxes, redundant elements
- **REMOVE**: Complex UI patterns and overcomplications
- **FOCUS**: Edge-to-edge layout like main website
- **PRIORITY**: Get functionality working correctly, not complex design
- **FUTURE**: May redesign later - don't over-engineer now

## Architecture Discovery Results

### Documents Reviewed:
- **domain-layer-architecture.md**: Lines 725-997 - Found complete NSwag implementation for TypeScript type generation
- **DTO-ALIGNMENT-STRATEGY.md**: Lines 85-213 - Confirmed API as source of truth, NSwag auto-generation requirement  
- **migration-plan.md**: Lines 1-100 - Found React infrastructure status (complete) and Mantine v7 selection
- **functional-area-master-index.md**: User Dashboard area exists with current active work path identified
- **business-requirements-lessons-learned.md**: Critical validation requirements for DTO specification standards

### Legacy Wireframes Reviewed:
- **user-dashboard-visual.html**: Main dashboard layout with prominent event display and welcome section
- **member-security-settings-visual.html**: Security settings with sidebar navigation showing Profile/Membership/Security/Notifications structure
- **member-my-tickets-visual.html**: Events page with filter tabs for Upcoming/Past/Cancelled events
- **member-profile-settings-visual.html**: Profile editing interface with sidebar navigation
- **member-membership-settings.html**: Membership status and management interface
- **user-menu-wireframes.md**: Complete navigation patterns for authenticated and unauthenticated states

### Existing Solutions Found:
- **NSwag Type Generation**: Complete TypeScript interface generation from C# DTOs (lines 725-774)
- **Authentication System**: Complete React implementation (Milestone achieved 2025-08-19)
- **UI Framework**: Mantine v7 validated and integrated (ADR-004, Infrastructure complete 2025-08-18)
- **State Management**: TanStack Query v5 + Zustand patterns proven in authentication milestone
- **Design System v7**: Integrated on homepage with Mantine v7 components

### Verification Statement:
Confirmed existing solutions for all technical foundations. No manual DTO creation required - NSwag generates all TypeScript types. React infrastructure and design systems are production-ready. Legacy wireframes provide general direction - creating simplified version, not exact copies.

## Executive Summary

The User Dashboard Redesign project will create a **simple, clean dashboard** replacing the legacy Blazor Server implementation. The dashboard features a **left-mounted menu** with 5 sections (Dashboard, Events, Profile, Security, Membership) and **edge-to-edge layout** extending across the entire page. The primary focus is **functionality over complex design**, showing users their upcoming RSVP'd events without unnecessary UI complications.

## Business Context

### Problem Statement

The current Blazor Server dashboard needs to be replaced with a simple React implementation. Users need straightforward access to their upcoming events and basic account functions. **Previous requirements were too complex** - this is a simple website that should focus on getting the correct functionality and navigation working without floating boxes, layers, or overcomplicated UI patterns.

### Business Value

- **Simplified Navigation**: Clear 5-section menu without complexity
- **Direct Event Access**: Show user's actual events without extra layers
- **Clean Design**: Edge-to-edge layout like main website
- **Functional Focus**: Working features over visual complexity
- **Future-Ready**: Simple foundation that can be enhanced later

### Success Metrics

- **Performance**: Dashboard load time <1.5 seconds
- **Usability**: Users can complete tasks without confusion
- **Simplicity**: Zero UI complexity complaints
- **Functionality**: All basic features work correctly
- **Mobile Usage**: Clean mobile experience

## User Stories

### Story 1: Simple Event Display for Users
**As a** member with upcoming events  
**I want to** see my RSVP'd events displayed simply on my dashboard  
**So that** I can quickly see my commitments without UI clutter

**Acceptance Criteria:**
- Given I have upcoming events
- When I log into my dashboard
- Then I see "Welcome back, [Scene Name]" at the top
- And my RSVP'd/ticketed events are displayed directly below
- And events extend across the entire page (no floating boxes)
- And events show: name, date, time, location, status
- And there are NO redundant welcome messages or subtitles
- And there are NO quick action buttons (menu is visible)

### Story 2: Simple Left-Mounted Navigation
**As any** authenticated user  
**I want to** use a simple left-mounted menu for navigation  
**So that** I can access dashboard sections without complexity

**Acceptance Criteria:**
- Given I am on any dashboard page
- When I view the left navigation
- Then I see exactly 5 sections: Dashboard, Events, Profile, Security, Membership
- And the menu is mounted to the left side (not floating)
- And the current page is clearly highlighted
- And the menu works on mobile without complex patterns
- And there are NO multiple layers or background boxes

### Story 3: Simplified Security Settings
**As any** authenticated user  
**I want to** manage basic security settings in a simple interface  
**So that** I can change my password and manage essential security

**Acceptance Criteria:**
- Given I click on "Security" in the left navigation
- When the Security page loads
- Then I see basic password change functionality
- And I see basic 2FA setup (if enabled)
- And I see essential privacy settings
- And there are NO two-factor authentication backup codes
- And there are NO active sessions boxes/sections
- And there are NO security updates in notifications
- And the interface extends across the page (no floating layers)

### Story 4: Edge-to-Edge Layout Design
**As any** user  
**I want** all content to extend across the entire page  
**So that** I have a clean, simple interface without floating elements

**Acceptance Criteria:**
- Given I access any dashboard page
- When I view the content
- Then all content extends across the entire page width
- And there are NO floating boxes or layers
- And there are NO multiple background containers
- And the design is clean and minimal
- And the layout matches the main website's edge-to-edge approach

### Story 5: Mobile Simple Experience
**As any** user accessing via mobile device  
**I want** a simple responsive experience  
**So that** I can use the dashboard effectively without complexity

**Acceptance Criteria:**
- Given I access the dashboard on mobile
- When I interact with the interface
- Then the left menu collapses appropriately
- And all content remains readable
- And there are NO complex mobile patterns
- And touch targets work properly
- And the design remains simple and clean

## Business Rules

### Design Simplicity Rules
1. **NO Floating Boxes**: All content extends across entire page width
2. **NO Multiple Layers**: Simple, flat design without background containers
3. **NO Redundant Elements**: Remove duplicate welcome messages, subtitles, quick actions
4. **NO Over-Engineering**: This is a simple website - keep functionality simple
5. **Edge-to-Edge Layout**: Content spans full width like main website

### Navigation Rules
1. **5-Section Menu**: Dashboard, Events, Profile, Security, Membership only
2. **Left-Mounted**: Menu attached to left side, not floating
3. **Simple Mobile**: Basic responsive collapse, no complex patterns
4. **Current Page Highlighting**: Clear indication of current location
5. **Quick Actions Removed**: Menu is visible - buttons are redundant

### Content Display Rules
1. **User Events Only**: Show only events user has RSVP'd to or purchased tickets for
2. **Simple Welcome**: "Welcome back, [Scene Name]" only - no subtitles
3. **Direct Display**: Events shown directly without extra containers
4. **No Caching**: Real-time data, immediate updates
5. **Status Clarity**: Clear event status without visual complexity

### Security Page Simplification
1. **Basic Password Change**: Simple form for password updates
2. **Basic 2FA**: Essential two-factor authentication setup only
3. **Essential Privacy**: Core privacy settings only
4. **NO Backup Codes**: Remove 2FA backup codes section
5. **NO Active Sessions**: Remove sessions monitoring box
6. **NO Security Notifications**: Remove from notification options

## Constraints & Assumptions

### Simplification Constraints
- **No Complex UI**: Avoid templates that are too complex for our needs
- **Functionality First**: Get correct functionality working before visual enhancements
- **Simple Templates**: Use basic, proven React/Mantine patterns
- **Future Redesign**: May redesign later - don't over-engineer now
- **Edge-to-Edge Only**: No floating or contained layouts

### Technical Constraints
- **React Architecture**: Must use React + TypeScript + Vite stack
- **UI Framework**: Must use Mantine v7 components (simple ones)
- **Type Safety**: Must use NSwag generated types
- **Authentication**: Must integrate with existing auth system
- **Mobile Support**: Must work on mobile devices simply

### Business Constraints
- **Keep It Simple**: This is a simple website with simple functionality
- **No Over-Complication**: Most templates are too complex for our needs
- **Left Menu Focus**: Simple menu-based navigation
- **Functionality Priority**: Working features over complex design
- **Clean Interface**: Minimal UI without multiple layers

## Security & Privacy Requirements

### Authentication & Authorization
- **Session Management**: Use existing httpOnly cookie + JWT authentication
- **Role Verification**: Verify user roles on each request
- **Session Timeout**: 2-hour inactivity timeout
- **Simple Security**: Basic security settings only

### Data Protection
- **PII Handling**: Encrypt data in transit and at rest
- **Privacy Controls**: Basic user privacy settings
- **Data Minimization**: Only necessary data for dashboard
- **Simple Forms**: Basic validation without complexity

## Examples/Scenarios

### Scenario 1: Simple Dashboard Experience
**Context**: User logs in to check their events

**Step-by-step walkthrough**:
1. User opens dashboard and sees simple "Welcome back, [Scene Name]"
2. Below welcome, events are displayed edge-to-edge across page
3. Each event shows basic info: name, date, time, location, status
4. Left menu shows 5 simple sections
5. User clicks event to see details
6. No floating boxes, layers, or complex UI elements

**Success Criteria**: Clean, simple experience without UI complexity

### Scenario 2: Mobile Simple Navigation
**Context**: User accesses dashboard on mobile

**Step-by-step walkthrough**:
1. Dashboard loads with simple mobile layout
2. Left menu collapses to basic mobile menu
3. Content extends across mobile screen width
4. User taps menu to see 5 simple sections
5. Navigation works without complex animations
6. All functionality accessible but simple

**Success Criteria**: Mobile works simply without complexity

### Scenario 3: Security Settings Simplification
**Context**: User wants to change password

**Step-by-step walkthrough**:
1. User clicks "Security" in left menu
2. Simple security page loads edge-to-edge
3. Basic password change form is visible
4. Basic 2FA options if available
5. Essential privacy settings shown
6. No backup codes, active sessions, or complex elements

**Success Criteria**: Security functions work simply

## Product Manager Answers

Based on stakeholder feedback emphasizing simplification:

### Design Philosophy
- **Simple Website**: This is a simple website with simple functionality
- **No Over-Engineering**: Most templates are too complex for our needs
- **Functionality First**: Get correct functionality working, not complex design
- **Future Enhancement**: May redesign later - don't over-engineer now

### Removed Features
- **Dashboard**: NO quick action buttons, redundant welcomes, floating boxes
- **Security**: NO backup codes, active sessions, security notifications
- **Layout**: NO multiple layers, background boxes, complex containers
- **Navigation**: NO complex mobile patterns - use proven simple templates

### Development Priority
- **Start with**: Dashboard landing page with simple layout
- **Focus on**: Getting navigation and functionality correct
- **Timeline**: 4-week development cycle focusing on simplicity

## Quality Gate Checklist (95% Required)

- [x] **Simplification requirements addressed**: Complex features removed per stakeholder feedback
- [x] **Edge-to-edge layout specified**: Content extends across entire page
- [x] **Floating boxes eliminated**: No containers, layers, or background boxes
- [x] **Redundant elements removed**: No duplicate welcomes, subtitles, quick actions
- [x] **Security page simplified**: Removed backup codes, active sessions, security notifications
- [x] **Simple navigation emphasized**: Left-mounted menu without complexity
- [x] **Functionality focus clarified**: Working features over complex design
- [x] **Mobile simplification noted**: Basic responsive patterns, no complex animations
- [x] **Clean design principles**: Minimal UI without visual complications
- [x] **Future redesign acknowledged**: Don't over-engineer for later enhancement
- [x] **Technical constraints maintained**: React + Mantine + NSwag requirements
- [x] **Business value focused**: Simple functionality over complex UI
- [x] **User experience simplified**: Clean, straightforward interactions
- [x] **Implementation guidance clear**: Use simple templates, avoid complexity

## Implementation Readiness Verification

### Simplification Confirmed ✅
- **Design Approach**: Simple, clean interface without floating elements
- **Layout Strategy**: Edge-to-edge content spanning full page width
- **Navigation**: Basic left-mounted menu with 5 sections
- **Content Display**: Direct event display without extra containers
- **Security Settings**: Simplified to essential functions only

### Technical Foundation ✅
- **React Infrastructure**: Available for simple component implementation
- **Mantine v7**: Provides basic components without over-engineering
- **Authentication**: Integration with existing simple auth patterns
- **API Integration**: NSwag provides necessary type generation

### Business Requirements Met ✅
- **Stakeholder Feedback**: All complex features removed as requested
- **Functionality Focus**: Emphasis on working features over design
- **Simple Website**: Approach aligns with "simple functionality" requirement
- **Future Flexibility**: Foundation allows for later enhancement

---

*This simplified business requirements document prioritizes functionality and simplicity over complex UI patterns. The dashboard will provide essential functionality with a clean, edge-to-edge layout and simple left-mounted navigation. All stakeholder feedback regarding over-complexity has been addressed by removing floating boxes, redundant elements, and unnecessary features.*

*Next Phase*: Simple UI implementation focusing on functionality with basic Mantine v7 components and edge-to-edge layout patterns.