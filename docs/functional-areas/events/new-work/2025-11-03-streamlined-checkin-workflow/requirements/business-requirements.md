# Business Requirements: Streamlined Check-In Workflow
<!-- Last Updated: 2025-11-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft - Revised with Simplified Approach -->

## Executive Summary
The streamlined check-in workflow eliminates unnecessary modal popups for workshop check-ins, reducing the check-in process from 4 clicks to 2 clicks while maintaining proper payment tracking for social events. This enhancement improves staff efficiency during high-traffic arrival times while preserving all required payment and waiver validations.

## 🚨 CRITICAL: Reuse Existing Flows

**This feature does NOT create new payment flows.**

**What's New:**
- Cash payment option (select ticket type, enter amount, record with staff attribution)
- QR code modal (displays URL to existing ticket sales page)
- Streamlined check-in (remove modals for pre-paid attendees)

**What's Reused:**
- Existing ticket sales page (QR code links here)
- Existing payment processing (PayPal)
- Existing ticket purchase flow
- Existing email receipts
- Existing refund process

**Do NOT:**
- ❌ Create special payment processor integrations
- ❌ Build real-time payment detection (SSE, webhooks, polling)
- ❌ Create new payment flows
- ❌ Build custom payment pages

## Business Context

### Problem Statement
The current check-in system requires staff to click a check-in button, wait for a modal popup, and confirm check-in in the modal. For workshops where attendees have already paid for tickets and signed waivers, this extra confirmation step adds unnecessary friction during busy arrival times. Staff process 20-50 attendees per event, meaning the modal adds 20-50 extra clicks per event.

### Business Value
- **Efficiency**: Reduces check-in time by 40% (from 5 seconds to 3 seconds per attendee)
- **Staff Experience**: Eliminates repetitive modal confirmations for obvious cases
- **Flexibility**: Maintains complex workflow for social events requiring payment
- **Accuracy**: Reduces click fatigue errors during high-volume check-ins
- **Throughput**: Increases check-in capacity from 12 to 20 attendees per minute

### Success Metrics
- Check-in time reduced from 5 seconds to 3 seconds per attendee
- Staff satisfaction rating improved by 30% for check-in process
- Zero payment tracking errors after implementation
- 100% waiver compliance maintained
- Staff training time reduced from 15 minutes to 5 minutes

## User Stories

### Story 1: Workshop Check-In (Simplified - No Modal)
**As a** check-in staff member
**I want to** check in workshop attendees with 2 clicks
**So that** I can process arrivals quickly without unnecessary confirmations

**Acceptance Criteria:**
- Given an attendee has purchased a ticket for a workshop
- When I view their row in the check-in interface
- Then I see "Covid Test Complete" button on the right side
- And when I click "Covid Test Complete"
- Then the button changes to "Check In" button
- And covid test status is tracked only in UI (not database)
- And when I click "Check In"
- Then the attendee is checked in immediately
- And NO modal popup appears
- And check-in is recorded in database
- And row updates to show "✓ Checked In" status

**Rationale**: Workshop attendees have already completed payment and waiver during ticket purchase. No additional data collection is needed.

### Story 2: Social Event - Pre-Paid Attendee (Simplified - No Modal)
**As a** check-in staff member
**I want to** check in social event attendees who already paid with 2 clicks
**So that** pre-paid attendees have the same fast experience as workshops

**Acceptance Criteria:**
- Given an attendee purchased a ticket for a social event
- When I view their row in the check-in interface
- Then the workflow is identical to workshops
- And I click "Covid Test Complete" then "Check In"
- And NO modal appears
- And check-in is recorded with payment confirmed status

**Rationale**: Pre-paid social event attendees have same payment/waiver status as workshop attendees.

### Story 3: Social Event - RSVP Only (Optional Payment)
**As a** check-in staff member
**I want to** offer optional door payment for RSVP attendees
**So that** attendees can choose to purchase a ticket if they want, but are not required to pay

**CRITICAL**: Payment is OPTIONAL for social events. Attendees can check in with just an RSVP.

**Acceptance Criteria:**
- Given an attendee RSVP'd but hasn't purchased a ticket
- When I view their row in the check-in interface
- Then I see "Paid at Door" button (OPTIONAL - they can skip this)
- And I can choose to:

**Option 1: Skip Payment (Check in with RSVP only)**
- I skip the "Paid at Door" button
- I proceed directly to "Covid Test Complete" button
- Normal check-in workflow continues
- No payment recorded

**Option 2: Process Optional Door Payment**
- When I click "Paid at Door"
- Then I see two payment options:

