# Ticket & RSVP Process Guide
<!-- Last Updated: 2025-12-07 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

WitchCityRope uses a unified participation system for events. This guide explains how RSVPs and ticket purchases work, including the business logic, user experience flows, and high-level implementation architecture.

**Purpose**: This guide documents the CURRENT ticket and RSVP process to help developers, admins, and stakeholders understand how event participation works in the platform.

**Scope**: RSVP system for social events + Ticket purchasing for classes/workshops + Payment integration + Capacity management

## Event Types

### Social Events
Social events are community gatherings, networking sessions, and social rope practice.

**Participation Model**:
- **RSVP**: Required (free) - Marks attendance intent and reserves spot
- **Ticket**: Optional - Suggested donation to support the community
- **Unique Rule**: Users CAN have both RSVP AND ticket for the same event
- **Vetting**: Only vetted members can attend social events

**Example**: "Rope Night Social" - Free RSVP required, optional $10 suggested donation ticket

### Class/Workshop Events
Educational content with instructors teaching specific skills.

**Participation Model**:
- **Ticket**: Required (paid) - Attendance requires ticket purchase
- **No Free RSVP Option**: Payment is mandatory for attendance
- **Ticket Types**: Various options available
  - Single session tickets
  - Multi-session tickets
  - Full event passes
- **Pricing**: Fixed price OR sliding scale ($35-$65, pay what you can afford)

**Example**: "Advanced Suspension Workshop" - $75-$150 sliding scale ticket required

## Business Rules

### Current Limitations (Per-Event)

**One Participation Per User**:
1. ✅ ONE active ticket per user per event
2. ✅ ONE active RSVP per user per event
3. ✅ User CAN have BOTH ticket AND RSVP (for social events with optional tickets)
4. ✅ Cancelled tickets/RSVPs don't prevent new purchases (status: cancelled, not deleted)
5. ❌ CANNOT RSVP/purchase for other people (only yourself)
6. ❌ NO multi-spot reservations (future scope)

**Enforcement**: Database unique constraint on `(UserId, EventId)` in EventParticipations table

### Capacity Management

**Event-Level Capacity** (Current Implementation):
- Hard capacity limit set when event is created (e.g., 30 people)
- Capacity checked BEFORE creating RSVP/ticket purchase
- Race condition handling prevents overbooking
- Automatic capacity adjustment when users cancel
- Admin override capability for capacity increases

**Capacity States**:
- **Available**: `current_participants < capacity` → "RSVP Now" / "Purchase Ticket" enabled
- **At Capacity**: `current_participants = capacity` → "Event Full" button disabled
- **Future Enhancement**: Waitlist option when at capacity (not yet implemented)

### Timing Windows

**Registration Windows** (Configurable Per Event):
- **Registration Opens**: X hours before event start (e.g., 2 weeks before)
- **Registration Closes**: Y hours before event start (e.g., 2 hours before)
- **Outside Window**: RSVP/Purchase buttons disabled with messaging

**Cancellation Windows**:
- **Social Events**: Free cancellation anytime before event starts
- **Paid Classes**: Full refund up to configurable timeframe (typically 48 hours before)
- **After Deadline**: Manual admin approval required for refunds

### User Access Requirements

**Role-Based Access Matrix**:

| Role | Social Event RSVP | Social Event Ticket | Class Tickets | Notes |
|------|------------------|---------------------|---------------|-------|
| **Guest** (not logged in) | ❌ | ❌ | ❌ | Must create account |
| **General Member** | ❌ | ❌ | ✅ | Cannot attend social events |
| **Vetted Member** | ✅ | ✅ | ✅ | Full access |
| **Teacher** | ✅ | ✅ | ✅ | Roles stack (Vetted + Teacher) |
| **Admin** | ✅ | ✅ | ✅ | Roles stack (Vetted + Admin) |
| **Banned** | ❌ | ❌ | ❌ | Zero access |

**Additional Requirements**:
- ✅ Age verification (21+) via checkbox confirmation
- ✅ Safety waiver acceptance required for ALL events
- ✅ Active user account in good standing

## User Experience Flows

### RSVP Flow (Social Events)

**Preconditions**:
- User is logged in
- User is vetted member
- Event has available capacity
- Registration window is open
- User hasn't already RSVP'd

