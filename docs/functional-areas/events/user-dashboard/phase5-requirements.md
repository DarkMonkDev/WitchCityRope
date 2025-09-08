# Business Requirements: Phase 5 - User Dashboard & Member Features
<!-- Last Updated: 2025-09-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary
Phase 5 implements comprehensive member-facing features including personal dashboards, profile management, event registration tracking, and role-based functionality. This phase transforms WitchCityRope from a public information platform into a full member management system, providing personalized experiences based on membership tiers and community standing.

## Business Context
### Problem Statement
Members need personalized access to their account information, event registrations, and community features. Currently, Phase 4 completed public event pages, but members cannot manage their profiles, view their registrations, or access member-specific features based on their role in the community.

### Business Value
- **Member Retention**: Personal dashboards increase engagement and community connection
- **Administrative Efficiency**: Self-service profile management reduces admin workload
- **Revenue Protection**: Registration tracking and management prevents lost sales
- **Community Safety**: Vetting status visibility maintains community standards
- **User Experience**: Role-based features provide appropriate access levels

### Success Metrics
- Member login frequency increases by 40%
- Profile completion rates reach 85%
- Self-service profile updates reduce admin requests by 60%
- Event registration conversion improves by 25%
- Member satisfaction scores improve for account management features

## User Stories

### Story 1: Member Dashboard Overview
**As a** logged-in member
**I want to** see a personalized dashboard with my upcoming events and account status
**So that** I can quickly understand my current community engagement and take relevant actions

**Acceptance Criteria:**
- Given I am logged in as any member role
- When I visit the dashboard page
- Then I see a welcome message with my scene name
- And I see my next 5 upcoming registered events
- And I see my membership status and level
- And I see quick action buttons for common tasks
- And the page loads within 2 seconds

### Story 2: Upcoming Events Display
**As a** member with event registrations
**I want to** see my upcoming events with clear date/time information
**So that** I can plan my schedule and not miss events I've registered for

**Acceptance Criteria:**
- Given I have registered for future events
- When I view my dashboard
- Then I see events sorted by date (earliest first)
- And each event shows: date, time, title, location, registration status
- And I can click an event to view full details
- And I see a visual indicator for events happening soon (within 24 hours)
- And canceled or postponed events are clearly marked

### Story 3: Role-Based Dashboard Customization
**As a** member with different roles (General, Vetted, Teacher, Admin)
**I want to** see dashboard content appropriate to my role
**So that** I can access the features and information relevant to my community standing

**Acceptance Criteria:**
- Given I am a General Member
- When I view the dashboard
- Then I see public events and my basic profile information
- And I see prompts to complete vetting if applicable
- 
- Given I am a Vetted Member
- When I view the dashboard  
- Then I see all events including vetted-only events
- And I see member directory access
- And I see community tools appropriate to my status
- 
- Given I am a Teacher
- When I view the dashboard
- Then I see my upcoming classes I'm teaching
- And I see quick links to manage my events
- And I see student/attendee information for my events
- 
- Given I am an Admin
- When I view the dashboard
- Then I see system overview information
- And I see quick links to admin functions
- And I see member management tools

### Story 4: User Profile Management
**As a** member
**I want to** view and update my profile information
**So that** I can keep my community information current and control my privacy

**Acceptance Criteria:**
- Given I am logged in
- When I visit my profile page
- Then I see all my profile information including: scene name, email, pronouns, join date
- And I can edit my scene name, pronouns, and other allowed fields
- And I can see but not edit my email (security requirement)
- And I can see my member ID and role
- And changes are saved immediately with confirmation
- And invalid data shows clear error messages

### Story 5: Registration History Tracking
**As a** member
**I want to** view my complete event registration history
**So that** I can track my community participation and manage upcoming events

**Acceptance Criteria:**
- Given I have registered for events
- When I view my registration history
- Then I see all my registrations (past and future) sorted by date
- And each registration shows: event title, date, registration date, payment status, attendance status
- And I can filter by: upcoming, past, paid, unpaid, attended, not attended
- And I can cancel future registrations if cancellation window is open
- And I can see refund information for canceled registrations

### Story 6: Event Registration Management
**As a** member with upcoming event registrations
**I want to** manage my registrations and see payment/attendance status
**So that** I can ensure I'm prepared for events and understand my commitments

**Acceptance Criteria:**
- Given I have upcoming event registrations
- When I view my registration details
- Then I see payment status (paid, pending, refunded)
- And I see cancellation options with deadline information
- And I can request special accommodations
- And I see event-specific information (what to bring, prerequisites)
- And I can add the event to my personal calendar
- And I receive email confirmations for any changes