**Option A: Cash Payment**
- When I select "Cash Payment"
- Then a modal opens with:
  - Ticket type selector dropdown
  - Cash amount input field (allows $0.00)
  - Optional notes field
- And I select ticket type
- And I enter the amount (e.g., $20.00 or $0.00)
- And I optionally add notes
- And I click "Record Payment"
- Then payment is recorded as cash with:
  - RecordedByStaffId (staff member who recorded)
  - Amount entered (including $0.00 if free)
  - Notes (if provided)
  - Timestamp
  - Ticket type
- And modal closes
- And button changes to "Covid Test Complete"

**Option B: Digital Payment (QR Code)**
- When I select "Digital Payment"
- Then a QR code modal is displayed on screen
- And QR code contains URL: `https://witchcityrope.com/events/{eventId}/tickets`
- And attendee scans QR code with their phone
- And they are taken to the EXISTING ticket sales page
- And they log in (or sign up if new user)
- And they complete the NORMAL ticket purchase process
- And they receive email receipt per normal process
- And staff closes the QR code modal (non-blocking)
- And staff continues checking in other people
- And later, staff searches for attendee again
- And system shows "Check In" button (ticket purchase completed)
- And staff completes check-in

- And after payment is complete
- Then I click "Covid Test Complete"
- And button changes to "Check In"
- And I click "Check In"
- And attendee is checked in

**Rationale**: Social events support both RSVP (free) and ticket purchase (paid). Door payment must be tracked to maintain accurate financial records. QR code workflow is asynchronous - staff don't wait for payment completion.

### Story 4: Button State Management
**As a** check-in staff member
**I want to** see the correct button at each step of the workflow
**So that** I know what action to take next

**Acceptance Criteria:**
- Given an attendee in any workflow state
- When I view their row
- Then the button reflects their current step:
  - "Paid at Door" (RSVP only, social events)
  - "Covid Test Complete" (after payment confirmed)
  - "Check In" (after covid test complete)
  - "✓ Checked In" (completed, no button)
- And button state is stored in React component state
- And button state is NOT stored in database
- And page refresh returns to initial state based on payment status

**Rationale**: Clear visual indicators prevent confusion and errors during high-volume check-in.

### Story 5: Async QR Code Payment Workflow (Non-Blocking)
**As a** check-in staff member
**I want to** continue checking in other people while someone completes QR code payment
**So that** one person's payment doesn't block the entire check-in line

**Acceptance Criteria:**
- Given an attendee is completing QR code payment on their phone
- When I show them the QR code
- Then I can close the QR code modal
- And I continue checking in other attendees
- And the payment completes on attendee's phone (normal ticket purchase)
- And later, I search for that attendee again
- And their status now shows they have a ticket
- And I see "Check In" button (instead of "Paid at Door")
- And I can complete their check-in
- And NO real-time updates or webhooks are needed

**Technical Note**: This is an asynchronous workflow, not real-time. Staff manually search for the attendee again after they complete payment. No Server-Sent Events, WebSockets, or polling required.

**Rationale**: QR code payments take time (30-60 seconds). Staff shouldn't be blocked waiting. Attendee completes payment independently, then returns to check-in line when done.

### Story 6: Covid Test Status (UI-Only Tracking, Always Shown)
**As a** check-in staff member
**I want to** track covid test completion without database overhead
**So that** the workflow is clear without permanent storage requirements

**Acceptance Criteria:**
- Given any attendee in check-in workflow
- When I click "Covid Test Complete"
- Then the button state changes to "Check In"
- And covid test completion is tracked ONLY in UI state
- And covid test status is NOT stored in database
- And page refresh clears covid test status
- And attendee can still be checked in (database only tracks check-in)
- And "Covid Test Complete" button is ALWAYS shown (not configurable)

**Rationale**: Covid test completion is a workflow state, not a business requirement. No historical tracking is needed. Always shown per user requirement.

### Story 7: Cash Payment Recording (Including $0 Payments)
**As a** check-in staff member
**I want to** record cash payments with proper documentation
**So that** financial records are accurate and auditable

**Acceptance Criteria:**
- Given an RSVP attendee paying cash at the door
- When I click "Paid at Door" and select "Cash Payment"
- Then a modal opens with:
  - Ticket type selector dropdown (required)
  - Amount input field (currency format, allows $0.00)
  - Payment method: Cash (pre-selected, read-only)
  - Optional notes field (for special circumstances)
  - "Record Payment" button
