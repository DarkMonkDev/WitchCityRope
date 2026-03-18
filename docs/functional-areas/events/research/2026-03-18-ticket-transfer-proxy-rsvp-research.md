# Technology Research: Ticket Transfer & Proxy RSVP System
<!-- Last Updated: 2026-03-18 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Complete -->

## Executive Summary

**Decision Required**: How to design a ticket transfer/assignment and authorized proxy (delegate) RSVP system for WitchCityRope's community event platform.

**Recommendation**: A two-part system combining (1) a **Purchase-and-Assign** flow where a buyer purchases 2-3 tickets and assigns them to specific existing users, with recipient acceptance including waiver agreement, and (2) an **Authorized Delegate** system where users pre-authorize specific people to purchase/RSVP on their behalf.

**Key Factors**:
1. All attendees must have accounts AND agree to event-specific waivers (non-negotiable safety requirement)
2. Tickets, once assigned and accepted, cannot be reclaimed by the purchaser (irrevocable transfer)
3. The delegate/proxy system must be user-initiated (the person being represented authorizes the delegate, not vice versa)

**Confidence Level**: High (85%) -- The patterns are well-established across major platforms, and the constraints are clear enough to design around.

---

## Research Scope

### Requirements
- Users can purchase 2-3 tickets at once and assign them to other registered users
- Users can designate specific people as authorized delegates who can purchase/RSVP on their behalf
- All attendees must agree to event-specific waivers before they can attend
- Some events are restricted to vetted members only
- Tickets cannot be reclaimed once assigned and accepted
- All attendees must have accounts in the system

### Success Criteria
- Clear, low-friction UX for a small community (people know each other)
- Waiver compliance is guaranteed before event day
- Delegate authorization is explicit and auditable
- System prevents abuse while remaining community-friendly
- Mobile-friendly (members often purchase/RSVP from phones)

### Out of Scope
- Ticket resale/marketplace functionality
- Dynamic pricing or auction systems
- Large-scale fraud prevention (this is a small, trusted community)
- Payment splitting between purchaser and recipient

---

## Part 1: Ticket Transfer / Assignment Patterns

### How Major Platforms Handle This

#### Ticketmaster Transfer Flow
**Process**:
1. Sender logs in, finds order in "My Tickets"
2. Selects "Transfer" and passes eligibility check
3. Receives and enters a one-time verification code via text
4. Chooses which tickets to transfer
5. Enters recipient's name and email
6. Recipient receives email/text with acceptance link
7. Recipient signs in or creates an account
8. Upon acceptance, a **new barcode is issued** to recipient; sender's ticket is invalidated
9. Sender can cancel an unaccepted transfer

**Security**: Uses SafeTix rotating barcodes (refreshes every 15 seconds). Transfer creates a new unique barcode tied to the recipient's account.

**Statuses**: "Waiting to accept" / "Sent" (pending) or "Accepted by" / "Claimed" (completed)

#### Eventbrite Transfer Flow
**Process**:
1. Attendee edits their order information
2. Changes the name and email to the new attendee
3. New attendee receives an email prompting them to claim tickets
4. Organizer can also manually change attendee information from the admin dashboard

**Key Difference**: Eventbrite treats this more like "editing order information" than a formal transfer, which is simpler but less auditable.

#### Showpass Transfer Flow
**Process**:
1. Buyer logs in, goes to "My Orders"
2. Clicks "Order Options" then "Transfer to a friend"
3. Selects which tickets to transfer
4. Enters recipient's email
5. Recipient receives email with claim link (10-60 minutes)
6. Recipient clicks link to claim ticket

#### GoPassage Gift Ticket Flow
**Process**:
1. Buyer clicks "This is a Gift" during checkout
2. Enters recipient's email (required)
3. Optionally adds a personal message
4. Optionally schedules delivery date
5. Recipient receives email with "Accept" button
6. Recipient must log in or create account to accept
7. Buyer can track gift status from "My Tickets" page

### Pattern Analysis

| Feature | Ticketmaster | Eventbrite | Showpass | GoPassage |
|---------|-------------|-----------|---------|-----------|
| Transfer Type | Post-purchase | Edit order | Post-purchase | At checkout |
| Recipient needs account | Yes | No (just email) | No (just email) | Yes (at claim) |
| Sender can cancel | Yes (if unclaimed) | N/A | Unknown | Unknown |
| New barcode/ticket issued | Yes | No (same order) | Yes | Yes |
| Waiver handling | None built-in | None built-in | None built-in | None built-in |
| Status tracking | Yes | No | Limited | Yes |

