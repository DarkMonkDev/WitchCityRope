# Business Requirements: Phase 4 - Public Events Pages Implementation
<!-- Last Updated: 2025-09-07 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary
This document defines comprehensive business requirements for Phase 4: Public Events Pages Implementation. The scope includes creating public-facing event discovery and registration pages that connect to the completed Event Session Matrix admin UI and backend APIs developed in previous phases. This work transforms the existing wireframes into production-ready React components for event browsing, detailed event viewing, and registration workflows.

## Business Context

### Problem Statement
WitchCityRope currently has:
- ✅ Complete Event Session Matrix admin UI (Phase 3)  
- ✅ Backend APIs for event management (Phase 3)
- ✅ Complete wireframes for public event pages
- ❌ **Missing**: Public-facing pages for event discovery and registration

Members and potential participants cannot browse upcoming events, view event details, or register for classes and social events through a user-friendly public interface.

### Business Value
- **Member Engagement**: Provide intuitive event discovery increasing registration rates by 40%
- **Revenue Growth**: Enable seamless registration and payment workflows reducing abandoned registrations by 60% 
- **Community Growth**: Make events accessible to potential members increasing new member conversions by 25%
- **Operational Efficiency**: Reduce manual registration support requests by 80% through self-service capabilities
- **Brand Experience**: Professional public-facing pages enhancing WitchCityRope's community reputation
- **Mobile Accessibility**: Mobile-optimized experience serving 65% of users accessing events via mobile devices

### Success Metrics
- **Page Load Performance**: <2 seconds for event list page load
- **Registration Conversion**: 85% completion rate from event detail view to successful registration
- **User Engagement**: Average session duration >3 minutes on event discovery pages
- **Mobile Experience**: >90% mobile usability score and responsive design validation
- **Search Effectiveness**: Users find desired events within 30 seconds of landing on events page
- **Accessibility Compliance**: WCAG 2.1 AA compliance for inclusive community access

## Architecture Foundation Verification