**Steps**:
1. **User views event details page**
   - Sees event information (date, time, location)
   - Sees capacity indicator (e.g., "15 of 30 spots available")
   - Sees "RSVP Now" button (enabled if all conditions met)

2. **User clicks "RSVP Now"**
   - Modal/page appears with safety waiver
   - Age verification checkbox (21+)
   - Code of Conduct acceptance required

3. **User accepts waiver and clicks RSVP**
   - System checks capacity (race condition protection)
   - Creates EventParticipation record (Type: RSVP, Status: Active)
   - Decreases available capacity by 1
   - Logs participation in audit trail

4. **Confirmation shown**
   - "RSVP Confirmed!" success message
   - Event page updates to show "RSVP Confirmed" status
   - Optional: "Purchase Ticket" button for suggested donation

5. **Email confirmation sent**
   - Via SendGrid API
   - Contains event details, location, date/time
   - Includes cancellation instructions
   - Calendar invite (.ics file) attached

6. **Dashboard updated**
   - RSVP appears in "Upcoming Events" section
   - Shows event name, date, time, location
   - "Cancel RSVP" button available

### Optional Ticket Purchase After RSVP

**Scenario**: User has RSVP'd, wants to support with donation

**Steps**:
1. User views RSVP confirmation or dashboard
2. Sees "Purchase Ticket (Optional - Suggested Donation $10)"
3. Clicks purchase button
4. Redirected to PayPal checkout
5. Completes payment
6. TicketPurchase record created (linked to existing EventParticipation)
7. Receives separate ticket confirmation email
8. RSVP remains valid regardless of ticket purchase
9. Dashboard shows both RSVP status AND ticket purchase

**Note**: Purchasing a ticket automatically creates RSVP if not already done

### Ticket Purchase Flow (Classes/Workshops)

**Preconditions**:
- User is logged in
- User meets vetting requirements (if applicable)
- Event has available capacity
- Registration window is open
- User hasn't already purchased ticket

**Steps**:
1. **User views event details page**
   - Sees class information (instructor, prerequisites, what to bring)
   - Sees pricing (fixed or sliding scale range)
   - Sees capacity indicator
   - Sees "Purchase Ticket" button (enabled if all conditions met)

2. **User clicks "Purchase Ticket"**
   - Checkout page/modal appears
   - Event details displayed
   - Safety waiver and age verification required

3. **Sliding Scale Selection** (if applicable)
   - Interactive slider shows price range (e.g., $35-$65)
   - User selects amount they can afford
   - Help text: "Pay what you can afford - no questions asked"
   - Selected amount updates in real-time

4. **User proceeds to payment**
   - Clicks "Pay with PayPal"
   - System creates pending EventParticipation (Type: Ticket, Status: Active)
   - System creates pending TicketPurchase (Status: Pending)
   - User redirected to PayPal checkout

5. **PayPal payment processing**
   - User completes payment on PayPal
   - PayPal webhook confirms payment to our system
   - TicketPurchase status updated to "Completed"
   - Capacity decreased by 1

6. **User returns to confirmation page**
   - Confirmation number displayed
   - Event details, ticket type, amount paid shown
   - Instructor information, prerequisites listed
   - Refund policy and deadline displayed

7. **Confirmation email sent**
   - Via SendGrid API
   - Contains all confirmation details
   - Includes calendar invite (.ics file)
   - Refund policy and cancellation instructions

8. **Dashboard updated**
   - Ticket appears in "My Tickets" section
   - Shows class name, date, time, amount paid
   - "Confirmed" status displayed
   - "Request Refund" button (if within cancellation window)

### Cancellation Flow

**Preconditions**:
- User has active RSVP or ticket
- User is viewing their dashboard
- Cancellation is within allowed window (or manual admin approval)

**Steps**:
1. **User views participation on dashboard**
   - Sees "Cancel RSVP" or "Request Refund" button

2. **User clicks cancel/refund button**
   - Confirmation dialog appears
   - "Are you sure you want to cancel?"
   - Warning if past deadline (requires admin approval)

3. **User confirms cancellation**
   - EventParticipation status changed to "Cancelled" (NOT deleted)
   - CancelledAt timestamp recorded
   - Participation remains in system for liability tracking
   - Capacity increased by 1