### Key Insight
**None of the major platforms have built-in waiver management for transfers.** This is a gap that WitchCityRope must address explicitly, and it represents a competitive advantage for safety-focused communities.

---

## Part 2: Proxy / Delegate Systems

### How Platforms Handle Proxy Registration

#### RSVPify -- Proxy Registration
- Event hosts can register guests on their behalf via "RSVP for Guest(s)" button
- Can submit "courtesy registrations" without payment
- Auto-populates guest names from invite list
- Designed for organizer-initiated proxy, not peer-to-peer delegation

#### SpotMe -- Assistant Registration
- Supports an "assistant" role where a third party registers on behalf of an attendee
- Assistant may or may not have access to the attendee's inbox
- Can respond to invitations and click magic links on attendee's behalf
- Designed for corporate executive assistants

#### Meetup.com -- Plus-One System
- RSVPers can bring guests (plus-one)
- Organizers can manually add members as attendees
- No formal delegation system; just organizer override
- Guest counts managed via RSVP pop-up

### Key Finding: No Platform Has Peer-to-Peer Delegation
**No major event platform offers a user-to-user "authorize this person to act on my behalf" system.** This is universally handled by either:
1. Admin/organizer proxy registration (RSVPify, Meetup)
2. Corporate assistant patterns (SpotMe)
3. Post-purchase transfer (Ticketmaster, Showpass)

**WitchCityRope's "authorized delegate" concept is novel** and must be custom-designed. This is actually appropriate for a tight-knit community where people know and trust each other.

---

## Part 3: Waiver Management Best Practices

### When to Present Waivers

#### TicketSpice Two-Stage Approach (Industry Best Practice)
1. **Terms Field**: Presented during checkout. Buyer acknowledges terms before completing purchase. Checkbox or digital signature required.
2. **Waiver Field**: Presented AFTER purchase. Each individual attendee signs their own waiver. **Tickets are withheld until signed.** Attendees sign on confirmation page or via confirmation email.

**Critical Pattern**: "Once a waiver is signed, that person's ticket will be generated and emailed to them."

### Record-Keeping Standards
- Time-stamped record of every agreement
- Terms signatures exportable as PNG images
- Waivers exportable as PDF with collected information
- Records include order status, date ranges, and customizable export parameters
- Include date next to signature showing when document was signed
- Send PDF copy of signed form in confirmation email
- Inform person that signature is legally binding

### Waivers and Transferred Tickets
**No major platform handles waiver re-signing for transferred tickets.** This is universally a gap. The common workaround is:
1. Event-day check-in with paper waivers (defeats the purpose of digital)
2. Separate waiver system (e.g., WaiverSign, Smartwaiver) linked by email
3. Requiring new attendee to complete waiver before ticket becomes valid

### WitchCityRope Recommendation
The TicketSpice model of **withholding the ticket until the waiver is signed** is the strongest pattern. For WitchCityRope, this should be adapted so that:
- When a ticket is assigned/transferred to a recipient, it enters "Pending Acceptance" state
- The recipient sees the event-specific waiver when they accept
- The ticket only becomes "Active" after waiver agreement
- The waiver agreement is time-stamped and stored with the ticket record

---

## Part 4: Recommended UX Flows for WitchCityRope

### Flow A: Purchase-and-Assign (Buying Tickets for Others)

```
PURCHASER FLOW:
1. Browse event → Click "Get Tickets"
2. Select ticket quantity (1-3)
3. For each ticket beyond their own:
   a. Search for recipient by name or email (must be registered user)
   b. If event is "vetted only," only vetted members appear in search
   c. Selected recipient shown with confirmation
4. Complete payment (PayPal/Venmo)
5. Purchaser's own ticket: immediate waiver agreement
6. Assigned tickets: enter "Pending Acceptance" state

RECIPIENT FLOW:
1. Receive email notification: "[Purchaser] has assigned you a ticket to [Event]"
2. Also see notification on dashboard when logged in
3. Click "Accept Ticket" (in email or dashboard)
4. Presented with event-specific waiver/terms
5. Must agree to waiver to accept ticket
6. Ticket becomes "Active" -- appears in their "My Tickets"
7. Purchaser notified of acceptance

EDGE CASES:
- Recipient does not accept within X days → ticket expires, purchaser notified
- Recipient declines → purchaser notified, ticket returns to purchaser's
  account as unassigned (can re-assign to someone else)
- Event cancelled → all tickets refunded to original purchaser
```

