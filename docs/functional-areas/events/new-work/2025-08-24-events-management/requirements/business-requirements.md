# Business Requirements: Events Management System
<!-- Last Updated: 2025-08-24 -->
<!-- Version: 2.4 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Event Session Matrix Added -->

## Executive Summary
The Events Management System provides comprehensive event lifecycle management for WitchCityRope, enabling Event Organizers to create, manage, and track classes, social events, and performances while supporting member registration, payment processing, and attendance tracking with automated refund capabilities. The system implements a flexible Event Session Matrix that naturally handles complex multi-ticket scenarios.

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

### Story 1: Event Creation and Management
**As an** Event Organizer
**I want to** create and manage all types of events with full control over settings
**So that** I can efficiently handle all event operations for the community

**Acceptance Criteria:**
- Given I am logged in as an Event Organizer
- When I access the events management section
- Then I can view, edit, and manage ALL events in the system
- And I can create new events of any type (Class, Social Event, Performance)
- And I can set pricing, capacity, vetting requirements, and refund policies
- And I can manage attendee lists and handle registrations

### Story 2: Member Event Registration
**As a** Vetted Member
**I want to** register for events with clear pricing and refund information
**So that** I can participate in community activities confidently

**Acceptance Criteria:**
- Given I am logged in as a Vetted Member
- When I view an event listing
- Then I see clear pricing, capacity, and refund policy information
- And I can register if spots are available or join waitlist if full
- And I receive confirmation of registration with payment options
- And I can cancel within the refund window for automatic refund

### Story 3: Multi-Ticket Type Event Registration
**As a** Member
**I want to** choose from different ticket types for multi-session events
**So that** I can attend just the sessions I'm interested in or get a discount for the full series

**Acceptance Criteria:**
- Given I am viewing a multi-session event (e.g., 3-day workshop series)
- When I see the ticket options
- Then I can choose from different ticket types (Full Series, Individual Days, Weekend Pass)
- And I can see which sessions each ticket type includes
- And I can see capacity remaining for the sessions I want to attend
- And I receive confirmation showing exactly which sessions I'm registered for

### Story 4: Event Session Matrix Management
**As an** Event Organizer
**I want to** create events with multiple sessions and flexible ticket types
**So that** I can offer complex pricing options while managing capacity accurately

**Acceptance Criteria:**
- Given I am creating a multi-day workshop
- When I define the event structure
- Then I can create multiple sessions (Day 1, Day 2, Day 3) each with individual capacity limits
- And I can create ticket types that include different combinations of sessions
- And the system prevents overselling by tracking capacity per session
- And I can see real-time capacity usage across all sessions
- And waitlists trigger when any included session reaches capacity

### Story 5: Automatic Refund Processing
**As a** Member who needs to cancel
**I want to** receive automatic refunds when canceling within the refund window
**So that** I don't have to wait for manual approval or processing

**Acceptance Criteria:**
- Given I registered for an event with a refund window
- When I cancel my registration within the refund window
- Then the system automatically processes my refund without admin approval
- And I receive confirmation of the refund processing
- And the refund is issued through the original payment method

### Story 6: Payment at Door Management
**As an** Event Organizer
**I want to** accept both cash and digital payments at the door
**So that** I can accommodate all members regardless of their payment preference

**Acceptance Criteria:**
- Given I am managing check-ins for an event using kiosk mode
- When a member pays at the door
- Then I can record cash payment OR process digital payment through the system
- And both payment types are tracked in the event financial records
- And the member's registration status is updated to paid

### Story 7: Teacher Event Modification Requests
**As a** Teacher
**I want to** request changes to events I'm teaching
**So that** necessary updates can be made through proper channels

**Acceptance Criteria:**
- Given I am assigned as a teacher for an event
- When I need to make changes to the event
- Then I contact the Event Organizer with my requested changes
- And the Event Organizer handles all modifications
- And I receive confirmation when changes are implemented