4. **Ticket refund processed** (if applicable)
   - For tickets: Refund initiated via PayPal
   - TicketPurchase status updated to "Refunded"
   - RefundAmountValue and RefundDate recorded
   - Full refund processed (no partial refunds)
   - PaymentTransaction record created (Type: Refund)

5. **Confirmation shown**
   - "Cancellation confirmed" success message
   - "Refund will be processed within 5-10 business days" (if ticket)
   - Event removed from active dashboard view

6. **Cancellation email sent**
   - Via SendGrid API
   - Confirms cancellation
   - Provides refund details (if applicable)
   - Spot now available for other users

**Admin View**:
- Admins can see ALL cancelled RSVPs and refunded tickets
- Historical tracking for liability and analytics
- Manual refund approval for past-deadline requests

## Ticket Types

### Configuration Options

**TicketType Entity Fields**:
- **Name**: Display name (e.g., "Full Workshop", "Day 1 Only")
- **Description**: What's included (e.g., "Access to all 3 days")
- **Pricing Type**: Fixed OR Sliding Scale
- **Price/Range**:
  - Fixed: Single amount (e.g., $50)
  - Sliding Scale: Min/Max range (e.g., $35-$65)
- **Quantity Available**: How many can be sold (e.g., 30 tickets)
- **Sessions Included**: Which event sessions this ticket covers
- **Sort Order**: Display order on purchase page
- **Is Active**: Enable/disable ticket type

### Multi-Session Events

**Scenario**: 3-day rope workshop with different attendance options

**Event Structure**:
- Event: "Advanced Suspension Workshop"
- Session 1: Friday 7pm-10pm (Fundamentals)
- Session 2: Saturday 10am-5pm (Techniques)
- Session 3: Sunday 10am-3pm (Practice)

**Ticket Type Examples**:
1. **"Friday Only"**
   - Price: $35-$50 sliding scale
   - Sessions: Session 1 only
   - Quantity: 20 tickets

2. **"Weekend Pass (Sat-Sun)"**
   - Price: $75-$100 sliding scale
   - Sessions: Sessions 2 & 3
   - Quantity: 30 tickets

3. **"Full Workshop"**
   - Price: $100-$150 sliding scale
   - Sessions: All 3 sessions
   - Quantity: 25 tickets

**Implementation Note**: Multi-session ticketing is designed but implementation is in progress

## Implementation Architecture (High-Level)

### Key Entities

**Event**:
- Main event record (title, description, dates, location)
- Capacity limit
- Event type (Social or Class)
- Published status
- Timing windows (registration open/close)

**EventParticipation**:
- User's participation record (RSVP or Ticket)
- Links User to Event
- Participation Type: RSVP (1) or Ticket (2)
- Status: Active (1), Cancelled (2), Refunded (3), Waitlisted (4)
- Timestamps: CreatedAt, CancelledAt
- Metadata: JSONB for flexible data

**TicketType**:
- Purchasable ticket options for an event
- Name, description, pricing
- Session associations (which sessions ticket covers)
- Quantity available
- Active/inactive status

**TicketPurchase**:
- Financial transaction record
- Links to EventParticipation (one-to-one)
- Links to TicketType
- Amount paid, currency
- PayPal order ID
- Payment status: Pending, Completed, Failed, Refunded, PartiallyRefunded
- Refund tracking: amount, date, reason

**Session** (Future Enhancement):
- Individual time slots within multi-day events
- Start/end times
- Session-specific capacity limits

### Entity Relationships

```
Event (1) → (N) Sessions
Event (1) → (N) TicketTypes
Event (1) → (N) EventParticipations

ApplicationUser (1) → (N) EventParticipations

EventParticipation (1) ← (0..1) TicketPurchase

TicketType (1) → (N) TicketPurchases
TicketType (N) ↔ (N) Sessions (which sessions ticket covers)

TicketPurchase (1) → (N) PaymentTransactions
```

**Key Constraints**:
- One EventParticipation per (User, Event) combination
- One TicketPurchase per EventParticipation
- Cannot delete participations (only cancel/mark as inactive)

### Validation Flow

