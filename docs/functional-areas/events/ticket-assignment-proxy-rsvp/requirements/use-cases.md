# Use Cases: Ticket Assignment & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Purpose: Detailed use cases with user flows for implementation reference -->

## Actors

| Actor | Description |
|-------|-------------|
| **Principal** | A registered user who authorizes someone else to act on their behalf |
| **Delegate** | A registered user who has been authorized to buy tickets/RSVP for a Principal |
| **Admin** | An administrator with full system access |
| **System** | Automated processes (email triggers, scheduled jobs) |

A user can be BOTH a Principal and a Delegate simultaneously (for different people).

---

## UC-001: Setting Up Authorized Contacts

**Actor:** Any registered user (as Principal)
**Preconditions:** User is logged in, has an active account
**Trigger:** User navigates to Profile Settings

### Main Flow

1. Principal goes to **Profile Settings** → new **"Authorized Contacts"** tab
2. Page shows two sections:
   - **"People who can act on your behalf"** (Delegates the Principal has authorized)
   - **"People you can act for"** (Principals who have authorized this user as Delegate)
3. In the first section, Principal clicks **"Add Contact"**
4. A search field appears - Principal types a scene name
5. System shows matching registered users (scene name only, filtered as they type)
6. Principal selects a user from results
7. System creates the Authorized Contact relationship
8. The selected user (now Delegate) receives a notification/email: "[Principal SceneName] has authorized you to purchase tickets and RSVP on their behalf"
9. The relationship appears on both users' profile pages

### Alternative Flows

**AF-1: User not found**
- 5a. No matching scene names → Show "No members found" message
- 5b. Principal can try a different search term

**AF-2: Self-authorization attempt**
- 6a. Principal selects themselves → Show error "You cannot authorize yourself"

**AF-3: Duplicate authorization**
- 6b. Principal selects someone already authorized → Show "This person is already an authorized contact"

### Post-Conditions
- AuthorizedContact record created in database
- Both Principal and Delegate can see the relationship on their profiles

---

## UC-002: Revoking Authorized Contact

**Actor:** Principal
**Preconditions:** An active Authorized Contact relationship exists

### Main Flow

1. Principal goes to Profile Settings → Authorized Contacts tab
2. Sees list of their authorized Delegates
3. Clicks **"Remove"** next to a Delegate's name
4. Confirmation dialog: "Remove [Delegate SceneName]? They will no longer be able to purchase tickets or RSVP on your behalf. This does not affect any tickets already purchased."
5. Principal confirms
6. Authorized Contact relationship is deactivated (soft delete for audit trail)
7. Delegate is notified (optional - design decision)

### Post-Conditions
- Relationship marked as inactive
- Delegate can no longer buy tickets/RSVP for this Principal
- Existing purchased/assigned tickets are NOT affected

---

## UC-003: Purchasing Multiple Tickets (Assign at Checkout)

**Actor:** Delegate (who has been authorized by at least one Principal)
**Preconditions:** Delegate is logged in, event has tickets available, Delegate has authorized contacts
**Trigger:** Delegate navigates to event page and clicks "Get Tickets"

### Main Flow

1. Delegate arrives at the **Event Payment Page** (existing 3-step checkout)
2. **Step 1 - Select Tickets:**
   - Ticket types displayed (same as today)
   - Each ticket type now shows a **quantity selector** (1 to MaxQuantityPerPurchase, default shows 1)
   - Delegate selects quantity (e.g., 2 tickets for "Full Event")
   - If quantity > 1: For each additional ticket, a **"Assign to"** dropdown appears
   - Dropdown shows ONLY the Principals who have authorized this Delegate
   - **If event is VettedMembersOnly:** Only Principals with `VettingStatus=Approved` appear in dropdown
   - Delegate can leave "Assign to" as "Assign later" (optional at checkout)
   - Delegate can also assign to a specific Principal from the dropdown
   - System checks: assignee doesn't already have a ticket for this event/session
   - If sliding scale: ONE slider applies to the entire purchase
3. **Step 2 - Payment:**
   - Payment summary shows total for all tickets
   - Delegate accepts event waiver for their OWN ticket only
   - Proceeds through CC or PayPal payment (same atomic checkout)
4. **Step 3 - Confirmation:**
   - Shows all purchased tickets with assignment status
   - "Your ticket: Active"
   - "Ticket for [Principal SceneName]: Pending Acceptance" (if assigned)
   - "Unassigned ticket: You can assign this later from your dashboard" (if not assigned)

### Behind the Scenes (on successful payment)

1. One `TicketPurchase` record created per ticket (each with same `IdempotencyKey` prefix)
2. For Delegate's own ticket:
   - `EventAttendance` (AttendanceType=Ticket, Status=Active) - waiver accepted at checkout
   - `EventAttendance` (AttendanceType=RSVP, Status=Active) - auto-created for social events
3. For assigned tickets:
   - `EventAttendance` (AttendanceType=Ticket, Status=PendingAcceptance, UserId=Principal)
   - NO RSVP created yet (created when Principal accepts)
   - Assignment notification email sent to Principal