### Story 8: Secure Check-In Delegation
**As an** Event Organizer
**I want to** set up secure check-in stations without giving admin access to volunteers
**So that** I can delegate check-in duties while maintaining security

**Acceptance Criteria:**
- Given I need to set up check-in for an event
- When I generate a kiosk mode session link
- Then I can specify station name, expiration time, and security settings
- And volunteers can use the link to access check-in functionality only
- And the system prevents navigation to other admin functions
- And I can revoke access at any time if needed

## Business Rules

### Role-Based Permissions
1. **Event Organizers**:
   - Full access to ALL events (view, create, edit, delete)
   - Manage attendee lists and registrations
   - Process payments and handle refunds
   - Access financial reports for all events
   - Manage venue definitions and email templates
   - Generate secure kiosk mode sessions for check-in

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

### Event Management Rules
1. **Event Types**: 
   - **Classes** (all educational events including workshops, tutorials, skill-building sessions)
   - **Social Events** (community gatherings, parties, munches)
   - **Performances** (future phase - demonstrations, shows, exhibitions)
2. **Vetting Requirements**: Can be set per event (Public, Members Only, Vetted Only)
3. **Capacity Management**: Hard limits with waitlist functionality
4. **Pricing**: Single price per event (per person OR per couple), payment at door for social events only
5. **Refund Windows**: Configurable per event, automatic processing within window
6. **Venue Management**: Predefined venues with address and custom directions
7. **Email Templates**: Automated triggers (purchase, reminder, refund) and ad-hoc messaging
8. **Volunteer Positions**: Definable roles with time slots for social events
9. **Ticketing vs RSVP**: Classes use tickets (paid), Social events use RSVPs (free or paid)

### Multi-Ticket Type System Rules
1. **Session-Based Capacity**: Capacity tracked per session, not per ticket type
2. **Real-Time Calculation**: System sums all tickets including each session
3. **Sales Prevention**: Ticket sales stop when ANY included session reaches capacity
4. **Prerequisite Support**: Sessions can require attendance at previous sessions
5. **Waitlist Granularity**: Waitlists trigger when specific sessions fill up

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

## 7. Check-In Security (Kiosk Mode)

### The Problem
Event Organizers need to delegate check-in duties to volunteers without providing admin access to the entire system. Traditional solutions requiring password sharing or elevated permissions create security risks and administrative overhead.

### The Solution: Kiosk Mode with Session Links
The kiosk mode system provides secure, time-limited access to check-in functionality without requiring login credentials or admin permissions for front desk staff.

### How It Works

#### Session Generation (Admin/Event Organizer)
1. Admin clicks "Launch Check-In" from the admin dashboard
2. Selects station configuration:
   - Station name (Front Desk 1, Mobile Device, Volunteer Tablet, etc.)
   - Event selection (current or upcoming events)
   - Session expiration time (2-12 hours, default 4 hours)
   - Access permissions (check-in only, payment processing, etc.)
3. System generates secure session link with embedded authentication token
4. Admin can open link on target device or provide QR code for scanning
5. Admin can fully log out after launching kiosk mode on volunteer devices

#### Kiosk Mode Operation (Volunteer/Front Desk)
1. Volunteer accesses provided secure link (no login required)
2. Browser locks to check-in interface only
3. Cannot navigate to other pages or access admin functions
4. Check-in interface provides:
   - Member search and verification
   - Payment status tracking
   - Attendance recording
   - Basic event information display
5. Session automatically expires after configured time

### Security Features

#### Token-Based Authentication
- **POST-based validation**: Tokens validated server-side, not just URL parameters
- **Embedded session data**: No sensitive information in URL query strings
- **Cryptographically signed**: Prevents token tampering or forgery
- **Time-bound expiration**: Automatic invalidation after configured period

#### Device Security
- **Device fingerprinting**: Prevents URL sharing between devices
- **Browser session binding**: Token tied to specific browser session
- **IP address validation**: Optional restriction to launching location
- **Secure session storage**: Uses httpOnly cookies, not localStorage