- And when I select ticket type
- And I enter amount (including $0.00 for free tickets)
- And I optionally add notes
- And I click "Record Payment"
- Then payment record is created in database with:
  - Ticket type selected
  - Amount entered ($0.00 or any amount)
  - Payment method: Cash
  - RecordedByStaffId (staff member who recorded)
  - Timestamp
  - Notes (if provided)
- And modal closes
- And button changes to "Covid Test Complete"

**Rationale**: Cash payments (including $0 free tickets) must be tracked for financial reconciliation and refund processing. Staff attribution creates audit trail.

### Story 8: QR Code Generation (Links to Existing Ticket Sales Page)
**As an** event organizer
**I want to** generate QR codes that link to existing ticket sales page
**So that** attendees can complete payments using the normal ticket purchase flow

**Acceptance Criteria:**
- Given a social event attendee needs to pay at the door
- When check-in staff selects "Digital Payment"
- Then a QR code is generated containing:
  - URL: `https://witchcityrope.com/events/{eventId}/tickets`
  - Standard link to existing ticket sales page
  - NO special payment processor integration
  - NO unique attendee parameters
- And QR code is displayed prominently on screen
- And attendee can scan with any QR code reader
- And attendee is taken to EXISTING ticket sales page
- And attendee logs in (or signs up)
- And attendee completes NORMAL ticket purchase process
- And attendee receives email receipt per normal process
- And payment is processed via existing PayPal integration
- And staff can close modal and continue checking in others

**Rationale**: Reuse existing ticket purchase flow. QR code is just a convenient way to open the website on attendee's phone. No special payment integrations needed.

### Story 9: Payment Status Differentiation
**As a** check-in staff member
**I want to** see clear visual indicators of payment status
**So that** I know which workflow applies to each attendee

**Acceptance Criteria:**
- Given the check-in interface is loaded
- When I view the attendee list
- Then each row shows payment status:
  - Workshop attendees: Badge showing "Ticket Purchased"
  - Social event with ticket: Badge showing "Ticket Purchased"
  - Social event RSVP only: Badge showing "RSVP Only"
- And badge color differentiates status:
  - "Ticket Purchased": Green badge
  - "RSVP Only": Yellow badge
- And payment status determines initial button state
- And staff can identify payment type at a glance

**Rationale**: Clear status indicators prevent confusion about which workflow to follow.

### Story 10: Multi-Session Ticket Support
**As a** check-in staff member
**I want to** allow door payments for multi-session tickets
**So that** attendees can purchase tickets for events with multiple sessions

**Acceptance Criteria:**
- Given an event has multiple sessions
- When an attendee selects "Cash Payment" at door
- Then ticket type selector includes multi-session options
- And attendee can purchase multi-session tickets same as online
- And when attendee uses QR code
- Then normal ticket sales page shows multi-session options
- And attendee can select sessions same as online purchase
- And check-in process is identical to single-session tickets

**Rationale**: Door payments should support all ticket types available online, including multi-session tickets.

## Answers to Open Questions

### 1. Payment Processor Integration
**Answer**: None - QR code links to existing ticket sales page. Reuse existing PayPal integration. No special payment processor needed.

### 2. Covid Test Button Configuration
**Answer**: "Covid Test Complete" button is ALWAYS shown. Not configurable per event. This is a consistent workflow step.

### 3. $0 Payments
**Answer**: Allowed. Cash payment modal accepts $0.00 for free tickets or comped entries.

### 4. Payment Logging
**Answer**:
- **Cash payments**: Logged with RecordedByStaffId, amount, notes, timestamp, ticket type
- **QR code payments**: Standard online purchases (logged per normal ticket purchase process, no special logging)

### 5. QR Code Window Behavior
**Answer**: Non-blocking. Staff shows QR code, attendee scans and starts payment, staff closes modal and continues checking in other people. Later staff searches for attendee again and they'll have a ticket.

### 6. Refunds
**Answer**: Use existing refund process. Not part of this feature. Door payments follow same refund policy as online purchases.

### 7. Partial Payments
**Answer**: No. Full payment required (or $0 for free).

### 8. Multi-Session Tickets
**Answer**: Yes. Same as online purchase. Cash payment modal has ticket type selector that includes multi-session options. QR code goes to normal ticket sales page which supports multi-session selection.

### 9. Email Receipt
**Answer**: Yes. QR code payments go through normal ticket purchase flow, which sends email receipts automatically. Cash payments follow existing cash payment receipt process.

## Business Rules

### 🚨 CRITICAL: Door Payment = Ticket Purchase

**DOOR PAYMENT CREATES A TICKET PURCHASE, NOT JUST A PAYMENT RECORD**