4. For unassigned extra tickets:
   - `EventAttendance` (AttendanceType=Ticket, Status=Active, UserId=Delegate)
   - Delegate owns these until they assign them later

### Alternative Flows

**AF-1: No authorized contacts**
- 2a. Quantity selector still shows 1 (no option for more) OR shows quantity but with note: "You need authorized contacts to assign tickets. Add them in Profile Settings."

**AF-2: Event at capacity**
- 2b. Capacity check happens for ALL tickets in cart at once
- If total (existing reserved + cart quantity) > capacity → Error: "Only X tickets remaining"

**AF-3: Assignee already has ticket**
- 2c. If selected Principal already has an active/pending ticket for this event/session → Error: "[SceneName] already has a ticket for this event"

**AF-4: Payment failure**
- 3a. ALL pending records rolled back (same as current rollback behavior)

**AF-5: Session overlap**
- 2d. If ticket covers sessions the assignee already has tickets for → Warning shown

---

## UC-004: Accepting an Assigned Ticket

**Actor:** Principal (ticket recipient)
**Preconditions:** A ticket has been assigned to the Principal (Status=PendingAcceptance)
**Trigger:** Principal receives email OR sees notification on dashboard

### Main Flow

1. Principal receives email: "[Delegate SceneName] purchased a ticket for you to [Event Name]"
2. Email contains a link to the event detail page (or their dashboard)
3. Principal logs in and navigates to their dashboard
4. Dashboard shows a **"Pending Tickets"** section (new section, visible only when pending tickets exist)
5. Pending ticket card shows: Event name, date, session(s), ticket type, purchased by [Delegate SceneName]
6. Principal clicks **"Accept Ticket"**
7. Acceptance form requires:
   - ☐ "I accept the Event Waiver" (checkbox, required)
   - ☐ "I accept the Terms of Service" (checkbox, required - only if not already accepted platform-wide)
8. Principal checks both boxes and clicks **"Confirm Acceptance"**
9. System updates:
   - `EventAttendance.Status` → Active
   - `EventAttendance.EventWaiverAccepted` → true
   - `EventAttendance.EventWaiverAcceptedAt` → UTC now
   - `ApplicationUser.TermsOfServiceAccepted` → true (if newly accepted)
   - Auto-create RSVP `EventAttendance` record (same as current ticket purchase behavior)
   - Create `EventAttendee` record for check-in system
   - Create `AttendanceHistory` record for audit
10. Principal sees confirmation: "Ticket accepted! You're registered for [Event Name]"
11. Delegate's dashboard updates to show "Accepted" status

### Alternative Flows

**AF-1: Principal declines ticket**
- 6a. Principal clicks **"Decline Ticket"**
- 6b. Confirmation: "Are you sure? The ticket will be returned to [Delegate SceneName]."
- 6c. Ticket status changes to a declined state
- 6d. Delegate is notified and can reassign

**AF-2: Principal's vetting revoked**
- 6e. If event is VettedMembersOnly and Principal's vetting status has changed since assignment
- 6f. Show error: "This event requires vetted membership. Your vetting status has changed. Please contact an admin."
- 6g. Ticket remains in PendingAcceptance (Delegate is notified)

**AF-3: Principal hasn't accepted platform ToS yet**
- 7a. ToS checkbox appears because `TermsOfServiceAccepted` is false
- 7b. Both Event Waiver AND ToS must be checked before acceptance

**AF-4: Event has already passed**
- 6h. If event start time has passed → "This event has already started. Contact an admin for assistance."
- Note: Per AD-004, tickets don't auto-expire, but we should still show helpful messaging

---

## UC-005: Proxy RSVP (Free Events)

**Actor:** Delegate
**Preconditions:** Delegate is logged in, event allows RSVPs (AllowRsvps=true), Delegate has authorized contacts
**Trigger:** Delegate navigates to a free event page

### Main Flow

1. Delegate navigates to the event detail page
2. Below the standard RSVP section, a new section appears: **"RSVP for someone else"**
   - Only visible if Delegate has at least one authorized contact
3. Dropdown shows Principals who have authorized this Delegate
   - **If event is VettedMembersOnly:** Only Principals with `VettingStatus=Approved` shown
   - Only Principals who DON'T already have an active RSVP shown
4. Delegate selects a Principal and clicks **"RSVP for [Principal SceneName]"**
5. Confirmation dialog: "RSVP for [Principal SceneName]? They will need to accept the event waiver before their RSVP is confirmed."
6. Delegate confirms
7. System creates:
   - `EventAttendance` (AttendanceType=RSVP, Status=PendingAcceptance, UserId=Principal, CreatedBy=Delegate)
   - `AttendanceHistory` record with action "ProxyRSVPCreated"
8. Principal receives assignment notification email
9. Delegate sees confirmation: "RSVP created for [Principal SceneName]. They'll need to accept the event waiver."

### Alternative Flows

**AF-1: Principal already has RSVP**
- 3a. Principal doesn't appear in dropdown (filtered out)
- OR if race condition: 6a. Error "This person already has an RSVP for this event"

