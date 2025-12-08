# Business Requirements: Per-Session Ticket Limitations
<!-- Last Updated: 2025-12-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary

WitchCityRope currently enforces ONE ticket per EVENT per user, which prevents members from purchasing tickets for non-consecutive sessions of multi-day workshops. This business requirement document defines the transition to ONE ticket per SESSION per user, enabling flexible attendance patterns while maintaining capacity management and preventing duplicate purchases.

**Current Problem**: Members who can only attend specific sessions of a multi-day workshop (e.g., Day 1 and Day 3, skipping Day 2) must either purchase a full event pass and waste money on unused sessions, or skip the workshop entirely.

**Proposed Solution**: Implement per-session ticket validation that allows users to purchase tickets for different sessions of the same event while preventing duplicate coverage of any single session.

**Business Value**:
- Increases revenue by enabling partial attendance ticket sales
- Improves member satisfaction and accessibility
- Maintains capacity management integrity
- Supports flexible workshop formats and pricing models

## Business Context

### Problem Statement

The current per-event ticket limitation creates barriers to participation and revenue opportunities:

1. **Member Accessibility**: Members with schedule conflicts cannot participate in valuable workshops if they can't attend all sessions
2. **Revenue Loss**: Teachers lose ticket sales from members who would attend some sessions but not all
3. **Pricing Inflexibility**: Cannot offer session-specific pricing (e.g., Friday evening intro session at lower cost)
4. **Workshop Design Constraints**: Teachers hesitate to create multi-day workshops knowing attendance will be limited

**Real-World Scenario**: "Advanced Suspension Workshop" runs Friday evening, all-day Saturday, and Sunday morning. A member can attend Friday and Sunday but not Saturday. Currently, they cannot purchase tickets for just those sessions.

### Business Value

**Revenue Impact**:
- **Increased Ticket Sales**: Capture revenue from members with partial availability
- **Dynamic Pricing**: Enable session-specific pricing strategies (early bird for Friday, premium for Saturday intensive)
- **Waitlist Optimization**: Fill individual session spots without blocking full-event passes

**Member Experience**:
- **Accessibility**: Members can participate in workshops despite schedule constraints
- **Affordability**: Pay only for sessions attended, reducing financial barrier
- **Flexibility**: Choose attendance patterns that fit personal schedules

**Teacher Benefits**:
- **Workshop Design Freedom**: Create multi-day workshops with confidence
- **Session Optimization**: Adjust capacity per session based on venue/content
- **Revenue Predictability**: Better forecasting with session-level sold counts

### Success Metrics

**Quantitative Metrics**:
- **Ticket Conversion Rate**: Increase in partial-attendance ticket purchases (target: +15% for multi-session events)
- **Revenue Per Event**: Increase in total revenue from flexible pricing (target: +20% for workshops)
- **Session Utilization**: Percentage of available session spots sold (target: >85% across all sessions)
- **Cart Abandonment**: Decrease in purchase abandonments for multi-session events (target: -25%)

**Qualitative Metrics**:
- **Member Satisfaction**: Survey feedback on session flexibility (target: >90% positive)
- **Teacher Satisfaction**: Feedback from teachers on workshop planning (target: >85% positive)
- **Support Ticket Reduction**: Decrease in "can I attend just one day?" inquiries (target: -50%)

## User Stories

### Story 1: Purchase Non-Consecutive Sessions

**As a** vetted member
**I want to** purchase tickets for specific sessions of a multi-day workshop
**So that** I can participate in the sessions I can attend without paying for the full event

**Acceptance Criteria**:

**Given** a 3-day workshop event with sessions Friday, Saturday, Sunday
**And** ticket types: "Friday Only" ($35-$50), "Saturday Only" ($75-$100), "Sunday Only" ($35-$50), "Full Weekend" ($100-$150)
**When** I purchase "Friday Only" ticket
**Then** purchase succeeds
**And** I see confirmation: "You have a ticket for Friday session"
**And** I can still purchase "Sunday Only" ticket (different session)
**And** I cannot purchase "Full Weekend" ticket (overlaps Friday)

**Business Rules**:
- Session overlap detection prevents duplicate coverage
- Clear messaging identifies which sessions are covered by each ticket type
- Capacity counts update for all included sessions

---

### Story 2: Prevent Duplicate Session Coverage

**As a** member
**I want to** be prevented from purchasing overlapping tickets
**So that** I don't waste money on duplicate access to the same session

**Acceptance Criteria**:

**Given** I have purchased "Full Weekend" ticket (covers Friday, Saturday, Sunday)
**When** I attempt to purchase "Saturday Only" ticket
**Then** purchase fails with error message
**And** error states: "You already have a ticket that includes the Saturday session"
**And** error lists my existing ticket: "Full Weekend (Friday-Sunday)"
**And** purchase button is disabled for overlapping ticket types
**And** I see badge on overlapping tickets: "Already Purchased"

**Edge Cases**:
- Purchasing second ticket type that partially overlaps (e.g., "Weekend Pass" when I have "Friday Only")
- Cancelled tickets don't prevent new purchases for same sessions
- Refunded tickets release session coverage

---

### Story 3: Session-Based Capacity Display

**As a** member browsing events
**I want to** see capacity availability per session
**So that** I know which sessions still have available spots before purchasing

**Acceptance Criteria**:

**Given** a multi-session workshop with different capacity per session
**When** I view the event detail page
**Then** I see capacity for each session clearly displayed
**And** display format: "Friday Evening: 5 of 20 spots available"
**And** sold-out sessions show: "Saturday Workshop: SOLD OUT"
**And** available sessions show: "Sunday Practice: 12 of 15 spots available"
**And** ticket types show which sessions they include
**And** sold-out ticket types are disabled with clear messaging

**Display Requirements**:
- Real-time capacity updates as tickets are purchased
- Visual distinction between available, low availability (<5 spots), and sold-out
- Mobile-responsive capacity display

---

### Story 4: Admin Session Capacity Management

**As an** admin or teacher
**I want to** manage capacity at the session level
**So that** I can set different limits based on venue size, content complexity, or instructor availability

**Acceptance Criteria**:

**Given** I am creating/editing a multi-session event
**When** I configure ticket types
**Then** I can specify capacity limits per session
**And** I can set different capacities for different sessions
**And** I see total event capacity calculated from session maximums
**And** I can override session capacity if needed (e.g., venue expansion)
**And** system validates: ticket type capacity ≤ minimum session capacity it covers

**Example Configuration**:
- Friday evening (smaller room): 15 capacity
- Saturday workshop (main room): 30 capacity
- Sunday practice (main room): 30 capacity
- "Full Weekend" ticket type: Max 15 (limited by Friday session)

---

### Story 5: Multi-Session Ticket Sold Count Accuracy

**As an** admin
**I want to** see accurate sold counts that reflect session overlap
**So that** I can track demand and adjust pricing/capacity accordingly

**Acceptance Criteria**:

**Given** a workshop with multiple ticket types covering different sessions
**When** I view the admin event dashboard
**Then** I see sold count per ticket type
**And** I see sold count per session (accounting for multi-session tickets)
**And** I see capacity utilization per session as percentage
**And** I can export attendee list filtered by session
**And** reports show accurate revenue attribution per session

**Display Example**:
```
Ticket Type Sales:
- Friday Only: 8 sold
- Weekend Pass: 12 sold
- Full Workshop: 5 sold

Session Attendance:
- Friday: 25/30 (8 Friday Only + 12 Weekend + 5 Full) - 83% capacity
- Saturday: 17/30 (12 Weekend + 5 Full) - 57% capacity
- Sunday: 17/30 (12 Weekend + 5 Full) - 57% capacity
```

---

### Story 6: RSVP Session Selection (Social Events)

**As a** vetted member
**I want to** RSVP for specific sessions of a multi-session social event
**So that** I can indicate attendance for only the sessions I plan to attend

**Acceptance Criteria**:

**Given** a social event with multiple sessions (e.g., Friday night social + Saturday rope practice)
**When** I RSVP
**Then** I can select which sessions I will attend
**And** RSVP follows same per-session rules as tickets
**And** I cannot RSVP for same session twice
**And** capacity counts update for selected sessions only
**And** confirmation email lists my selected sessions

**Note**: Social events maintain free RSVP model but add session selection granularity

---

### Story 7: Mobile Ticket Purchase with Session Selection

**As a** member using mobile device
**I want to** easily select and purchase session-specific tickets on my phone
**So that** I can complete registration while on-the-go

**Acceptance Criteria**:

**Given** I am on mobile device viewing a multi-session workshop
**When** I tap "Purchase Ticket"
**Then** I see mobile-optimized session selection interface
**And** sessions are clearly labeled with dates/times
**And** capacity indicators are visible without scrolling
**And** ticket type selection shows included sessions with visual badges
**And** purchase flow is streamlined for touch interface
**And** confirmation displays session details clearly

**Mobile-Specific Requirements**:
- Touch-friendly session selection
- Readable capacity indicators on small screens
- Clear visual hierarchy for session coverage
- Fast load times for capacity checks

---

## Business Rules

### BR-1: One Ticket Per Session Per User (Core Rule)

**Rule**: A user SHALL have at most ONE active ticket covering any specific session

**Implementation**:
- Validation occurs at ticket purchase time
- Check: Does user have existing ticket that includes ANY of the requested sessions?
- If YES → Reject purchase with clear error message
- If NO → Allow purchase and create ticket

**Examples**:
- ✅ **ALLOWED**: User has "Friday Only", purchases "Sunday Only" (different sessions)
- ❌ **BLOCKED**: User has "Full Weekend", attempts "Saturday Only" (Saturday overlap)
- ✅ **ALLOWED**: User cancels "Full Weekend", then purchases "Saturday Only" (no active overlap)

**Cancellation Handling**:
- Cancelled tickets (Status: Cancelled) do NOT block new purchases
- Refunded tickets (Status: Refunded) do NOT block new purchases
- Only Active tickets prevent session overlap

---

### BR-2: Session-Level Capacity Enforcement

**Rule**: Capacity SHALL be tracked and enforced at the session level

**Calculation**:
```
Session Available Capacity = Session.Capacity - COUNT(Active Tickets Covering Session)
```

**Multi-Session Ticket Impact**:
- A "Full Weekend" ticket consumes 1 capacity spot from EACH included session
- A "Weekend Pass" (Sat-Sun) consumes 1 spot from Saturday AND 1 from Sunday
- Capacity checks MUST verify ALL sessions included in ticket type