**RSVP Creation**:
1. ✅ Check user authentication (logged in)
2. ✅ Check event exists and is published
3. ✅ Check registration window is open
4. ✅ Check user doesn't already have RSVP (one per event)
5. ✅ Check capacity available
6. ✅ Check vetting requirements met (social events)
7. ✅ Check age verification (21+)
8. ✅ Check safety waiver accepted
9. ✅ Create EventParticipation record (Type: RSVP, Status: Active)
10. ✅ Send confirmation email

**Ticket Purchase Creation**:
1. ✅ Check user authentication
2. ✅ Check event exists and is published
3. ✅ Check registration window is open
4. ✅ Check user doesn't already have ticket
5. ✅ Check capacity available
6. ✅ Check vetting requirements met (if applicable)
7. ✅ Check age verification (21+)
8. ✅ Check safety waiver accepted
9. ✅ Validate payment amount (within sliding scale range if applicable)
10. ✅ Create EventParticipation record (Type: Ticket, Status: Active)
11. ✅ Create TicketPurchase record (Status: Pending)
12. ✅ Redirect to PayPal checkout
13. ✅ Wait for PayPal webhook confirmation
14. ✅ Update TicketPurchase status to Completed
15. ✅ Send confirmation email

**Cancellation Validation**:
1. ✅ Check user owns the participation
2. ✅ Check participation is active
3. ✅ Check cancellation deadline (or admin override)
4. ✅ Update EventParticipation status to Cancelled
5. ✅ Record cancellation timestamp and reason
6. ✅ If ticket: Initiate PayPal refund
7. ✅ If ticket: Update TicketPurchase status to Refunded
8. ✅ Increase event capacity by 1
9. ✅ Send cancellation confirmation email

## Admin Features

### Event Management
**Admin Event Dashboard**:
- Create/edit events with sessions
- Configure ticket types (name, price, quantity, sessions)
- Set capacity and timing windows
- Enable/disable event publishing
- View participation statistics

**Capacity Management**:
- Real-time capacity tracking
- Override capacity limits (emergency expansion)
- View participation trends

### Participation Management
**Admin Event Details Page**:
- View all RSVPs (active and cancelled)
- View all ticket purchases (completed and refunded)
- See total participation count vs capacity
- Export attendee information (CSV/Excel)
- Historical tracking of cancellations

**Participant List Display**:
- Member scene name
- Participation type (RSVP or Ticket)
- Status (Active, Cancelled, Refunded)
- Registration date/time
- Amount paid (if ticket)
- Payment status
- Check-in status (linked to check-in system)

### Ticket Management
**Admin Payment Operations**:
- View all ticket purchases for event
- Process manual refunds (past-deadline requests)
- Generate financial reports
- Track revenue per event
- Monitor payment failures

**Cash Ticket Creation**:
- Create tickets at check-in for walk-ins
- Manual payment recording (cash/check)
- Immediate ticket activation
- Bypass PayPal for in-person transactions

### Check-In Integration
**Event Check-In Screen**:
- Scan attendees at event entrance
- Verify ticket validity for specific session
- Process walk-in tickets with cash payment
- Real-time attendance tracking
- Link EventParticipation to EventAttendee (check-in system)

## Payment Processing (PayPal Integration)

### PayPal Webhook Flow

**Setup**:
- PayPal webhook URL: `https://api.witchcityrope.com/paypal/webhook`
- Cloudflare tunnel provides permanent webhook endpoint
- Webhook events: `PAYMENT.CAPTURE.COMPLETED`, `PAYMENT.CAPTURE.REFUNDED`

**Payment Capture Flow**:
1. User completes PayPal payment
2. PayPal sends webhook to our endpoint
3. System validates webhook signature (security)
4. Extracts PayPal order ID from webhook payload
5. Finds pending TicketPurchase by PayPal order ID
6. Updates TicketPurchase status to "Completed"
7. Records payment date, amount, metadata
8. Creates PaymentTransaction audit record
9. Triggers confirmation email to user

**Refund Processing Flow**:
1. User requests refund (or admin processes manual refund)
2. System validates refund eligibility (deadline, policy)
3. Initiates PayPal refund API call
4. PayPal processes refund
5. PayPal sends refund webhook
6. System updates TicketPurchase status to "Refunded"
7. Records refund amount, date, reason
8. Creates PaymentTransaction record (Type: Refund)
9. Triggers refund confirmation email

