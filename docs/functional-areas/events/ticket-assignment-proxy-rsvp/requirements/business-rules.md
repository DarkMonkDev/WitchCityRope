# Business Rules: Ticket Assignment & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Purpose: Comprehensive business rules and constraints for implementation -->

## Authorization Rules

### BR-001: Authorization Direction
Authorization flows FROM the Principal TO the Delegate. Only the Principal can grant authorization. The Delegate cannot request or self-assign authorization.

### BR-002: Single Authorization Level
Authorization covers both ticket purchasing AND RSVP proxy. There is no granular permission split. If you're authorized, you can do both.

### BR-003: Self-Authorization Blocked
A user cannot add themselves as their own authorized contact.

### BR-004: Mutual Authorization Allowed
User A can authorize User B, and User B can independently authorize User A. These are separate relationships.

### BR-005: Revocation Does Not Affect Existing Tickets
Revoking an authorized contact relationship does NOT affect tickets or RSVPs already purchased/created. Those follow their normal lifecycle.

### BR-006: Account Required
Both Principal and Delegate must have registered accounts in the system. There is no anonymous ticket assignment.

---

## Ticket Purchase Rules

### BR-010: Configurable Maximum Quantity
Each `TicketType` has a `MaxQuantityPerPurchase` field (default: 3). The checkout quantity selector ranges from 1 to this value.

### BR-011: Default Quantity Display
The checkout defaults to showing quantity 1. Users actively choose to increase quantity.

### BR-012: One Ticket Per Assignee Per Event/Session
An assignee cannot receive a ticket for an event/session they already have an active or pending ticket for.

### BR-013: Capacity Check Includes Full Cart
When purchasing multiple tickets, ALL tickets in the cart count against capacity simultaneously. If total (existing reserved + cart size) exceeds capacity, the purchase is blocked.

### BR-014: Sliding Scale Applies Uniformly
When purchasing multiple tickets with sliding scale pricing, the chosen sliding scale percentage applies to ALL tickets in that purchase. One slider, one percentage, multiple tickets.

### BR-015: Payment Is Atomic
If payment fails for any reason, ALL ticket records (including assignments) are rolled back. No partial purchases.

### BR-016: Unassigned Tickets Belong to Purchaser
Extra tickets not assigned at checkout are created as Active for the purchaser. They can attend those sessions themselves or assign later.

---

## Assignment Rules

### BR-020: Assignment Requires Authorization
A user can only assign tickets to people who have authorized them as a contact (Delegate→Principal relationship required). Exception: Admin assignments (BR-040).

### BR-021: Assign at Checkout or Later
Purchasers can assign tickets during checkout or later from their dashboard. Both paths are supported.

### BR-022: Assigned Tickets Enter PendingAcceptance
When a ticket is assigned to another user, its status becomes `PendingAcceptance`. The assignee must accept before the ticket is Active.

### BR-023: Assignee Search Shows Scene Name Only
When selecting an assignee, only scene names are displayed (privacy protection).

### BR-024: Tickets Are Irrevocable Once Accepted
Once the assignee accepts the ticket (Status=Active, waiver signed), the original purchaser cannot reclaim it.

### BR-025: Declined Tickets Can Be Reassigned
If an assignee declines, the ticket returns to the purchaser who can reassign to a different authorized contact.

### BR-026: No Auto-Expiration
Assigned tickets do NOT auto-expire. Recipients can accept right up until the event. No timeout on PendingAcceptance status.

### BR-027: Reassignment Audit Trail
Every assignment and reassignment is logged in `AttendanceHistory` with full details (who assigned, who was assigned, timestamps).

---

## Waiver & Terms of Service Rules

### BR-030: Waiver Must Be Personally Accepted
The event waiver MUST be accepted by the actual attendee. A delegate CANNOT accept the waiver on behalf of someone else.

### BR-031: ToS Must Be Accepted
If the assignee has not previously accepted the platform Terms of Service, they must accept it as part of the ticket/RSVP acceptance flow.

