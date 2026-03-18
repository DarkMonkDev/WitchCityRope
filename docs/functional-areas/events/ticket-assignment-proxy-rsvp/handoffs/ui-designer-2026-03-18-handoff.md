# UI Designer Handoff: Ticket Assignment & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Phase: Design -->
<!-- Agent: UI Designer -->
<!-- Status: Complete -->

## Summary

Completed comprehensive UI design for the Ticket Assignment & Proxy RSVP feature covering 6 screens. All designs follow existing Mantine v7 patterns, the WitchCityRope design system v7 color palette, and the established ProfileSettingsPage/EventPaymentPage component patterns.

## Design Document Location

**Primary design document**: `/docs/functional-areas/events/ticket-assignment-proxy-rsvp/design/ui-design.md`

## Key Design Decisions

### 1. No New Routes
All new UI is added to existing pages (ProfileSettingsPage, EventPaymentPage, EventDetailPage, MyEventsPage). No new pages or routes needed.

### 2. Mobile Pattern: Accordion on Mobile, Tab on Desktop
The Authorized Contacts feature uses the same pattern as existing ProfileSettingsPage: Tabs on desktop (>= 992px), Accordion on mobile (< 992px).

### 3. Progressive Disclosure for Multi-Ticket
- Quantity selector defaults to 1 (current single-ticket behavior preserved)
- Assignment rows only appear when quantity > 1
- "RSVP for someone else" section only visible when user has eligible contacts

### 4. TicketAcceptanceModal is Shared
The same modal component handles both ticket acceptance and RSVP acceptance with minor text differences. Single component, two modes.

### 5. Existing Button Styling Pattern
All new buttons use the existing WCR button pattern: `borderRadius: '12px 6px 12px 6px'`, `fontFamily: var(--font-heading)`, `color="burgundy"`.

### 6. Status Badge System
Five distinct states with color-coded badges: Active (green), Pending (yellow), Declined (red), Unassigned (gray), Active-assigned (green).

## What the React Developer Needs

### New Feature Directories
- `features/authorized-contacts/` -- Components, API hooks, types
- `features/ticket-assignment/` -- Components, API hooks, types
- `features/proxy-rsvp/` -- Components, API hooks

### API Endpoints Expected
- `GET /api/authorized-contacts` -- Both delegates and principals
- `POST /api/authorized-contacts` -- Add contact
- `DELETE /api/authorized-contacts/{id}` -- Remove contact
- `GET /api/members/search?sceneName={query}` -- Scene name search
- `GET /api/authorized-contacts/delegates-for-event/{eventId}` -- Filtered contacts
- `GET /api/dashboard/pending-assignments` -- Pending tickets/RSVPs
- `POST /api/attendance/{id}/accept` -- Accept ticket/RSVP
- `POST /api/attendance/{id}/decline` -- Decline ticket/RSVP
- `POST /api/events/{eventId}/proxy-rsvp` -- Create proxy RSVP
- Enhanced checkout endpoint to support `tickets[]` array with `assignToUserId`

### Modified Files
1. `pages/dashboard/ProfileSettingsPage.tsx` -- Add 4th tab/accordion
2. `features/payments/pages/EventPaymentPage.tsx` -- Quantity + assignments
3. `pages/events/EventDetailPage.tsx` -- Proxy RSVP section
4. `pages/dashboard/MyEventsPage.tsx` -- Pending section + ticket statuses

### React Query Keys to Add
- `authorized-contacts`
- `authorized-contacts/event/{eventId}`
- `pending-assignments`
- `member-search/{query}`

## What the Backend Developer Needs

Detailed API contracts and data shapes are in the design document under "Data Requirements" sections for each screen. Key points:

- `AuthorizedContact` entity and CRUD endpoints
- `AttendanceStatus.PendingAcceptance` enum value (= 6)
- `MaxQuantityPerPurchase` field on `TicketType` (default: 3)
- Enhanced checkout to create multiple `EventAttendance` records
- Proxy RSVP endpoint
- Accept/Decline endpoints with waiver tracking
- Vetting re-check at acceptance time (AD-014)
- Scene-name-only member search endpoint

## What the Test Developer Needs

Key test scenarios:
1. Add/remove authorized contacts (happy path + self-auth + duplicate)
2. Multi-ticket checkout (quantity changes, assignment at checkout, "assign later")
3. Accept ticket with waiver (both checkboxes required)
4. Decline ticket (confirmation, ticket returned to purchaser)
5. Proxy RSVP (create, confirmation, contact filtered out after)
6. Vetting edge case (revoked between assignment and acceptance)
7. Capacity edge case (multi-ticket exceeds remaining capacity)
8. Mobile responsive layouts for all 6 screens

## Implementation Priority

1. Authorized Contacts Tab (Screen 1) -- no dependencies
2. Acceptance Modal (Screen 6) -- needed by Screens 4 and 5
3. Pending Tickets/RSVPs Card (Screen 4) -- uses modal
4. My Tickets Enhancement (Screen 5) -- uses assign patterns
5. Enhanced Checkout (Screen 2) -- largest change
6. Proxy RSVP Section (Screen 3) -- last, similar patterns

## Open Questions

None. All design decisions were derived from the 14 architectural decisions (AD-001 through AD-014) confirmed by the stakeholder.