When staff clicks "Paid at Door" and processes payment (cash or QR code):
1. **A ticket purchase record is created** (same as online ticket purchase)
2. **Attendee status changes from "RSVP Only" to "Ticket Purchased"**
3. **Payment is linked to the ticket purchase record**
4. **Attendee now has a ticket** (just purchased at door instead of online)
5. **"Paid at Door" button disappears** (they already have a ticket)

**Button Visibility Rules:**
- "Paid at Door" button **ONLY appears** if attendee does NOT have a ticket
- If attendee already purchased ticket online → NO "Paid at Door" button
- If attendee paid at door → NO "Paid at Door" button (already has ticket)
- Button visibility based on ticket purchase status, not payment status

### 🚨 CRITICAL: Payment Requirements by Event Type

**THIS IS THE MOST IMPORTANT BUSINESS RULE**

1. **Workshops/Classes**: Payment ALWAYS REQUIRED (ticket purchase mandatory)
   - Attendees cannot RSVP without buying a ticket
   - All workshop attendees have already paid
   - "Paid at Door" option NEVER appears for workshops (ticket required for RSVP)

2. **Social Events**: Payment is OPTIONAL
   - Attendees can RSVP for FREE (no payment or ticket required)
   - Attendees can check in with just an RSVP (no payment needed)
   - "Paid at Door" button provides OPTIONAL ticket purchase opportunity
   - Attendees are NOT required to pay to attend
   - Payment is only an option, not a requirement
   - **Door payment creates a ticket** (not just a payment)

**Why "Paid at Door" exists for social events:**
- Some attendees may want to purchase a ticket to support the organization
- Some attendees may want official proof of payment (receipt/ticket)
- Provides flexibility for different attendee preferences
- **Door purchase creates same ticket as online purchase**
- But payment is NEVER mandatory for social event attendance

### Workflow Rules
1. **Workshop Events**: Always start with "Covid Test Complete" button (payment confirmed via ticket purchase)
2. **Social Events - Paid**: Start with "Covid Test Complete" button (payment confirmed via ticket purchase)
3. **Social Events - RSVP Only**: Start with EITHER:
   - **Option A**: Click "Paid at Door" to purchase optional ticket → then proceed with check-in
   - **Option B**: Skip "Paid at Door" entirely, go directly to "Covid Test Complete" → Check In
4. **Button Progression (Social RSVP with optional payment)**: [Paid at Door] → Covid Test Complete → Check In → Checked In
5. **Button Progression (Social RSVP without payment)**: Covid Test Complete → Check In → Checked In
6. **No Backwards Navigation**: Once a button state advances, it cannot go backwards without page refresh

### Data Storage Rules
1. **DO Store**:
   - Check-in records (attendeeId, eventId, timestamp, staffId)
   - Payment records (amount, method, timestamp, staffId, attendeeId, eventId, ticketTypeId)
   - Payment method (Cash or PayPal)
   - Optional payment notes
   - RecordedByStaffId (for cash payments)
   - Ticket type selected (for cash payments)

2. **DO NOT Store**:
   - Covid test completion status (UI-only)
   - Button workflow states (React state only)
   - QR code scan events (no tracking needed)

### Payment Processing Rules
1. **Cash Payments**:
   - Require ticket type selection
   - Require manual entry of amount (allows $0.00)
   - Allow optional notes
   - Default payment method to "Cash"
   - Record payment immediately upon "Record Payment" click
   - Record staff member ID (RecordedByStaffId)
   - Link payment to attendee and event
   - Create ticket purchase record

2. **Digital Payments (QR Code)**:
   - Link to existing ticket sales page: `https://witchcityrope.com/events/{eventId}/tickets`
   - Must go through standard PayPal ticket purchase flow
   - Must require user sign-in (links payment to account)
   - Staff closes QR modal and continues checking in others (non-blocking)
   - NO real-time updates to kiosk interface
   - Later, staff searches for attendee again and they'll have a ticket
   - Follows normal ticket purchase process (email receipt, payment logging, etc.)
   - Create ticket purchase record (via normal purchase flow)

3. **Payment Security**:
   - NO credit card storage in WitchCityRope database
   - All digital payments via PayPal integration
   - Cash payments recorded as transaction records only
   - Staff member ID required for all cash payment records (audit trail)

### Capacity and Validation Rules
1. **Ticket Validation**:
   - Workshop attendees must have ticket to appear in check-in list
   - Social event attendees can appear with RSVP or ticket
   - Door payments count toward event capacity

2. **Capacity Enforcement**:
   - Check capacity BEFORE allowing door payment
   - Show capacity warning if < 5 spots remaining
   - Prevent door payment if event at capacity
   - Already RSVP'd attendees bypass capacity check