### Story 7: Membership Status and Vetting
**As a** member
**I want to** see my current membership status and vetting progress
**So that** I understand my access level and what steps I can take to advance

**Acceptance Criteria:**
- Given I am any member type
- When I view my membership page
- Then I see my current role (Guest, General Member, Vetted Member, Teacher, Admin)
- And I see my join date and membership duration
- And I see my vetting status with clear next steps if applicable
- And I see the benefits of each membership level
- And I can access vetting applications if eligible
- And I see my community contributions and participation stats

### Story 8: Personal Settings and Preferences
**As a** member
**I want to** manage my account settings and notification preferences
**So that** I can control how the platform interacts with me

**Acceptance Criteria:**
- Given I am logged in
- When I visit my settings page
- Then I can change my password with proper validation
- And I can set email notification preferences for events, announcements, and community updates
- And I can set privacy preferences for profile visibility
- And I can enable/disable two-factor authentication
- And I can see my login history for security
- And I can download my personal data (privacy compliance)

### Story 9: Teacher Dashboard Features
**As a** Teacher
**I want to** see my upcoming classes and manage my teaching schedule
**So that** I can efficiently prepare for and manage my educational events

**Acceptance Criteria:**
- Given I am a Teacher
- When I view my dashboard
- Then I see my upcoming classes with attendee counts
- And I can click to view attendee lists and contact information
- And I see class materials and preparation reminders
- And I can send messages to registered attendees
- And I see feedback/ratings from past classes
- And I can quickly access event editing tools

### Story 10: Mobile Dashboard Experience
**As a** member accessing the platform on mobile
**I want to** have a responsive dashboard experience optimized for mobile use
**So that** I can manage my membership and events while on the go

**Acceptance Criteria:**
- Given I access the dashboard on a mobile device
- When I view any dashboard page
- Then the layout adapts to mobile screen sizes
- And touch targets are appropriately sized (minimum 44px)
- And text is readable without zooming
- And navigation is easily accessible
- And page load time is under 3 seconds on 4G connection
- And all features work on mobile browsers

## Business Rules

### Profile Management Rules
1. **Scene Name Uniqueness**: Scene names must be unique across all members with real-time validation
2. **Email Changes**: Email changes require verification to the new email address before activation
3. **Profile Completeness**: Minimum profile completion required for vetting application (80% complete)
4. **Privacy Controls**: Members can set profile visibility (public, vetted-only, private)

### Event Registration Rules
1. **Registration Windows**: Members can only cancel registrations up to 24 hours before event start time
2. **Refund Policy**: Cancellations follow sliding scale refund policy (>1 week: 100%, >24 hours: 50%, <24 hours: 0%)
3. **Waitlist Management**: When events are full, members can join waitlist and receive automatic notification of openings
4. **Prerequisites**: Members must meet event prerequisites before registration is allowed

### Role-Based Access Rules
1. **General Members**: Can register for public events, view public profiles, access general community areas
2. **Vetted Members**: Can register for all events, view all member profiles, access member directory, participate in vetted discussions
3. **Teachers**: Can create/manage events, access attendee information, send communications to their students
4. **Admins**: Full access to all features plus member management, system settings, and community moderation tools

### Data Security Rules
1. **Personal Information**: Legal names and sensitive data are encrypted and access-logged
2. **Password Security**: Passwords must meet complexity requirements and expire after 90 days for admins
3. **Session Management**: Sessions timeout after 30 minutes of inactivity
4. **Audit Logging**: All profile changes, registrations, and sensitive actions are logged with timestamps

### Vetting Status Rules
1. **Vetting Application**: Must be General Member for 30+ days and 80%+ profile complete
2. **Vetting Process**: Requires admin approval and background verification
3. **Vetting Revocation**: Can be revoked by admins with 30-day appeal window
4. **Status Display**: Vetting status is visible on profiles to other vetted members only

## Constraints & Assumptions

### Technical Constraints
- Must integrate with existing ASP.NET Core Identity system
- Must maintain existing PostgreSQL database schema
- Must work with current React + Mantine UI framework
- API responses must be under 200ms for dashboard data
- Must support minimum browser versions (Chrome 90+, Firefox 88+, Safari 14+)

### Business Constraints
- Cannot modify existing event registration payment flows
- Must maintain GDPR compliance for personal data
- Cannot break existing public event access patterns
- Must support existing user roles without changes
- SSL/TLS required for all data transmission