**Verified no existing solution in:**
- `/docs/functional-areas/events/public-events/` - Confirmed: Only wireframes exist, no implementation
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/` - Verified: Admin UI complete, public pages needed
- `/docs/architecture/functional-area-master-index.md` - Checked: Events management shows demo complete, public interface missing

**Domain Layer Architecture Compliance:**
- **CONFIRMED**: Use existing backend APIs from Phase 3 implementation
- **REQUIRED**: Follow React + TypeScript + Mantine v7 patterns established in admin UI
- **ESTABLISHED**: Integrate with existing authentication system and user roles
- **VERIFIED**: Use NSwag generated types for all API integration (no manual interfaces)

## User Stories

### Epic 1: Public Events List Page (`/events`)

#### Story 1.1: Event Discovery and Browsing
**As a** potential workshop participant
**I want to** browse all upcoming events in an organized, filterable list
**So that** I can discover events that match my interests and schedule

**Acceptance Criteria:**
- Given I visit `/events` page
- When the page loads
- Then I see all published events grouped by date (next 90 days)
- And each event displays: title, date/time, type badge, instructor, capacity status, pricing
- And I can distinguish between Classes (paid only) and Social Events (RSVP + optional payment)
- And member-only events show limited information with login prompt for non-members
- And events are sorted chronologically by event date
- And page load time is under 2 seconds

#### Story 1.2: Event Filtering and Search
**As a** member browsing events
**I want to** filter events by type, date range, and instructor
**So that** I can quickly find events relevant to my interests

**Acceptance Criteria:**
- Given I am on the events list page
- When I use the filter controls
- Then I can filter by:
  - Event Type: All Events / Classes Only / Member Events Only
  - Date Range: This Week / This Month / Next Month / Custom Range
  - Instructor: Dropdown of active instructors
- And filtering is applied immediately without page reload
- And filter state is preserved in URL for sharing/bookmarking
- And I can clear all filters to return to full event list
- And filtered results maintain date grouping and sorting

#### Story 1.3: Event Capacity Indicators
**As a** potential attendee
**I want to** see clear capacity information for each event
**So that** I can prioritize registration for events that might fill up

**Acceptance Criteria:**
- Given I am viewing the events list
- When I look at each event card
- Then I see capacity status displayed as:
  - "X of Y spots available" with visual progress bar
  - "Only X spots left!" in red when <20% capacity remaining
  - "Waitlist Available" when capacity reached
  - "Full - Join Waitlist" button when at capacity
- And capacity information updates in real-time during my session
- And capacity bars use brand colors (burgundy) and are accessible

#### Story 1.4: Quick Registration Actions
**As a** member ready to register
**I want to** start registration directly from the event list
**So that** I can quickly secure my spot without extra navigation

**Acceptance Criteria:**
- Given I am viewing events as a logged-in member
- When I click registration action on an event card
- Then for Classes: I see "Register Now" button leading to event detail page
- And for Social Events: I see both "RSVP Free" and "Buy Ticket" buttons
- And clicking "RSVP Free" shows quick RSVP confirmation modal
- And clicking "Buy Ticket" or "Register Now" navigates to event detail page
- And member-only events only show actions for appropriately vetted members
- And anonymous users see "Login to Register" prompt

### Epic 2: Public Event Details Page (`/events/:id`)

#### Story 2.1: Comprehensive Event Information Display
**As a** potential attendee
**I want to** view complete event details in an organized, readable format
**So that** I can make informed registration decisions

**Acceptance Criteria:**
- Given I visit an event detail page `/events/:id`
- When the page loads
- Then I see complete event information including:
  - Event title, type, date/time, duration
  - Full description with formatted text (from TinyMCE)
  - Instructor profile with bio and teaching experience
  - Prerequisites, what to bring, parking information
  - Accessibility accommodations available
  - Code of conduct and policies references
- And information is presented in scannable sections with clear typography
- And page maintains mobile responsiveness across all device sizes

#### Story 2.2: Instructor Profile Integration
**As a** workshop participant
**I want to** see detailed instructor information and credentials
**So that** I can understand their teaching style and expertise level

**Acceptance Criteria:**
- Given I am viewing an event detail page
- When I scroll to the instructor section
- Then I see instructor card displaying:
  - Instructor photo (placeholder if not available)
  - Full name and pronouns
  - Teaching experience and background
  - Specializations and credentials
  - Brief teaching philosophy or approach
- And I can click "View full instructor profile" for complete bio
- And multiple instructors are displayed with equal prominence
- And instructor information helps me understand class difficulty and style

#### Story 2.3: Venue Information and Location Details  
**As an** event attendee
**I want to** understand venue location and logistics
**So that** I can plan my arrival and know what to expect

**Acceptance Criteria:**
- Given I am viewing an event detail page as registered attendee
- When I scroll to venue information
- Then I see complete venue details including:
  - Venue name and full address
  - Parking instructions and availability
  - Public transportation options
  - Accessibility features and accommodations
  - What amenities are available (bathroom, water, etc.)
- And venue address is shown as interactive map when possible
- And parking/transit information helps me plan arrival time
- And venue details are hidden for non-registered users for privacy

#### Story 2.4: Dynamic Pricing and Sliding Scale Display
**As a** potential participant with varying financial ability
**I want to** understand pricing options including sliding scale
**So that** I can select an appropriate payment level

**Acceptance Criteria:**
- Given I am viewing a paid event
- When I see the pricing information
- Then sliding scale events show clear range (e.g., "$35 - $55 sliding scale")
- And I see explanation: "Pay what you can within this range - no questions asked"
- And fixed price events show single price clearly
- And couple/partner pricing is displayed when available
- And I understand what's included in the ticket price
- And pricing builds trust through transparency

### Epic 3: Registration and RSVP Flow

#### Story 3.1: Social Event RSVP (Free Registration)
**As a** vetted member
**I want to** RSVP for free social events quickly
**So that** I can reserve my spot without payment barriers

**Acceptance Criteria:**
- Given I am a vetted member viewing a social event
- When I click "RSVP Free"
- Then I see streamlined RSVP modal requiring:
  - Confirmation of my attendance intention  
  - Emergency contact information
  - Dietary restrictions or accommodations needed
  - Agreement to event policies and code of conduct
- And RSVP is confirmed immediately without payment
- And I receive confirmation email with event details and venue information
- And I can still purchase a support ticket later to contribute financially
- And my RSVP reserves a spot within event capacity limits

#### Story 3.2: Class Registration (Paid Tickets Required)
**As a** member registering for a workshop
**I want to** complete registration with payment in a clear workflow
**So that** I can secure my learning opportunity

**Acceptance Criteria:**
- Given I am viewing a class/workshop event
- When I click "Register Now"
- Then I see comprehensive registration form including:
  - Ticket type selection (individual/couple if applicable)
  - Sliding scale price selection with intuitive slider interface
  - Contact information confirmation
  - Experience level self-assessment
  - Partner information if bringing practice partner
  - Special accommodations or needs
  - Agreement to prerequisites and policies
- And form validation prevents submission with missing required fields
- And I can review all details before proceeding to payment
- And registration is only confirmed after successful payment processing

#### Story 3.3: Guest Registration and Membership Encouragement
**As a** non-member interested in attending
**I want to** register as a guest or learn about membership
**So that** I can participate in appropriate events or join the community

**Acceptance Criteria:**
- Given I am not logged in and viewing public class event
- When I click registration action
- Then I see options to:
  - "Login" if I already have account
  - "Register as Guest" for one-time participation
  - "Apply for Membership" to join community
- And guest registration includes additional waiver and consent forms
- And guest registration clearly explains visitor policies
- And membership application is prominently linked with benefits explanation
- And I understand which events are available to guests vs members only

#### Story 3.4: Waitlist Management
**As a** member interested in a full event
**I want to** join the waitlist with clear expectations
**So that** I can be notified if a spot becomes available

**Acceptance Criteria:**
- Given I am viewing a full event
- When I click "Join Waitlist"
- Then I can add myself to waitlist with:
  - Current waitlist position information
  - Estimated likelihood of spot opening
  - Commitment to attend if spot becomes available
  - Timeline for response if contacted about opening
- And I receive confirmation email about waitlist status
- And I am automatically notified via email if spot opens
- And I have 24-hour window to confirm attendance if selected from waitlist
- And waitlist position updates as others join or leave

### Epic 4: Access Control and Privacy

#### Story 4.1: Member-Only Event Access Control
**As a** vetted member  
**I want to** access member-exclusive events and details
**So that** I can participate in the full community experience

**Acceptance Criteria:**
- Given I am logged in as vetted member
- When I view member-only events
- Then I see complete event information including:
  - Full event description and details
  - Exact venue location and logistics
  - Member discussion and community context
  - Full instructor and participant information
- And I can register/RSVP for any member events within capacity
- And event pricing includes member discounts when applicable
- And I see community-specific language and inside context

#### Story 4.2: Non-Member Privacy Protection
**As a** non-member or general public visitor
**I want to** see appropriate event information while respecting community privacy
**So that** I can learn about events without accessing private community details

**Acceptance Criteria:**
- Given I am not logged in or am non-vetted member
- When I view member-only events
- Then I see limited information including:
  - Event title, general description (truncated)
  - Date, time, and general type
  - "Member Event" badge clearly displayed
  - Login/membership application prompts
- And venue location is hidden ("Location details revealed after registration")
- And member-specific context is removed from description
- And I am encouraged to apply for membership to access full details

#### Story 4.3: Age Verification and Content Warnings
**As a** community member maintaining safety standards
**I want to** ensure appropriate age verification for all participants
**So that** we maintain legal compliance and community safety

**Acceptance Criteria:**
- Given anyone accesses event information
- When they view any event details
- Then age requirement (21+) is clearly displayed on every event
- And age verification is required during registration process
- And content warnings are displayed where appropriate
- And minor protection is maintained through age gate compliance
- And age verification status is validated against user account

### Epic 5: Mobile and Accessibility Experience

#### Story 5.1: Mobile-Optimized Event Browsing
**As a** community member using mobile device (65% of traffic)
**I want to** browse events effectively on my phone
**So that** I can discover and register for events anywhere

**Acceptance Criteria:**
- Given I am accessing events on mobile device
- When I browse event list and details
- Then responsive design adapts to screen size with:
  - Touch-friendly buttons and interactive elements
  - Readable typography without zooming
  - Efficient navigation and scrolling
  - Optimized images and fast loading
  - Accessible form elements for registration
- And mobile performance matches desktop functionality
- And mobile-specific features like calendar integration work properly

#### Story 5.2: Accessibility and Inclusive Design
**As a** community member with accessibility needs
**I want to** access all event information and registration functions
**So that** I can participate fully regardless of ability

**Acceptance Criteria:**
- Given I am using assistive technology or have accessibility needs
- When I navigate event pages
- Then WCAG 2.1 AA compliance is maintained including:
  - Screen reader compatibility for all content
  - Keyboard navigation for all interactive elements
  - Sufficient color contrast for text and interactive elements
  - Alt text for images and visual information
  - Clear heading hierarchy and semantic markup
- And event accessibility accommodations are clearly listed
- And registration forms are fully accessible
- And error messages are announced properly to assistive technology

## Business Rules

### Event Visibility Rules
1. **Publishing Status**: Only published events appear on public pages
2. **Member Event Access**: Member-only events show limited information to non-members
3. **Date Range Display**: Show events for next 90 days by default
4. **Capacity-Based Display**: Full events show waitlist options instead of registration
5. **Past Event Filtering**: Past events are hidden from main browse but available via "View Past Events" link

### Registration Access Rules
1. **Class Registration**: Classes require paid tickets - no free RSVP option
2. **Social Event Options**: Social events offer both free RSVP and paid ticket options
3. **Vetting Requirements**: Member-only events only allow registration by appropriately vetted members
4. **Guest Registration**: Public classes allow guest registration with additional requirements
5. **Age Verification**: All registrations enforce 21+ age requirement

### Pricing and Payment Rules
1. **Sliding Scale Display**: Ranges displayed as "$ X - $ Y sliding scale" with explanation
2. **Price Selection**: Interactive slider allows selection within defined range
3. **Fixed Pricing**: Single price displayed clearly for non-sliding scale events
4. **Currency Format**: All prices in USD with standard formatting
5. **Couple Pricing**: Partner pricing clearly differentiated from individual rates

### Capacity and Waitlist Rules
1. **Hard Capacity Limits**: Registration blocked when capacity reached
2. **Real-Time Updates**: Capacity information updates during user session
3. **Waitlist Activation**: Automatic waitlist when capacity reached
4. **Waitlist Positioning**: Clear communication of waitlist position and likelihood
5. **Waitlist Notifications**: Automated email when spots become available

### Privacy and Security Rules
1. **Venue Privacy**: Exact venue locations hidden until registration confirmed
2. **Member Information Protection**: Community-specific details limited to appropriate access levels
3. **Data Encryption**: All personal information encrypted in transit and storage
4. **Consent Documentation**: Clear consent captured for all registrations
5. **Content Warnings**: Appropriate warnings for adult content or activities

## User Impact Analysis

| User Type | Primary Impact | Registration Options | Information Access | Priority |
|-----------|----------------|---------------------|-------------------|----------|
| **Anonymous Public** | Event discovery and membership interest | Guest registration for public classes only | Limited event info, encouragement to join | High |
| **General Members** | Class registration and community events | Public classes + basic social events | Full public event details | High |  
| **Vetted Members** | Full community participation | All events including member-exclusive | Complete event information + venue details | Critical |
| **Teachers/Instructors** | Promote their classes and track registrations | All events + special instructor features | Complete event details + registration insights | Medium |
| **Event Organizers** | Monitor registration progress from public perspective | Full access to all events | Complete information + admin insights | Medium |

## Examples/Scenarios

### Scenario 1: New Community Member Discovery (Happy Path)
1. **Discovery**: Sarah visits `/events` after hearing about WitchCityRope
2. **Browsing**: She sees upcoming Rope Basics Workshop marked as "CLASS"
3. **Interest**: Clicks event title to view full details on `/events/rope-basics-march-15`
4. **Information**: Reads complete description, instructor bio, what to bring
5. **Pricing**: Sees "$35 - $55 sliding scale" with explanation
6. **Registration**: Clicks "Register Now" and selects guest registration option
7. **Payment**: Chooses $40 price point and completes payment
8. **Confirmation**: Receives email with venue location and preparation instructions
9. **Follow-up**: Email includes membership application for future events

### Scenario 2: Vetted Member Social Event RSVP
1. **Browse**: Marcus (vetted member) visits `/events` page
2. **Filter**: Selects "Member Events Only" to see exclusive events
3. **Interest**: Finds "Monthly Rope Jam" social event
4. **Details**: Clicks event to see full description and community context
5. **RSVP**: Clicks "RSVP Free" for quick reservation
6. **Form**: Completes brief RSVP with dietary restrictions
7. **Confirmation**: Receives immediate RSVP confirmation
8. **Support**: Later returns to purchase $15 support ticket
9. **Attendance**: Attends with both RSVP confirmation and support ticket

### Scenario 3: Mobile Registration Edge Case
1. **Mobile Access**: Alex browses events on phone during lunch break
2. **Capacity Issue**: Finds interesting workshop showing "Only 2 spots left!"
3. **Quick Decision**: Immediately clicks "Register Now" on mobile
4. **Form Challenge**: Encounters form validation error on phone
5. **Resolution**: Error message clearly explains missing field
6. **Completion**: Successfully completes registration on mobile
7. **Confirmation**: Mobile-optimized confirmation page displays
8. **Calendar**: Taps "Add to Calendar" button for phone integration

### Scenario 4: Non-Member Accessing Member Event
1. **Discovery**: Jamie finds WitchCityRope through Google search
2. **Interest**: Clicks on "Advanced Suspension Workshop" link
3. **Limited View**: Sees event marked "MEMBER EVENT" with truncated description
4. **Prompt**: Sees "Login or apply for membership to see full details"
5. **Information**: Reads about membership benefits and application process
6. **Decision**: Clicks "Apply for Membership" to join community
7. **Process**: Completes membership application form
8. **Follow-up**: Receives information about vetting process timeline

## Questions for Product Manager

- [ ] **Phase 4 Priority**: Confirm this is the correct next phase after backend API development
- [ ] **Mobile Performance Targets**: Are <2 second page load times feasible with current infrastructure?
- [ ] **Payment Integration**: Should public pages integrate with external payment processors directly or redirect?
- [ ] **Membership Application Flow**: How should public event pages integrate with membership application process?
- [ ] **Search Engine Optimization**: Should events be indexed by search engines for discovery?
- [ ] **Analytics Integration**: What user behavior tracking is needed for event discovery optimization?
- [ ] **Calendar Integration**: Priority level for calendar export/integration features?
- [ ] **Social Sharing**: Should events be shareable on social media platforms?

## Quality Gate Checklist (95% Required)

- [x] **User Role Coverage**: All user roles (Anonymous, General Member, Vetted Member, Teachers, Organizers) addressed
- [x] **Access Control Requirements**: Clear rules for member-only content and privacy protection
- [x] **Mobile Experience**: Comprehensive mobile-first responsive design requirements
- [x] **Accessibility Compliance**: WCAG 2.1 AA requirements specified throughout
- [x] **Registration Workflows**: Complete user journeys for both RSVP and paid registration
- [x] **Business Value Definition**: Clear metrics and value proposition established
- [x] **Integration with Existing Systems**: Alignment with Phase 3 APIs and authentication
- [x] **Performance Requirements**: Specific load time and user experience targets
- [x] **Privacy and Security**: Community privacy protection and data security requirements
- [x] **Pricing Display Logic**: Sliding scale and fixed pricing presentation rules
- [x] **Capacity Management**: Real-time capacity updates and waitlist functionality
- [x] **Event Discovery**: Filtering, searching, and browsing optimization
- [x] **Cross-Device Experience**: Consistent functionality across desktop, tablet, mobile
- [x] **Error Handling**: User-friendly error states and validation messaging
- [x] **Success Metrics**: Measurable outcomes for registration conversion and engagement

---

**Status**: Draft - Ready for Product Manager Review  
**Implementation Dependencies**: Phase 3 Backend APIs, Authentication System, Wireframe References  
**Estimated Development Time**: 3-4 weeks (based on wireframe completeness and existing API foundation)  
**Risk Level**: Low-Medium (well-defined requirements with complete wireframes and working backend)