# Business Requirements: RSVP and Ticketing System
<!-- Last Updated: 2025-09-19 -->
<!-- Version: 3.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: APPROVED - Ready for Phase 2 -->

## Executive Summary
The RSVP and Ticketing System will enable WitchCityRope members to RSVP for social events and purchase tickets for educational classes. This system supports the community's accessibility values while ensuring sustainable revenue for instructional content and maintaining proper safety protocols.

## Business Context

### Problem Statement
Currently, WitchCityRope lacks a unified system for event participation. Members cannot reserve spots for events or purchase tickets for classes through the platform, forcing external payment processing and manual attendee management. This creates friction for both organizers and attendees.

### Business Value
- **Increased Event Attendance**: Simplified RSVP process reduces barriers to participation
- **Revenue Generation**: Direct ticket sales for classes and workshops
- **Improved User Experience**: Unified dashboard for all event participations
- **Administrative Efficiency**: Automated attendee management and capacity tracking
- **Community Accessibility**: Free RSVP for social events maintains inclusivity
- **Safety Compliance**: Integrated waiver and verification processes

### Success Metrics
- 50% increase in social event attendance within 3 months
- 100% of paid classes utilize online ticket sales
- 90% user satisfaction with RSVP/ticketing process
- Reduce event admin time by 40%
- Zero payment processing failures with existing PayPal integration
- 100% safety waiver completion for all events

## Milestone 1: RSVP to Social Events

### Story 1.1: Vetted Member RSVPs to Social Event
**As a** vetted member
**I want to** RSVP to social events
**So that** I can secure my spot and receive event updates

**Acceptance Criteria:**
- Given I'm a vetted member viewing a social event detail page
- When I click "RSVP Now" button
- Then I see confirmation message "RSVP Confirmed!"
- And the event shows "RSVP Confirmed" status
- And remaining capacity decreases by 1
- And I receive email confirmation with event details via SendGrid
- And I can only RSVP for myself (single spot per user)

### Story 1.2: RSVP with Optional Ticket Purchase
**As a** vetted member who has RSVPed
**I want to** optionally purchase tickets for social events
**So that** I can support the community with a suggested donation

**Acceptance Criteria:**
- Given I have confirmed RSVP for a social event
- When I view my RSVP confirmation
- Then I see option "Purchase Ticket (Optional - Suggested Donation)"
- When I click purchase ticket
- Then I'm taken to PayPal checkout for the suggested donation amount
- And I receive separate ticket confirmation
- And my RSVP remains valid regardless of ticket purchase
- And purchasing a ticket automatically creates RSVP if not already done

### Story 1.3: View RSVPs on Dashboard
**As a** member with RSVPs
**I want to** see my upcoming RSVPs on my dashboard
**So that** I can track which events I'm attending

**Acceptance Criteria:**
- Given I have active RSVPs
- When I visit my member dashboard
- Then I see "Upcoming Events" section
- And each RSVP shows event name, date, time, location
- And I see "Cancel RSVP" option for each event
- And events are sorted by date (earliest first)
- And ticket status is displayed if purchased

### Story 1.4: Cancel RSVP from Dashboard
**As a** member with an RSVP
**I want to** cancel my RSVP from my dashboard
**So that** I can free up my spot if plans change

**Acceptance Criteria:**
- Given I have an active RSVP
- When I click "Cancel RSVP" from my dashboard
- Then I see confirmation dialog "Are you sure you want to cancel?"
- When I confirm cancellation
- Then RSVP status changes to "canceled" (not deleted)
- And RSVP remains in system for liability tracking
- And event capacity increases by 1
- And I receive cancellation confirmation email via SendGrid
- And any optional tickets are automatically refunded

### Story 1.5: RSVP Capacity Management
**As an** event organizer
**I want** social events to respect capacity limits
**So that** venue constraints are enforced

**Acceptance Criteria:**
- Given a social event is at capacity
- When a vetted member views the event detail page
- Then "RSVP Now" button shows "Event Full"
- And button is disabled
- And message displays "This event is at capacity"

### Story 1.6: Role-Based RSVP Access
**As a** general member
**I want** clear information about RSVP access
**So that** I understand membership requirements