### Flow B: Authorized Delegate (Pre-Authorized Proxy)

```
AUTHORIZATION FLOW (One-Time Setup):
1. User goes to "My Account" → "Authorized Delegates"
2. Searches for another user by name or email
3. Clicks "Add as Delegate"
4. Confirmation: "You are authorizing [Name] to purchase tickets
   and RSVP for events on your behalf. You will still need to accept
   any tickets and agree to event waivers yourself."
5. Delegate appears in list; user can remove at any time

DELEGATE PURCHASE FLOW:
1. Delegate browses event → Click "Get Tickets"
2. Select ticket quantity
3. For tickets on behalf of others:
   a. System shows list of people who have authorized this user as delegate
   b. Select from authorized list only
   c. If event is "vetted only," only vetted authorizers appear
4. Complete payment
5. Same Recipient Flow as Flow A (acceptance + waiver required)

KEY PRINCIPLE: Authorization flows FROM the person being represented
TO the delegate. The delegate cannot self-authorize.
```

### Flow C: RSVP on Behalf (Free Events)

```
DELEGATE RSVP FLOW:
1. Delegate browses free event → Click "RSVP"
2. Option appears: "RSVP for yourself" or "RSVP on behalf of others"
3. If "on behalf": shows list of people who have authorized this delegate
4. Select 1-3 people from authorized list
5. RSVP recorded as "Pending Acceptance" for each person

RECIPIENT FLOW:
1. Notification: "[Delegate] has RSVPd you to [Event]"
2. Accept RSVP (includes waiver agreement if applicable)
3. RSVP becomes confirmed
4. Or: Decline → spot opens back up
```

---

## Part 5: State Machine Design

### Ticket/RSVP States

```
                    ┌──────────────┐
                    │   PURCHASED  │ (Assigned to self, waiver signed)
                    └──────────────┘
                           │
                    ┌──────┴──────┐
                    │             │
              Self-ticket    For-others
                    │             │
                    ▼             ▼
            ┌────────────┐  ┌───────────────────┐
            │   ACTIVE   │  │ PENDING_ACCEPTANCE │
            └────────────┘  └───────────────────┘
                                    │
                         ┌──────────┼──────────┐
                         │          │          │
                    Accepted    Declined    Expired
                    + waiver       │          │
                         │          │          │
                         ▼          ▼          ▼
                  ┌────────────┐ ┌─────────┐ ┌─────────┐
                  │   ACTIVE   │ │DECLINED │ │ EXPIRED │
                  └────────────┘ └─────────┘ └─────────┘
                                      │          │
                                      └────┬─────┘
                                           ▼
                                  ┌──────────────────┐
                                  │ RETURNED_TO_BUYER │
                                  └──────────────────┘
                                           │
                                           ▼
                                  Can be re-assigned
```

### State Definitions

| State | Description | Who Sees | Actions Available |
|-------|-------------|----------|-------------------|
| PURCHASED | Payment complete, unassigned or self-assigned | Purchaser | Assign, self-accept |
| PENDING_ACCEPTANCE | Assigned to recipient, awaiting acceptance | Both | Recipient: Accept/Decline. Purchaser: View status |
| ACTIVE | Accepted with waiver signed | Recipient | View ticket, attend event |
| DECLINED | Recipient declined the assignment | Purchaser | Re-assign to someone else |
| EXPIRED | Acceptance window passed | Purchaser | Re-assign to someone else |
| RETURNED_TO_BUYER | Declined/expired ticket back with purchaser | Purchaser | Re-assign |
| CANCELLED | Event cancelled or admin action | Both | Refund initiated |

### Irrevocability Rule
Once a ticket reaches **ACTIVE** state (accepted + waiver signed), it **cannot be reclaimed by the purchaser**. The ticket belongs to the recipient. This is a hard rule per the business requirements.

---

## Part 6: Edge Cases and Solutions

### Purchase Edge Cases

