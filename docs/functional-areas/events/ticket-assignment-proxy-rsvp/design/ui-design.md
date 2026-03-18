# UI Design: Ticket Assignment & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Table of Contents

1. [Design Overview](#design-overview)
2. [Component Hierarchy](#component-hierarchy)
3. [Screen 1: Profile Settings - Authorized Contacts Tab](#screen-1-profile-settings---authorized-contacts-tab)
4. [Screen 2: Enhanced Checkout - Multi-Ticket Purchase](#screen-2-enhanced-checkout---multi-ticket-purchase)
5. [Screen 3: Event Detail Page - Proxy RSVP Section](#screen-3-event-detail-page---proxy-rsvp-section)
6. [Screen 4: Dashboard - Pending Tickets/RSVPs](#screen-4-dashboard---pending-ticketsrsvps)
7. [Screen 5: Dashboard - My Tickets (Enhanced)](#screen-5-dashboard---my-tickets-enhanced)
8. [Screen 6: Ticket/RSVP Acceptance Modal](#screen-6-ticketrsvp-acceptance-modal)
9. [Route Changes](#route-changes)
10. [State Management](#state-management)
11. [Responsive Design](#responsive-design)
12. [Error States](#error-states)
13. [Loading States](#loading-states)
14. [Accessibility](#accessibility)
15. [Mantine Component Summary](#mantine-component-summary)

---

## Design Overview

This feature adds the ability for community members to:
- Designate trusted "Authorized Contacts" who can buy tickets or RSVP on their behalf
- Purchase multiple tickets in a single checkout and assign them to authorized contacts
- Create proxy RSVPs for authorized contacts at free events
- Accept or decline assigned tickets/RSVPs with personal waiver acknowledgment

### Design Principles Applied
- **Safety first**: Waiver and ToS acceptance is always personal, never delegated
- **Privacy**: Scene names only in all contact/assignee searches (AD-009)
- **Progressive disclosure**: Multi-ticket and proxy features only appear when relevant
- **Consistency**: All new UI follows existing Mantine v7 patterns, color palette, and typography from the ProfileSettingsPage, EventPaymentPage, and MyEventsPage
- **Mobile-first**: All screens designed for mobile first, enhanced for desktop

### User Personas
- **Principal**: A member who authorizes others to act on their behalf
- **Delegate**: A member authorized to purchase tickets/RSVP for others
- **Admin**: System administrator with unrestricted assignment capability

---

## Component Hierarchy

### New Components (to create)

```
apps/web/src/
  features/
    authorized-contacts/
      components/
        AuthorizedContactsTab.tsx        -- New tab content for ProfileSettingsPage
        ContactSearchInput.tsx           -- Scene name search autocomplete
        DelegateList.tsx                 -- "People who can act on your behalf"
        PrincipalList.tsx                -- "People you can act for" (read-only)
      api/
        queries.ts                       -- useAuthorizedContacts, useDelegatesFor
        mutations.ts                     -- useAddAuthorizedContact, useRemoveAuthorizedContact
      types/
        authorizedContact.types.ts       -- TypeScript interfaces

    ticket-assignment/
      components/
        TicketQuantitySelector.tsx       -- Quantity +/- control per ticket type
        TicketAssignmentRow.tsx          -- "Assign to" dropdown for each extra ticket
        PendingTicketsCard.tsx           -- Dashboard card for pending tickets/RSVPs
        TicketStatusBadge.tsx            -- Status badge with assignment info
        TicketAcceptanceModal.tsx        -- Waiver + ToS acceptance modal
        AssignTicketDropdown.tsx         -- Reusable dropdown of authorized contacts
        TicketDeclineModal.tsx           -- Decline confirmation modal
      api/
        queries.ts                       -- usePendingTickets, useMyAssignedTickets
        mutations.ts                     -- useAcceptTicket, useDeclineTicket, useAssignTicket, useReassignTicket
      types/
        ticketAssignment.types.ts        -- TypeScript interfaces

    proxy-rsvp/
      components/
        ProxyRsvpSection.tsx             -- "RSVP for someone else" section
      api/
        mutations.ts                     -- useCreateProxyRsvp
```

### Modified Components (existing)

| Component | File | Modification |
|-----------|------|-------------|
| ProfileSettingsPage | `pages/dashboard/ProfileSettingsPage.tsx` | Add 4th tab: "Authorized Contacts" |
| EventPaymentPage | `features/payments/pages/EventPaymentPage.tsx` | Add quantity selector + assignment dropdowns |
| EventDetailPage | `pages/events/EventDetailPage.tsx` | Add ProxyRsvpSection below RSVP button |
| MyEventsPage | `pages/dashboard/MyEventsPage.tsx` | Add PendingTicketsCard section, enhance ticket status badges |

---

## Screen 1: Profile Settings - Authorized Contacts Tab

### Wireframe

```
DESKTOP (>= 992px)
+------------------------------------------------------------------+
| Profile Settings                          [View Dashboard]        |
+------------------------------------------------------------------+
| [Personal] [Change Password] [Vetting] [Authorized Contacts]     |
+------------------------------------------------------------------+
|                                                                    |
|  +--------------------------------------------------------------+ |
|  | People who can act on your behalf                             | |
|  |                                                                | |
|  | These people can purchase event tickets and RSVP on your      | |
|  | behalf. They cannot accept event waivers for you -- you will  | |
|  | always need to personally accept before attending.             | |
|  |                                                                | |
|  | +----------------------------------------------------------+  | |
|  | | RopeWitch42          [Remove]                             |  | |
|  | +----------------------------------------------------------+  | |
|  | | KnotMaster           [Remove]                             |  | |
|  | +----------------------------------------------------------+  | |
|  |                                                                | |
|  | [+ Add Contact]                                                | |
|  |                                                                | |
|  | +----------------------------------------------------------+  | |
|  | | Search by scene name: [_________________________] (live)  |  | |
|  | |   > RopeFriend99                                          |  | |
|  | |   > RopeArtist23                                          |  | |
|  | +----------------------------------------------------------+  | |
|  +--------------------------------------------------------------+ |
|                                                                    |
|  +--------------------------------------------------------------+ |
|  | People you can act for                                        | |
|  |                                                                | |
|  | These people have authorized you to purchase tickets and RSVP | |
|  | on their behalf.                                               | |
|  |                                                                | |
|  | +----------------------------------------------------------+  | |
|  | | BunnyRope             (added Mar 15, 2026)                |  | |
|  | +----------------------------------------------------------+  | |
|  | | SilkTies              (added Feb 28, 2026)                |  | |
|  | +----------------------------------------------------------+  | |
|  |                                                                | |
|  | (This list is managed by others. You cannot remove yourself.) | |
|  +--------------------------------------------------------------+ |
+------------------------------------------------------------------+


MOBILE (< 992px) -- Accordion layout
+----------------------------------+
| < View Dashboard                 |
|     Profile Settings             |
+----------------------------------+
| v Personal                       |
+----------------------------------+
| > Change Password                |
+----------------------------------+
| > Vetting & Membership           |
+----------------------------------+
| v Authorized Contacts            |
+----------------------------------+
| People who can act on your       |
| behalf                           |
|                                  |
| These people can purchase event  |
| tickets and RSVP on your behalf. |
|                                  |
| +------------------------------+ |
| | RopeWitch42       [Remove]   | |
| +------------------------------+ |
| | KnotMaster        [Remove]   | |
| +------------------------------+ |
|                                  |
| [+ Add Contact]  (full width)   |
|                                  |
| +------------------------------+ |
| | Search: [________________]   | |
| | > RopeFriend99               | |
| | > RopeArtist23               | |
| +------------------------------+ |
|                                  |
| ---                              |
|                                  |
| People you can act for           |
|                                  |
| +------------------------------+ |
| | BunnyRope  (added Mar 15)    | |
| +------------------------------+ |
| | SilkTies   (added Feb 28)    | |
| +------------------------------+ |
+----------------------------------+
```

### Mantine Components

| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `Tabs.Tab` / `Accordion.Item` | New "Authorized Contacts" tab/accordion item | Matches existing pattern in ProfileSettingsPage: desktop=Tabs, mobile=Accordion |
| `Box` | Section containers | `p="md"`, white background, 12px border-radius, 1px taupe border (matches existing form boxes) |
| `Text` | Section titles and descriptions | `fw={600}`, `fontFamily: var(--font-heading)`, `color: var(--color-burgundy)` for titles |
| `Stack` | Vertical layout of contact items | `gap="xs"` for tight list spacing |
| `Group` | Contact row: scene name + remove button | `justify="space-between"`, `align="center"` |
| `Paper` | Individual contact row | `p="sm"`, light border, subtle hover |
| `Button` | "Add Contact" and "Remove" | "Add Contact": variant="outline", color="burgundy"; "Remove": variant="subtle", color="red", compact |
| `TextInput` | Scene name search | `placeholder="Search by scene name..."`, with Mantine `Autocomplete` or custom dropdown |
| `Autocomplete` | Live search results | Displays scene names only (AD-009), debounced 300ms |
| `Divider` | Between the two sections | `my="lg"` |
| `Loader` | During search and add operations | Inline, small |

### Interaction Flow

1. **Adding a contact**:
   - User clicks "Add Contact" button
   - Search field appears below the list (progressive disclosure)
   - User types scene name; after 2+ characters, live results appear (debounced 300ms)
   - User clicks a result to add
   - Confirmation: contact appears immediately in list (optimistic update)
   - Notification toast: "[SceneName] added as authorized contact"
   - Search field clears and collapses
   - If search returns no results: "No members found matching '[query]'"
   - If user tries to add themselves: "You cannot authorize yourself" (inline error)
   - If user tries to add duplicate: "[SceneName] is already an authorized contact" (inline error)

2. **Removing a contact**:
   - User clicks "Remove" button on a contact row
   - Confirmation modal (Mantine Modal, centered):
     - Title: "Remove [SceneName]?"
     - Body: "They will no longer be able to purchase tickets or RSVP on your behalf. This does not affect any tickets already purchased."
     - Buttons: [Cancel] [Remove] (right-aligned, Cancel secondary, Remove primary/red)
   - On confirm: contact removed (optimistic), notification: "[SceneName] removed"

3. **Viewing "People you can act for"**:
   - Read-only list
   - Each row shows scene name and date added
   - Explanatory text: "This list is managed by others. You cannot remove yourself."

### Data Requirements

```typescript
// Query: GET /api/authorized-contacts (returns both directions)
interface AuthorizedContactsResponse {
  delegates: AuthorizedContactDto[];    // People I've authorized (I am Principal)
  principals: AuthorizedContactDto[];   // People who authorized me (I am Delegate)
}

interface AuthorizedContactDto {
  id: string;                // Relationship ID
  userId: string;            // The other user's ID
  sceneName: string;         // The other user's scene name
  createdAt: string;         // When the relationship was created
}

// Mutation: POST /api/authorized-contacts { targetUserId: string }
// Mutation: DELETE /api/authorized-contacts/{relationshipId}
// Query: GET /api/members/search?sceneName={query} (scene name only results)
```

---

## Screen 2: Enhanced Checkout - Multi-Ticket Purchase

### Wireframe

```
DESKTOP (>= 992px)
+------------------------------------------------------------------+
| < Back                              Secure Payment - SSL Encrypted|
+------------------------------------------------------------------+
| Step 1 of 3: Ticket Selection                                     |
+------------------------------------------------------------------+
|                                                                    |
| +------------------------------------------+ +------------------+ |
| | Select Tickets                           | | Order Summary    | |
| |                                          | |                  | |
| | +--------------------------------------+ | | Event Title      | |
| | | Full Event           $25.00-$50.00   | | | Mar 25, 2026     | |
| | | Session A - Mar 25, 7pm              | | |                  | |
| | | Session B - Mar 26, 7pm              | | | Full Event x2    | |
| | |                                      | | |   $35.00 x 2     | |
| | | Quantity: [ - ] 2 [ + ]              | | |                  | |
| | +--------------------------------------+ | | Total: $70.00    | |
| |                                          | +------------------+ |
| | Ticket Assignments                       |                      |
| | +---------+----------------------------+ |                      |
| | | Ticket 1 | Your ticket (you)         | |                      |
| | +---------+----------------------------+ |                      |
| | | Ticket 2 | Assign to: [v Select...  ]| |                      |
| | |          |   > RopeWitch42           | |                      |
| | |          |   > BunnyRope             | |                      |
| | |          |   > Assign later          | |                      |
| | +---------+----------------------------+ |                      |
| |                                          |                      |
| | Choose Your Payment Amount               |                      |
| | [==========|===========] $35.00          |                      |
| | $25.00                         $50.00    |                      |
| | (Applies to all tickets in this purchase)|                      |
| |                                          |                      |
| |          [Continue to Payment]           |                      |
| +------------------------------------------+                      |
+------------------------------------------------------------------+
```

```
MOBILE (< 992px)
+----------------------------------+
| < Back       SSL Encrypted       |
+----------------------------------+
| Step 1 of 3: Ticket Selection    |
+----------------------------------+
|                                  |
| Select Tickets                   |
|                                  |
| +------------------------------+ |
| | Full Event       $25-$50     | |
| | Session A - Mar 25, 7pm      | |
| | Session B - Mar 26, 7pm      | |
| |                               | |
| | Quantity:  [ - ] 2 [ + ]     | |
| +------------------------------+ |
|                                  |
| Ticket Assignments               |
| +------------------------------+ |
| | Ticket 1: Your ticket        | |
| +------------------------------+ |
| | Ticket 2:                     | |
| | Assign to: [v Select...    ] | |
| +------------------------------+ |
|                                  |
| Choose Your Payment Amount       |
| [========|==========] $35.00    |
| $25.00                  $50.00  |
| (Applies to all tickets)        |
|                                  |
| Order Summary                    |
| Full Event x2 ......... $70.00  |
| Total .................. $70.00  |
|                                  |
|    [Continue to Payment]         |
+----------------------------------+
```

### Mantine Components

| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `NumberInput` or custom `Group` | Quantity selector | Min=1, max=`MaxQuantityPerPurchase`, step=1. Custom +/- buttons using `ActionIcon` for better touch targets |
| `Select` | "Assign to" dropdown per extra ticket | Data from authorized contacts query, scene names only. Includes "Assign later" option at top |
| `Paper` | Ticket assignment rows | Subtle background, matches existing ticket card style |
| `Text` | Labels like "Ticket 1", "Your ticket" | Size="sm", color as appropriate |
| `Badge` | Assignment status indicators | "Your ticket" in green, "Pending" in yellow |
| `Divider` | Between ticket selection and assignment sections | |
| `SlidingScaleSelector` | Existing component, unchanged | Already handles single slider; add note "(Applies to all tickets in this purchase)" |
| `PaymentSummary` | Existing component, enhanced | Show quantity and per-unit + total price |

### Quantity Selector Component Design

```
+-----------------------------------+
| Full Event           $25.00       |
| Session A - Mar 25, 7pm          |
|                                   |
| Quantity:  [-]  2  [+]           |
+-----------------------------------+
```

- `[-]` and `[+]` are `ActionIcon` components with `variant="light"`, `color="burgundy"`, size 36px (touch-friendly)
- Quantity number displayed between them, `fw={700}`, `size="lg"`
- Min value: 1 (always buying at least one for yourself)
- Max value: `ticketType.maxQuantityPerPurchase` (default 3, from API)
- If user has NO authorized contacts AND quantity > 1: show info text below: "Add authorized contacts in Profile Settings to assign tickets to others."
- Quantity selector appears inside the existing ticket card Paper component

### Assignment Rows Design

When quantity > 1, a "Ticket Assignments" section appears below the ticket cards:

- **Ticket 1**: Always labeled "Your ticket" (the purchaser's own ticket) -- not editable
- **Ticket 2 through N**: Each shows a `Select` dropdown with:
  - Placeholder: "Select person or assign later..."
  - Option group 1: Authorized contacts (scene names only)
    - Filtered by vetting status if event is VettedMembersOnly (BR-035/BR-037)
    - Filtered to exclude anyone who already has a ticket for this event (BR-012)
  - Last option: "Assign later" -- always available
- If a selected contact already has a ticket (race condition check on submit): error message inline below the dropdown

### Interaction Flow

1. **Increasing quantity**:
   - User clicks [+] on quantity selector
   - Quantity increases, assignment row appears for each new ticket
   - Order summary updates to show total (quantity x price)
   - Sliding scale slider (if present) adds note: "(Applies to all tickets)"

2. **Decreasing quantity**:
   - User clicks [-]
   - Last assignment row is removed
   - If the removed row had a contact selected, it's cleared silently
   - Order summary updates

3. **Selecting an assignee**:
   - User opens "Assign to" dropdown on a ticket row
   - Sees only their authorized contacts (filtered by vetting for vetted events)
   - Selects a contact or "Assign later"
   - Selection persists through step navigation

4. **Proceeding to payment**:
   - "Continue to Payment" button
   - Validation: at least 1 ticket selected (existing behavior)
   - No validation required on assignments (all can be "Assign later")
   - Capacity check includes ALL tickets in cart (BR-013)

5. **Confirmation step (Step 3) enhancements**:
   - After payment, show each ticket's status:
     - "Your ticket: Active"
     - "Ticket for [SceneName]: Pending Acceptance" (if assigned at checkout)
     - "Unassigned ticket: Assign from your dashboard" (if "Assign later" was chosen)

### Data Requirements

```typescript
// Enhanced checkout request (modify existing)
interface MultiTicketCheckoutRequest {
  eventId: string;
  tickets: TicketCheckoutItem[];
  eventWaiverAccepted: boolean;       // For purchaser's own ticket only
  nonce: string;
  dataDescriptor: string;
  amount: number;                     // Total for all tickets
  lastFourDigits?: string;
  cardType?: string;
  idempotencyKey: string;
}

interface TicketCheckoutItem {
  ticketTypeId: string;
  assignToUserId?: string;            // null = unassigned (purchaser owns it)
}

// Query: GET /api/authorized-contacts/delegates-for-event/{eventId}
// Returns contacts filtered by event's vetting requirements + existing tickets
interface EventEligibleContactDto {
  userId: string;
  sceneName: string;
  hasExistingTicket: boolean;         // True if already has ticket for this event
}
```

---

## Screen 3: Event Detail Page - Proxy RSVP Section

### Wireframe

```
DESKTOP & MOBILE (below existing RSVP/ticket section)
+------------------------------------------------------------------+
|                                                                    |
| [existing RSVP button or ticket purchase area]                    |
|                                                                    |
| ---  (Divider)                                                    |
|                                                                    |
| +--------------------------------------------------------------+ |
| | RSVP for someone else                                          | |
| |                                                                | |
| | Select a person:  [v Select authorized contact...           ]  | |
| |                                                                | |
| | [RSVP for RopeWitch42]                                         | |
| |                                                                | |
| | (only people who have authorized you appear here)              | |
| +--------------------------------------------------------------+ |
+------------------------------------------------------------------+
```

### Mantine Components

| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `Paper` | Container for proxy RSVP section | `p="md"`, `radius="md"`, subtle border, light background (`gray.0`) |
| `Text` | Section title "RSVP for someone else" | `fw={600}`, `size="md"`, `fontFamily: var(--font-heading)`, `color: var(--color-burgundy)` |
| `Select` | Contact picker | Scene names only, filtered by vetting + existing RSVPs |
| `Button` | "RSVP for [SceneName]" | color="burgundy", styled with existing button pattern (borderRadius: '12px 6px 12px 6px') |
| `Modal` | Confirmation dialog | Centered, explains waiver requirement |
| `Text` | Explanatory footnote | `size="xs"`, `c="dimmed"` |

### Visibility Rules

- **Not visible** if:
  - User is not logged in
  - User has zero authorized contacts who are eligible (all already have RSVPs, or none pass vetting filter)
  - Event does not allow RSVPs (`AllowRsvps=false`)
  - Event is at full capacity
- **Visible** if:
  - User is logged in AND has at least one authorized contact who can receive an RSVP for this event

### Interaction Flow

1. Section appears below the existing RSVP/ticket area with a divider
2. User selects a contact from the dropdown
   - Dropdown data: authorized contacts (people who authorized this user as delegate)
   - Filtered by: vetting (if VettedMembersOnly), existing RSVPs (excluded), capacity
3. User clicks "RSVP for [SceneName]"
4. Confirmation modal appears:
   - Title: "RSVP for [SceneName]?"
   - Body: "This will create an RSVP for [SceneName] to attend [Event Title]. They will need to personally accept the event waiver before their RSVP is confirmed."
   - Buttons: [Cancel] [Confirm RSVP] (right-aligned)
5. On confirm:
   - Proxy RSVP created (PendingAcceptance status)
   - Success notification: "RSVP created for [SceneName]. They'll receive an email to accept."
   - Section updates: the contact is removed from the dropdown (they now have a pending RSVP)
   - Contact selected in dropdown is cleared
6. On error:
   - "This person already has an RSVP for this event" (race condition)
   - "Event is at full capacity"
   - "[SceneName]'s vetting status has changed" (rare edge case)

### Data Requirements

```typescript
// Query: GET /api/authorized-contacts/delegates-for-event/{eventId}?type=rsvp
// Same endpoint as checkout, but filtered for RSVP eligibility

// Mutation: POST /api/events/{eventId}/proxy-rsvp
interface CreateProxyRsvpRequest {
  principalUserId: string;    // The person being RSVP'd for
}
```

---

## Screen 4: Dashboard - Pending Tickets/RSVPs

### Wireframe

```
DESKTOP (>= 992px) -- Appears at TOP of MyEventsPage, before the events list
+------------------------------------------------------------------+
| {FirstName}'s Dashboard                   [Edit Profile]          |
+------------------------------------------------------------------+
| [vetting alert box if applicable]                                 |
+------------------------------------------------------------------+
|                                                                    |
| +--------------------------------------------------------------+ |
| | Pending Tickets & RSVPs                              [2 new]  | |
| |                                                                | |
| | +----------------------------------------------------------+  | |
| | | Shibari Workshop - Advanced                               |  | |
| | | Mar 25, 2026 at 7:00 PM                                  |  | |
| | | Session A, Session B                                      |  | |
| | | Full Event ticket -- Purchased by RopeWitch42             |  | |
| | |                                                           |  | |
| | |                           [Decline]  [Accept Ticket]      |  | |
| | +----------------------------------------------------------+  | |
| |                                                                | |
| | +----------------------------------------------------------+  | |
| | | Community Rope Night                                      |  | |
| | | Apr 2, 2026 at 8:00 PM                                   |  | |
| | | RSVP -- Created by KnotMaster                             |  | |
| | |                                                           |  | |
| | |                            [Decline]  [Accept RSVP]       |  | |
| | +----------------------------------------------------------+  | |
| +--------------------------------------------------------------+ |
|                                                                    |
| [rest of dashboard: filter bar, events list, etc.]                |
+------------------------------------------------------------------+


MOBILE (< 992px) -- Stacked cards
+----------------------------------+
| {FirstName}'s Dashboard          |
| [Edit Profile]                   |
+----------------------------------+
| [vetting alert]                  |
+----------------------------------+
| Pending Tickets & RSVPs  [2]    |
+----------------------------------+
| +------------------------------+ |
| | Shibari Workshop - Advanced  | |
| | Mar 25, 2026 at 7:00 PM     | |
| | Session A, Session B         | |
| | Full Event ticket            | |
| | Purchased by RopeWitch42     | |
| |                               | |
| | [Decline]   [Accept Ticket]  | |
| +------------------------------+ |
|                                  |
| +------------------------------+ |
| | Community Rope Night         | |
| | Apr 2, 2026 at 8:00 PM      | |
| | RSVP by KnotMaster           | |
| |                               | |
| | [Decline]   [Accept RSVP]    | |
| +------------------------------+ |
+----------------------------------+
| [Filter bar, events list, etc.]  |
+----------------------------------+
```

### Mantine Components

| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `Paper` | Pending section container | `p="lg"`, `radius="md"`, border-left: 4px solid `var(--color-brass)` to draw attention |
| `Group` | Section header: title + count badge | `justify="space-between"` |
| `Title` | "Pending Tickets & RSVPs" | `order={3}`, `fontFamily: var(--font-heading)`, `color: var(--color-burgundy)` |
| `Badge` | Count indicator | `color="yellow"`, variant="filled", shows number of pending items |
| `Stack` | List of pending cards | `gap="md"` |
| `Paper` | Individual pending ticket/RSVP card | `p="md"`, `radius="md"`, white background, subtle border |
| `Text` | Event title | `fw={700}`, `size="md"` |
| `Text` | Date, sessions, ticket type, purchaser | `size="sm"`, `c="dimmed"` for metadata |
| `Text` | "Purchased by [SceneName]" | `size="sm"`, `fw={500}` |
| `Group` | Action buttons row | `justify="flex-end"`, `gap="sm"` |
| `Button` | "Decline" | `variant="subtle"`, `color="gray"` |
| `Button` | "Accept Ticket" / "Accept RSVP" | `color="burgundy"`, styled with existing button pattern |

### Visibility Rules

- **Entire section hidden** if there are zero pending tickets/RSVPs
- Each card shows:
  - Event title (bold)
  - Event date and time
  - Session names (if multi-session)
  - Ticket type name (for tickets) or "RSVP" (for RSVPs)
  - "Purchased by [SceneName]" or "Created by [SceneName]" (the delegate's scene name)

### Interaction Flow

1. **Accept button clicked**: Opens TicketAcceptanceModal (Screen 6)
2. **Decline button clicked**: Opens decline confirmation modal:
   - Title: "Decline this ticket?" / "Decline this RSVP?"
   - Body: "The ticket will be returned to [DelegateSceneName] who can reassign it to someone else."
   - Buttons: [Cancel] [Decline] (Cancel secondary, Decline red variant)
3. On accept: Card removed from pending list (optimistic), success toast
4. On decline: Card removed from pending list (optimistic), notification to delegate

### Data Requirements

```typescript
// Query: GET /api/dashboard/pending-assignments
interface PendingAssignment {
  id: string;                          // EventAttendance ID
  eventId: string;
  eventTitle: string;
  eventDate: string;
  eventEndDate: string;
  sessions: { name: string; startTime: string; endTime: string }[];
  type: 'Ticket' | 'RSVP';
  ticketTypeName?: string;             // Only for tickets
  delegateSceneName: string;           // Who purchased/created it
  requiresVetting: boolean;            // Whether event is VettedMembersOnly
  eventWaiverText?: string;            // For the acceptance modal
}
```

---

## Screen 5: Dashboard - My Tickets (Enhanced)

### Wireframe (enhancements to existing event cards/table rows)

```
DESKTOP -- Grid view, existing EventCard enhanced
+----------------------------------------------+
| Shibari Workshop - Advanced                  |
| Mar 25, 2026 at 7:00 PM                     |
| Session A, Session B                         |
|                                              |
| YOUR TICKETS:                                |
| +------------------------------------------+|
| | Full Event -- Active (You)     [green]   ||
| +------------------------------------------+|
| | Full Event -- Pending: RopeWitch42       ||
| |    [yellow badge]                         ||
| +------------------------------------------+|
| | Full Event -- Declined          [Reassign]||
| |    [red badge]                            ||
| +------------------------------------------+|
| | Full Event -- Unassigned        [Assign] ||
| |    [gray badge]                           ||
| +------------------------------------------+|
+----------------------------------------------+


MOBILE -- same content, stacked
+----------------------------------+
| Shibari Workshop - Advanced      |
| Mar 25, 2026 at 7:00 PM         |
+----------------------------------+
| Your Tickets:                    |
|                                  |
| Full Event                       |
| [Active (You)]                   |
|                                  |
| Full Event                       |
| [Pending: RopeWitch42]           |
|                                  |
| Full Event                       |
| [Declined]         [Reassign]   |
|                                  |
| Full Event                       |
| [Unassigned]         [Assign]   |
+----------------------------------+
```

### TicketStatusBadge Component

| Status | Badge Color | Badge Text | Action Button |
|--------|------------|------------|---------------|
| Active (own ticket) | `green` | "Active (You)" | None |
| Active (accepted by assignee) | `green` | "Active: [SceneName]" | None (irrevocable, AD-008) |
| PendingAcceptance | `yellow` | "Pending: [SceneName]" | None (waiting on them) |
| Declined | `red` | "Declined" | [Reassign] button |
| Unassigned (extra ticket owned by purchaser) | `gray` | "Unassigned" | [Assign] button |

### Mantine Components

| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `Badge` | Ticket status | Colors as per table above, `variant="light"` |
| `Button` | "Assign" / "Reassign" | `size="xs"`, `variant="outline"`, `color="burgundy"` |
| `Stack` | List of tickets within an event card | `gap="xs"` |
| `Group` | Ticket row: type + status + action | `justify="space-between"`, `wrap="nowrap"` |
| `Modal` | Assignment dropdown (opened by Assign/Reassign) | Contains `Select` of authorized contacts |

### Interaction Flow

1. **[Assign] button** (on unassigned tickets):
   - Opens a small modal or inline dropdown
   - Shows authorized contacts filtered by event vetting + existing tickets
   - On selection: ticket assigned, status changes to "Pending: [SceneName]"
   - Notification email sent to assignee

2. **[Reassign] button** (on declined tickets):
   - Same flow as Assign
   - Previous assignee excluded from dropdown
   - On selection: new assignment created, notification sent

3. **No action for accepted tickets** (AD-008):
   - Once accepted, ticket shows "Active: [SceneName]" with no action buttons
   - Matches irrevocable transfer model

### Data Requirements

```typescript
// Enhanced MyEventsPage data includes ticket assignment info
// Query: GET /api/dashboard/my-events?includePast={boolean}
// Each event's tickets include:
interface UserTicketDto {
  attendanceId: string;
  ticketTypeName: string;
  status: 'Active' | 'PendingAcceptance' | 'Declined' | 'Unassigned';
  isOwnTicket: boolean;                // True if the user is the attendee
  assigneeSceneName?: string;          // Scene name of person ticket is assigned to
  purchasedBySceneName?: string;       // Scene name of purchaser (for received tickets)
  canAssign: boolean;                  // True if Assign/Reassign action available
  canReassign: boolean;                // True if ticket was declined and can be reassigned
  eventHasPassed: boolean;             // If true, no action buttons shown
}
```

---

## Screen 6: Ticket/RSVP Acceptance Modal

### Wireframe

```
DESKTOP & MOBILE (Mantine Modal, centered, size="lg")
+--------------------------------------------------+
|                                                    |
|   Accept Your Ticket                         [X]  |
|                                                    |
|   Event: Shibari Workshop - Advanced               |
|   Date: March 25, 2026 at 7:00 PM                 |
|   Sessions: Session A, Session B                   |
|   Venue: Studio Salem                              |
|   Ticket type: Full Event                          |
|   Purchased by: RopeWitch42                        |
|                                                    |
|   ------------------------------------------------ |
|                                                    |
|   Event Waiver                                     |
|   +----------------------------------------------+ |
|   | [Scrollable waiver text area, max-height     | |
|   |  200px. Full waiver text from event config.  | |
|   |  User must scroll through to read.]          | |
|   +----------------------------------------------+ |
|                                                    |
|   [ ] I have read and accept the Event Waiver     |
|                                                    |
|   [ ] I accept the Terms of Service               |
|       (only shown if user hasn't already accepted) |
|                                                    |
|   ------------------------------------------------ |
|                                                    |
|              [Cancel]  [Accept Ticket]             |
|                                                    |
+--------------------------------------------------+
```

### Mantine Components

| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `Modal` | Container | `centered`, `size="lg"`, `title="Accept Your Ticket"` / `"Accept Your RSVP"` |
| `Stack` | Event details layout | `gap="xs"` |
| `Text` | Event name, date, sessions, venue, ticket type | `size="sm"` for labels, `fw={600}` for values |
| `Divider` | Between details and waiver | `my="md"` |
| `ScrollArea` | Waiver text container | `h={200}`, `type="auto"` |
| `Text` | Waiver text | `size="sm"`, rendered from event waiver HTML/text |
| `Checkbox` | "I accept the Event Waiver" | `color="burgundy"`, required |
| `Checkbox` | "I accept the Terms of Service" | `color="burgundy"`, conditionally shown (only if `!user.termsOfServiceAccepted`) |
| `Group` | Button row | `justify="flex-end"`, `gap="sm"`, `mt="lg"` |
| `Button` | "Cancel" | `variant="subtle"`, `color="gray"` |
| `Button` | "Accept Ticket" / "Accept RSVP" | `color="burgundy"`, disabled until all checkboxes checked, loading state during submission |

### Interaction Flow

1. Modal opens when user clicks "Accept Ticket" or "Accept RSVP" from dashboard
2. Event details displayed at top for context
3. Waiver text shown in scrollable area
4. User must check "I accept the Event Waiver" (always required)
5. If user has not previously accepted platform ToS: additional "I accept the Terms of Service" checkbox shown
6. "Accept" button disabled until ALL checkboxes are checked
7. On click "Accept":
   - Button enters loading state
   - API call to accept the ticket/RSVP
   - On success:
     - Modal closes
     - Pending card removed from dashboard (optimistic update)
     - Success notification: "Ticket accepted! You're registered for [Event Title]"
     - Event appears in "My Events" list with Active status
   - On error:
     - If vetting revoked: "This event requires vetted membership. Your vetting status has changed. Please contact an admin." (Alert inside modal, modal stays open)
     - If event passed: "This event has already started. Contact an admin for assistance."
     - Network error: "Failed to accept. Please try again." (retry-able)

### Vetting Edge Case (AD-014)

If the event is VettedMembersOnly and the user's vetting was revoked between assignment and acceptance:

```
+--------------------------------------------------+
|   Accept Your Ticket                         [X]  |
|                                                    |
|   +----------------------------------------------+ |
|   | [!] Unable to Accept                         | |
|   |                                               | |
|   | This event requires vetted membership. Your   | |
|   | vetting status has changed since this ticket   | |
|   | was assigned. Please contact an admin for      | |
|   | assistance.                                    | |
|   +----------------------------------------------+ |
|                                                    |
|                                       [Close]      |
+--------------------------------------------------+
```

- Uses `Alert` component, `color="red"`, inside the modal
- Waiver checkboxes and Accept button hidden
- Only "Close" button shown

### Data Requirements

```typescript
// Mutation: POST /api/attendance/{attendanceId}/accept
interface AcceptAssignmentRequest {
  eventWaiverAccepted: boolean;
  termsOfServiceAccepted: boolean;
}

// Mutation: POST /api/attendance/{attendanceId}/decline
// No body needed

// Response: 200 OK on success, 409 for vetting conflict, 410 for past event
```

---

## Route Changes

No new routes are required. All new UI is added to existing pages:

| Existing Route | Modification |
|----------------|-------------|
| `/dashboard/profile-settings` | Add "Authorized Contacts" tab (4th tab/accordion item) |
| `/checkout/:eventId` | Add quantity selector + assignment dropdowns to Step 1 |
| `/events/:id` | Add ProxyRsvpSection below RSVP/ticket area |
| `/dashboard` | Add PendingTicketsCard section at top, enhance ticket status in event cards |

No new pages, no new route definitions in `router.tsx`.

---

## State Management

### React Query Keys (new)

```typescript
// Add to existing queryKeys object
const queryKeys = {
  // ... existing keys
  authorizedContacts: () => ['authorized-contacts'] as const,
  authorizedContactsForEvent: (eventId: string, type?: 'ticket' | 'rsvp') =>
    ['authorized-contacts', 'event', eventId, type] as const,
  pendingAssignments: () => ['pending-assignments'] as const,
  memberSearch: (query: string) => ['member-search', query] as const,
};
```

### Cache Invalidation Strategy

| Action | Invalidate |
|--------|-----------|
| Add authorized contact | `authorizedContacts`, recipient's `authorizedContacts` |
| Remove authorized contact | `authorizedContacts`, `authorizedContactsForEvent` |
| Purchase multi-ticket | `pendingAssignments` (for assignees), event data, `currentUser` |
| Accept ticket/RSVP | `pendingAssignments`, user events, event attendees |
| Decline ticket/RSVP | `pendingAssignments`, purchaser's events |
| Assign ticket from dashboard | `pendingAssignments` (for assignee), user events |
| Create proxy RSVP | `pendingAssignments` (for principal), `authorizedContactsForEvent`, event data |

### Component State (local)

| Component | State | Type |
|-----------|-------|------|
| AuthorizedContactsTab | `showSearchField` | `boolean` |
| AuthorizedContactsTab | `searchQuery` | `string` (debounced) |
| TicketQuantitySelector | `quantity` | `number` per ticket type |
| EventPaymentPage (enhanced) | `ticketAssignments` | `Record<number, string \| 'assign-later' \| null>` mapping ticket index to userId |
| PendingTicketsCard | `acceptingId` | `string \| null` (which ticket acceptance modal is open) |
| PendingTicketsCard | `decliningId` | `string \| null` (which ticket decline modal is open) |
| TicketAcceptanceModal | `waiverChecked` | `boolean` |
| TicketAcceptanceModal | `tosChecked` | `boolean` |

---

## Responsive Design

### Breakpoint Strategy

All designs follow the existing project pattern of using `useMediaQuery('(max-width: 991px)')` for the mobile/desktop split.

| Screen | Mobile (< 992px) | Desktop (>= 992px) |
|--------|-------------------|---------------------|
| **Authorized Contacts** | Accordion item (matches existing pattern) | Tab (matches existing pattern) |
| **Checkout quantity** | Full-width quantity selector below ticket card | Inline within ticket card |
| **Checkout assignments** | Stacked cards, full-width Select dropdowns | Side-by-side rows |
| **Proxy RSVP** | Full-width Paper, stacked layout | Same (section is narrow by context) |
| **Pending tickets** | Full-width stacked cards, buttons full-width | Horizontal cards, buttons right-aligned |
| **Acceptance modal** | `Modal` with `fullScreen` on mobile | `Modal` centered, `size="lg"` |
| **My Tickets status** | Stacked within event card, badges wrap | Inline row with badges |

### Touch Targets

All interactive elements meet 44px minimum touch target:
- Quantity +/- buttons: 36px icon + padding = 44px effective
- Remove buttons on contacts: compact but with adequate padding
- Accept/Decline buttons: standard Mantine button sizing (minimum 36px height)
- Checkbox touch targets: Mantine default (adequate)
- Select dropdowns: Mantine default (48px height on mobile)

---

## Error States

### Authorized Contacts Tab

| Error | Display | Recovery |
|-------|---------|----------|
| Failed to load contacts | Alert with retry button inside tab content | "Failed to load your contacts. [Try Again]" |
| Search failed | Inline text below search field | "Search failed. Please try again." |
| Add contact failed | Notification toast (red) | "Failed to add [SceneName]. Please try again." |
| Remove contact failed | Notification toast (red), contact re-appears (rollback) | "Failed to remove [SceneName]. Please try again." |
| Self-authorization | Inline error below search results | "You cannot authorize yourself." |
| Duplicate contact | Inline error below search results | "[SceneName] is already an authorized contact." |

### Enhanced Checkout

| Error | Display | Recovery |
|-------|---------|----------|
| Capacity exceeded (multi-ticket) | Alert below quantity selector | "Only [N] tickets remaining for this event." |
| Assignee already has ticket | Inline error below the specific Select dropdown | "[SceneName] already has a ticket for this event." (red Text below Select) |
| Payment failure | Existing error handling in EventPaymentPage (Alert + retry) | All ticket records rolled back (BR-015) |

### Proxy RSVP

| Error | Display | Recovery |
|-------|---------|----------|
| Already has RSVP (race condition) | Notification toast + contact removed from dropdown | "This person already has an RSVP for this event." |
| Event at capacity | Notification toast | "Event is at full capacity." |
| Vetting changed | Notification toast | "[SceneName]'s vetting status has changed." |
| Network failure | Notification toast | "Failed to create RSVP. Please try again." |

### Ticket Acceptance

| Error | Display | Recovery |
|-------|---------|----------|
| Vetting revoked | Alert inside modal (replaces form) | "Your vetting status has changed. Contact an admin." |
| Event already passed | Alert inside modal | "This event has already started. Contact an admin." |
| Network failure | Notification toast, modal stays open | "Failed to accept. Please try again." |

---

## Loading States

| Component | Loading State |
|-----------|--------------|
| Authorized Contacts tab | `Loader` component centered in tab content while contacts load |
| Contact search | `Loader` inside search dropdown while searching (Mantine Autocomplete supports this) |
| Add/Remove contact | Button enters `loading` state (spinner replaces text) |
| Quantity selector | Instant (local state only, no API call) |
| Assignment dropdown data | `Select` shows "Loading contacts..." while fetching |
| Pending Tickets section | `Skeleton` cards while loading (2 skeleton cards) |
| Accept/Decline buttons | Button enters `loading` state during API call |
| Acceptance modal submit | "Accept" button shows `loading={true}` |
| Proxy RSVP confirm | "Confirm RSVP" button shows `loading={true}` |

---

## Accessibility

### Keyboard Navigation

| Component | Keyboard Support |
|-----------|-----------------|
| Quantity selector | +/- buttons focusable via Tab, activatable via Enter/Space |
| Contact search | Autocomplete supports arrow keys for navigation, Enter to select |
| Select dropdowns | Mantine Select has full keyboard support (arrow keys, Enter, Escape) |
| Accept/Decline buttons | Standard button focus and activation |
| Acceptance modal | Focus trapped inside modal, Escape to close |
| Checkboxes in modal | Space to toggle, Tab to navigate |
| Remove contact button | Focusable, Enter/Space to activate |

### ARIA Labels

```tsx
// Quantity selector
<ActionIcon aria-label="Decrease quantity">-</ActionIcon>
<Text aria-live="polite" aria-label={`Quantity: ${quantity}`}>{quantity}</Text>
<ActionIcon aria-label="Increase quantity">+</ActionIcon>

// Assignment dropdown
<Select
  aria-label={`Assign ticket ${index + 1} to an authorized contact`}
  ...
/>

// Pending ticket card
<Paper role="article" aria-label={`Pending ${type} for ${eventTitle}`}>

// Accept button
<Button aria-label={`Accept ${type} for ${eventTitle}`}>Accept {type}</Button>

// Decline button
<Button aria-label={`Decline ${type} for ${eventTitle}`}>Decline</Button>

// Waiver checkbox
<Checkbox
  aria-label="I have read and accept the Event Waiver"
  aria-required="true"
/>

// Contact in list
<Paper role="listitem" aria-label={`Authorized contact: ${sceneName}`}>
```

### Screen Reader Announcements

- After adding contact: "Added [SceneName] as authorized contact" (via notification)
- After removing contact: "[SceneName] removed from authorized contacts" (via notification)
- After accepting ticket: "Ticket accepted. You are registered for [EventTitle]" (via notification)
- After declining: "Ticket declined" (via notification)
- Pending count: `aria-live="polite"` on the Badge showing pending count

### Color Contrast

All status badges use Mantine's built-in color variants which meet WCAG 2.1 AA contrast requirements:
- Green badges: White text on green (#2B8A3E) = 4.5:1+
- Yellow badges: Dark text on yellow = 4.5:1+
- Red badges: White text on red = 4.5:1+
- Gray badges: Dark text on gray = 4.5:1+

---

## Mantine Component Summary

| Component | Count | Screens Used |
|-----------|-------|-------------|
| `Tabs.Tab` | 1 new | Screen 1 (desktop) |
| `Accordion.Item` | 1 new | Screen 1 (mobile) |
| `Autocomplete` | 1 | Screen 1 (contact search) |
| `Select` | 3+ | Screen 2 (assignment per ticket), Screen 3 (proxy RSVP), Screen 5 (assign modal) |
| `ActionIcon` | 2 | Screen 2 (quantity +/-) |
| `NumberInput` (alternative) | 1 | Screen 2 (quantity, if custom +/- not used) |
| `Modal` | 4 | Screen 1 (remove confirm), Screen 3 (RSVP confirm), Screen 4 (decline confirm), Screen 6 (acceptance) |
| `Checkbox` | 2 | Screen 6 (waiver + ToS) |
| `ScrollArea` | 1 | Screen 6 (waiver text) |
| `Badge` | 5+ | Screen 4 (count), Screen 5 (status per ticket) |
| `Paper` | Many | All screens (card containers) |
| `Button` | Many | All screens (actions) |
| `Stack` / `Group` | Many | All screens (layout) |
| `Text` / `Title` | Many | All screens (content) |
| `Divider` | 2 | Screen 1 (between sections), Screen 3 (before proxy section) |
| `Alert` | 2 | Screen 6 (vetting error), error states |
| `Loader` | 3 | Screen 1 (search, contacts), Screen 4 (pending) |
| `Skeleton` | 1 | Screen 4 (pending cards loading) |
| `Notification` (via `notifications.show`) | Many | All screens (success/error feedback) |

---

## Implementation Priority

Recommended build order based on dependencies:

1. **Authorized Contacts Tab** (Screen 1) -- Foundation, no dependencies on other new screens
2. **Acceptance Modal** (Screen 6) -- Core waiver/ToS flow needed by Screens 4 and 5
3. **Pending Tickets/RSVPs Card** (Screen 4) -- Uses acceptance modal
4. **My Tickets Enhancement** (Screen 5) -- Uses assign dropdown, acceptance patterns
5. **Enhanced Checkout** (Screen 2) -- Largest change, uses contact data from Screen 1
6. **Proxy RSVP Section** (Screen 3) -- Uses contact data, similar patterns to checkout
