# Business Requirements: Events Management System
<!-- Last Updated: 2025-10-05 -->
<!-- Version: 3.1 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Updated with User Clarifications -->

## Document Change History

### Version 3.1 (2025-10-05)
**Critical Clarifications Applied:**
- **RSVP+Tickets**: Corrected social events to support BOTH RSVP (free) AND ticket purchases (separate actions)
- **Check-in System**: Clarified as staff-assisted (not kiosk), IN SCOPE for Phase 1
- **Teacher Permissions**: Confirmed teachers CANNOT edit events (already correctly documented)
- **Event Images**: Deferred to future phase, removed from current scope
- **Email Automation**: Confirmed as in-scope, no changes needed

### Version 3.0 (2025-08-25)
- Updated to match final wireframes

## Executive Summary
The Events Management System provides comprehensive event lifecycle management for WitchCityRope, enabling Event Organizers to create, manage, and track classes, social events, and performances while supporting member registration, payment processing, and attendance tracking with automated refund capabilities. The system implements a flexible Event Session Matrix that naturally handles complex multi-ticket scenarios with auto-generated Session IDs (S1, S2, S3) for consistent referencing.

## Business Context
### Problem Statement
WitchCityRope currently lacks a centralized system for managing events from creation to completion. Event management is manual, payment tracking is inconsistent, and member registration processes are cumbersome. The platform needs a comprehensive event management solution that maintains community safety standards while streamlining operations.

### Business Value
- Reduces administrative overhead for event management by 70%
- Provides centralized payment tracking and automated refund processing
- Improves member experience with streamlined registration and clear event information
- Enables better capacity management and waitlist functionality
- Supports multiple payment methods for accessibility
- Maintains safety and vetting requirements for appropriate events

### Success Metrics
- 90% reduction in manual event administration tasks
- 95% member satisfaction with event registration process
- 100% payment accuracy with automated refund processing
- 50% reduction in event-related support inquiries
- Zero payment data security incidents

## User Stories

### Story 1: Event Creation with Session Management
**As an** Event Organizer
**I want to** create events with auto-generated session IDs and manage all event details through tabbed interface
**So that** I can efficiently handle all event operations with consistent session referencing

**Acceptance Criteria:**
- Given I am logged in as an Event Organizer
- When I create a new event
- Then the system provides a tabbed interface with: Basic Info, Tickets/Orders, Emails, Volunteers/Staff
- And sessions are auto-generated with S1, S2, S3 format with user-editable names
- And all references to sessions use the S# format consistently
- And I can edit session details through modal dialogs
- And all tables follow standardized format: Edit first column, Delete last column

### Story 2: Basic Info Tab Configuration
**As an** Event Organizer
**I want to** configure event basic information without schedule fields
**So that** I can focus on event details separate from session scheduling

**Acceptance Criteria:**
- Given I am in the Basic Info tab
- When I configure event details
- Then I can set Event Type (Class or Social Event)
- And I can enter Event Title, Short Description, Full Event Description
- And I can specify Policies & Procedures (not Prerequisites and Safety)
- And I can select venue from predefined list or add new venue
- And I can assign teachers using card-based selection interface
- And schedule information is managed in the Tickets/Orders tab
- And image upload is NOT available (deferred to future phase)

### Story 3: Session-Based Event Management
**As an** Event Organizer
**I want to** manage events through sessions with standardized ID format
**So that** I can easily reference and organize multi-session events

**Acceptance Criteria:**
- Given I am in the Tickets/Orders tab
- When I view the Event Sessions table
- Then sessions are displayed with S1, S2, S3 auto-generated IDs
- And each session has an editable name field
- And the table shows: Actions, S#, Name, Date, Start Time, End Time, Capacity, Sold, Delete
- And Edit action opens modal for full session configuration
- And I can add new sessions which get next available S# ID
- And session capacity directly impacts ticket sales availability