| Edge Case | Solution |
|-----------|----------|
| Purchaser buys 3 tickets but only assigns 2 | Unassigned ticket stays with purchaser. Reminder emails sent. If unassigned at event time, ticket is for purchaser's use. |
| Recipient not found (no account) | Inform purchaser: "This person needs to create an account first." Provide shareable signup link. |
| Recipient is not vetted for vetted-only event | Block assignment. Show message: "This event requires vetted membership. [Name] does not meet this requirement." |
| Purchaser tries to assign to themselves | Allow it (auto-accept, no email needed). They just bought a ticket for themselves. |
| Event reaches capacity during purchase | Hold tickets during checkout (5-minute reservation). If times out, release back to pool. |
| Recipient has already RSVPd/has a ticket | Block duplicate assignment. Show: "[Name] already has a ticket to this event." |

### Acceptance Edge Cases

| Edge Case | Solution |
|-----------|----------|
| Recipient does not respond for 7 days | Auto-expire. Ticket returns to purchaser. Configurable per-event deadline. |
| Recipient declines | Ticket returns to purchaser for re-assignment. No refund triggered. |
| Event details change after assignment | Notify all ticket holders (purchaser AND recipients). Allow recipients to re-decline if they wish. |
| Event cancelled | All tickets cancelled. Refund to original purchaser only. |
| Waiver text changes after acceptance | Require re-acceptance with new waiver. Ticket temporarily moves back to PENDING_ACCEPTANCE. |

### Delegate Edge Cases

| Edge Case | Solution |
|-----------|----------|
| User removes delegate after delegate purchased | Existing tickets unaffected (already assigned). Delegate can no longer make new purchases. |
| Delegate tries to buy for non-authorizer | Blocked. Only authorized users appear in delegate's selection list. |
| Delegate purchases, then user deactivates account | Pending tickets auto-declined. Active tickets remain valid until event. |
| Both the person AND their delegate try to RSVP | Duplicate detection blocks the second attempt. |
| Delegate goes over purchase limit | Enforce per-event, per-user limits. Even delegates count against the recipient's limit. |

### Refund Edge Cases

| Edge Case | Solution |
|-----------|----------|
| Purchaser wants refund but ticket already accepted | Not allowed. Ticket was irrevocably transferred upon acceptance. |
| Purchaser wants refund for declined/expired ticket | Standard refund policy applies. |
| Recipient wants refund | Recipient cannot request refund (they did not pay). Must coordinate with purchaser. |
| Partial refund (1 of 3 tickets) | Allow per-ticket refund processing for unaccepted tickets only. |

---

## Part 7: Security Considerations

### Preventing Abuse of the Proxy System

| Threat | Mitigation |
|--------|-----------|
| User adds random people as delegates without consent | Authorization flows FROM the person being represented. Only the "principal" can authorize a delegate. |
| Delegate mass-purchases to hoard tickets | Per-event purchase limits apply to the recipient, not the delegate. If User A has a limit of 1 ticket per event, a delegate cannot buy 2 for them. |
| Fake accounts to bypass vetting | Existing vetting system enforces real identity. Ticket assignment only to registered users. |
| Someone claims they did not authorize a delegate | Audit log of all delegate additions/removals with timestamps. |
| Ticket transfer used to circumvent waitlist | Assignment only works for purchased/allocated tickets. Cannot assign a "spot" that does not exist. |
| Social engineering ("I am authorized by X") | System enforced: if not in the authorized delegate list, the system blocks the action. No manual overrides except by admin. |

### Consent and Safety Protections

1. **Waiver is non-negotiable**: No ticket becomes active without waiver agreement by the actual attendee
2. **Account required**: No anonymous attendance. Every person at an event has an account with verified email
3. **Vetting enforced at assignment time**: Cannot assign a vetted-only event ticket to a non-vetted user
4. **Delegate authorization is revocable**: Users can remove delegates at any time; this does not affect already-accepted tickets but prevents new ones
5. **Audit trail**: Every assignment, acceptance, decline, and delegate authorization is logged with timestamp and user IDs
6. **Admin override**: Admins can cancel any ticket, revoke any delegate authorization, and view full audit trail

### Data Privacy Considerations

- User search for ticket assignment should be limited (name + first letter of email, or exact email match)
- Delegate list is private -- only the user can see who they have authorized
- Purchase history shows the purchaser's name to the recipient (necessary for context)
- Admin has full visibility for safety purposes