#### Access Controls
- **Function-specific permissions**: Check-in only, no broader admin access
- **Event-specific scope**: Access limited to specified events only
- **Real-time revocation**: Admin can instantly terminate any kiosk session
- **Full audit logging**: All check-in actions logged with session attribution

### User Experience Benefits

#### For Event Organizers
- **2-minute setup**: Quick session generation and device handoff
- **Zero training required**: Volunteers need no system knowledge
- **Works on any device**: Borrowed laptops, tablets, phones all supported
- **Clean handoff process**: No password sharing or account creation
- **Remote management**: Can revoke access from anywhere

#### For Volunteers
- **No login complexity**: Simple link access
- **Focused interface**: Only check-in functions visible
- **Intuitive operation**: Self-explanatory controls
- **No system access fears**: Cannot accidentally access admin functions

### Implementation Requirements

#### Technical Security
- Session tokens must use cryptographically secure random generation
- All kiosk mode communications use HTTPS only
- Session data encrypted at rest in secure session store
- Automatic cleanup of expired sessions and associated data

#### User Interface
- Kiosk mode interface clearly branded as limited-access mode
- Visual indicators showing session expiration countdown
- Emergency admin contact information always visible
- Graceful session expiration with clear messaging

#### Audit and Monitoring
- Real-time session monitoring dashboard for administrators
- Complete audit trail of all check-in actions by session
- Alert system for suspicious activity or multiple device access attempts
- Session usage analytics for optimization

## 8. Multi-Ticket Type System (Event Session Matrix)

### 🚨 CRITICAL FEATURE: EVENT SESSION MATRIX 🚨
**This is a CORE architectural feature that handles complex ticketing scenarios naturally. Developers MUST understand this system to implement events correctly.**

### Core Concept
Events are composed of **Sessions**, and **Tickets** grant access to specific **Sessions**. This creates a flexible matrix that handles complex scenarios naturally without special case logic.

**Formula: Event = Sessions + Ticket Types (where each Ticket Type = Session Combination)**

### Key Components

#### 1. Sessions
Events can have 1 or more sessions representing discrete time blocks or content modules:

**Examples:**
- **3-Day Workshop**: Day 1 (Fundamentals), Day 2 (Intermediate), Day 3 (Advanced)
- **Progressive Class**: Morning Session (Beginner), Afternoon Session (Advanced)  
- **Single Class**: Single Session (the entire class)
- **Weekend Intensive**: Saturday Session, Sunday Session
- **Multi-Location Event**: Venue A Session, Venue B Session

**Session Properties:**
- **Name**: "Day 1: Rope Fundamentals" or "Saturday Morning Session"
- **Date/Time**: Specific scheduling for each session
- **Capacity Limit**: Individual capacity for each session (can be different)
- **Location**: Can override main event venue if needed
- **Prerequisites**: Can require completion of previous sessions

#### 2. Ticket Types
Each event can have multiple ticket types, and each ticket specifies which sessions it includes:

**Examples for 3-Day Workshop:**
- **"Full 3-Day Series Pass"** → Includes: Day 1, Day 2, Day 3 → Price: $150
- **"Day 1 Only"** → Includes: Day 1 → Price: $60
- **"Day 2 Only"** → Includes: Day 2 → Price: $60  
- **"Day 3 Only"** → Includes: Day 3 → Price: $60
- **"Weekend Pass (Days 2-3)"** → Includes: Day 2, Day 3 → Price: $100

**Examples for Progressive Class:**
- **"All-Day Pass"** → Includes: Morning Session, Afternoon Session → Price: $80
- **"Morning Only (Beginner)"** → Includes: Morning Session → Price: $45
- **"Afternoon Only (Advanced)"** → Includes: Afternoon Session → Price: $50

**Examples for Single Class:**
- **"Standard Ticket"** → Includes: Single Session → Price: $35