**Example**:
- Friday: 20 capacity, 15 tickets sold → 5 available
- Saturday: 20 capacity, 18 tickets sold → 2 available
- Sunday: 20 capacity, 12 tickets sold → 8 available
- "Full Weekend" ticket requires: Friday available ≥ 1 AND Saturday ≥ 1 AND Sunday ≥ 1

**Overselling Prevention**:
- Purchase blocked if ANY included session is at capacity
- Race condition handling: Database-level locking on capacity updates
- Error message: "Saturday session is sold out. Please choose a different ticket type."

---

### BR-3: Ticket Type Session Association

**Rule**: Each ticket type MUST define which sessions it includes

**Configuration**:
- Ticket types have many-to-many relationship with sessions (TicketTypeSessions table)
- At least ONE session required per ticket type
- Admin configures session associations when creating ticket types

**Validation**:
- Ticket type capacity ≤ minimum capacity of included sessions
- If ticket includes Friday (15 capacity) and Saturday (30 capacity), max tickets = 15
- Admin warning if ticket capacity exceeds session capacity

**Display Requirements**:
- Public event pages show which sessions each ticket type includes
- Clear badges: "Includes: Friday Evening, Sunday Morning"
- Calendar icons showing session dates/times

---

### BR-4: Session Capacity Independence

**Rule**: Each session CAN have different capacity limits

**Rationale**:
- Venue size may vary (Friday: small room, Saturday: main hall)
- Content complexity may limit attendance (intensive Saturday session)
- Instructor availability may differ per session

**Implementation**:
- Session.Capacity field (integer, required)
- Session capacity set during event creation
- Admin can modify session capacity (with warnings if tickets already sold)

**Capacity Adjustment Rules**:
- Increasing capacity: Always allowed
- Decreasing capacity: Allowed ONLY if new capacity ≥ tickets already sold
- Error if decrease would oversell: "Cannot reduce capacity to 15. 18 tickets already sold for this session."

---

### BR-5: EventAttendance Session Tracking

**Rule**: EventAttendance records MUST track which sessions the user is attending

**Current Schema**: EventAttendance links User to Event (event-level)

**Required Enhancement** (Option 1 - Recommended):
- Add `SessionId` field to EventAttendance table
- FK constraint: SessionId → Sessions.Id
- NULL allowed for single-session events (backward compatibility)
- For multi-session tickets, create EITHER:
  - Multiple EventAttendance records (one per session), OR
  - Single record with SessionIds collection (JSONB array)

**Data Migration**:
- Backfill SessionId for existing EventAttendance records
- Use TicketPurchase → TicketType → Sessions to determine session(s)
- For tickets covering multiple sessions, create multiple records or populate array

---

### BR-6: Backward Compatibility with Single-Session Events

**Rule**: Single-session events SHALL work exactly as before

**No Behavior Change**:
- Events with one session maintain current validation logic
- Ticket purchase validation works identically
- Capacity management unchanged
- User experience unchanged

**Implementation**:
- If Event has only 1 session → Use existing validation (event-level check still works)
- If Event has multiple sessions → Use new session-level validation
- UI adapts: Single-session events don't show session selection

**Testing Requirement**:
- Comprehensive regression tests for single-session events
- Zero breaking changes to existing functionality

---

### BR-7: Refund and Cancellation Session Rules

**Rule**: Refunds and cancellations SHALL release capacity for affected sessions

**Cancellation Impact**:
- User cancels "Full Weekend" ticket → Capacity increases for ALL included sessions (Friday +1, Saturday +1, Sunday +1)
- User cancels "Saturday Only" ticket → Only Saturday capacity increases (+1)

**Partial Refunds** (Future Scope - Not Implemented):
- Currently: Full refund or no refund (all-or-nothing)
- Future: Partial refunds for multi-session tickets if some sessions already occurred
- Out of scope for initial implementation

**Refund Validation**:
- Session-level refund policy: Apply refund window to EARLIEST session in ticket
- "Full Weekend" ticket: Refund deadline based on Friday session start time
- "Weekend Pass" (Sat-Sun): Refund deadline based on Saturday session start time

---

### BR-8: Admin Override Capabilities

**Rule**: Admins SHALL have emergency override capabilities for capacity management