### Story 4: Flexible Ticket Type Management
**As an** Event Organizer
**I want to** create ticket types that include multiple sessions
**So that** I can offer flexible pricing and attendance options

**Acceptance Criteria:**
- Given I am in the Tickets/Orders tab
- When I configure ticket types
- Then the table shows: Edit, Ticket Name, Type, Sessions Included, Price Range, Quantity, Sales End, Delete
- And Sessions Included column displays S# format (e.g., "S1, S2, S3")
- And I can create tickets for any combination of sessions
- And Edit action opens modal for complex settings like session selection
- And system prevents overselling by tracking session-specific capacity

### Story 5: Unified Email Management
**As an** Event Organizer
**I want to** manage both templates and ad-hoc emails in one interface
**So that** I can efficiently handle all event communications

**Acceptance Criteria:**
- Given I am in the Emails tab
- When I manage email communications
- Then I see template cards for editing and ad-hoc email card permanently available
- And "Send Ad-Hoc Email" card is always present and accessible
- And template cards show session targeting (S1, S2, S3, All)
- And clicking any card opens unified editor below
- And I can add new templates from available dropdown
- And session targeting dropdown shows S1, S2, S3 options

### Story 6: Volunteer Position Management
**As an** Event Organizer
**I want to** manage volunteer positions with session assignments
**So that** I can coordinate staff across multi-session events

**Acceptance Criteria:**
- Given I am in the Volunteers/Staff tab
- When I manage volunteer positions
- Then the table shows: Edit, Position, Sessions, Time, Description, Needed, Assigned, Delete
- And Sessions column displays which sessions need this position (S1, S2, All)
- And I can create positions with session-specific assignments
- And Edit action allows modification of session assignments
- And position form includes session dropdown with S1, S2, S3, All options

### Story 7: Admin Events Dashboard
**As an** Event Organizer
**I want to** view and manage all events in a dashboard format
**So that** I can efficiently oversee the entire events calendar

**Acceptance Criteria:**
- Given I access the admin events dashboard
- When I view the events table
- Then I see columns: Date, Time, Event Title, Capacity, Tickets, Actions
- And Date and Event Title columns are sortable
- And Capacity and Tickets columns show numeric values without extra labels
- And Actions column shows Copy and Edit buttons
- And I can filter by event type and show/hide past events
- And Create Event button uses amber gradient styling

### Story 8: Staff-Assisted Check-In System (Phase 1 - REQUIRED)
**As an** Event Organizer
**I want to** provide staff with a check-in interface for event arrival processing
**So that** I can efficiently manage attendee check-in with proper tracking

**Acceptance Criteria:**
- Given I need to set up event check-in
- When I launch the check-in interface
- Then staff can access the check-in screen with proper authentication
- And staff can search and check in attendees who have RSVPs or tickets
- And staff can process ticket purchases at the door during check-in
- And the system tracks both RSVP confirmations and ticket purchases
- And check-in interface shows clear attendee status (RSVP only, ticket purchased, checked in)
- And I can monitor check-in activity in real-time

**Note:** This is staff-assisted check-in, NOT self-service kiosk. Staff member checks everyone in when they arrive.

### Story 9: Public Events Display
**As a** Member or Guest
**I want to** view available events in card or list format
**So that** I can find and register for events that interest me

**Acceptance Criteria:**
- Given I access the public events page
- When I browse available events
- Then I can switch between card view and list view
- And multi-session events show constraint information (e.g., "5/20 (Day 2)")
- And I can see which sessions are included in different ticket types
- And member-only events show appropriate access prompts
- And I can use search and filtering to find specific events
- And event cards do NOT show images (deferred feature)

### Story 10: Event Details and Registration
**As a** Member
**I want to** view detailed event information and select appropriate ticket types
**So that** I can make informed registration decisions