#### 3. Capacity Management (THE CRITICAL PIECE)
Capacity is tracked **PER SESSION**, not per ticket type or event. This is the key to the entire system.

**Real-Time Calculation:**
For each session, the system counts **ALL tickets that include that session**.

**Example Scenario - Day 2 of 3-Day Workshop:**
- Day 2 Session Capacity: 20 people
- Current sales:
  - 5 people bought "Full 3-Day Series" (includes Day 2) = 5 spots used
  - 10 people bought "Day 2 Only" (includes Day 2) = 10 spots used  
  - 3 people bought "Weekend Pass" (includes Day 2) = 3 spots used
- **Day 2 Total: 18/20 spots filled**
- **Remaining availability: 2 spots**

**Sales Impact:**
- Someone wanting "Day 2 Only" → Can purchase (2 spots remaining)
- Someone wanting "Full 3-Day Series" → Can purchase ONLY if Day 1 AND Day 3 also have availability
- Someone wanting "Weekend Pass" → Can purchase ONLY if Day 3 also has availability

#### 4. Business Rules

**Capacity Enforcement:**
- Ticket sales STOP when ANY included session is full
- System validates ALL sessions before allowing purchase
- Real-time capacity checking prevents overselling

**Waitlist Logic:**
- Waitlists trigger when specific sessions fill up
- Members can join waitlists for specific ticket types
- When someone cancels, system offers spot to appropriate waitlist members

**Prerequisite Support:**
- Sessions can require completion of previous sessions
- Example: Day 2 requires Day 1 attendance
- System validates prerequisites during ticket purchase

**Refund Handling:**
- Canceling a ticket frees up capacity for ALL included sessions
- Waitlist notifications trigger automatically
- Partial refunds possible for multi-session tickets (future phase)

#### 5. Common Use Cases

##### Multi-Day Workshop Series
```
Event: "Advanced Shibari Techniques - 3-Day Intensive"

Sessions:
- Day 1: "Fundamentals Review" (Friday 7-10pm) - Capacity: 20
- Day 2: "Single Column Ties" (Saturday 2-6pm) - Capacity: 20  
- Day 3: "Suspension Basics" (Sunday 1-5pm) - Capacity: 16 (smaller space)

Ticket Types:
- "Full Series Pass" → Day 1, 2, 3 → $180 (save $30)
- "Day 1 Only" → Day 1 → $70
- "Day 2 Only" → Day 2 → $70  
- "Day 3 Only" → Day 3 → $70
- "Weekend Intensive" → Day 2, 3 → $130 (save $10)

Capacity Logic:
- Day 3 fills first (smallest capacity at 16)
- "Full Series Pass" and "Weekend Intensive" sales stop when Day 3 is full
- "Day 1 Only" and "Day 2 Only" can still sell until their sessions fill
```

##### Tiered Pricing (Member vs Non-Member)
```
Event: "Rope Fundamentals Workshop"

Sessions:
- Single Session: "Rope Fundamentals" (Saturday 2-5pm) - Capacity: 25

Ticket Types:
- "Member Rate" → Single Session → $40
- "Non-Member Rate" → Single Session → $60

Capacity Logic:
- Both ticket types count toward the same session capacity
- No difference in session access, only pricing
```

##### Progressive Skill Building
```
Event: "Beginner to Advanced Rope Journey"  

Sessions:
- Morning: "Beginner Basics" (10am-1pm) - Capacity: 30
- Afternoon: "Advanced Techniques" (2-5pm) - Capacity: 20

Ticket Types:
- "Beginner Only" → Morning Session → $50
- "Advanced Only" → Morning Session (prerequisite) → $60  
- "Full Day Journey" → Morning + Afternoon → $90

Prerequisites:
- Afternoon session requires Morning session completion
- "Advanced Only" ticket still includes Morning (for prerequisite)
- "Full Day Journey" most cost-effective for both sessions
```

#### 6. Technical Implementation Notes