### BR-032: Waiver Acceptance Activates Ticket
The ticket/RSVP transitions from PendingAcceptance → Active only when the waiver (and ToS if needed) are accepted.

### BR-033: Purchaser's Own Waiver at Checkout
The purchaser accepts the waiver for their own ticket at checkout (current behavior). Assigned tickets do NOT have waiver accepted at checkout.

---

## Vetting Rules

### BR-035: Vetting Checked at Assignment Time
For VettedMembersOnly events, the assignee's vetting status is checked when the ticket is assigned. Non-vetted users are excluded from the assignee dropdown.

### BR-036: Vetting Checked at Acceptance Time
For VettedMembersOnly events, the assignee's vetting status is re-checked when they try to accept. If their status changed (e.g., revoked), acceptance is blocked.

### BR-037: Vetting Filter on Contact Dropdown
The "Assign to" dropdown only shows authorized contacts who pass the vetting check for the specific event. Non-vetted contacts are hidden (not shown as disabled) for VettedMembersOnly events.

---

## Admin Rules

### BR-040: Admin Can Assign to Any User
Administrators can assign tickets to any registered user, bypassing the authorized contacts requirement.

### BR-041: Admin Assignment Still Requires Recipient Waiver
Even admin-assigned tickets require the recipient to personally accept the waiver. Admins cannot bypass waiver acceptance.

### BR-042: Admin Audit Trail
All admin ticket assignments are logged with the admin's user ID for accountability.

---

## Proxy RSVP Rules

### BR-050: Same Authorization as Tickets
Proxy RSVP uses the same Authorized Contacts system as ticket assignment.

### BR-051: Same Pending Acceptance Flow
Proxy RSVPs follow the same PendingAcceptance → waiver acceptance → Active flow as assigned tickets.

### BR-052: Same Email Patterns
Proxy RSVPs trigger the same two email templates (assignment notification + day-before reminder).

### BR-053: Delegate Cannot Accept Waiver for RSVP
Same as tickets - the waiver must be personally accepted by the Principal.

### BR-054: Capacity Check for Proxy RSVP
Proxy RSVPs count against event capacity. The PendingAcceptance RSVP reserves a spot.

---

## Email Rules

### BR-060: Assignment Notification Immediately
When a ticket is assigned or a proxy RSVP is created, the recipient gets an email immediately.

### BR-061: Reminder 1 Day Before Event
If a ticket/RSVP is still in PendingAcceptance status 1 day before the event's first session, a reminder email is sent.

### BR-062: One Reminder Per Assignment
Each pending assignment gets at most one reminder email (prevent spam).

### BR-063: New Recipient Group
A new email recipient group is created: "Users with unaccepted tickets/RSVPs for upcoming events" - filtered by PendingAcceptance status and event date within 24 hours.

---

## Edge Cases

### EC-001: Principal Deactivates Account
If the Principal's account is deactivated, their pending ticket assignments remain but cannot be accepted. Admin intervention needed.

### EC-002: Event Cancelled
If an event is cancelled, all PendingAcceptance tickets follow the same cancellation/refund flow as Active tickets.

### EC-003: Event Capacity Reduced
If capacity is reduced after tickets are in PendingAcceptance, those tickets are still valid (they were already "reserved" at purchase time).

### EC-004: Delegate Revokes Authorization After Purchase
Already-purchased/assigned tickets are not affected (BR-005). The Delegate can't assign NEW tickets to this Principal, but existing assignments proceed normally.

### EC-005: Same Person Assigned Multiple Times
A person can be assigned tickets to multiple different events, but NOT multiple tickets to the same event/session (BR-012).

### EC-006: Refund of Assigned Ticket
If a refund is processed on a ticket that was assigned (PendingAcceptance or Active), the standard refund flow applies. The assignee loses the ticket.

### EC-007: Race Condition - Two Delegates Assign to Same Principal
Unique constraint `(UserId, EventId, AttendanceType, SessionId)` where Status IN (Active, PendingAcceptance) prevents this. Second assignment fails with "already has a ticket" error.