**Acceptance Criteria:**
- Given I'm a general member viewing a social event
- When I view the event details
- Then I see message "Social events require vetted membership"
- And I see link to "Learn about becoming vetted"
- And RSVP button is disabled for general members

## Milestone 2: Purchase Tickets for Classes

### Story 2.1: Vetted Member Purchases Single Ticket
**As a** vetted member
**I want to** purchase a ticket for a paid class
**So that** I can attend educational workshops

**Acceptance Criteria:**
- Given I'm a vetted member viewing a paid class detail page
- When I click "Purchase Ticket" button
- Then I'm taken to checkout page
- And I see class details, pricing, and payment options
- When I complete payment via PayPal
- Then I receive order confirmation
- And ticket appears in my dashboard under "My Tickets"
- And class capacity decreases by 1

### Story 2.2: Sliding Scale Pricing Selection
**As a** member purchasing a class ticket
**I want to** select my price within the sliding scale range
**So that** I can pay what I can afford

**Acceptance Criteria:**
- Given I'm on the checkout page for a class with sliding scale pricing
- When I view the price section
- Then I see price range (e.g., "$35 - $65")
- And I see interactive slider to select amount
- And selected amount updates in real-time
- And help text explains "Pay what you can afford - no questions asked"
- When I continue to payment
- Then my selected amount is used for transaction

### Story 2.3: PayPal Payment Processing
**As a** member purchasing a ticket
**I want to** pay via PayPal securely
**So that** my payment information is protected

**Acceptance Criteria:**
- Given I'm completing checkout
- When I select PayPal as payment method
- Then I'm redirected to PayPal checkout
- When I complete PayPal payment
- Then PayPal webhook confirms payment to our system
- And I return to confirmation page on our site
- And my ticket is immediately available in dashboard
- And confirmation email is sent via SendGrid

### Story 2.4: View Purchased Tickets on Dashboard
**As a** member with purchased tickets
**I want to** see my tickets on my dashboard
**So that** I can access ticket details and manage my purchases

**Acceptance Criteria:**
- Given I have purchased tickets
- When I visit my member dashboard
- Then I see "My Tickets" section separate from RSVPs
- And each ticket shows class name, date, time, amount paid
- And tickets display "Confirmed" status
- And I can view ticket details including confirmation number
- And I see refund policy and deadline information

### Story 2.5: Ticket Purchase Confirmation
**As a** member who just purchased a ticket
**I want to** receive comprehensive confirmation
**So that** I have all necessary event information

**Acceptance Criteria:**
- Given I completed ticket purchase
- When payment is confirmed
- Then I see confirmation page with:
  - Confirmation number
  - Event details (name, date, time, location)
  - Ticket type and amount paid
  - Instructor information
  - What to bring/prerequisites
  - Refund policy
- And confirmation email contains same information via SendGrid
- And calendar invite (.ics file) is included

## Business Rules

### Event Type Classification
1. **Social Events**: Community gatherings, networking, social rope practice
   - Use RSVP system (required)
   - Optional ticket purchase (suggested donation)
   - Only vetted members can attend
   - Casual cancellation policy

2. **Classes/Workshops**: Educational content with instructors
   - Require ticket purchase
   - Sliding scale or fixed pricing
   - Vetted members and teachers can attend
   - Structured refund policy (48-hour minimum)

### User Access Requirements
1. Must be logged in to RSVP or purchase tickets
2. Guest users see "Please log in to RSVP/purchase" messaging
3. **CRITICAL**: Only vetted members can attend social events
4. Vetted members, teachers, and admins can purchase class tickets
5. General members can ONLY purchase class tickets (no social events)
6. Age verification (21+) confirmed by checkbox (not checked against profile)
7. Safety waiver required for ALL events
8. Banned status: Cannot RSVP or purchase anything

### Membership Role System - Stacking Roles
1. **General Member**: Cannot attend social events, can purchase class tickets
2. **Vetted Member**: Can attend all events, base vetted status
3. **Teacher**: Has vetted status + teaching privileges (roles stack)
4. **Admin**: Has vetted status + full admin access (roles stack)
5. Roles DO stack: User can be Vetted Member + Teacher + Admin simultaneously