**Acceptance Criteria:**
- Given I view an event detail page
- When I review registration options
- Then I see session availability for each session (Day 1, Day 2, Day 3)
- And I can choose from available ticket types with clear session inclusion
- And unavailable ticket types show constraint reason (e.g., "Sold Out (Day 2 full)")
- And I see capacity warnings for sessions filling up
- And purchase button updates based on selected ticket type

### Story 11: Social Event RSVP and Ticket Purchase (CRITICAL)
**As a** Member
**I want to** RSVP to social events AND optionally purchase tickets
**So that** I can confirm attendance and secure my spot with flexible payment options

**Acceptance Criteria:**
- Given I am viewing a social event
- When I want to participate
- Then I can RSVP for free to confirm my attendance intent
- And I can ALSO purchase tickets (as a separate action from RSVP)
- And I can purchase tickets ahead of time through the event page
- And I can purchase tickets at the door during check-in
- And the system tracks my RSVP status and ticket purchase status separately
- And I can see my RSVP status and ticket status clearly displayed
- And event capacity considers both RSVPs and ticket purchases

**Note:** RSVPs and ticket purchases are SEPARATE actions for social events. Both must be tracked independently.

## Business Rules

### Session Management Rules
1. **Session ID Format**: All sessions use auto-generated S1, S2, S3 format for consistency
2. **Session Names**: User-editable descriptive names while maintaining S# ID reference
3. **Session Capacity**: Individual capacity limits per session control overall event availability
4. **Session References**: All system references use S# format (emails, reports, interfaces)

### Table Standardization Rules
1. **Column Order**: ALL data tables have Edit as first column, Delete as last column
2. **Action Consistency**: Edit actions open modal dialogs for complex configuration
3. **CSS Standards**: All implementation must use Design System v7 standardized classes
4. **No Inline Styles**: All styling through predefined CSS classes, no page-specific styling

### Event Form Tab Rules
1. **Basic Info Tab**: Event details, venue, teachers - NO schedule fields, NO image upload
2. **Tickets/Orders Tab**: Sessions grid, ticket types grid, sales tracking, RSVPs
3. **Emails Tab**: Unified template editing with ad-hoc email always available
4. **Volunteers/Staff Tab**: Position management with session-specific assignments

### Event Types and Registration Rules (UPDATED 2025-10-05)
1. **Classes**:
   - Ticket-based registration with payment required
   - No RSVP option

2. **Social Events**:
   - RSVP system (free, confirms attendance intent)
   - Ticket purchases (separate action, can be done ahead of time OR at door)
   - Both RSVP and ticket purchase must be tracked independently
   - Event displays both RSVP table AND tickets sold table

3. **RSVP vs Tickets for Social Events**:
   - RSVPs are FREE and confirm attendance intent
   - Tickets are PAID and can be purchased separately from RSVP
   - A member can: RSVP only, Purchase ticket only, OR both RSVP and purchase ticket
   - Ticket purchases can happen: Online ahead of time, OR at door during check-in
   - Check-in system must handle both RSVP confirmation and ticket purchase

4. **Capacity Management**:
   - Social events: Consider both RSVPs and ticket purchases for capacity
   - Classes: Only ticket purchases count toward capacity

### Email System Rules
1. **Session Targeting**: All email templates target specific sessions (S1, S2, S3, All)
2. **Ad-hoc Always Available**: "Send Ad-Hoc Email" card permanently visible
3. **Unified Editor**: Same editor interface for templates and ad-hoc emails
4. **Template Management**: Add/remove templates through dropdown selection
5. **Automation Confirmed**: Automated emails ARE part of the event process (in-scope)

### Role-Based Permissions
1. **Event Organizers**:
   - Full access to ALL events (view, create, edit, delete)
   - All tabs accessible in event creation/editing
   - Manage check-in interface access for staff
   - Access admin events dashboard with full controls

2. **Teachers**:
   - View events they're assigned to teach
   - Contact Event Organizers for any changes needed
   - **NO direct editing capabilities** (confirmed restriction)

3. **Admin**:
   - Same permissions as Event Organizers
   - Additional system administration capabilities
   - User role management

