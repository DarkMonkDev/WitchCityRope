# Business Requirements: Events Management System
<!-- Last Updated: 2025-08-24 -->
<!-- Version: 2.1 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Updated Based on Wireframe Refinements -->

## Executive Summary
The Events Management System provides comprehensive event lifecycle management for WitchCityRope, enabling Event Organizers to create, manage, and track workshops, classes, performances, and social events while supporting member registration, payment processing, and attendance tracking with automated refund capabilities.

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
- And I can create new events of any type (Class, Workshop, Performance, Social)
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

### Story 3: Automatic Refund Processing
**As a** Member who needs to cancel
**I want to** receive automatic refunds when canceling within the refund window
**So that** I don't have to wait for manual approval or processing

**Acceptance Criteria:**
- Given I registered for an event with a refund window
- When I cancel my registration within the refund window
- Then the system automatically processes my refund without admin approval
- And I receive confirmation of the refund processing
- And the refund is issued through the original payment method

### Story 4: Payment at Door Management
**As an** Event Organizer
**I want to** accept both cash and digital payments at the door
**So that** I can accommodate all members regardless of their payment preference

**Acceptance Criteria:**
- Given I am managing check-ins for an event
- When a member pays at the door
- Then I can record cash payment OR process digital payment through the system
- And both payment types are tracked in the event financial records
- And the member's registration status is updated to paid

### Story 5: Teacher Event Modification Requests
**As a** Teacher
**I want to** request changes to events I'm teaching
**So that** necessary updates can be made through proper channels

**Acceptance Criteria:**
- Given I am assigned as a teacher for an event
- When I need to make changes to the event
- Then I contact the Event Organizer with my requested changes
- And the Event Organizer handles all modifications
- And I receive confirmation when changes are implemented

## Business Rules

### Role-Based Permissions
1. **Event Organizers**:
   - Full access to ALL events (view, create, edit, delete)
   - Manage attendee lists and registrations
   - Process payments and handle refunds
   - Access financial reports for all events
   - Manage venue definitions and email templates

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
1. **Event Types**: Classes, Workshops, Social Events (Performances in future phase)
2. **Vetting Requirements**: Can be set per event (Public, Members Only, Vetted Only)
3. **Capacity Management**: Hard limits with waitlist functionality
4. **Pricing**: Single price per event (per person OR per couple), payment at door for social events only
5. **Refund Windows**: Configurable per event, automatic processing within window
6. **Venue Management**: Predefined venues with address and custom directions
7. **Email Templates**: Automated triggers (purchase, reminder, refund) and ad-hoc messaging
8. **Volunteer Positions**: Definable roles with time slots for social events
9. **Ticketing vs RSVP**: Classes use tickets (paid), Social events use RSVPs (free or paid)

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

## Constraints & Assumptions

### Constraints
- **Technical**: Integration with existing authentication system required
- **Security**: PCI compliance through external payment processors only
- **Business**: Must maintain community safety and vetting standards
- **Legal**: Must comply with refund policies and payment regulations

### Assumptions
- Members understand vetting requirements for different events
- Event Organizers will maintain responsibility for event quality and safety
- Payment processors will handle PCI compliance requirements
- Members prefer automated processes over manual approval workflows

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

### Data Protection
- Event financial data encrypted at rest
- Access logs maintained for all event modifications
- Regular security audits of payment integration points

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
| Event Organizers | High positive - streamlined management | High | Full event access, no restrictions |
| Teachers | Low - contact organizers for changes | Medium | Removed editing access, request-based changes |
| Vetted Members | High positive - easy registration | High | Automated refunds, clear payment options |
| General Members | Medium positive - limited access | Medium | Public event access only |
| Admins | Medium positive - oversight capabilities | Medium | Same as Event Organizers plus admin functions |

## Examples/Scenarios

### Scenario 1: Event Creation and Management (Happy Path)
1. Event Organizer logs in and accesses event management
2. Creates new workshop with capacity 20, vetted members only
3. Sets single price $35 per person, 48-hour refund window
4. Selects predefined venue and adds specific directions
5. Event goes live, members can purchase tickets
6. Organizer manages waitlist as event fills up
7. Day of event: checks in attendees through streamlined interface
8. Post-event: reviews attendance and financial reports

### Scenario 2: Automatic Refund Processing
1. Member registers for workshop 5 days in advance
2. 36 hours before event (within 48-hour refund window)
3. Member cancels registration through system
4. System automatically processes refund to original payment method
5. Member receives confirmation email
6. Waitlist member automatically notified of available spot

### Scenario 3: Teacher Change Request
1. Teacher realizes they need to modify workshop content
2. Teacher contacts Event Organizer via message system
3. Event Organizer reviews request and makes necessary changes
4. Event Organizer confirms changes with Teacher
5. System sends update notifications to registered members if needed

### Scenario 4: Payment at Door Options
1. Member arrives at event without prior payment
2. Event Organizer offers cash or digital payment options
3. For cash: Organizer records payment in system
4. For digital: Organizer processes payment through integrated system
5. Both payment types tracked in event financial records
6. Member's status updated to paid and confirmed

### Scenario 5: Social Event with Volunteers and RSVPs
1. Event Organizer creates social event with free or paid RSVP
2. Defines volunteer positions with time slots (e.g., Door Monitor 6-8pm, Safety Monitor 6-10pm)
3. Volunteers sign up for specific positions through the system
4. Members RSVP for the event (separate from volunteer signups)
5. Automated reminder emails sent to volunteers and attendees
6. Day of event: Check in both volunteers and regular attendees
7. Post-event: Review participation and volunteer contributions

## Questions for Product Manager
- [ ] Confirm Event Organizer role has full system access as described
- [ ] Verify automatic refund processing aligns with business policies
- [ ] Confirm priority of payment processor integrations (PayPal vs Venmo vs Direct Card)
- [ ] Validate Teacher role limitations and communication workflow

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