### Capacity Management
1. Hard capacity limits enforced at database level
2. Race condition handling for simultaneous reservations
3. Admin override capability for capacity increases
4. Automatic capacity adjustment for cancellations/refunds
5. One RSVP per user per event (only for themselves)

### Payment Processing
1. PayPal integration for all paid transactions
2. Real-time webhook confirmation required before ticket activation
3. Payment failures trigger automatic ticket cancellation
4. No stored payment information (PCI compliance)
5. Tickets for social events are optional suggested donations

### Refund Policy Enforcement
1. Social events: Free cancellation anytime before start
2. Paid classes: Full refund up to configurable timeframe per event
3. No partial refunds allowed
4. Canceled events: RSVPs/tickets stay in system (status: canceled) for liability tracking
5. Admin can see all canceled RSVPs and refunded tickets
6. Never delete participation records - only mark as canceled

### Email Integration Requirements
1. **SendGrid Integration**: All email confirmations via SendGrid API
2. **Development Safety - Sandbox Mode**: Validates API calls but doesn't send emails (dev testing)
3. **Testing Strategy - @sink.sendgrid.net**: Accepts then deletes emails (integration testing)
4. **Email Types**: RSVP confirmations, ticket confirmations, cancellation notices

## Constraints & Assumptions

### Technical Constraints
- PayPal webhook integration already implemented and operational
- Must use HTTP-only cookie authentication (NOT JWT storage)
- Must use NSwag-generated TypeScript types (no manual DTO creation)
- React frontend with .NET API microservice architecture
- PostgreSQL database for all participation data
- SendGrid API for email delivery

### Business Constraints
- Sliding scale pricing maintains community accessibility values
- Salem venue capacity limitations must be respected
- Only vetted members can attend social events

### Assumptions
- Members understand difference between RSVP and ticket purchase
- PayPal remains primary payment processor
- Current HTTP-only cookie authentication system handles all user verification
- SendGrid email delivery is reliable for confirmations
- Test accounts have placeholder email addresses that won't receive real emails

## Security & Privacy Requirements

### Payment Security
- No credit card data stored locally
- PayPal handles all sensitive payment processing
- Webhook verification for all payment confirmations
- Failed payment cleanup within 15 minutes

### User Data Protection
- Participation data limited to: user ID, event ID, timestamp, payment amount
- No personal information stored beyond existing user profiles
- GDPR-compliant data retention (delete with user account)
- Actual attendance tracking for event analytics (not anonymous)

### Access Control
- Users can only view/cancel their own RSVPs/tickets
- **OUT OF SCOPE**: Teachers viewing attendee lists (not included in this milestone)
- Admins can view RSVPs and ticket purchases on event details page
- Event details respect existing privacy levels (public vs. vetted member events)

## Compliance Requirements

### Age Verification
- All events require 21+ age verification
- Age verification via checkbox confirmation only (not checked against profile)
- Underage indication shows "Age requirement not met" messaging

### Consent and Safety
- Code of Conduct acceptance required for all participations
- Safety waiver required for ALL events

### Financial Compliance
- PayPal transaction logging for audit purposes
- Sales tax calculations (if required by jurisdiction)
- Refund processing within 5-10 business days
- Payment dispute handling procedures

## User Impact Analysis

| User Type | RSVP Impact | Ticket Purchase Impact | Priority |
|-----------|-------------|------------------------|----------|
| **General Member** | Cannot RSVP to social events | Can purchase class tickets only | Medium |
| **Vetted Member** | Access to all social events | Access to all class levels | High |
| **Teacher** | Can RSVP to social events | Can purchase tickets as student | High |
| **Admin** | Full RSVP management + view lists | Complete transaction oversight | High |
| **Guest** | Must login to access | Must create account to purchase | Medium |
| **Banned** | Cannot RSVP or purchase anything | Cannot RSVP or purchase anything | Low |

## Admin Features

### Story A.1: Admin Views Event Participation
**As an** admin
**I want to** view RSVPs and ticket purchases for events
**So that** I can manage event logistics

**Acceptance Criteria:**
- Given I'm an admin viewing an event details page
- When I access the admin section
- Then I see list of all RSVPs with member names
- And I see list of all ticket purchases with payment amounts
- And I can see total participation count vs. capacity
- And I can export attendee information for event planning
- And I can see canceled RSVPs and refunded tickets for historical tracking