4. **Members**:
   - View and register for events based on vetting level
   - RSVP to social events (free)
   - Purchase tickets for any event (ahead of time or at door)
   - Cancel registrations within refund window
   - View their registration history

### Multi-Ticket Type System Rules (Event Session Matrix)
1. **Session-Based Capacity**: Capacity tracked per session (S1, S2, S3), not per ticket type
2. **Real-Time Calculation**: System sums all tickets including each session
3. **Sales Prevention**: Ticket sales stop when ANY included session reaches capacity
4. **Prerequisite Support**: Sessions can require attendance at previous sessions
5. **Waitlist Granularity**: Waitlists trigger when specific sessions fill up

### Check-In System Rules (UPDATED 2025-10-05)
1. **Staff-Assisted**: Staff member checks in attendees (NOT self-service kiosk)
2. **Phase 1 Priority**: Check-in system is REQUIRED for Phase 1 implementation
3. **Dual Path Support**: Handle both RSVP confirmations and ticket purchases
4. **Door Sales**: Support ticket purchase during check-in process
5. **Status Tracking**: Clear display of attendee status (RSVP only, ticket purchased, checked in)
6. **Audit Logging**: All check-in actions logged with staff attribution

### Payment Processing Rules
1. **No Credit Card Storage**: All payment processing is immediate and external
2. **Supported Methods (Future Implementation)**:
   - Direct credit card processing (immediate, no storage)
   - PayPal integration
   - Venmo integration
3. **Phase 1 Implementation**: Payment tracking only, external payment processing
4. **Door Sales**: Support payment processing at check-in
5. **Refunds**: Automatic within refund window, manual approval required outside window

### Safety and Vetting Rules
1. Events can require specific vetting levels
2. Vetted-only events hidden from non-vetted members
3. Anonymous reporting available for all events
4. Incident tracking linked to events

## Implementation Requirements

### CSS and Styling Standards
1. **MANDATORY**: Use Design System v7 standardized CSS classes
2. **FORBIDDEN**: Inline styles or page-specific styling
3. **REQUIRED**: Consistent table styling across all grids
4. **MANDATORY**: Burgundy header styling for all data tables
5. **STANDARDIZED**: Button styling following established patterns

### Data Structure Requirements

#### Event Sessions Data
- sessionId: string (auto-generated S1, S2, S3 format, required)
- sessionName: string (user-editable, required, 3-100 characters)
- eventId: string (required, foreign key to Event)
- sessionDate: DateTime (required, ISO 8601 format)
- startTime: TimeSpan (required)
- endTime: TimeSpan (required)
- capacityLimit: integer (required, minimum 1)
- currentRegistrations: integer (calculated, read-only)
- isActive: boolean (required, default true)