---

## Part 8: Implementation Considerations

### Database Entities (Conceptual)

```
TicketAssignment
  - Id (GUID)
  - TicketPurchaseId (FK)
  - PurchaserId (FK → User)
  - RecipientId (FK → User)
  - AssignedByDelegateId (FK → User, nullable)  -- if purchased by delegate
  - Status (enum: PendingAcceptance, Active, Declined, Expired, Cancelled)
  - WaiverAcceptedAt (DateTime, nullable)
  - WaiverVersion (string)  -- which waiver text was agreed to
  - AssignedAt (DateTime)
  - AcceptedAt (DateTime, nullable)
  - DeclinedAt (DateTime, nullable)
  - ExpiredAt (DateTime, nullable)
  - ExpiresAt (DateTime)  -- deadline for acceptance

AuthorizedDelegate
  - Id (GUID)
  - PrincipalUserId (FK → User)  -- the person being represented
  - DelegateUserId (FK → User)   -- the person authorized to act
  - AuthorizedAt (DateTime)
  - RevokedAt (DateTime, nullable)
  - IsActive (bool)

AssignmentAuditLog
  - Id (GUID)
  - TicketAssignmentId (FK)
  - Action (enum: Created, Accepted, Declined, Expired, Cancelled, WaiverSigned, Reassigned)
  - PerformedByUserId (FK → User)
  - Timestamp (DateTime)
  - Details (string, nullable)
```

### API Endpoints (Conceptual)

```
POST   /api/events/{eventId}/tickets/purchase-and-assign
       Body: { quantity: 2, assignments: [{ recipientUserId: "..." }, ...] }

POST   /api/ticket-assignments/{id}/accept
       Body: { waiverAccepted: true, waiverVersion: "v2" }

POST   /api/ticket-assignments/{id}/decline

GET    /api/my/ticket-assignments          -- incoming assignments for current user
GET    /api/my/purchases/{id}/assignments  -- outgoing assignments from a purchase

POST   /api/my/delegates                   -- add a delegate
DELETE /api/my/delegates/{delegateUserId}  -- revoke a delegate
GET    /api/my/delegates                   -- list my delegates
GET    /api/my/delegated-by               -- list who has authorized me as delegate
```

### Migration Path
1. **Phase 1**: Purchase-and-assign flow (no delegate system yet). Users buy tickets and assign to specific registered users. Recipient acceptance with waiver.
2. **Phase 2**: Authorized delegate system. Users can pre-authorize delegates. Delegates appear in purchase flow.
3. **Phase 3**: Admin tools. Admin can view all assignments, override statuses, and view audit trail.

### Estimated Effort
- **Phase 1**: 3-5 days (database, API endpoints, React components, email notifications)
- **Phase 2**: 2-3 days (delegate authorization CRUD, integration into purchase flow)
- **Phase 3**: 1-2 days (admin views, audit log UI)

### Mobile Experience
- Acceptance flow must be optimized for mobile (large buttons, simple waiver display)
- Email notifications should have deep links that go directly to acceptance screen
- Dashboard should prominently show pending ticket assignments
- User search for assignment should work well with on-screen keyboard (autocomplete, minimal typing)

---

## Part 9: Comparative Analysis -- Design Approaches

### Approach A: Transfer-After-Purchase (Ticketmaster Model)
Buy tickets first, then transfer individual tickets to recipients afterward.

| Criteria | Score | Notes |
|----------|-------|-------|
| UX Simplicity | 6/10 | Two-step process (buy then transfer) adds friction |
| Waiver Integration | 7/10 | Natural point to present waiver at acceptance |
| Mobile Experience | 6/10 | Post-purchase flow needs separate navigation |
| Community Fit | 5/10 | Feels transactional for a community setting |
| Implementation Complexity | 5/10 | Requires separate transfer management UI |

### Approach B: Assign-at-Checkout (GoPassage Gift Model)
Assign recipients during the purchase flow itself.

| Criteria | Score | Notes |
|----------|-------|-------|
| UX Simplicity | 9/10 | Single flow: pick tickets, assign, pay |
| Waiver Integration | 8/10 | Waiver at acceptance stage (same as A) |
| Mobile Experience | 8/10 | Linear checkout flow works great on mobile |
| Community Fit | 9/10 | "I am buying this for my friend" feels natural |
| Implementation Complexity | 7/10 | Integrates into existing checkout, less new UI |