**Payment Failure Handling**:
- Failed payments: TicketPurchase status → "Failed"
- Automatic cleanup: Failed purchases deleted after 15 minutes
- Capacity restored immediately
- User notified of payment failure

**Security**:
- Webhook signature verification (required)
- No credit card data stored locally
- PayPal handles all sensitive payment processing
- Only store: PayPal order ID, amount, status

### Testing Infrastructure

**Development Environment**:
- PayPal Sandbox account used
- Real sandbox webhooks via Cloudflare tunnel
- Mock PayPal service for CI/CD (no real API calls)

**Production Environment**:
- PayPal Live account
- Production webhook URL
- Real payment processing

## Email System (SendGrid Integration)

### Email Types

**RSVP Confirmation**:
- Sent when: User successfully RSVPs to social event
- Contains: Event details, location, date/time, cancellation instructions
- Includes: Calendar invite (.ics file)

**Ticket Confirmation**:
- Sent when: PayPal payment completes
- Contains: Confirmation number, event details, ticket type, amount paid, instructor info, prerequisites, refund policy
- Includes: Calendar invite (.ics file)

**Cancellation Confirmation**:
- Sent when: User cancels RSVP or requests refund
- Contains: Cancellation confirmation, refund details (if applicable), timeline for refund processing

**Event Updates**:
- Sent when: Event cancelled, rescheduled, or major changes
- Contains: Change notification, new event details, automatic refund information

### SendGrid Configuration

**Development Environment Safety**:
- **Sandbox Mode**: Validates API calls but doesn't send emails
- **Testing Domain**: `@sink.sendgrid.net` (accepts then deletes emails)
- **No Real Emails**: Prevents accidental delivery to test accounts

**Production Environment**:
- **Domain Authentication**: SPF, DKIM, DMARC records configured
- **Template Management**: Reusable templates for all email types
- **Delivery Monitoring**: Track bounce rates, delivery rates
- **Unsubscribe Handling**: Proper unsubscribe mechanisms

## Related Documentation

### Complete Implementation Details
- **Business Requirements**: `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/requirements/business-requirements.md`
  - Full user stories, acceptance criteria, business rules
  - Success metrics, compliance requirements
  - User impact analysis for all roles

- **Database Design**: `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/database-design.md`
  - Complete schema definition (EventParticipations, TicketPurchases, TicketTypes, PaymentTransactions)
  - Index strategy, performance benchmarks
  - Sample queries, audit trail implementation
  - Security considerations, monitoring queries

- **PayPal Integration**: `/docs/functional-areas/payments/handoffs/paypal-webhook-integration-complete-2025-09-14.md`
  - Webhook setup, Cloudflare tunnel configuration
  - Payment capture and refund workflows
  - Testing strategy, mock services for CI/CD

### Implementation Work
- **Feature Implementation**: `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/`
  - Full 5-phase workflow (Requirements → Design → Implementation → Testing → Finalization)
  - Handoff documents between phases
  - Test plans and QA validation

### Related Features
- **Check-In System**: Attendance tracking and ticket validation at events
- **Event Management**: Event creation, capacity management, admin operations
- **User Dashboard**: Member view of upcoming events and tickets
- **Vetting System**: Vetting status verification for social events

## Future Enhancements

### Planned Features (Not Yet Implemented)

**Waitlist Management**:
- Automatic waitlist when event at capacity
- Automatic promotion when spots open up
- Email notifications for waitlist movement

**Per-Session Ticket Limitations**:
- Capacity tracking at session level (not just event level)
- Session-specific ticket validation
- Multi-session event support (currently in design)

**Group Ticket Purchases**:
- Purchase multiple tickets in single transaction
- Family/partner registration together
- Group discount pricing

**Ticket Transfers**:
- Transfer ticket to another member
- Re-assignment of tickets
- Transfer fee option

**Advanced Refund Workflows**:
- Partial refunds (currently full refund only)
- Tiered refund policies (90% if >1 week, 50% if >48 hours)
- Store credit option instead of refund

**Dynamic Pricing**:
- Early bird pricing (discount for early registration)
- Last-minute pricing (discount close to event)
- Member tier pricing (different prices by membership level)

**Scholarship Program**:
- Free/discounted ticket allocation
- Need-based ticket distribution
- Scholarship application workflow