#### Ticket Types Data
- ticketTypeId: string (required, UUID)
- ticketTypeName: string (required, 3-100 characters)
- eventId: string (required, foreign key to Event)
- includedSessions: List<string> (required, session IDs in S# format)
- priceRange: PriceRange (required, min/max values)
- ticketTypeCategory: string (required, enum: Single, Couples)
- quantityAvailable: integer (required, minimum 1)
- salesEndDate: DateTime (required, ISO 8601 format)
- isActive: boolean (required, default true)

#### RSVP Data (Social Events Only)
- rsvpId: string (required, UUID)
- eventId: string (required, foreign key to Event)
- memberId: string (required, foreign key to Member)
- rsvpDate: DateTime (required, ISO 8601 format)
- hasTicket: boolean (indicates if member also purchased ticket)
- status: string (required, enum: Confirmed, Cancelled, CheckedIn)

#### Email Templates Data
- templateId: string (required, UUID)
- templateName: string (required, 3-100 characters)
- eventId: string (required, foreign key to Event)
- targetSessions: List<string> (required, S# format or "All")
- triggerType: string (required, enum: Manual, Confirmation, Reminder, Cancellation)
- subjectLine: string (required, 3-200 characters)
- emailContent: string (required, HTML content)
- isActive: boolean (required, default true)

#### Volunteer Positions Data
- positionId: string (required, UUID)
- positionName: string (required, 3-100 characters)
- eventId: string (required, foreign key to Event)
- assignedSessions: List<string> (required, S# format or "All")
- startTime: TimeSpan (required)
- endTime: TimeSpan (required)
- description: string (optional, 500 characters max)
- volunteersNeeded: integer (required, minimum 1)
- volunteersAssigned: integer (calculated, read-only)

## Constraints & Assumptions

### Constraints
- **Technical**: Integration with existing authentication system required
- **Security**: PCI compliance through external payment processors only
- **Business**: Must maintain community safety and vetting standards
- **Legal**: Must comply with refund policies and payment regulations
- **Capacity**: Session-based capacity limits must be strictly enforced
- **UI Standards**: All interfaces must follow Design System v7 exactly
- **Phase 1 Priority**: Check-in system is required for initial implementation

### Deferred Features (Future Phases)
- **Event Image Upload**: Deferred to future phase, not in current scope
- **Self-Service Kiosk**: May be added in future, current focus is staff-assisted check-in
- **Advanced Reporting**: Basic reporting in Phase 1, advanced analytics deferred

### Assumptions
- Event Organizers understand the Session + Ticket Type relationship
- Staff can be trusted with check-in functionality and credentials
- Members understand vetting requirements for different events
- Check-in devices will have reliable internet connectivity
- Users prefer tabbed interface for complex event management
- Session-based organization improves event management efficiency
- Social events benefit from dual RSVP + ticket purchase options

## Security & Privacy Requirements

### Payment Security
- **ZERO credit card data storage** in WitchCityRope systems
- All payment processing handled by external, PCI-compliant processors
- Payment tokens only stored if required for refund processing
- Immediate processing and disposal of sensitive payment data
- Door payment processing follows same security standards

### Check-In System Security (UPDATED 2025-10-05)
- Staff authentication required for check-in access
- Role-based access control for check-in functionality
- Secure session management for staff logins
- Audit logging of all check-in activities
- No sensitive payment data visible in check-in interface
- Secure handling of door payment transactions

### Member Privacy
- Event attendance lists visible only to Event Organizers and Admins
- Member contact information protected per existing privacy policies
- Anonymous options for sensitive events where appropriate
- Check-in staff access limited to check-in data only
- RSVP and ticket purchase data separately tracked and protected

## Compliance Requirements

### Platform Policies
- Adherence to community standards and safety protocols
- Integration with existing reporting and incident management
- Compliance with membership vetting requirements

### Payment Compliance
- PCI DSS compliance through external processors
- Refund policy compliance with consumer protection laws
- Financial record keeping per business requirements
- Door payment processing compliance

## User Impact Analysis

| User Type | Impact | Priority | Changes |
|-----------|--------|----------|---------|
| Event Organizers | High positive - tabbed interface, session management, check-in tools | High | Full event access, S# session format, standardized tables, staff check-in management |
| Teachers | Low - contact organizers for changes | Medium | NO editing access (confirmed), request-based changes only |
| Vetted Members | High positive - clear ticket options, session visibility, RSVP+ticket flexibility | High | Session-based event details, flexible registration choices, door payment option |
| General Members | Medium positive - public event access, door payment option | Medium | Public event access only, ticket type options, check-in support |
| Admins | Medium positive - same as Event Organizers | Medium | Event Organizer functionality plus admin functions |
| Check-In Staff | High positive - simple check-in interface | Medium | Staff-assisted check-in access, RSVP and ticket verification |

## Examples/Scenarios

### Scenario 1: Multi-Session Event Creation
1. Event Organizer creates "3-Day Advanced Workshop Series"
2. Basic Info tab: Sets event type, description, policies, venue, teachers (NO images)
3. Tickets/Orders tab: Creates sessions S1, S2, S3 with descriptive names
4. Creates ticket types: "Full Series" (S1,S2,S3), "Day 1 Only" (S1), etc.
5. Emails tab: Sets up confirmation email for "All" sessions, reminder for "S1"
6. Volunteers/Staff tab: Creates "Setup Crew" for S1, "Safety Monitor" for All
7. System prevents ticket sales when any included session reaches capacity

### Scenario 2: Admin Dashboard Management
1. Event Organizer accesses admin events dashboard
2. Views events in standardized table with sortable Date and Event Title columns
3. Uses Copy button to duplicate successful event structure
4. Filters by event type and toggles past events visibility
5. Creates new event using amber-gradient Create Event button

### Scenario 3: Social Event with RSVP and Tickets (NEW)
1. Member views social event "Community Rope Jam"
2. Clicks "RSVP" to confirm free attendance (tracked separately)
3. Optionally purchases ticket ahead of time for $10 (tracked separately)
4. System shows member has both RSVP and ticket
5. On event day, staff checks in member using check-in interface
6. Alternative: Member RSVPs but decides to purchase ticket at door during check-in
7. Staff processes ticket purchase and check-in simultaneously

### Scenario 4: Staff-Assisted Check-In (UPDATED)
1. Event day: Organizer grants check-in access to staff member
2. Staff logs in to check-in interface for specific event
3. Interface shows list of expected attendees with status indicators:
   - RSVP only (no ticket)
   - Ticket purchased (may or may not have RSVP)
   - Checked in
4. Staff searches for arriving attendee
5. For RSVP-only: Staff offers ticket purchase at door, processes payment, checks in
6. For ticket holder: Staff verifies ticket, checks in
7. System logs all actions with staff attribution

### Scenario 5: Public Event Discovery and Registration
1. Member browses public events page in card view (no images shown)
2. Sees "3-Day Series" with constraint indicator "5/20 (Day 2)"
3. Clicks event for details, views session availability breakdown
4. Selects "Full Series" ticket but sees constraint warning
5. Chooses "Day 1 Only" instead, completes registration
6. Receives confirmation email targeted to S1 session attendees

### Scenario 6: Email Campaign Management
1. Event Organizer accesses Emails tab for upcoming workshop
2. Clicks "Reminder - 1 Day Before" template card
3. Unified editor loads with current template content
4. Updates session targeting to "S1" for first-day attendees only
5. Saves template changes
6. Clicks "Send Ad-Hoc Email" card for immediate communication
7. Selects recipient group "S2 Attendees" for day-specific message

## Quality Gate Checklist (100% Complete)
- [x] All wireframe features documented exactly as designed
- [x] Session S# format specified throughout all requirements
- [x] Tabbed interface structure defined (Basic Info, Tickets/Orders, Emails, Volunteers/Staff)
- [x] Standardized table structure mandated (Edit first, Delete last)
- [x] Unified email interface specified with ad-hoc always available
- [x] Check-in system requirements detailed (staff-assisted, Phase 1 priority)
- [x] Admin dashboard specifications match wireframe exactly
- [x] Public event display requirements for card and list views
- [x] Event details page ticket selection interface specified
- [x] CSS standardization requirements enforced
- [x] Multi-session capacity constraint logic documented
- [x] RSVP vs ticket system clarified for social events (separate actions)
- [x] Volunteer position session assignment requirements defined
- [x] All user roles addressed with updated permissions
- [x] Teacher editing restrictions explicitly confirmed
- [x] Security requirements documented (no card storage, PCI compliance, staff check-in)
- [x] Compliance requirements checked
- [x] Success metrics defined and measurable
- [x] Technical implementation notes provided for developers
- [x] Data structure requirements specified with S# format enforcement
- [x] Deferred features documented (event images)
- [x] Email automation confirmed as in-scope
- [x] Social event dual-path registration (RSVP + tickets) fully specified
- [x] Door payment processing requirements included