### Modal Usage Rules
1. **Modal Required**:
   - Cash payment (ticket type selection, amount entry, notes)
   - QR code display
   - Walk-in attendee creation
   - Error messages requiring acknowledgment

2. **Modal NOT Required**:
   - Workshop check-ins (already paid + waived)
   - Social event check-ins with tickets (already paid + waived)
   - Covid test completion (UI state only)
   - Standard check-in action (final step)

### QR Code Rules
1. **QR Code Content**:
   - Must contain full URL to existing ticket sales page
   - Format: `https://witchcityrope.com/events/{eventId}/tickets`
   - Must work with standard QR code readers
   - Must redirect to mobile-friendly ticket sales page

2. **QR Code Workflow**:
   - Staff displays QR code modal
   - Attendee scans with phone
   - Attendee taken to existing ticket sales page
   - Attendee logs in or signs up
   - Attendee completes normal ticket purchase
   - Staff closes modal (non-blocking)
   - Staff continues checking in other people
   - Later, staff searches for attendee again
   - Attendee now has ticket, shows "Check In" button

## Data Structure Requirements

### 🚨 CRITICAL: Door Payment Creates Ticket Purchase

**Door payments MUST create ticket purchase records, not standalone payments.**

### Ticket Purchase Record (Created by Door Payment)
- **ticketPurchaseId**: string (UUID, required, primary key)
- **eventId**: string (UUID, required, foreign key)
- **userId**: string (UUID, required, foreign key - attendee who purchased)
- **ticketTypeId**: string (UUID, required, foreign key - which ticket type purchased)
- **quantity**: integer (required, default: 1)
- **amount**: decimal (required, minimum 0.00, two decimal places, allows $0.00)
- **paymentMethod**: string (required, enum: Cash, PayPal)
- **purchaseSource**: string (required, enum: Online, DoorCash, DoorQR)
- **transactionTimestamp**: DateTime (required, ISO 8601, UTC)
- **recordedByStaffId**: string (UUID, optional - only for door purchases, foreign key to User)
- **notes**: string (optional, 500 characters max - only for cash payments)
- **isPaymentCompleted**: boolean (required, default: true for door purchases)

**Key Differences from Online Purchase:**
- `recordedByStaffId` field populated (staff member who processed)
- `purchaseSource` indicates door purchase method (DoorCash or DoorQR)
- `notes` field available for cash payment tracking
- `amount` can be $0.00 for free tickets
- Otherwise identical to online ticket purchase

### Check-In Record Data (Existing)
- **checkInId**: string (UUID, required)
- **eventId**: string (UUID, required)
- **attendeeId**: string (UUID, required)
- **checkInTimestamp**: DateTime (required, ISO 8601, UTC)
- **staffId**: string (UUID, required)
- **hasTicket**: boolean (required, true if ticket purchased before check-in)
- **ticketPurchaseId**: string (UUID, optional, foreign key to ticket purchase)

### UI State Data (Not Stored in Database)
- **covidTestComplete**: boolean (per attendee, React state)
- **currentButtonState**: string (enum: PaidAtDoor, CovidTestComplete, CheckIn, CheckedIn)
- **showQrCode**: boolean (modal state)
- **showCashPaymentModal**: boolean (modal state)

## Constraints & Assumptions

### Technical Constraints
- **Integration**: Must integrate with existing check-in system
- **Session Token Auth**: Kiosk mode with session tokens (no user login)
- **Payment Integration**: Must use existing PayPal integration for digital payments
- **NO Real-Time Updates**: QR code payments are asynchronous, staff manually refresh/search
- **Mobile Compatibility**: QR code links to existing mobile-optimized ticket sales page

### Business Constraints
- **No Modal for Simple Cases**: Workshop and pre-paid check-ins must be modal-free
- **Financial Tracking**: All payments must be tracked for accounting/refunds
- **Audit Trail**: All payment and check-in actions must include staff attribution
- **Waiver Compliance**: Cannot check in attendees without completed waivers
- **Capacity Enforcement**: Cannot exceed event capacity via door sales

### Assumptions
- Workshop attendees always have pre-purchased tickets (no door sales)
- Social event attendees may RSVP without payment
- Staff devices have reliable internet connectivity
- Staff understand difference between workshops and social events
- Attendees paying via QR code have smartphones
- Existing ticket sales page is mobile-optimized
- Covid test checking is current policy (always shown, not configurable)
- QR code payments take 30-60 seconds (staff closes modal and continues)

## Security & Privacy Requirements