### Assumptions
- Members want self-service profile management capabilities
- Mobile access is important for community members attending events
- Email remains the primary communication method
- Members understand different access levels based on their role
- Two-factor authentication will be optional initially

## Security & Privacy Requirements

### Authentication & Authorization
- Multi-factor authentication support for sensitive account changes
- Role-based access control enforced at API level
- Session management with automatic timeout and renewal
- Password complexity requirements with strength indicators
- Account lockout protection against brute force attacks

### Data Protection
- Personal information encrypted at rest and in transit
- Legal name encryption with separate key management
- Access logging for all sensitive data operations
- Data export capability for GDPR compliance
- Data deletion workflows for account closure

### Privacy Controls
- Profile visibility settings (public/vetted/private)
- Granular notification preferences
- Opt-out capabilities for community communications
- Anonymous feedback and reporting options
- Clear data usage policies and consent management

## Compliance Requirements

### Legal Requirements
- GDPR Article 20 (Right to Data Portability) - data export functionality
- GDPR Article 17 (Right to Erasure) - account deletion workflows
- State privacy laws compliance (California, Massachusetts)
- Age verification requirements (21+ for vetted status)
- Accessibility compliance (WCAG 2.1 AA minimum)

### Platform Policies
- Community guidelines enforcement through profile restrictions
- Content moderation for user-generated profile content
- Harassment reporting and response workflows
- Terms of service acceptance tracking
- Privacy policy updates and re-consent workflows

## User Impact Analysis

| User Type | Impact | Priority | Key Benefits |
|-----------|--------|----------|--------------|
| General Member | High | High | Personal dashboard, profile management, registration tracking |
| Vetted Member | High | High | Enhanced event access, member directory, community features |
| Teacher | Medium | Medium | Teaching schedule management, student communication tools |
| Admin | Medium | Low | Member oversight capabilities, system health visibility |
| Guest Users | None | N/A | No impact - public features remain unchanged |

## Examples/Scenarios

### Scenario 1: New Member Profile Setup
1. Jane registers as a General Member
2. She completes her profile with scene name "Rope_Curious_Jane" 
3. System validates scene name uniqueness
4. She sets her pronouns and privacy preferences
5. Profile completion shows 60% - needs more info for vetting eligibility
6. She sees clear guidance on completing profile for future vetting application

### Scenario 2: Vetted Member Event Registration
1. Sarah (Vetted Member) logs into her dashboard
2. She sees 3 upcoming events including 1 vetted-only workshop
3. She clicks on "Advanced Suspension Techniques" (vetted-only)
4. She registers and pays using existing payment system
5. Event appears in her upcoming events with confirmation details
6. She receives email confirmation with event-specific preparation info

### Scenario 3: Teacher Class Management
1. Michael (Teacher) views his dashboard
2. He sees his upcoming "Beginner Rope Basics" class with 8/10 slots filled
3. He clicks to view attendee list and sees participant names and experience levels
4. He sends a pre-class email with what to bring and expectations
5. After the class, he can mark attendance and leave feedback for participants

### Scenario 4: Mobile Event Check-In
1. Alex is traveling to an event using his mobile phone
2. He opens the dashboard mobile site
3. He sees today's event with location and start time
4. He taps the event to see parking information and emergency contacts
5. He can quickly message other attendees or the teacher if needed

## Questions for Product Manager

- [ ] Should we implement a member directory feature in Phase 5 or defer to Phase 6?
- [ ] What is the priority for implementing waitlist management vs basic registration tracking?
- [ ] Should teacher communication tools be full messaging system or simple email integration?
- [ ] Do we need calendar integration (Google Calendar, Apple Calendar) for member events?
- [ ] Should we implement push notifications for mobile users or stick with email?
- [ ] What level of analytics should members see about their community participation?
- [ ] Should profile photos be included in Phase 5 or deferred due to moderation complexity?

## Quality Gate Checklist (95% Required)

- [x] All user roles addressed (General, Vetted, Teacher, Admin)
- [x] Clear acceptance criteria for each story
- [x] Business value clearly defined with metrics
- [x] Edge cases considered (cancellations, errors, mobile)
- [x] Security requirements documented (encryption, access control)
- [x] Compliance requirements checked (GDPR, accessibility)
- [x] Performance expectations set (<2s dashboard load, <200ms API)
- [x] Mobile experience considered with responsive design
- [x] Examples provided for key workflows
- [x] Success metrics defined with specific targets
- [x] Integration points identified (existing auth, payments)
- [x] Privacy controls specified (visibility, notifications)
- [x] Error handling requirements defined
- [x] Data validation rules documented
- [x] User impact analysis completed