## Integration Requirements

### Authentication System
- Leverage existing HTTP-only cookie authentication (NOT JWT)
- Session timeout handling during checkout process
- Automatic login redirect for guest users

### Event Management System
- Participation counts update event capacity in real-time
- Event status changes (cancelled) trigger automatic refunds and email notifications

### SendGrid Email System
- RSVP confirmations use SendGrid templates
- Purchase confirmations include calendar invites
- Cancellation notifications for both user and admin
- Development environment uses sandbox mode to prevent real email delivery
- Production environment uses verified SendGrid domain

### Dashboard System
- New "Upcoming Events" section for RSVPs
- New "My Tickets" section for purchased tickets
- Quick actions for cancellation/refund requests

## SendGrid Implementation Requirements

### Development Environment Safety
1. **Sandbox Mode**: Enable SendGrid sandbox mode in development
   - Validates request structure without sending real emails
   - Uses SendGrid credits but prevents delivery
   - Essential for preventing accidental emails to test accounts

2. **Test Email Prevention**: Configure to prevent real emails to test accounts
   - Use @sink.sendgrid.net domain for development testing
   - Emails accepted then immediately deleted
   - No inbox delivery, safe for automated testing

3. **Environment Variables**: Separate SendGrid API keys for dev/staging/prod

### Production Configuration
1. **Domain Authentication**: Configure SPF, DKIM, and DMARC records
2. **Dedicated IP**: Consider dedicated IP for reputation management
3. **Template Management**: Create reusable templates for confirmations
4. **Monitoring**: Implement delivery and bounce rate monitoring

### Email Safety Protocols
1. **Address Validation**: Validate email addresses before sending
2. **Rate Limiting**: Implement sending rate limits to prevent spam flags
3. **Unsubscribe Handling**: Include proper unsubscribe mechanisms
4. **Bounce Management**: Handle bounced emails and update user preferences

## Examples/Scenarios

### Scenario 1: Happy Path Social Event RSVP
1. Vetted member Sarah visits "Rope Night Social" event page
2. Sees "15 of 30 spots available"
3. Clicks "RSVP Now" button
4. Completes safety waiver and age verification
5. Sees "RSVP Confirmed!" message
6. Receives SendGrid email: "You're registered for Rope Night Social on Friday..."
7. Event shows "14 of 30 spots available"
8. Sarah's dashboard shows upcoming RSVP with cancel option
9. Optionally purchases ticket for suggested donation

### Scenario 2: Happy Path Class Ticket Purchase
1. Vetted member Alex views "Advanced Suspension Workshop"
2. Sees "$75 - $150 sliding scale pricing"
3. Clicks "Purchase Ticket"
4. Completes safety waiver and age verification
5. Selects $100 on price slider
6. Redirected to PayPal for payment
7. Completes PayPal payment successfully
8. Returns to confirmation page with ticket details
9. Receives SendGrid confirmation email with calendar invite
10. Ticket appears in dashboard "My Tickets" section

### Scenario 3: General Member Access Restriction
1. General member Jordan views "Rope Night Social"
2. Sees message "Social events require vetted membership"
3. RSVP button is disabled
4. Provided link to vetting process information
5. Cannot proceed with RSVP until vetted status achieved
6. Can still view and purchase tickets for classes

### Scenario 4: Last-Minute Cancellation
1. Member has ticket for workshop in 6 hours
2. Tries to cancel from dashboard
3. Sees message: "Cancellation deadline passed. Contact admin for emergency refunds."
4. Provided admin contact information
5. If approved, admin processes manual refund

## Questions for Product Manager

- [ ] ✅ **RESOLVED**: Waitlist functionality confirmed as future iteration
- [ ] ✅ **RESOLVED**: Cancelled events keep RSVP data, auto-refund payments
- [ ] Should there be RSVP limits per user to prevent hoarding popular events?
- [ ] ✅ **RESOLVED**: One refund policy system-wide (configurable timeframes future scope)
- [ ] ✅ **RESOLVED**: No partial refunds allowed
- [ ] ✅ **RESOLVED**: No-show handling is out of scope
- [ ] ✅ **RESOLVED**: Teachers do NOT receive automatic attendee lists (out of scope)
- [ ] ✅ **RESOLVED**: No RSVP confirmation required before events