**Override Scenarios**:
- Venue expansion: Increase session capacity mid-event
- Emergency booking: Add ticket beyond session capacity (e.g., instructor's partner)
- VIP access: Manually create ticket ignoring capacity limits

**Audit Requirements**:
- All capacity overrides logged in audit trail
- Admin user, timestamp, reason recorded
- Warning displayed: "This will exceed session capacity. Confirm?"

**Safety Limits**:
- Hard maximum: 2x configured session capacity (prevent accidental entry of 999)
- Override requires admin role (teachers cannot override)

---

### BR-9: Session-Level Financial Reporting

**Rule**: Financial reports SHALL attribute revenue to specific sessions

**Attribution Logic**:
- "Friday Only" ticket ($40) → $40 to Friday session
- "Full Weekend" ticket ($120) → Split evenly: $40 Friday, $40 Saturday, $40 Sunday
- "Weekend Pass" ($80) → $40 Saturday, $40 Sunday

**Reports Required**:
- Revenue per session (for multi-session events)
- Average ticket price per session
- Session profitability analysis (revenue vs. instructor cost per session)

**Export Capability**:
- CSV export: Event, Session, Ticket Type, Quantity Sold, Revenue
- Filter by date range, event type, session
- Aggregate by teacher, venue, or time period

---

## Constraints & Assumptions

### Technical Constraints

**Database**:
- PostgreSQL 15+ (current platform standard)
- TicketTypeSessions join table already exists (migration applied 2025-12-02)
- EventAttendance schema enhancement required (SessionId field OR SessionIds array)

**Performance**:
- Session capacity queries MUST complete in <100ms (95th percentile)
- Ticket purchase validation MUST complete in <200ms (95th percentile)
- Database indexing required on SessionId columns

**Concurrency**:
- Race condition handling for simultaneous ticket purchases
- Optimistic locking on session capacity updates
- Clear error messaging for "sold out during checkout" scenarios

### Business Constraints

**Pricing Model**:
- Sliding scale pricing maintained for all ticket types
- Session-specific pricing allowed (different ranges for different sessions)
- "Pay what you can afford" philosophy preserved

**Refund Policy**:
- Full refunds within configured window (typically 48 hours before earliest session)
- No partial refunds (all-or-nothing policy maintained)
- Admin manual approval for late refund requests

**Capacity Management**:
- Session capacity ≥ tickets already sold (cannot reduce below sold count)
- Event capacity = sum of all session capacities (informational only)
- Individual session capacity enforced (event capacity not enforced)

### Assumptions

**User Behavior**:
- Members understand session-based ticketing concept
- Members will read session descriptions before purchasing
- Mobile usage represents >50% of ticket purchases

**Event Configuration**:
- Teachers will properly configure session capacities based on venue
- Ticket types will be clearly named to indicate session coverage
- Session descriptions will include date, time, and location details

**System Availability**:
- Real-time capacity updates required (no caching of sold counts)
- PayPal webhook processing updates capacity immediately
- Email confirmations include accurate session details

**Data Migration**:
- Existing tickets can be accurately assigned to sessions via TicketType.Sessions
- All historical tickets remain valid post-migration
- No data loss during EventAttendance schema enhancement

---

## Security & Privacy Requirements

### Data Privacy

**Session Attendance Information**:
- **Public Visibility**: Aggregate session capacity ("15 of 30 spots available")
- **Vetted Member Visibility**: Same as public (no additional session details)
- **Member's Own Data**: Full details of sessions they purchased tickets for
- **Admin Visibility**: Complete attendee list per session with scene names

**Attendee Lists**:
- Session attendee lists are admin-only (not publicly visible)
- Members cannot see who else is attending specific sessions
- Privacy preserved consistent with current platform standards

### Payment Security

**Session-Specific Payment Data**:
- PayPal integration unchanged (session info in metadata only)
- No additional PCI compliance requirements
- Session details stored in purchase metadata for reporting

**Refund Tracking**:
- Session-specific refund reasons recorded
- Audit trail for all session-based capacity adjustments
- Admin actions logged with user ID and timestamp

### Authentication & Authorization

**Access Control**:
- **Public Users**: View session availability, cannot purchase
- **General Members**: Purchase class tickets (session-based), cannot RSVP social events
- **Vetted Members**: Full access to all sessions (social + class tickets)
- **Teachers**: Manage own event sessions, view attendee lists for own events
- **Admins**: Full session management, capacity overrides, attendee lists for all events

**API Endpoints**:
- Session capacity queries: Public (read-only)
- Ticket purchase validation: Authenticated users only
- Session attendee lists: Admin role required
- Capacity override: Admin role required

---

## Compliance Requirements

### Platform Policies

**Age Verification**:
- Maintained at event level (not session-specific)
- 21+ age verification required for ALL sessions of event
- No per-session age requirements

**Safety Waiver**:
- Accepted once per event (covers all sessions)
- Waiver text includes session-specific safety considerations if applicable
- No per-session waiver requirements

**Consent Framework**:
- Event-level consent maintained
- No session-specific consent requirements
- Code of conduct applies to all sessions equally

### Financial Compliance

**Refund Regulations**:
- Full refund policy maintained (state consumer protection laws)
- Session-based refund deadlines clearly communicated
- Partial refund policy deferred to future implementation (complexity)

**Sales Tax** (If Applicable):
- Session-specific pricing does not affect tax calculation
- Tax applied to total purchase amount (sum of session ticket prices)

**Financial Reporting**:
- Revenue attribution per session for internal reporting
- Tax reporting at event level (not session-specific)

---

## User Impact Analysis

| User Type | Impact | Changes | Priority |
|-----------|--------|---------|----------|
| **Vetted Member** | HIGH | Can purchase tickets for specific sessions, better affordability | **CRITICAL** |
| **General Member** | MEDIUM | Class ticket flexibility (session selection), no impact on social events | HIGH |
| **Teacher** | HIGH | Session-level capacity management, more pricing flexibility, better reporting | **CRITICAL** |
| **Admin** | HIGH | Session-based attendee management, new reporting capabilities, capacity overrides | **CRITICAL** |
| **Guest/Unauthenticated** | LOW | See session-level availability before login, no functional change | MEDIUM |

### Detailed User Impact

**Vetted Members**:
- **Benefit**: Attend multi-day workshops without full schedule commitment
- **Change**: New session selection step during ticket purchase
- **Learning Curve**: LOW - Session badges clearly show coverage
- **Risk**: Confusion about which sessions ticket includes (mitigation: clear UI)

**General Members**:
- **Benefit**: Class workshop flexibility (cannot attend social events regardless)
- **Change**: Same as vetted members for class tickets
- **Learning Curve**: LOW
- **Risk**: None (already cannot access social events)

**Teachers**:
- **Benefit**: Better workshop design options, revenue optimization
- **Change**: Must configure session capacities when creating events
- **Learning Curve**: MEDIUM - New capacity management interface
- **Training Required**: Admin guide on session configuration best practices

**Admins**:
- **Benefit**: Granular capacity control, session-level reporting
- **Change**: New session management UI, different sold count displays
- **Learning Curve**: MEDIUM - New reporting and override capabilities
- **Training Required**: Admin workshop on session-based ticketing

---

## Examples/Scenarios

### Scenario 1: Happy Path - Non-Consecutive Session Purchase

**Event**: "Rope Suspension Intensive" - 3-day workshop
**Sessions**:
- Friday 7pm-10pm: "Fundamentals" (20 capacity)
- Saturday 10am-5pm: "Advanced Techniques" (25 capacity)
- Sunday 10am-3pm: "Practice & Q&A" (25 capacity)

**Ticket Types**:
- "Friday Only": $35-$50 sliding scale → Covers Friday session
- "Weekend Days": $75-$100 sliding scale → Covers Saturday + Sunday
- "Full Workshop": $100-$150 sliding scale → Covers all 3 sessions

**User Journey**:

1. **Sarah** (vetted member) views event detail page
2. Sees session capacity:
   - Friday: 12 of 20 available
   - Saturday: 8 of 25 available
   - Sunday: 15 of 25 available
3. Clicks "Purchase Ticket"
4. Selects "Friday Only" ticket
5. Chooses $40 on sliding scale
6. Proceeds to PayPal, completes payment
7. ✅ **Purchase succeeds** - EventAttendance created for Friday session
8. Dashboard shows: "You have a ticket for Friday Fundamentals session"
9. Sarah returns to event page next week
10. Sees updated capacity (Friday now 11 of 20 available)
11. Clicks "Purchase Ticket" again
12. Selects "Weekend Days" ticket (Saturday + Sunday)
13. System validates: No overlap with existing Friday ticket ✅
14. Chooses $85 on sliding scale
15. Completes PayPal payment
16. ✅ **Purchase succeeds** - EventAttendance created for Saturday + Sunday sessions
17. Dashboard shows: "You have tickets for Friday, Saturday, and Sunday sessions"

**Session Capacity After Purchases**:
- Friday: 11 of 20 (Sarah's first ticket counted)
- Saturday: 7 of 25 (Sarah's second ticket counted)
- Sunday: 14 of 25 (Sarah's second ticket counted)

---

### Scenario 2: Error Path - Duplicate Session Coverage

**Event**: Same "Rope Suspension Intensive" as above

**User Journey**:

1. **Alex** (vetted member) purchases "Full Workshop" ticket ($120)
2. ✅ EventAttendance created for all 3 sessions (Friday, Saturday, Sunday)
3. Dashboard shows: "You have a ticket for all workshop sessions"
4. Capacity updates:
   - Friday: -1 (now 10 of 20)
   - Saturday: -1 (now 6 of 25)
   - Sunday: -1 (now 13 of 25)
5. Alex later visits event page (forgot they purchased)
6. Clicks "Purchase Ticket"
7. Selects "Weekend Days" ticket (Saturday + Sunday)
8. System validates: ❌ **OVERLAP DETECTED**
   - Alex has "Full Workshop" covering Saturday
   - Alex has "Full Workshop" covering Sunday
9. Error message displayed:

```
Cannot purchase this ticket type.

You already have a ticket that includes the Saturday and Sunday sessions.

Your existing ticket:
• Full Workshop (Friday-Sunday) - Purchased Dec 1, 2025

To attend only specific sessions, please cancel your existing
ticket first and then purchase session-specific tickets.
```

10. Purchase button disabled for "Weekend Days" ticket type
11. "Weekend Days" shows badge: "Already Purchased (Covered by Full Workshop)"
12. Alex realizes mistake, does not attempt purchase

**Capacity Unchanged** (no duplicate purchase occurred)

---

### Scenario 3: Sold-Out Session Handling

**Event**: Same "Rope Suspension Intensive"

**Current Capacity**:
- Friday: 0 of 20 available (SOLD OUT)
- Saturday: 8 of 25 available
- Sunday: 10 of 25 available

**User Journey**:

1. **Jordan** (vetted member) views event page
2. Sees capacity display:
   - Friday: **SOLD OUT**
   - Saturday: 8 spots available
   - Sunday: 10 spots available
3. "Full Workshop" ticket type shows: **UNAVAILABLE - Friday session sold out**
4. "Friday Only" ticket type shows: **SOLD OUT**
5. "Weekend Days" ticket type shows: **AVAILABLE** (Saturday + Sunday)
6. Jordan selects "Weekend Days" ticket ($80)
7. System validates:
   - Saturday: 8 available ✅
   - Sunday: 10 available ✅
8. Jordan completes purchase ✅
9. Capacity updates:
   - Friday: 0 of 20 (unchanged)
   - Saturday: 7 of 25 (decreased)
   - Sunday: 9 of 25 (decreased)

**Messaging Clarity**:
- Clear indication which sessions are sold out
- Ticket types automatically disabled if any included session sold out
- Alternative ticket types highlighted (Weekend Days still available)

---

### Scenario 4: Admin Session Capacity Management

**Event**: Teacher creating "Shibari Fundamentals" workshop

**Admin Configuration Flow**:

1. **Admin/Teacher** creates new event "Shibari Fundamentals"
2. Adds 2 sessions:
   - **Session 1**: Saturday 2pm-5pm, Venue: "Small Studio" (15 capacity)
   - **Session 2**: Sunday 10am-1pm, Venue: "Main Hall" (30 capacity)
3. Creates ticket types:
   - **"Saturday Only"**: $50-$75 sliding scale
     - Sessions: Saturday ✅
     - Quantity Available: 15 (matches session capacity)
   - **"Sunday Only"**: $50-$75 sliding scale
     - Sessions: Sunday ✅
     - Quantity Available: 30 (matches session capacity)
   - **"Full Workshop"**: $90-$130 sliding scale
     - Sessions: Saturday ✅ + Sunday ✅
     - Quantity Available: 15 (limited by Saturday session)
     - ⚠️ System warning: "Saturday session capacity is 15. Setting ticket quantity to 15."
4. Admin saves event configuration ✅
5. Event published with session-based ticketing

**Capacity Validation**:
- System prevents "Full Workshop" quantity > 15 (Saturday session limit)
- Admin sees clear warning if attempting to exceed session capacity
- Recommendation displayed: "To sell more Full Workshop tickets, increase Saturday session capacity"

**Mid-Event Capacity Adjustment**:

1. Saturday session proves popular (10 of 15 tickets sold)
2. Venue manager approves capacity increase to 20
3. Admin edits event, updates Saturday session capacity: 15 → 20
4. System validates: 10 tickets sold < 20 new capacity ✅
5. "Saturday Only" quantity increased: 15 → 20
6. "Full Workshop" quantity increased: 15 → 20
7. Changes saved, capacity immediately available ✅

**Attempt to Decrease Capacity** (Error Case):

1. Admin attempts to reduce Sunday capacity: 30 → 20
2. System checks: 25 tickets sold for Sunday session
3. ❌ **Error**: "Cannot reduce capacity to 20. 25 tickets already sold for Sunday session."
4. Admin cancels change, capacity remains 30

---

### Scenario 5: RSVP for Multi-Session Social Event

**Event**: "Rope Night Social Series" (Free social event)
**Sessions**:
- Friday 7pm-10pm: "Social Rope Practice" (30 capacity)
- Saturday 6pm-9pm: "Rope Jam & Discussion" (25 capacity)

**RSVP Options**:
- RSVP for Friday session only
- RSVP for Saturday session only
- RSVP for both sessions

**User Journey**:

1. **Taylor** (vetted member) views social event page
2. Sees session options:
   - Friday: 18 of 30 spots available
   - Saturday: 12 of 25 spots available
3. Clicks "RSVP Now"
4. Modal shows session selection:
   - ☐ Friday Social Rope Practice
   - ☐ Saturday Rope Jam & Discussion
5. Taylor selects both sessions ✅✅
6. Accepts safety waiver, age verification
7. Clicks "Confirm RSVP"
8. System creates EventAttendance for both sessions
9. Confirmation: "RSVP confirmed for Friday and Saturday sessions"
10. Capacity updates:
    - Friday: 17 of 30 available
    - Saturday: 11 of 25 available
11. Taylor receives email with both session details

**Optional Donation Ticket**:

1. Taylor sees: "Optional $10 suggested donation to support the space"
2. Clicks "Purchase Ticket"
3. Completes $10 PayPal payment
4. TicketPurchase created (linked to existing EventAttendance)
5. Dashboard shows: "RSVP: Friday + Saturday | Ticket: $10 donation"

**Note**: RSVP remains free, ticket is optional donation (social event pattern maintained)

---

### Scenario 6: Mobile Ticket Purchase Experience

**Device**: iPhone 13, Safari browser
**Event**: "Rope Suspension Intensive" (3-day workshop)

**Mobile User Journey**:

1. **Morgan** opens event page on phone
2. Taps "Purchase Ticket" button
3. Mobile-optimized modal appears:
   - Large touch-friendly session cards
   - Session 1: "Friday 7pm Fundamentals" - 12 spots left ✅
   - Session 2: "Saturday 10am Advanced" - 3 spots left ⚠️ (low availability badge)
   - Session 3: "Sunday 10am Practice" - 15 spots left ✅
4. Swipes through ticket type options (card carousel):
   - **Card 1**: "Friday Only" - $35-$50
     Includes: Friday Fundamentals ✅
     Available: YES
   - **Card 2**: "Weekend Days" - $75-$100
     Includes: Saturday Advanced ✅, Sunday Practice ✅
     Available: YES (3 spots left badge)
   - **Card 3**: "Full Workshop" - $100-$150
     Includes: All sessions ✅✅✅
     Available: YES (3 spots left badge)
5. Morgan taps "Weekend Days" card
6. Sliding scale selector (large touch target):
   - Drags slider to $85
   - Live preview: "You selected $85"
7. Taps "Continue to Payment"
8. Redirected to PayPal mobile checkout
9. Completes payment with Touch ID
10. Returns to confirmation page (mobile-optimized):
    - "Ticket Confirmed!" with checkmark animation
    - Session badges: "Saturday 10am", "Sunday 10am"
    - Add to Calendar button (large)
    - View in Dashboard button

**Mobile Performance**:
- Page load: <2 seconds on 4G
- Session capacity checks: <100ms
- Smooth scrolling, no lag
- Touch targets ≥44px (Apple guidelines)

---

## Questions for Product Manager

### Pricing Strategy

- [ ] **Session-Specific Pricing Ranges**: Should different sessions have different sliding scale ranges?
  - Example: Friday intro session $25-$40, Saturday intensive $60-$90
  - Consideration: Complexity vs. revenue optimization

- [ ] **Bulk Purchase Discounts**: Should "Full Workshop" tickets offer discount vs. sum of individual sessions?
  - Example: Individual sessions total $150-$225, Full Workshop offered at $120-$180
  - Consideration: Incentivize full attendance vs. maximize session revenue

### Capacity Management

- [ ] **Overbooking Policy**: Should we allow intentional overbooking (airlines model)?
  - Example: Sell 32 tickets for 30-capacity session (expecting 2 no-shows)
  - Consideration: Revenue vs. customer satisfaction if everyone shows up

- [ ] **Waitlist Implementation**: Should waitlists be session-specific or event-level?
  - Deferred to future implementation, but impacts initial design

### Refund Policy

- [ ] **Partial Refund Timing**: How to handle refunds when some sessions already occurred?
  - Example: User purchased Fri+Sat+Sun, requests refund after Friday (attended 1 of 3)
  - Options: (1) No refund, (2) Prorated refund for unattended sessions, (3) Admin discretion
  - Current Implementation: Full refund only (all-or-nothing)

- [ ] **Refund Deadline Per Session**: Should each session have separate refund deadline?
  - Example: Friday 48-hour deadline (Wed midnight), Saturday 48-hour deadline (Thu midnight)
  - Current Implementation: Refund deadline based on EARLIEST session in ticket

### User Experience

- [ ] **Default Session Selection**: When purchasing, should all sessions be pre-selected?
  - Option 1: Pre-select all (user unchecks to exclude)
  - Option 2: No pre-selection (user must explicitly choose)
  - Consideration: Friction vs. clarity

- [ ] **Session Naming Convention**: Standardized naming format for sessions?
  - Example: "Day 1: Fundamentals", "Session A: Theory", "Friday Evening: Intro"
  - Recommendation: Teacher discretion with best practice guidelines

### Admin Features

- [ ] **Session Reordering**: Should admins be able to reorder session display?
  - Current: Chronological by start time (automatic)
  - Alternative: Manual sort order field

- [ ] **Cross-Event Session Reporting**: Should we track session popularity across all events?
  - Example: "Friday evening sessions average 85% capacity, Sunday mornings 62%"
  - Use Case: Help teachers optimize scheduling

---

## Quality Gate Checklist (95% Required)

### Business Requirements Quality

- [x] **All user roles addressed** (Vetted, General Member, Teacher, Admin, Guest)
- [x] **Clear acceptance criteria for each story** (Given/When/Then format)
- [x] **Business value clearly defined** (Revenue, accessibility, flexibility)
- [x] **Edge cases considered** (Overlap, sold-out, cancellations, refunds)
- [x] **Security requirements documented** (Privacy, payment security, auth)
- [x] **Compliance requirements checked** (Age verification, refund policy, consent)
- [x] **Performance expectations set** (< 100ms capacity queries, < 200ms validation)
- [x] **Mobile experience considered** (Touch-friendly, responsive, fast)
- [x] **Examples provided** (6 detailed scenarios covering happy/error paths)
- [x] **Success metrics defined** (Conversion, revenue, utilization, satisfaction)

### Domain Knowledge Application

- [x] **Session-based pricing aligns with sliding scale philosophy** (Pay what you can afford maintained)
- [x] **Safety and consent at event level** (No per-session safety requirements)
- [x] **Vetting requirements maintained** (Social events vetted-only, classes open to all members)
- [x] **PayPal integration unaffected** (Session metadata only, no payment flow changes)
- [x] **RSVP social event pattern preserved** (Free RSVP, optional donation tickets)

### Alignment with Platform Architecture

- [x] **Database schema leverages existing TicketTypeSessions** (Migration already applied 2025-12-02)
- [x] **DTO alignment strategy followed** (NSwag types, no manual interfaces)
- [x] **Backward compatibility ensured** (Single-session events unchanged)
- [x] **API design patterns consistent** (RESTful, vertical slice)
- [x] **React patterns anticipated** (TanStack Query, Mantine v7, hooks)

### Stakeholder Communication

- [x] **Questions for product manager documented** (Pricing, capacity, refunds, UX, admin)
- [x] **User impact analysis complete** (All roles, learning curve, training needs)
- [x] **Business rules explicitly stated** (9 rules with clear implementation guidance)
- [x] **Constraints and assumptions documented** (Technical, business, user behavior)

### Handoff Readiness

- [x] **Clear scope definition** (What's included, what's out of scope)
- [x] **Migration strategy outlined** (EventAttendance schema, data backfill)
- [x] **Success criteria measurable** (Quantitative and qualitative metrics)
- [x] **Risk assessment implicit** (Edge cases, error scenarios documented)

---

## Out of Scope (Deferred to Future Implementations)

### Not Included in Initial Implementation

**Group Ticket Purchases**:
- Purchasing multiple tickets in single transaction (Quantity > 1)
- Rationale: Adds complexity to session capacity calculations and refund handling
- Future Priority: MEDIUM

**Ticket Transfers Between Users**:
- Transferring ticket to another member
- Re-assignment of tickets with admin approval
- Rationale: Requires complex ownership transfer logic and audit trail
- Future Priority: LOW

**Partial Refunds for Multi-Session Tickets**:
- Refunding unattended sessions when some sessions already occurred
- Prorated refund calculations
- Rationale: Complex business logic, edge cases, financial reporting impact
- Future Priority: HIGH (high user demand)

**Waitlist Management**:
- Automatic waitlist when session sold out
- Automatic promotion when spots open
- Session-specific waitlist queues
- Rationale: Separate feature requiring notification system, queue management
- Future Priority: MEDIUM

**Dynamic Pricing**:
- Early bird pricing (discount for early registration)
- Last-minute pricing (discount close to event)
- Member tier pricing (different prices by membership level)
- Rationale: Requires pricing engine, complex business rules
- Future Priority: LOW

**Session-Level Safety Waivers**:
- Different waivers for different sessions (e.g., suspension vs. floor work)
- Per-session age requirements
- Rationale: Unlikely business need, adds significant complexity
- Future Priority: VERY LOW

**Cross-Event Session Bundles**:
- "Buy 5 Friday sessions across different events" package
- Session punch cards
- Rationale: Very complex, unclear business value
- Future Priority: VERY LOW

---

## Migration Strategy

### Existing Data Preservation

**Current Ticket Purchases**:
- All existing EventAttendance records remain valid
- Tickets purchased before migration assigned to appropriate session(s)
- No user-visible changes to historical tickets

**Data Backfill Process**:

1. **Identify Session for Each Ticket**:
   - Query: TicketPurchase → TicketType → Sessions
   - Single-session ticket: Assign to that session
   - Multi-session ticket: Create EventAttendance records for each session OR populate SessionIds array

2. **Validation**:
   - Verify 100% of existing tickets have SessionId populated
   - Check capacity calculations remain accurate
   - Ensure no data loss

3. **Rollback Plan**:
   - Database backup before migration
   - Ability to restore EventAttendance to pre-migration state
   - Feature flag to disable session-based logic if issues detected

### Deployment Strategy

**Phase 1: Database Migration** (No User Impact):
- Apply EventAttendance schema changes (SessionId field)
- Backfill existing records
- Validate data integrity
- Test capacity calculations

**Phase 2: Backend Validation** (No User Impact):
- Deploy session-level validation logic
- Feature flag OFF (use old event-level validation)
- Monitor logs for validation differences

**Phase 3: Gradual Rollout** (Controlled User Impact):
- Enable feature flag for 10% of events (new events only)
- Monitor ticket purchases, capacity updates
- Verify no errors, correct behavior

**Phase 4: Full Rollout** (All Users):
- Enable feature flag for 100% of events
- Monitor for 1 week
- Remove feature flag code (permanent implementation)

**Rollback Capability**:
- Feature flag can disable session-based logic instantly
- Fallback to event-level validation if critical bug detected
- Database migration reversible if needed

---

## Success Criteria

### Technical Success

- ✅ **Zero Breaking Changes**: Single-session events work identically to before
- ✅ **Performance Targets Met**: Session capacity queries < 100ms (p95), validation < 200ms (p95)
- ✅ **Data Integrity**: 100% of tickets have accurate session assignments
- ✅ **Test Coverage**: >90% code coverage for session validation logic
- ✅ **Zero Overselling**: No session exceeds configured capacity

### Business Success

- ✅ **Increased Revenue**: +15% ticket sales for multi-session events within 3 months
- ✅ **Member Satisfaction**: >90% positive feedback on session flexibility
- ✅ **Teacher Adoption**: >80% of multi-session events use session-specific pricing within 6 months
- ✅ **Support Reduction**: -50% "can I attend just one day?" support tickets

### User Experience Success

- ✅ **Purchase Completion**: Cart abandonment for multi-session events decreases by 25%
- ✅ **Mobile Usability**: >85% of mobile purchases complete successfully
- ✅ **Error Clarity**: Users understand why duplicate purchases blocked (measured by support tickets)
- ✅ **Session Selection**: Users correctly select intended sessions (measured by refund requests)

### Operational Success

- ✅ **Admin Efficiency**: Event configuration time does not increase (measured by admin feedback)
- ✅ **Reporting Accuracy**: Session-level revenue reports match actual ticket sales (100% accuracy)
- ✅ **Capacity Management**: Zero manual capacity corrections needed due to system errors

---

**Document Status**: Draft - Ready for Product Manager Review
**Next Steps**:
1. Product Manager review and approve business rules
2. Answer deferred questions (pricing strategy, refund policy, UX defaults)
3. Create handoff document for UI Designer (Phase 2)
4. Proceed to Functional Specifications (Phase 2)

**Related Documentation**:
- **Impact Analysis**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/research/impact-analysis.md`
- **Process Guide**: `/docs/guides-setup/ticket-rsvp-process-guide.md`
- **Database Research**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/research/database-research.md`
- **DTO Alignment Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
