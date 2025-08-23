# Events Management - Business Requirements
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Events Team -->
<!-- Status: Active -->

## Overview
The WitchCityRope events system manages educational workshops and social community gatherings with distinct rules for each type. The system handles event creation, RSVPs, ticketing, capacity management, and check-in processes.

## Event Types and Core Rules

### 1. Classes/Workshops (Educational Events)
**Characteristics:**
- **Payment**: REQUIRED - Must purchase ticket in advance
- **RSVP**: NOT AVAILABLE - No free registration option
- **Walk-ins**: NOT ALLOWED - Pre-purchased tickets only
- **Vetting**: Not required (open to all authenticated members)
- **UI Display**: Only "Purchase Ticket" button shown

**Examples:** Rope techniques workshop, safety class, skill-building session

### 2. Social Events (Community Gatherings)
**Characteristics:**
- **Payment**: OPTIONAL - Suggested donation/ticket price
- **RSVP**: AVAILABLE - Can reserve spot for free
- **Walk-ins**: ALLOWED - Can attend without pre-registration
- **Vetting**: REQUIRED - Only vetted members can participate
- **UI Display**: Both "RSVP" and "Purchase Ticket" buttons

**Examples:** Rope jams, play parties, social mixers, practice labs

**Special Rule**: Users who RSVP can still purchase a ticket later to financially support the event.

## Registration Types

### RSVP (Social Events Only)
- Free reservation for social events
- No payment required
- Confirms attendance intention
- Can be upgraded to paid ticket later
- Generates confirmation code: `RSVP-XXXXXXXX`

**RSVP Statuses:**
- `Confirmed` - Spot reserved
- `Waitlisted` - On waitlist for full event
- `CheckedIn` - Attended the event
- `Cancelled` - Cancelled by user

### Tickets (All Event Types)
- Paid registration for any event
- Payment required to confirm
- Supports multiple pricing tiers
- Includes additional attendee information
- Generates confirmation code: `TKT-YYYYMMDDHHMM-XXXX`

**Ticket Statuses:**
- `Pending` - Awaiting payment
- `Confirmed` - Payment complete, spot reserved
- `Waitlisted` - Paid but waiting for capacity
- `CheckedIn` - Attended the event
- `Cancelled` - Cancelled with potential refund

## Capacity Management

### Capacity Rules
- Events have maximum capacity setting
- Available spots = Capacity - (Confirmed RSVPs + Confirmed Tickets)
- Registration blocked when capacity reached
- Capacity can be increased but not decreased below current attendance

### Waitlist Functionality
- Automatic waitlist when event reaches capacity
- Separate waitlists for RSVPs and tickets
- Automatic promotion when spots become available
- Waitlist position visible to users
- Email notification when promoted from waitlist

### Overbooking
- Not supported - hard capacity limits enforced
- Staff cannot manually override capacity
- Only option is to increase event capacity

## Access Control

### Vetting Requirements
**Social Events:**
- All social events automatically require vetting
- Non-vetted members see "Apply for Vetting" message
- Pending vetting shows "Vetting Pending" status
- Cannot be disabled for social event types

**Educational Events:**
- Vetting optional (organizer choice)
- Most classes open to all authenticated users
- Advanced workshops may require vetting

### Age Verification
- All events require attendees to be 21+
- Age verified during account registration
- No per-event age verification needed

## Financial Rules

### Pricing Models

#### Fixed Price
- Single ticket price for all attendees
- Example: "$25 per person"

#### Sliding Scale
- Multiple price tiers available
- User selects based on ability to pay
- Example: "$15 (student), $25 (standard), $35 (supporter)"

#### Free Events
- No payment required
- RSVP only for social events
- Classes cannot be free (must have ticket price)

### Payment Processing
- Payments processed immediately at purchase
- No holds or authorizations
- Failed payments don't reserve spots
- Multiple payment methods supported

### Refund Policy
**Standard Policy:**
- Full refund if cancelled 48+ hours before event
- No refund if cancelled <48 hours before event
- Refunds processed to original payment method
- Refund window configurable per event

**Exceptions:**
- No refunds after check-in
- Event cancellation = full refund regardless of timing
- Medical emergencies handled case-by-case

## Check-In Process

### Check-In Requirements
- Valid confirmation code (RSVP or ticket)
- Can only check in confirmed attendees
- Check-in opens when event starts
- Late check-in allowed

### Check-In Workflow
1. Staff scans QR code or enters confirmation code
2. System validates registration status
3. Attendee confirms emergency contact info
4. Waiver acceptance recorded (if required)
5. Check-in timestamp recorded with staff ID

### Check-In Restrictions
- Cannot check in cancelled registrations
- Cannot check in before event start time
- Each code can only be used once
- No transferring codes between users

## Event Management

### Event Creation
**Required Information:**
- Title and description
- Date, time, duration
- Location (physical or online)
- Event type (class or social)
- Capacity
- At least one pricing tier
- Lead organizer

**Optional Information:**
- Co-organizers
- Prerequisites
- What to bring
- Parking information
- Accessibility notes

### Event Modifications
**Can Change Anytime:**
- Description and details
- Location (with notification)
- Capacity (increase only)
- Add organizers

**Cannot Change After Registrations:**
- Date/time (requires cancellation)
- Pricing (locked after first sale)
- Event type (class vs social)

**Cannot Change After Event Start:**
- Any core details
- Can only add notes/updates

### Event Cancellation
- Can cancel until event starts
- All attendees notified via email
- Automatic full refunds processed
- Event marked as cancelled (not deleted)
- Cancellation reason required

## Communication

### Automated Emails
1. **Registration Confirmation** - Sent immediately
2. **Event Reminder** - 24 hours before event
3. **Waitlist Promotion** - When spot available
4. **Cancellation Notice** - If event cancelled
5. **Refund Confirmation** - When refund processed

### Custom Communications
- Organizers can email all attendees
- Filter by registration type (RSVP vs ticket)
- Filter by status (confirmed vs waitlist)
- Communications logged in system

## Reporting Requirements

### Attendance Reports
- Total registrations (RSVP + tickets)
- Actual attendance (checked in)
- No-show rate
- Revenue collected
- Refunds processed

### Financial Reports
- Gross revenue by event
- Refunds and adjustments
- Net revenue
- Pricing tier breakdown
- Payment method analysis

### Demographic Reports
- New vs returning attendees
- Member vs non-member split
- Geographic distribution
- Dietary restrictions summary

## Special Scenarios

### Combined RSVP + Ticket
- User RSVPs for free spot
- Later decides to support financially
- Purchases ticket while keeping RSVP
- Only counted once in capacity
- Check-in uses either code

### Transferring Registrations
- Not supported directly
- User must cancel and new user register
- No automatic transfer of waitlist position
- Refund subject to standard policy

### Group Registrations
- Not currently supported
- Each person registers individually
- No group discounts available
- No reserved blocks of tickets

---

*This document describes business-level requirements. For technical implementation details, see [functional-design.md](functional-design.md)*