**AF-2: Event at capacity**
- 6b. Same capacity check as regular RSVP → Error "Event is at full capacity"

**AF-3: No authorized contacts**
- 2a. "RSVP for someone else" section not shown

---

## UC-006: Accepting a Proxy RSVP

**Actor:** Principal (RSVP recipient)
**Preconditions:** A proxy RSVP has been created for the Principal (Status=PendingAcceptance)

### Main Flow

Same as UC-004 (Accepting an Assigned Ticket) but for RSVPs:
1. Principal receives email notification
2. Dashboard shows pending RSVP in "Pending RSVPs" section
3. Must accept Event Waiver + ToS
4. On acceptance: RSVP becomes Active, EventAttendee created

---

## UC-007: Post-Purchase Ticket Assignment (From Dashboard)

**Actor:** Delegate (ticket purchaser)
**Preconditions:** Delegate owns unassigned extra tickets
**Trigger:** Delegate navigates to their dashboard

### Main Flow

1. Delegate goes to their **"My Tickets"** section on the dashboard
2. Sees all purchased tickets grouped by event
3. Unassigned tickets show an **"Assign"** button
4. Delegate clicks "Assign"
5. Dropdown shows Principals who have authorized this Delegate
   - Filtered by vetting status if event is VettedMembersOnly
   - Filtered to exclude anyone who already has a ticket for this event
6. Delegate selects a Principal
7. System:
   - Changes `EventAttendance.UserId` from Delegate to Principal
   - Changes `EventAttendance.Status` to PendingAcceptance
   - Sets `EventAttendance.EventWaiverAccepted` to false (recipient must accept)
   - Creates `AttendanceHistory` record: "TicketAssigned"
   - Sends assignment notification email to Principal
8. Delegate sees the ticket now shows "Assigned to [Principal SceneName] - Pending Acceptance"

### Alternative Flows

**AF-1: Ticket for passed event**
- 3a. If all sessions for this ticket have passed → "Assign" button not shown

---

## UC-008: Reassigning a Declined Ticket

**Actor:** Delegate (original purchaser)
**Preconditions:** A ticket was declined by the original assignee

### Main Flow

1. Delegate's dashboard shows the declined ticket with status "Declined by [Principal SceneName]"
2. Delegate clicks **"Reassign"**
3. Same flow as UC-007 from step 5 onwards
4. Previous assignee no longer appears in dropdown (excluded)
5. New assignment created, new notification email sent

### Edge Cases
- Maximum reassignment limit: Not currently enforced, but `AttendanceHistory` tracks all reassignments for audit
- If the purchaser wants to use the ticket themselves: They can choose not to reassign

---

## UC-009: Viewing Assigned Ticket/RSVP Status

**Actor:** Delegate (purchaser/RSVP creator)
**Preconditions:** Delegate has purchased tickets for others or created proxy RSVPs

### Main Flow

1. Delegate goes to dashboard → "My Tickets" or "My Events" section
2. Each ticket/RSVP shows:
   - Event name, date, session(s)
   - **Status badge:** "Active (You)", "Pending Acceptance ([SceneName])", "Declined", "Assigned to [SceneName]"
   - For accepted tickets: Checkmark showing waiver was signed
3. Delegate CANNOT reclaim active (accepted) tickets
4. Delegate CAN reassign declined tickets (UC-008)
5. Delegate CAN assign unassigned tickets (UC-007)

---

## UC-010: Admin Ticket Assignment

**Actor:** Administrator
**Preconditions:** Admin is logged in with Administrator role

### Main Flow

1. Admin navigates to event management → attendee/participation section
2. Admin clicks **"Assign Ticket"** (new button)
3. Admin searches for ANY user (not limited to authorized contacts)
4. Admin selects a user and a ticket type
5. System creates the ticket assignment (same PendingAcceptance flow)
6. Recipient gets notification email
7. Recipient must still accept waiver + ToS (admin can't bypass this)

### Differences from User Flow
- No authorized contacts restriction
- No quantity limits
- Can assign to any registered user
- Creates a "comp" ticket (no payment) or links to existing purchase
- Audit trail shows admin who made the assignment

---

## UC-011: Reminder Email for Unaccepted Tickets/RSVPs

**Actor:** System (automated)
**Preconditions:** Event is happening tomorrow, there are PendingAcceptance tickets/RSVPs

### Main Flow

1. Scheduled job runs daily (or checks upcoming events)
2. For each event happening within 24 hours:
   - Query all `EventAttendance` where `Status=PendingAcceptance`
3. For each pending attendance:
   - Send reminder email to the assignee: "Reminder: You have a ticket/RSVP for [Event Name] tomorrow that needs your acceptance"
   - Email includes direct link to accept
4. Log reminder sent (prevent duplicate reminders)

### New Recipient Group
- **"Unaccepted Ticket/RSVP Holders for Upcoming Events"**
- Filter: `EventAttendance.Status = PendingAcceptance` AND event session starts within 24 hours
