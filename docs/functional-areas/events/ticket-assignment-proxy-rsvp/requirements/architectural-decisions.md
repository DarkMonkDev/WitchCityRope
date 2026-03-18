# Architectural Decisions: Ticket Assignment & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Purpose: Record all design decisions made with stakeholder so future agents don't re-ask -->

## Decision Log

All decisions confirmed by stakeholder (Chad) on 2026-03-18 unless otherwise noted.

---

### AD-001: Authorization Model - "Authorized Contacts"

**Decision:** Users designate specific people who can buy tickets or RSVP on their behalf via a profile setting called "Authorized Contacts."

**Rationale:**
- Prevents arbitrary users from purchasing for anyone
- Creates a small, filtered list rather than searching all members
- Authorization flows FROM the person being represented (the "Principal") TO the person acting on their behalf (the "Delegate")
- No industry equivalent exists - this is a custom design for community safety

**Alternatives Considered:**
- Open member search (rejected - privacy concerns, too broad)
- Admin-only proxy (rejected - doesn't scale for community use)
- Invite codes/links (rejected - too complex, doesn't tie to accounts)

---

### AD-002: Pending Acceptance Model for Assigned Tickets/RSVPs

**Decision:** Assigned tickets and proxy RSVPs require the recipient to personally accept before becoming active. Tickets enter a `PENDING_ACCEPTANCE` state.

**Rationale:**
- Both Event Waiver and Terms of Service must be personally accepted by the attendee
- Legal requirement - delegate cannot accept waiver on someone else's behalf
- Safety requirement - each attendee must acknowledge community guidelines

**Implementation:** New `AttendanceStatus.PendingAcceptance = 6` enum value (after existing PendingPayment=5)

---

### AD-003: Waiver + ToS Must Be Personally Accepted

**Decision:** The recipient of an assigned ticket or proxy RSVP MUST personally check both the Event Waiver and Terms of Service before the ticket/RSVP becomes active.

**Rationale:**
- Legal compliance - waivers must be signed by the actual attendee
- Safety - each person attending must acknowledge community safety guidelines
- Existing fields (`EventWaiverAccepted`, `TermsOfServiceAccepted`) already support this

---

### AD-004: No Ticket Expiration / No Auto-Expiration

**Decision:** Assigned tickets do NOT auto-expire. Recipients can accept a ticket right up until they walk in the door at the event.

**Rationale:**
- Stakeholder preference for flexibility
- Community events are informal - last-minute decisions are normal
- Reminder email 1 day before handles the "forgot to accept" case
- Avoids complex expiration logic and edge cases

---

### AD-005: Unaccepted Tickets Follow Standard Refund Policy

**Decision:** Unaccepted tickets are treated identically to any other ticket for refund purposes. No special refund handling.

**Rationale:**
- Simplicity - one refund policy for all tickets
- The purchaser chose to buy the ticket; they own the refund liability
- Consistent with existing refund infrastructure

---

### AD-006: Configurable Maximum Ticket Quantity Per Ticket Type

**Decision:** Maximum tickets per purchase is configurable per event AND per ticket type. Default maximum is 3. Default quantity shown in checkout is 1.

**Rationale:**
- Different events may have different policies
- Different ticket types within the same event may have different limits
- Default of 3 covers the common "me + partner + friend" scenario
- Default display of 1 maintains current single-ticket UX for users who don't need multi-ticket

**Implementation:** New `MaxQuantityPerPurchase` field on `TicketType` entity (default 3).

---

### AD-007: Post-Purchase Assignment Supported

**Decision:** Purchasers can assign tickets EITHER at checkout OR later from their dashboard. They don't have to assign all tickets at purchase time.

**Rationale:**
- Common real-world scenario: "I'll buy the tickets now and figure out who's coming later"
- Reduces friction at checkout
- Unassigned tickets remain owned by the purchaser (they can attend themselves or assign later)

**Implementation:** Unassigned extra tickets are created as Active for the purchaser. Assignment can happen later via dashboard, which changes them to PendingAcceptance for the assignee.

---

### AD-008: Tickets Are Irrevocable Once Accepted

**Decision:** Once an assignee accepts a ticket (ACTIVE status with waiver signed), the original purchaser CANNOT reclaim it.

**Rationale:**
- Clean ownership model - avoids "tug of war" over tickets
- Consistent with physical ticket transfer expectations
- If the purchaser needs the ticket back, the assignee would need to cancel it (following normal cancellation rules)

---

### AD-009: Scene Name Only for Contact Search

**Decision:** When searching for members to add as Authorized Contacts, users see scene names only (no email, no real names).

**Rationale:**
- Privacy protection - real names and emails are sensitive in this community
- Scene names are the primary identifier used in the community
- Sufficient for identifying the right person in a small community

---

### AD-010: Delegate Can Both Purchase Tickets AND RSVP

**Decision:** The Authorized Contacts authorization covers BOTH ticket purchasing and RSVP proxy capabilities. It's a single authorization, not separate permissions.

**Rationale:**
- Simplicity - one authorization covers all event participation
- If you trust someone enough to buy tickets for you, you trust them to RSVP too
- Reduces UI complexity in the profile settings
- Reduces confusion about what each authorization means

---

### AD-011: Same Email Pattern for Tickets and RSVPs

**Decision:** Both assigned tickets AND proxy RSVPs follow the same two-email pattern:
1. Immediate notification when assigned
2. Reminder 1 day before the event if still not accepted

**Rationale:**
- Consistency - same experience regardless of event type
- Both require waiver acceptance, so both need the same nudging
- Simplifies email template management

---

### AD-012: Sliding Scale Applies to All Tickets in Purchase

**Decision:** When a purchaser buys multiple tickets with sliding scale pricing, the sliding scale percentage applies to ALL tickets in that purchase uniformly.

**Rationale:**
- Simpler checkout UX - one slider for the whole purchase
- The purchaser is paying for all tickets, so they set the price
- Per-ticket sliding scale would complicate the checkout significantly

---

### AD-013: Admin Can Assign Any Ticket to Any User

**Decision:** Administrators can assign any ticket (not just pre-purchased ones) to any user. This is separate from the user-facing authorized contacts system.

**Rationale:**
- Admins need flexibility for special cases (comps, corrections, etc.)
- Admin assignment should bypass the authorized contacts requirement
- Assigned-by-admin tickets should still require recipient waiver acceptance

---

### AD-014: Vetting Checked at Both Assignment and Acceptance Time

**Decision:** For vetted-members-only events, the assignee's vetting status is checked BOTH when the ticket is assigned AND when they accept it.

**Rationale:**
- Prevents edge case where someone's vetting is revoked between assignment and acceptance
- Both checks are cheap (cached vetting status)
- Safety-critical - vetted-only events must enforce vetting at all stages