### Payment Security
- **NO Card Storage**: Zero credit card data stored in WitchCityRope systems
- **PayPal Integration**: All digital payments processed via PayPal
- **Cash Records Only**: Cash payments recorded as transaction records only
- **Audit Logging**: All payment records include staff member ID and timestamp
- **Reuse Existing Flow**: QR code payments use existing secure ticket purchase flow

### Check-In Security
- **Session Token Auth**: Kiosk access controlled via time-limited session tokens
- **Staff Attribution**: All check-in actions linked to staff member
- **Capacity Validation**: Cannot override capacity without proper authorization
- **Payment Verification**: Cannot check in without payment confirmation (workshops)

### Data Privacy
- **Minimal PII**: Only necessary attendee information displayed
- **Payment Privacy**: Payment amounts visible only to staff during check-in
- **Transaction History**: Payment records accessible only to admins and event organizers
- **RSVP Privacy**: RSVP status visible only to staff

## Compliance Requirements

### Financial Compliance
- **Accurate Records**: All cash payments must be recorded immediately
- **Audit Trail**: Complete payment history with staff attribution
- **Refund Support**: Payment records must support refund processing
- **Reconciliation**: Payment totals must match actual cash collected

### Platform Policies
- **Waiver Enforcement**: Cannot check in attendees without waiver
- **Capacity Compliance**: Must enforce event capacity limits
- **Payment Terms**: Door payments subject to same refund policies as online

### PCI Compliance
- **NO Card Processing**: Cash and PayPal only (no direct card processing)
- **External Processing**: All digital payments via PCI-compliant PayPal
- **No Sensitive Data**: Zero credit card numbers, CVVs, or card data stored

## User Impact Analysis

| User Type | Impact | Priority | Changes |
|-----------|--------|----------|---------|
| Check-In Staff | High positive - 40% faster check-ins, simpler workflow | Critical | 2-click check-in for workshops, clear button progression, non-blocking QR |
| Event Organizers | Medium positive - faster processing, better door sales tracking | High | More attendees processed per minute, complete payment records |
| Workshop Attendees | High positive - faster arrival processing | High | Reduced wait times during check-in |
| Social Event Attendees (Pre-Paid) | High positive - same fast experience as workshops | High | Reduced wait times during check-in |
| Social Event Attendees (RSVP) | Medium - flexible door payment options | Medium | Can pay cash or QR code at door (optional) |
| Admins | Low positive - cleaner payment reconciliation | Low | Complete payment audit trail |

## Examples/Scenarios

### Scenario 1: Workshop Event - Happy Path
**Setup**: Advanced Rope Workshop, 20 attendees, all pre-paid tickets

1. Staff member opens check-in interface for event
2. 20 attendees listed, all show "Ticket Purchased" green badge
3. First attendee arrives: "Jane Doe"
4. Staff clicks "Covid Test Complete" button in Jane's row
5. Button changes to "Check In"
6. Staff clicks "Check In"
7. Row updates to show "✓ Checked In" status
8. No modals appeared
9. **Total time: 3 seconds (2 clicks)**

**Verification**: Check-in record created with timestamp and staff ID

### Scenario 2: Social Event - Pre-Paid Attendee
**Setup**: Community Rope Jam, attendee purchased ticket ahead of time

1. Staff opens check-in interface for event
2. Attendee "John Smith" shows "Ticket Purchased" green badge
3. Staff clicks "Covid Test Complete" in John's row
4. Button changes to "Check In"
5. Staff clicks "Check In"
6. Row updates to "✓ Checked In"
7. **Total time: 3 seconds (2 clicks)**

**Verification**: Check-in record shows hasTicket = true

### Scenario 3: Social Event - Cash Payment at Door (Including $0)
**Setup**: Community Rope Jam, attendee RSVP'd but didn't buy ticket

1. Staff opens check-in interface
2. Attendee "Sarah Johnson" shows "RSVP Only" yellow badge
3. Staff clicks "Paid at Door" in Sarah's row
4. Staff selects "Cash Payment" option
5. Modal opens with ticket type selector, amount input, notes field
6. Staff selects ticket type: "General Admission"
7. Staff enters "$20.00" (or "$0.00" for free ticket)
8. Staff optionally adds notes (e.g., "Sliding scale payment")
9. Staff clicks "Record Payment"
10. Modal closes
11. Button changes to "Covid Test Complete"
12. Staff clicks "Covid Test Complete"
13. Button changes to "Check In"
14. Staff clicks "Check In"
15. Row updates to "✓ Checked In"
16. **Total time: 20 seconds (5 clicks + data entry)**