**Enhanced Reporting**:
- Revenue analytics per event type
- Attendance trends over time
- Popular event identification
- Cancellation rate analysis

## Examples/Scenarios

### Scenario 1: Social Event RSVP (Happy Path)
**Event**: "Rope Night Social" (Free RSVP, optional $10 donation ticket)

1. Sarah (vetted member) visits event page
2. Sees "15 of 30 spots available"
3. Clicks "RSVP Now"
4. Accepts safety waiver and age verification
5. System validates:
   - Sarah is logged in ✅
   - Sarah is vetted member ✅
   - Event has capacity ✅
   - Registration window open ✅
   - Sarah hasn't RSVP'd yet ✅
6. EventParticipation created (Type: RSVP, Status: Active)
7. Capacity updated: "14 of 30 spots available"
8. Confirmation: "RSVP Confirmed!"
9. Email sent via SendGrid with event details
10. Dashboard shows upcoming RSVP with "Cancel RSVP" button
11. Sarah optionally clicks "Purchase Ticket ($10 suggested donation)"
12. PayPal payment completed
13. TicketPurchase created (linked to existing EventParticipation)
14. Dashboard shows RSVP + Ticket purchase

### Scenario 2: Class Ticket Purchase with Sliding Scale
**Event**: "Advanced Suspension Workshop" ($75-$150 sliding scale)

1. Alex (vetted member) visits workshop page
2. Sees "$75-$150 sliding scale pricing"
3. Clicks "Purchase Ticket"
4. Accepts safety waiver and age verification
5. Sees price slider, selects $100
6. Help text: "Pay what you can afford - no questions asked"
7. Clicks "Pay with PayPal"
8. System validates all requirements (logged in, capacity, etc.)
9. EventParticipation created (Type: Ticket, Status: Active)
10. TicketPurchase created (Status: Pending, Amount: $100)
11. Redirected to PayPal
12. Alex completes PayPal payment
13. PayPal webhook received
14. TicketPurchase updated (Status: Completed)
15. Alex returns to confirmation page
16. Confirmation email sent with ticket details + calendar invite
17. Dashboard shows ticket in "My Tickets" section

### Scenario 3: General Member Access Restriction
**Event**: "Rope Night Social" (vetted members only)

1. Jordan (general member, not vetted) visits event page
2. Sees message: "Social events require vetted membership"
3. RSVP button is disabled (greyed out)
4. Sees link: "Learn about becoming vetted"
5. Cannot proceed with RSVP
6. Jordan clicks link to vetting process
7. Jordan CAN still view and purchase tickets for classes (not social events)

### Scenario 4: Cancellation Within Deadline
**Event**: "Suspension Workshop" in 5 days (48-hour cancellation policy)

1. Sarah has ticket purchased 2 weeks ago
2. Sarah visits dashboard
3. Sees "Request Refund" button (within 48-hour deadline)
4. Clicks "Request Refund"
5. Confirmation dialog: "Are you sure? This will cancel your ticket."
6. Sarah confirms
7. System validates: within cancellation window ✅
8. EventParticipation status → Cancelled
9. CancelledAt timestamp recorded
10. PayPal refund initiated
11. TicketPurchase status → Refunded
12. Capacity increased by 1
13. Confirmation: "Refund will be processed within 5-10 business days"
14. Email sent with refund confirmation
15. Ticket removed from dashboard active view

### Scenario 5: Cancellation Past Deadline (Requires Admin)
**Event**: "Workshop" in 6 hours (past 48-hour deadline)

1. Jordan has ticket, emergency situation
2. Jordan visits dashboard
3. Clicks "Request Refund"
4. Sees warning: "Cancellation deadline passed. Contact admin for emergency refunds."
5. Provided admin contact information
6. Jordan emails admin explaining emergency
7. Admin reviews request
8. Admin manually processes refund
9. Admin marks EventParticipation as Cancelled
10. Admin initiates PayPal refund
11. Jordan receives refund confirmation email

---

**Document Purpose**: This guide provides comprehensive understanding of the RSVP and ticketing system for all stakeholders. For implementation details, consult the related documentation listed above.

**Current Status**: System is 90% functional with minor API issues being addressed. Core RSVP and ticketing flows are production-ready.

**Last Updated**: December 7, 2025