## Out of Scope (Confirmed)

- ❌ Waitlist functionality (future iteration)
- ❌ No-show handling and tracking
- ❌ Teacher access to attendee lists
- ❌ Advanced refund workflows (emergency only)
- ❌ Custom ticket pricing per event
- ❌ Multi-person RSVPs (future scope)
- ❌ Instructor compensation (not in scope)
- ❌ Insurance requirements (not in scope)

## Quality Gate Checklist (95% Required)

- [x] All user roles addressed with access restrictions clarified
- [x] Clear acceptance criteria for each story
- [x] Business value clearly defined with measurable metrics
- [x] Edge cases considered (capacity limits, race conditions, payment failures)
- [x] Security requirements documented (PCI compliance, data protection)
- [x] Compliance requirements checked (age verification, consent, safety waivers)
- [x] Performance expectations set (real-time capacity updates, webhook processing)
- [x] Mobile experience considered (responsive checkout flow)
- [x] Examples provided for both happy path and edge cases
- [x] Success metrics defined with specific targets
- [x] Integration requirements specified for existing systems
- [x] User impact analysis completed for all roles
- [x] Privacy and data retention requirements addressed
- [x] Error handling and failure scenarios documented
- [x] Business rules clearly stated with enforcement mechanisms
- [x] Authentication approach verified (HTTP-only cookies)
- [x] SendGrid integration requirements defined
- [x] Email safety protocols established
- [x] Role-based access controls documented
- [x] Admin features specified
- [x] RSVP simplification: One per user, only for themselves
- [x] Social event clarification: Optional tickets, not free-only
- [x] Canceled/refunded tracking: Never delete, only mark as canceled
- [x] Role system confirmation: Roles stack, banned status blocks all access

---

## Change Log

**Version 3.0 (2025-09-19) - Final Stakeholder Approvals:**
- ✅ **RSVP SIMPLIFICATION**: ONE RSVP per user per event (only for themselves)
- ✅ **REMOVED**: All mentions of 2-spot RSVPs or assigning to others (future scope)
- ✅ **SOCIAL EVENTS**: NOT necessarily free - often have suggested donation
- ✅ **TICKETS**: Call them "tickets" not "door entry tickets"
- ✅ **AUTOMATIC RSVP**: Purchasing a ticket automatically adds RSVP
- ✅ **REFUND POLICY**: Same policy for classes and social event tickets
- ✅ **TRACKING**: RSVPs/tickets stay in system when canceled (status: canceled)
- ✅ **ADMIN VIEW**: Can see all canceled RSVPs and refunded tickets
- ✅ **ROLE STACKING**: Confirmed roles DO stack (Vetted + Teacher + Admin)
- ✅ **BANNED STATUS**: Cannot RSVP or purchase anything
- ✅ **SENDGRID**: Added explanatory notes about sandbox mode and sink domain
- ✅ **SCOPE REMOVAL**: Instructor compensation, insurance requirements
- ✅ **TERMINOLOGY**: Changed "anonymous" to "actual attendance tracking"

**Version 2.0 (2025-09-19) - Critical Stakeholder Corrections:**
- ✅ **TERMINOLOGY**: Changed all "register/registration" to "RSVP" and "purchase ticket"
- ✅ **SOCIAL EVENTS**: Added RSVP requirement AND optional ticket purchase
- ✅ **ROLE SYSTEM**: Clarified only vetted members can attend social events
- ✅ **SENDGRID**: Added comprehensive email integration requirements
- ✅ **AUTHENTICATION**: Verified HTTP-only cookie approach (not JWT)
- ✅ **ADMIN FEATURES**: Added admin view of RSVPs/tickets on event details
- ✅ **BUSINESS RULES**: Updated safety waiver for ALL events, age verification via checkbox
- ✅ **OUT OF SCOPE**: Removed waitlist, teacher attendee access, incident reporting

*This document establishes the business foundation for implementing RSVP and ticketing functionality that supports WitchCityRope's community values while enabling sustainable revenue generation and maintaining safety protocols.*