**Data Relationships:**
- Event → HasMany → Sessions
- Event → HasMany → TicketTypes  
- TicketType → BelongsToMany → Sessions (junction table)
- Registration → BelongsTo → TicketType
- Attendance → BelongsTo → Session + Registration

**Key Queries:**
- Session capacity = COUNT(Registrations WHERE TicketType includes Session)
- Available tickets = MIN(capacity remaining for ALL included sessions)
- Waitlist eligibility = Which sessions are full for desired ticket type

**API Endpoints Needed:**
- `GET /events/{id}/availability` - Real-time capacity for all ticket types
- `POST /events/{id}/register` - Validates session capacity before purchase
- `GET /events/{id}/sessions/{sessionId}/attendance` - Session check-in data

#### 7. User Interface Implications

**Event Display:**
- Show ticket types with included sessions clearly marked
- Display capacity remaining for each ticket type (not sessions)
- Indicate when ticket types are unavailable due to session capacity

**Registration Flow:**
- Clear breakdown of included sessions for selected ticket type
- Calendar view showing when member will attend
- Prerequisite warnings if applicable

**Admin Interface:**
- Session-by-session capacity monitoring
- Visual matrix showing ticket type ↔ session relationships
- Easy setup for common patterns (single day, multi-day, tiered pricing)

### Why This Architecture Matters

1. **Flexibility**: Handles simple single-session events AND complex multi-session events with the same code
2. **Accuracy**: Prevents overselling by tracking the RIGHT metric (session capacity)
3. **Scalability**: New ticket combinations don't require code changes
4. **User Experience**: Natural pricing options without confusing capacity logic
5. **Business Value**: Enables complex pricing strategies that increase revenue

### 🚨 Developer Alert: Common Mistakes to Avoid

1. **❌ WRONG**: Tracking capacity by ticket type
2. **❌ WRONG**: Separate logic for single vs multi-session events  
3. **❌ WRONG**: Manual capacity calculations
4. **❌ WRONG**: Assuming one ticket = one session

**✅ CORRECT**: Always think Sessions + Tickets, with capacity tracked per session

This Event Session Matrix is the FOUNDATION of the entire events system. Every other feature (payments, refunds, check-in, reporting) builds on this concept.

## Constraints & Assumptions

### Constraints
- **Technical**: Integration with existing authentication system required
- **Security**: PCI compliance through external payment processors only
- **Business**: Must maintain community safety and vetting standards
- **Legal**: Must comply with refund policies and payment regulations
- **Capacity**: Session-based capacity limits must be strictly enforced

### Assumptions
- Members understand vetting requirements for different events
- Event Organizers will maintain responsibility for event quality and safety
- Payment processors will handle PCI compliance requirements
- Members prefer automated processes over manual approval workflows
- Volunteers can be trusted with limited check-in functionality
- Kiosk devices will have reliable internet connectivity
- Event Organizers understand the Session + Ticket Type relationship

## Security & Privacy Requirements

### Payment Security
- **ZERO credit card data storage** in WitchCityRope systems
- All payment processing handled by external, PCI-compliant processors
- Payment tokens only stored if required for refund processing
- Immediate processing and disposal of sensitive payment data

### Member Privacy
- Event attendance lists visible only to Event Organizers and Admins
- Member contact information protected per existing privacy policies
- Anonymous options for sensitive events where appropriate
- Kiosk mode access limited to check-in data only

### Data Protection
- Event financial data encrypted at rest
- Access logs maintained for all event modifications
- Regular security audits of payment integration points
- Kiosk session data encrypted and automatically purged
- Real-time monitoring of all kiosk mode sessions

## Compliance Requirements

### Payment Compliance
- PCI DSS compliance through external processors
- Refund policy compliance with consumer protection laws
- Financial record keeping per business requirements

### Platform Policies
- Adherence to community standards and safety protocols
- Integration with existing reporting and incident management
- Compliance with membership vetting requirements

## User Impact Analysis