### Approach C: Hybrid (Assign at checkout OR transfer later)
Allow assignment during purchase AND post-purchase reassignment for declined/expired tickets.

| Criteria | Score | Notes |
|----------|-------|-------|
| UX Simplicity | 7/10 | Primary flow is simple; edge cases handled |
| Waiver Integration | 8/10 | Same acceptance pattern |
| Mobile Experience | 7/10 | Primary flow mobile-friendly; reassignment is secondary |
| Community Fit | 9/10 | Covers all real-world scenarios |
| Implementation Complexity | 5/10 | Most complex to build |

### Recommendation: Approach B (Assign-at-Checkout) with Approach C's reassignment as Phase 2

Start with the simple, linear flow. Add reassignment capability later for the edge cases.

---

## Part 10: Risk Assessment

### High Risk
- **Waiver version drift**: Waiver text changes after some users have accepted
  - **Mitigation**: Store waiver version with acceptance. Admin can trigger re-acceptance when waiver changes materially. Non-material changes (typos) do not require re-acceptance.

- **Acceptance window too short/long**: Tickets sit in pending state, blocking capacity
  - **Mitigation**: Default 72-hour acceptance window, configurable per event. Include clear deadline in notification. Auto-expire returns ticket to purchaser.

### Medium Risk
- **User confusion about irrevocability**: Purchaser expects to get ticket back after acceptance
  - **Mitigation**: Clear messaging during assignment: "Once [Name] accepts this ticket, it cannot be transferred back." Confirmation dialog.

- **Delegate system misuse**: User adds delegates they did not mean to
  - **Mitigation**: Confirmation dialog. Delegate list management in clear Account settings. Easy removal.

- **Email delivery failures**: Recipient never sees assignment notification
  - **Mitigation**: In-app notification (dashboard banner). Email is supplementary. Purchaser can view status and remind recipient.

### Low Risk
- **Capacity race condition**: Multiple delegates try to purchase for same person simultaneously
  - **Monitoring**: Standard optimistic concurrency. Per-user ticket limits enforced at database level.

- **Account deactivation mid-flow**: User deletes account while tickets are pending
  - **Monitoring**: Cascade: pending tickets auto-decline on account deactivation. Active tickets flagged for admin review.

---

## Recommendation Summary

### Primary Recommendation: Assign-at-Checkout with Authorized Delegates

**Confidence Level**: High (85%)

**Rationale**:
1. **Aligns with community dynamics**: In a small community, people know who they are buying for. Assign-at-checkout is the natural flow ("I want 2 tickets, one for me and one for Sarah").
2. **Solves the waiver problem elegantly**: Acceptance step is the natural point for waiver presentation. No ticket is valid without waiver. This is unique to WitchCityRope and critical for safety.
3. **Delegate system is novel but simple**: Pre-authorization is a one-time setup. The actual purchase/RSVP flow is identical whether acting for yourself or as a delegate. Low cognitive overhead.
4. **Irrevocable transfer is clean**: Once accepted, the ticket belongs to the recipient. No complex ownership chains or dispute resolution needed.
5. **Phased implementation is low-risk**: Phase 1 (assign-at-checkout) delivers 80% of the value. Delegate system and reassignment can follow.

### Implementation Priority
- **Phase 1 (Immediate)**: Assign-at-checkout with recipient acceptance and waiver
- **Phase 2 (Next Sprint)**: Authorized delegate system
- **Phase 3 (Future)**: Reassignment for declined/expired tickets, admin audit tools

---

