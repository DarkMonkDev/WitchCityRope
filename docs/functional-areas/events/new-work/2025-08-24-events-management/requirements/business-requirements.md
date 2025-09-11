# Business Requirements: Events Management System
<!-- Last Updated: 2025-08-25 -->
<!-- Version: 3.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Updated to Match Final Wireframes -->

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

### Story 8: Kiosk Mode Check-In System
**As an** Event Organizer
**I want to** generate secure kiosk mode sessions for volunteer check-in
**So that** I can delegate check-in duties without compromising system security

**Acceptance Criteria:**
- Given I need to set up event check-in
- When I launch kiosk mode
- Then system generates secure session with station name and expiration
- And kiosk interface shows session timer and security indicators
- And interface displays event code and session ID in status bar
- And volunteers can search and check in attendees
- And kiosk mode prevents navigation to other admin functions
- And I can revoke kiosk access at any time

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
1. **Basic Info Tab**: Event details, venue, teachers - NO schedule fields
2. **Tickets/Orders Tab**: Sessions grid, ticket types grid, sales tracking, RSVPs
3. **Emails Tab**: Unified template editing with ad-hoc email always available
4. **Volunteers/Staff Tab**: Position management with session-specific assignments

### Event Types and RSVP Rules
1. **Classes**: Ticket-based registration with payment required
2. **Social Events**: RSVP system with optional ticket purchases
3. **RSVP vs Tickets**: Social events show both RSVP table AND tickets sold table
4. **Ticket Purchase Status**: RSVP table shows whether member purchased optional ticket

### Email System Rules
1. **Session Targeting**: All email templates target specific sessions (S1, S2, S3, All)
2. **Ad-hoc Always Available**: "Send Ad-Hoc Email" card permanently visible
3. **Unified Editor**: Same editor interface for templates and ad-hoc emails
4. **Template Management**: Add/remove templates through dropdown selection

### Role-Based Permissions
1. **Event Organizers**:
   - Full access to ALL events (view, create, edit, delete)
   - All tabs accessible in event creation/editing
   - Generate kiosk mode sessions
   - Access admin events dashboard with full controls

2. **Teachers**:
   - View events they're assigned to teach
   - Contact Event Organizers for any changes needed
   - No direct editing capabilities

3. **Admin**:
   - Same permissions as Event Organizers
   - Additional system administration capabilities
   - User role management

4. **Members**:
   - View and register for events based on vetting level
   - Cancel registrations within refund window
   - View their registration history

### Multi-Ticket Type System Rules (Event Session Matrix)
1. **Session-Based Capacity**: Capacity tracked per session (S1, S2, S3), not per ticket type
2. **Real-Time Calculation**: System sums all tickets including each session
3. **Sales Prevention**: Ticket sales stop when ANY included session reaches capacity
4. **Prerequisite Support**: Sessions can require attendance at previous sessions
5. **Waitlist Granularity**: Waitlists trigger when specific sessions fill up

### Kiosk Mode Security Rules
1. **Session-Based Authentication**: Secure tokens with time-based expiration
2. **Device Binding**: Tokens tied to specific device/browser session
3. **Function Limitation**: Check-in only, no navigation to other admin functions
4. **Visual Security Indicators**: Clear kiosk mode branding and session information
5. **Audit Logging**: All check-in actions logged with session attribution

### Payment Processing Rules
1. **No Credit Card Storage**: All payment processing is immediate and external
2. **Supported Methods (Future Implementation)**:
   - Direct credit card processing (immediate, no storage)
   - PayPal integration
   - Venmo integration
3. **Phase 1 Implementation**: Payment tracking only, external payment processing
4. **Refunds**: Automatic within refund window, manual approval required outside window

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

### Assumptions
- Event Organizers understand the Session + Ticket Type relationship
- Volunteers can be trusted with limited check-in functionality through kiosk mode
- Members understand vetting requirements for different events
- Kiosk devices will have reliable internet connectivity
- Users prefer tabbed interface for complex event management
- Session-based organization improves event management efficiency

## Security & Privacy Requirements

### Payment Security
- **ZERO credit card data storage** in WitchCityRope systems
- All payment processing handled by external, PCI-compliant processors
- Payment tokens only stored if required for refund processing
- Immediate processing and disposal of sensitive payment data

### Kiosk Mode Security
- Cryptographically secure session token generation
- Time-bound session expiration with automatic cleanup
- Device binding to prevent URL sharing
- Function-specific access limitation (check-in only)
- Real-time session monitoring and audit logging

### Member Privacy
- Event attendance lists visible only to Event Organizers and Admins
- Member contact information protected per existing privacy policies
- Anonymous options for sensitive events where appropriate
- Kiosk mode access limited to check-in data only

## Compliance Requirements

### Platform Policies
- Adherence to community standards and safety protocols
- Integration with existing reporting and incident management
- Compliance with membership vetting requirements

### Payment Compliance
- PCI DSS compliance through external processors
- Refund policy compliance with consumer protection laws
- Financial record keeping per business requirements

## User Impact Analysis

| User Type | Impact | Priority | Changes |
|-----------|--------|----------|---------|
| Event Organizers | High positive - tabbed interface, session management | High | Full event access, S# session format, standardized tables |
| Teachers | Low - contact organizers for changes | Medium | Removed editing access, request-based changes |
| Vetted Members | High positive - clear ticket options, session visibility | High | Session-based event details, flexible ticket choices |
| General Members | Medium positive - public event access | Medium | Public event access only, ticket type options |
| Admins | Medium positive - same as Event Organizers | Medium | Event Organizer functionality plus admin functions |
| Volunteers | High positive - simple kiosk check-in | Medium | Kiosk mode access, no training required |

## Examples/Scenarios

### Scenario 1: Multi-Session Event Creation
1. Event Organizer creates "3-Day Advanced Workshop Series"
2. Basic Info tab: Sets event type, description, policies, venue, teachers
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

### Scenario 3: Kiosk Mode Check-In
1. Event day: Organizer clicks "Launch Check-In" for specific event
2. Selects station name "Front Desk 1", 4-hour session duration
3. System generates secure kiosk link with embedded authentication
4. Volunteer accesses link, sees kiosk-branded interface
5. Interface shows session timer, event code, secure indicators
6. Volunteer searches attendees, processes check-ins with modal dialogs
7. System logs all actions, auto-expires session after 4 hours

### Scenario 4: Public Event Discovery and Registration
1. Member browses public events page in card view
2. Sees "3-Day Series" with constraint indicator "5/20 (Day 2)"
3. Clicks event for details, views session availability breakdown
4. Selects "Full Series" ticket but sees constraint warning
5. Chooses "Day 1 Only" instead, completes registration
6. Receives confirmation email targeted to S1 session attendees

### Scenario 5: Email Campaign Management
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
- [x] Kiosk mode security and interface requirements detailed
- [x] Admin dashboard specifications match wireframe exactly
- [x] Public event display requirements for card and list views
- [x] Event details page ticket selection interface specified
- [x] CSS standardization requirements enforced
- [x] Multi-session capacity constraint logic documented
- [x] RSVP vs ticket system clarified for social events
- [x] Volunteer position session assignment requirements defined
- [x] All user roles addressed with updated permissions
- [x] Security requirements documented (no card storage, PCI compliance, kiosk mode)
- [x] Compliance requirements checked
- [x] Success metrics defined and measurable
- [x] Technical implementation notes provided for developers
- [x] Data structure requirements specified with S# format enforcement