| User Type | Impact | Priority | Changes |
|-----------|--------|----------|---------|
| Event Organizers | High positive - streamlined management | High | Full event access, kiosk mode generation capabilities, session matrix management |
| Teachers | Low - contact organizers for changes | Medium | Removed editing access, request-based changes |
| Vetted Members | High positive - easy registration | High | Automated refunds, clear payment options, flexible ticket choices |
| General Members | Medium positive - limited access | Medium | Public event access only, ticket type options |
| Admins | Medium positive - oversight capabilities | Medium | Same as Event Organizers plus admin functions |
| Volunteers | High positive - simple check-in process | Medium | Kiosk mode access, no training required |

## Examples/Scenarios

### Scenario 1: Event Creation and Management (Happy Path)
1. Event Organizer logs in and accesses event management
2. Creates new class with capacity 20, vetted members only
3. Sets single price $35 per person, 48-hour refund window
4. Selects predefined venue and adds specific directions
5. Event goes live, members can purchase tickets
6. Organizer manages waitlist as event fills up
7. Day of event: generates kiosk mode session for volunteer check-in
8. Post-event: reviews attendance and financial reports

### Scenario 2: Multi-Session Event with Flexible Ticketing
1. Event Organizer creates "3-Day Advanced Workshop Series"
2. Sets up sessions:
   - Day 1: "Fundamentals" (Friday) - Capacity 25
   - Day 2: "Intermediate" (Saturday) - Capacity 25  
   - Day 3: "Advanced" (Sunday) - Capacity 20
3. Creates ticket types:
   - "Full 3-Day Pass" → All sessions → $150
   - "Day 1 Only" → Day 1 → $60
   - "Day 2 Only" → Day 2 → $60
   - "Day 3 Only" → Day 3 → $60
   - "Weekend Pass" → Day 2,3 → $100
4. Members purchase various ticket types
5. Day 3 fills up first (lowest capacity)
6. System stops selling "Full 3-Day Pass" and "Weekend Pass"
7. "Day 1 Only" and "Day 2 Only" continue selling
8. Real-time dashboard shows: Day 1 (18/25), Day 2 (22/25), Day 3 (20/20 FULL)

### Scenario 3: Automatic Refund Processing
1. Member registers for class 5 days in advance
2. 36 hours before event (within 48-hour refund window)
3. Member cancels registration through system
4. System automatically processes refund to original payment method
5. Member receives confirmation email
6. Waitlist member automatically notified of available spot

### Scenario 4: Teacher Change Request
1. Teacher realizes they need to modify class content
2. Teacher contacts Event Organizer via message system
3. Event Organizer reviews request and makes necessary changes
4. Event Organizer confirms changes with Teacher
5. System sends update notifications to registered members if needed

### Scenario 5: Payment at Door Options
1. Member arrives at event without prior payment
2. Volunteer at check-in (using kiosk mode) offers cash or digital payment options
3. For cash: Volunteer records payment in kiosk interface
4. For digital: Volunteer processes payment through integrated kiosk system
5. Both payment types tracked in event financial records
6. Member's status updated to paid and confirmed

### Scenario 6: Social Event with Volunteers and RSVPs
1. Event Organizer creates social event with free or paid RSVP
2. Defines volunteer positions with time slots (e.g., Door Monitor 6-8pm, Safety Monitor 6-10pm)
3. Volunteers sign up for specific positions through the system
4. Members RSVP for the event (separate from volunteer signups)
5. Automated reminder emails sent to volunteers and attendees
6. Day of event: Check in both volunteers and regular attendees through kiosk mode
7. Post-event: Review participation and volunteer contributions

### Scenario 7: Kiosk Mode Setup and Operation
1. Event Organizer arrives 30 minutes before event start
2. Opens admin dashboard and clicks "Launch Check-In" for the event
3. Configures kiosk session:
   - Station: "Front Desk Laptop"
   - Duration: 4 hours (event + cleanup time)
   - Permissions: Check-in + Payment processing