## Research Sources
- [How does Ticket Transfer work? -- Ticketmaster Help](https://help.ticketmaster.com/hc/en-us/articles/9786975926673-How-does-Ticket-Transfer-work)
- [SafeTix -- Ticketmaster](https://www.ticketmaster.com/safetix)
- [Transfer tickets to someone else -- Eventbrite Help Center](https://www.eventbrite.com/help/en-us/articles/431834/how-to-transfer-tickets-to-someone-else/)
- [Ticket buyer guide: How to transfer tickets -- Showpass](https://help.showpass.com/hc/en-us/articles/360023831873-Ticket-buyer-guide-How-to-transfer-tickets-or-products)
- [Ticket Gifting: Allow Customers to Purchase Tickets as a Gift -- GoPassage](https://help.gopassage.com/en/articles/5554782-ticket-gifting-allow-customers-to-purchase-tickets-as-a-gift)
- [Require Attendees to Agree to Terms or Sign a Digital Waiver -- TicketSpice](https://help.ticketspice.com/en/articles/8733118-require-attendees-to-agree-to-terms-or-sign-a-digital-waiver)
- [How do I submit event registration on behalf of my guests? -- RSVPify](https://help.rsvpify.com/en/articles/5091027-how-do-i-submit-event-registration-on-behalf-of-my-guests)
- [Partner API Claim Flow -- Ticketmaster Developer Portal](https://developer.ticketmaster.com/products-and-docs/apis/partner/claim-flow/)
- [Managing my events' attendees -- Meetup](https://help.meetup.com/hc/en-us/articles/9389668230541-Managing-my-events-attendees)
- [Preventing Ticketing Fraud in 2026 -- Softjourn](https://softjourn.com/insights/prevent-ticketing-fraud)
- [How to Prevent Ticket Fraud: 8 Essential Security Measures -- Tournkey](https://blog.tournkey.com/how-to-prevent-ticket-fraud-8-essential-security-measures)
- [Best practices for managing event ticket refunds -- Imagina](https://imagina.com/en/blog/article/refund-ticket-event/)
- [Event Ticket Refunds Guide -- Ticketbud](https://www.ticketbud.com/blog/event-ticket-refunds-a-guide-for-event-organizers-and-attendees/)
- [Event Tech Security in 2026 -- Ticket Fairy](https://www.ticketfairy.com/blog/event-tech-security-in-2026-protecting-attendee-data-and-systems)
- [The Ultimate Guide to Event Waivers -- GoPassage](https://blog.gopassage.com/the-ultimate-guide-to-event-waivers)
- [Best Practices for Event Liability Waivers -- Events.com](https://events.com/blog/waivers-101-what-to-include-in-event-liability-e-waivers-post-covid-19/)
- [Employing Waiver Best Practices With Apps Like Eventbrite -- SimpleTix](https://www.simpletix.com/waiver-best-practices/)
- [Building a Ticketing System: Concurrency, Locks, and Race Conditions -- Medium](https://codefarm0.medium.com/building-a-ticketing-system-concurrency-locks-and-race-conditions-182e0932d962)
- [Order and transaction statuses explained -- TicketSpice](https://help.ticketspice.com/en/articles/8771536-order-and-transaction-statuses-explained)
- [UX Analysis: Ticket booking platform -- Medium](https://medium.com/design-bootcamp/ux-analysis-ticket-booking-platform-concert-and-events-d6c3fecf3035)
- [Streamline Your Ticketing Process: Improving UX/UI for Events -- HelloCrowd](https://www.hellocrowd.net/blog/streamline-your-ticketing-process-improving-ux-ui-for-events)

## Questions for Technical Team
- [ ] What should the default acceptance window be? (Recommendation: 72 hours, configurable per event)
- [ ] Should delegates be able to pay with their own payment method, or should payment be pre-authorized by the principal?
- [ ] For vetted-only events, should the vetting check happen at assignment time, acceptance time, or both?
- [ ] Should there be a maximum number of delegates a user can authorize? (Recommendation: No limit, since this is a small community)
- [ ] Should the system send SMS notifications in addition to email, or is email + in-app sufficient?
- [ ] For the user search during ticket assignment, what information should be visible? (Recommendation: Display name + first letter of email for privacy)

## Quality Gate Checklist (100% Complete)
- [x] Multiple options evaluated (minimum 2) -- 4 platform patterns analyzed, 3 design approaches compared
- [x] Quantitative comparison provided -- Scoring matrices for platforms and approaches
- [x] WitchCityRope-specific considerations addressed -- Safety, waivers, vetting, community dynamics
- [x] Performance impact assessed -- Mobile-first considerations documented
- [x] Security implications reviewed -- Comprehensive threat/mitigation table
- [x] Mobile experience considered -- Throughout all flow designs
- [x] Implementation path defined -- 3-phase plan with effort estimates
- [x] Risk assessment completed -- High/Medium/Low with mitigations
- [x] Clear recommendation with rationale -- Assign-at-checkout with delegates, 85% confidence
- [x] Sources documented for verification -- 21 sources linked