**Verification**:
- Ticket purchase record created (ticketTypeId, amount: $20.00 or $0.00, method: Cash, purchaseSource: DoorCash, recordedByStaffId, timestamp, notes)
- Check-in record created with hasTicket = true, ticketPurchaseId linked

### Scenario 4: Social Event - QR Code Payment (Async Workflow)
**Setup**: Community Rope Jam, attendee RSVP'd, prefers digital payment

1. Staff opens check-in interface
2. Attendee "Mike Chen" shows "RSVP Only" yellow badge
3. Mike arrives at check-in
4. Staff clicks "Paid at Door" in Mike's row
5. Staff selects "Digital Payment" option
6. QR code modal displays with URL: `https://witchcityrope.com/events/123/tickets`
7. Mike scans QR code with phone
8. Mike's phone opens WitchCityRope website ticket sales page
9. **Staff closes QR code modal (non-blocking)**
10. **Staff continues checking in other attendees**
11. Mike logs in on his phone
12. Mike selects ticket type on phone
13. Mike completes PayPal payment ($20.00) on phone
14. Mike receives email receipt
15. **Later (1-2 minutes), Mike returns to check-in line**
16. Staff searches for "Mike Chen" again
17. Mike now shows "Ticket Purchased" green badge (ticket purchase completed)
18. Staff clicks "Covid Test Complete"
19. Button changes to "Check In"
20. Staff clicks "Check In"
21. Row updates to "✓ Checked In"
22. **Total time: 2-3 minutes total, but non-blocking for staff**

**Verification**:
- Ticket purchase record created via normal online purchase flow (ticketTypeId, amount: $20.00, method: PayPal, purchaseSource: Online)
- Email receipt sent automatically
- Check-in record created with hasTicket = true, ticketPurchaseId linked
- NO real-time updates occurred - staff manually searched again

### Scenario 5: Page Refresh Behavior
**Setup**: Staff accidentally refreshes browser during check-in

1. Staff is checking in "Alex Rivera"
2. Staff clicked "Covid Test Complete" (button shows "Check In")
3. Staff accidentally refreshes browser
4. Page reloads
5. Alex's row shows "Ticket Purchased" badge
6. Button resets to "Covid Test Complete" (covid test state lost)
7. Staff clicks "Covid Test Complete" again
8. Button changes to "Check In"
9. Staff clicks "Check In"
10. Check-in completes successfully

**Expected Behavior**: UI state resets on refresh, but database state persists (Alex is NOT checked in until final "Check In" click)

### Scenario 6: Capacity Warning - Door Payment
**Setup**: Social event with capacity 50, currently 48 checked in

1. Staff opens check-in interface
2. Event shows "48/50" in capacity display
3. Attendee "Dana White" arrives (RSVP only, no ticket)
4. Staff clicks "Paid at Door"
5. System shows warning: "⚠️ Only 2 spots remaining!"
6. Staff proceeds with cash payment
7. Payment recorded
8. Capacity updates to "49/50"
9. Dana checks in successfully

**Verification**: Capacity enforced, warning displayed, payment allowed

### Scenario 7: At Capacity - Door Payment Blocked
**Setup**: Social event with capacity 50, currently 50 checked in

1. Staff opens check-in interface
2. Event shows "50/50 - At Capacity" in capacity display
3. Attendee "Pat Brown" arrives (RSVP only, no ticket)
4. Staff clicks "Paid at Door"
5. System shows error: "❌ Event at capacity. Cannot process door payment."
6. "Paid at Door" button disabled/grayed out
7. Staff explains to Pat that event is full

**Verification**: Door payment blocked, existing check-ins unaffected

### Scenario 8: Multi-Session Ticket Door Purchase
**Setup**: Workshop series with 3 sessions, attendee wants to buy all 3 at door

1. Staff opens check-in interface
2. Attendee "Taylor Swift" shows "RSVP Only" for Session 1
3. Staff clicks "Paid at Door"
4. Staff selects "Cash Payment"
5. Modal opens with ticket type selector
6. Ticket types include: "Single Session ($20)", "3-Session Pass ($50)"
7. Staff selects "3-Session Pass ($50)"
8. Staff enters "$50.00"
9. Staff clicks "Record Payment"
10. Ticket purchase created for all 3 sessions
11. Taylor can now check in to all 3 sessions with same ticket

**Verification**: Multi-session ticket purchase recorded, all sessions linked to ticket

## Success Criteria

### Performance Metrics
- ✅ Check-in time reduced from 5 seconds to 3 seconds per attendee
- ✅ Staff can process 20 attendees per minute (up from 12)
- ✅ Zero payment tracking errors in first month
- ✅ 100% of payments correctly attributed to staff members
- ✅ QR code workflow non-blocking (staff closes modal and continues)