4. Generates secure session link and opens on volunteer's laptop
5. Logs out of own admin session on the laptop
6. Hands laptop to volunteer with simple instructions
7. Volunteer uses kiosk interface to check in attendees throughout event
8. System automatically expires kiosk access 4 hours later
9. Post-event: Admin reviews check-in audit logs and session usage

### Scenario 8: Complex Multi-Ticket Capacity Management
1. "Advanced Rope Workshop" with 3 sessions:
   - Session A: Capacity 20 
   - Session B: Capacity 15 (smaller room)
   - Session C: Capacity 25
2. Ticket sales progress:
   - 8 "Full Pass" tickets sold (includes A+B+C) 
   - 5 "A+B Pass" tickets sold
   - 2 "B Only" tickets sold
3. Current session usage:
   - Session A: 13/20 (8 full + 5 A+B = 13)
   - Session B: 15/15 FULL (8 full + 5 A+B + 2 B-only = 15)  
   - Session C: 8/25 (8 full only)
4. System behavior:
   - "Full Pass" sales STOP (Session B is full)
   - "A+B Pass" sales STOP (Session B is full)
   - "B Only" sales STOP (Session B is full)
   - "A Only" and "C Only" tickets can still sell
   - Waitlist forms for any ticket including Session B

## Questions for Product Manager
- [ ] Confirm Event Organizer role has full system access as described
- [ ] Verify automatic refund processing aligns with business policies
- [ ] Confirm priority of payment processor integrations (PayPal vs Venmo vs Direct Card)
- [ ] Validate Teacher role limitations and communication workflow
- [ ] Approve kiosk mode security implementation and session duration limits
- [ ] Confirm volunteer access scope and audit requirements
- [ ] **Validate Event Session Matrix approach for multi-ticket scenarios**
- [ ] **Confirm session-based capacity tracking vs ticket-based capacity**
- [ ] **Approve complexity of ticket type creation interface for Event Organizers**

## Future Payment Integration Planning

### Phase 1 (Current): Payment Tracking Only
- Manual payment processing outside system
- Payment status tracking within system
- Basic refund management

### Phase 2 (Future): Direct Payment Processing
- **Direct Credit Card**: Immediate processing, no storage, PCI through processor
- **PayPal Integration**: OAuth integration, immediate processing
- **Venmo Integration**: API integration where available

### Phase 3 (Future): Advanced Features
- Recurring payments for memberships
- Split payments for couples/groups
- Advanced financial reporting and analytics
- Partial refunds for multi-session tickets

## Quality Gate Checklist (100% Complete)
- [x] All user roles addressed with updated permissions
- [x] Clear acceptance criteria for each story
- [x] Business value clearly defined with updated metrics
- [x] Edge cases considered (refunds, payment methods, teacher requests)
- [x] Security requirements documented (no card storage, PCI compliance)
- [x] Compliance requirements checked
- [x] Performance expectations set
- [x] Mobile experience considered
- [x] Examples provided for all major scenarios
- [x] Success metrics defined and measurable
- [x] Event Organizer role simplified to full access
- [x] Teacher role limitations clearly defined
- [x] Automatic refund processing specified
- [x] Payment processing options documented for future phases
- [x] Payment at door clarified (cash OR digital)
- [x] Credit card security explicitly addressed
- [x] Event types corrected - Classes unified (workshops are classes)
- [x] Check-in security (kiosk mode) fully documented
- [x] Volunteer access model defined with security controls
- [x] Session-based authentication specified for kiosk mode
- [x] Audit and monitoring requirements documented
- [x] **Multi-ticket type system (Event Session Matrix) prominently documented**
- [x] **Session-based capacity management clearly defined**
- [x] **Business rules for complex ticketing scenarios specified**
- [x] **User stories covering multi-session event scenarios included**
- [x] **Technical implementation notes provided for developers**
- [x] **Common use cases and examples extensively documented**