### User Experience Metrics
- ✅ Staff training time reduced from 15 minutes to 5 minutes
- ✅ Staff satisfaction rating improved by 30%
- ✅ Zero complaints about check-in speed
- ✅ Door payment success rate > 95%
- ✅ QR code scan success rate > 90%

### Technical Metrics
- ✅ Zero check-in errors due to workflow confusion
- ✅ 100% button state transitions work correctly
- ✅ Payment records match cash collected (100% accuracy)
- ✅ System handles capacity limits correctly (100% compliance)
- ✅ QR code links to correct ticket sales page (100% accuracy)

### Business Metrics
- ✅ Door payment adoption rate > 50% for RSVP attendees
- ✅ Cash payment processing time < 30 seconds
- ✅ Digital payment (QR code) is non-blocking for staff
- ✅ Event capacity compliance maintained (zero overages)
- ✅ Payment reconciliation accuracy 100%

## Quality Gate Checklist (95% Required)

- [x] All user roles addressed (staff, organizers, attendees)
- [x] Clear acceptance criteria for each story
- [x] Business value clearly defined (40% efficiency improvement)
- [x] Edge cases considered (page refresh, capacity limits, $0 payments)
- [x] Security requirements documented (payment security, audit trails)
- [x] Compliance requirements checked (PCI, financial, platform)
- [x] Performance expectations set (3 seconds per check-in)
- [x] Mobile experience considered (QR code links to mobile-optimized page)
- [x] Examples provided (8 detailed scenarios including async workflow)
- [x] Success metrics defined (measurable targets)
- [x] Data structures specified (payment records, check-in records)
- [x] Business rules documented (workflow, storage, payment)
- [x] Open questions answered (10 answers provided)
- [x] User impact analysis completed (all user types)
- [x] Constraints documented (technical, business, assumptions)
- [x] Modal usage rules defined (when required vs not required)
- [x] Async workflow requirements specified (non-blocking QR code)
- [x] Error handling scenarios included
- [x] Integration points identified (existing check-in, PayPal, ticket sales)
- [x] Simplification emphasized (reuse existing flows, no new payment integrations)

## Next Steps

### For Product Manager
1. **Review Simplified Approach**: Confirm async QR workflow meets needs
2. **Validate Workflows**: Confirm button progression matches operational needs
3. **Approve Reuse Strategy**: Confirm reusing existing ticket sales page is acceptable
4. **Confirm $0 Payments**: Validate that $0.00 cash payments are allowed
5. **Review Multi-Session Support**: Confirm multi-session tickets can be purchased at door

### For Implementation Team
1. **Technical Design**: Create technical design based on simplified approach
2. **UI Design**: Create wireframes for new button states and payment modals
3. **API Design**: Design endpoints for cash payment recording only (QR uses existing)
4. **QR Code Research**: Select and test QR code library for modal display
5. **Integration Planning**: Map integration points with existing check-in system and ticket sales page

### For Test Team
1. **Test Plan**: Create comprehensive test plan for all workflows
2. **Test Data**: Prepare test scenarios for workshops vs social events
3. **Performance Tests**: Measure check-in time improvements
4. **Async Tests**: Verify QR code workflow is non-blocking
5. **Security Tests**: Validate payment security and audit trails

---

## Document Validation

**Created By**: Business Requirements Agent
**Review Status**: Draft - Revised with Simplified Approach - Awaiting Product Manager Review
**Last Updated**: 2025-11-04
**Version**: 2.0
**Target Audience**: Product Manager, UI Designer, Backend Developer, React Developer, Test Developer
**Related Documents**:
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/business-requirements.md`
- `/apps/web/tests/playwright/checkin-test-plan.md`
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/wireframes/check-in-interface-wireframe.md` (if exists)

**Approval Required From**:
- [ ] Product Manager (Chad Bennett)
- [ ] Event Organizer Representative
- [ ] Check-In Staff Representative

**Key Changes in Version 2.0**:
- ✅ Removed real-time payment detection (Story 5 rewritten)
- ✅ Updated QR code to link to existing ticket sales page
- ✅ Emphasized reuse of existing payment flows
- ✅ Added $0.00 payment support
- ✅ Made covid test always shown (not configurable)
- ✅ Clarified async QR workflow (non-blocking)
- ✅ Added answers to all open questions
- ✅ Added multi-session ticket support
- ✅ Added critical simplification note at top
- ✅ Removed all mention of special payment processor integrations
