# UI Design: Session-Based Ticket Selection
<!-- Last Updated: 2025-12-08 -->
<!-- Version: 2.0 - MINIMAL CHANGES APPROACH -->
<!-- Owner: Orchestrator (after UI Designer rework) -->
<!-- Status: Draft - Pending Approval -->

## Design Philosophy

**CRITICAL PRINCIPLE: USE EXISTING IMPLEMENTATION**

The current UI is 95%+ complete. This document specifies ONLY the minimal changes needed for session-based ticket validation. NO new pages, NO new components, NO redesigns.

---

## Summary of Changes

| Component | Change Type | Description |
|-----------|-------------|-------------|
| EventPaymentPage.tsx | Business Logic | Add session overlap prevention for checkbox selection |
| EventPaymentPage.tsx | Display | Show "Already Purchased" for owned ticket types |
| PaymentConfirmation.tsx | Minor Addition | Add ticket type name and sessions to Registration Details |
| ParticipationCard.tsx | Text Format | Update capacity display to "X sold, Y Available" |
| ParticipationCard.tsx | Business Logic | Show purchase button if user can buy tickets for other sessions |
| Dashboard EventCard.tsx | Minor Addition | Show which sessions ticket/RSVP covers |

**NOT CHANGING:**
- TicketTypeFormModal.tsx - Already has MultiSelect for sessions
- EventTicketPurchaseModal.tsx - Not used in main flow
- Checkout stepper/layout - Already works
- PaymentSummary sidebar - Already shows multiple tickets
- Admin per-session sold counts - Already exists

---

## 1. EventPaymentPage.tsx - Ticket Selection (Step 1)

### Current Implementation (KEEP)
- Checkbox multi-select for ticket types ✅
- Session dates displayed under ticket names ✅
- Sliding scale per ticket ✅
- PaymentSummary sidebar ✅

### Changes Needed

#### 1.1 Session Overlap Prevention Logic

When a ticket is checked, disable other tickets that share sessions:

```tsx
// Add to EventPaymentPage.tsx - around line 351 (handleTicketTypeToggle)

/**
 * Check if selecting a ticket would create session overlap
 */
const getDisabledTicketIds = (selectedIds: string[]): Set<string> => {
  const disabledIds = new Set<string>();

  // Get all sessions covered by currently selected tickets
  const coveredSessionIds = new Set<string>();
  selectedIds.forEach(ticketId => {
    const ticket = ticketTypes.find(tt => tt.id === ticketId);
    ticket?.sessionIdentifiers?.forEach(sessionId => {
      coveredSessionIds.add(sessionId);
    });
  });

  // Find tickets that have overlapping sessions (disable them)
  ticketTypes.forEach(ticket => {
    if (selectedIds.includes(ticket.id || '')) return; // Already selected

    const hasOverlap = ticket.sessionIdentifiers?.some(
      sessionId => coveredSessionIds.has(sessionId)
    );

    if (hasOverlap) {
      disabledIds.add(ticket.id || '');
    }
  });

  return disabledIds;
};

// Usage in render:
const disabledTicketIds = getDisabledTicketIds(selectedTicketTypeIds);
```

#### 1.2 Already Purchased Ticket Display

For tickets user already owns, show pre-selected with "Already Purchased" instead of price:

```tsx
// In ticket card render (around line 535-598):

const isAlreadyPurchased = userPurchasedTicketIds?.includes(tt.id || '');
const isDisabled = disabledTicketIds.has(tt.id || '') || isAlreadyPurchased;

// In price display area:
{isAlreadyPurchased ? (
  <Text fw={700} size="lg" c="green" style={{ whiteSpace: 'nowrap' }}>
    Already Purchased
  </Text>
) : (
  <Text fw={700} size="lg" c="#880124" style={{ whiteSpace: 'nowrap' }}>
    {priceDisplay}
  </Text>
)}
```

**Behavior for Already Purchased:**
- Checkbox shown as checked and disabled
- NOT added to cart total
- NOT included in purchase API call
- Visual: Green "Already Purchased" text instead of price

#### 1.3 Session Overlap Visual Indicator

When a ticket is disabled due to overlap, show a subtle message:

```tsx
{disabledTicketIds.has(tt.id || '') && !isAlreadyPurchased && (
  <Text size="xs" c="dimmed" mt={4}>
    Sessions overlap with selected ticket
  </Text>
)}
```

### No Modal Popup Needed

The "Cannot Purchase This Ticket" modal is **NOT NEEDED**. Users are prevented from selecting overlapping tickets in the first place through disabled checkboxes.

---

## 2. PaymentConfirmation.tsx - Registration Details

### Current Implementation (KEEP)
- Success header ✅
- Event title and date/time ✅
- Payment amount ✅
- What's Next section ✅
- Receipt information ✅

### Changes Needed

Add ticket details under "Registration Details" title (line ~110):

```tsx
{/* After line 112: <Title order={3} c="#880124">Registration Details</Title> */}

{/* Ticket Information - NEW */}
{purchasedTickets && purchasedTickets.length > 0 && (
  <Stack gap="xs" mb="md">
    {purchasedTickets.map((ticket, index) => (
      <Group key={ticket.id || index} gap="sm">
        <IconTicket size={18} color="#6B0119" />
        <Box>
          <Text fw={600}>{ticket.name}</Text>
          {ticket.sessionDates && (
            <Text size="sm" c="dimmed">
              {ticket.sessionDates}
            </Text>
          )}
        </Box>
      </Group>
    ))}
  </Stack>
)}

{/* Existing Event Information continues... */}
```

### Data Requirements

Pass `purchasedTickets` array to PaymentConfirmation:
```tsx
interface PurchasedTicket {
  id: string;
  name: string;
  sessionDates: string; // e.g., "Sun, Dec 1 • Sat, Dec 7"
}
```

**NO OTHER CHANGES to confirmation page layout/design.**

---

## 3. ParticipationCard.tsx - Session Availability Display

### Current Implementation
Shows capacity as progress bar or "X / Y" format.

### Changes Needed

#### 3.1 Session Availability Format

Use "X sold, Y Available" format with standard date • time:

```tsx
{/* Session Availability Section */}
{sessions && sessions.length > 1 && (
  <Stack gap="xs" mb="md">
    <Text fw={600} size="sm" c="dimmed" tt="uppercase">
      Session Availability
    </Text>
    {sessions.map(session => (
      <Group key={session.id} justify="space-between">
        <Text size="sm">
          {formatUtcToLocalDate(session.startTime, eventTimeZone, {
            weekday: 'short',
            month: 'short',
            day: 'numeric'
          })} • {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}
        </Text>
        <Text size="sm" c="dimmed">
          {session.soldCount} sold, {session.availableCount} Available
        </Text>
      </Group>
    ))}
  </Stack>
)}
```

**Note:** Only show for multi-session events. Single-session events continue to use existing event-level capacity display.

#### 3.2 Purchase Button Logic for Multi-Session Events

**Current Logic (WRONG for multi-session):**
```tsx
// Current: Hides purchase button if user has ANY ticket
const canPurchaseTicket = !hasTicket;
```

**New Logic (CORRECT for session-based):**
```tsx
// New: Show purchase button if there are sessions user doesn't have tickets for
// AND those sessions have available ticket types within purchase window

const userOwnedSessionIds = participation?.ownedSessionIds || [];
const availableSessions = sessions.filter(session =>
  !userOwnedSessionIds.includes(session.id) &&
  session.availableCount > 0
);

// Check if any ticket types for available sessions are within purchase window
const canPurchaseMoreTickets = availableSessions.length > 0 &&
  ticketTypes.some(tt =>
    tt.canPurchase && // Within timing window
    tt.sessionIdentifiers?.some(sid =>
      availableSessions.map(s => s.sessionIdentifier).includes(sid)
    )
  );
```

**User States:**

| State | What User Sees |
|-------|----------------|
| No tickets | "Purchase Ticket" button + all session availability |
| Ticket for Session 1 only | "You have a ticket for [Session 1]" + "Purchase Ticket" button (if Session 2 available) |
| Ticket for all sessions | "You have tickets for all sessions" (no purchase button) |
| Ticket for some, others sold out | "You have a ticket for [Session 1]" + "[Session 2] - Sold Out" |

**Display When User Has Partial Tickets:**

```tsx
{/* Show owned sessions */}
{userOwnedSessionIds.length > 0 && (
  <Alert color="green" variant="light" icon={<IconCheck />} mb="md">
    <Text size="sm" fw={500}>You have tickets for:</Text>
    <Stack gap={4} mt="xs">
      {ownedSessions.map(session => (
        <Text key={session.id} size="sm">
          • {formatSessionDateTime(session)}
        </Text>
      ))}
    </Stack>
  </Alert>
)}

{/* Show purchase option if more sessions available */}
{canPurchaseMoreTickets && (
  <>
    <Text size="sm" c="dimmed" mb="sm">
      Additional sessions available:
    </Text>
    {/* Session availability list for un-owned sessions */}
    {/* Purchase Ticket button */}
  </>
)}
```

**Backend Data Required:**

Add to `EnhancedParticipationStatusDto`:
```csharp
/// <summary>
/// Session IDs the user already has tickets for
/// </summary>
public List<Guid> OwnedSessionIds { get; set; } = new();

/// <summary>
/// Whether user can purchase tickets for additional sessions
/// (has available sessions they don't own, within timing window)
/// </summary>
public bool CanPurchaseAdditionalSessions { get; set; }
```

---

## 4. Dashboard EventCard.tsx - Ticket/RSVP Sessions

### Current Implementation (KEEP)
- Multi-session date/time display ✅
- Volunteer shifts display ✅
- Status badges (Ticket Purchased, RSVP) ✅

### Changes Needed

#### 4.1 Show Which Sessions Ticket Covers

Add session info under ticket badge for multi-session events:

```tsx
{/* After ticket badge (around line 310) */}
{event.hasTicket && event.ticketSessions && event.ticketSessions.length > 0 && (
  <Text size="xs" c="dimmed" mt={4}>
    Ticket covers: {event.ticketSessions.map(s =>
      formatUtcToLocalDate(s.startTime, eventTimeZone, { weekday: 'short', month: 'short', day: 'numeric' })
    ).join(' • ')}
  </Text>
)}
```

**Data Requirement:** Backend needs to include `ticketSessions` in UserEventDto when user has a ticket.

---

## 5. Admin Interface - NO CHANGES NEEDED

### TicketTypeFormModal.tsx - Already Complete

Current implementation already has:
- MultiSelect for sessions (line 197-207)
- Session validation requiring saved sessions
- All pricing options (Fixed, Sliding Scale)
- Quantity available configuration

**NO CHANGES NEEDED.**

### Per-Session Sold Counts - Already Exists

Admin event management already shows:
- Session-level capacity
- Sold counts per session
- Available counts per session

**NO CHANGES NEEDED.**

---

## 6. Implementation Checklist

### Frontend Changes

- [ ] **EventPaymentPage.tsx**
  - [ ] Add `getDisabledTicketIds()` function for session overlap detection
  - [ ] Add `userPurchasedTicketIds` prop/fetch for already-owned tickets
  - [ ] Update ticket card to show disabled state for overlapping tickets
  - [ ] Show "Already Purchased" instead of price for owned tickets
  - [ ] Exclude already-purchased tickets from cart total and API call

- [ ] **PaymentConfirmation.tsx**
  - [ ] Add `purchasedTickets` prop
  - [ ] Add ticket name and sessions under Registration Details title

- [ ] **ParticipationCard.tsx**
  - [ ] Add session availability section for multi-session events
  - [ ] Use "X sold, Y Available" format
  - [ ] Use standard date • time format
  - [ ] Update purchase button logic for partial session ownership
  - [ ] Show owned sessions with green alert when user has partial tickets
  - [ ] Show "Additional sessions available" when user can buy more

- [ ] **Dashboard EventCard.tsx**
  - [ ] Add `ticketSessions` display under ticket badge

### Backend Changes (for data)

- [ ] **UserEventDto** - Add `ticketSessions` array when user has ticket
- [ ] **EnhancedParticipationStatusDto** - Add per-session availability counts
- [ ] **EnhancedParticipationStatusDto** - Add `OwnedSessionIds` (sessions user has tickets for)
- [ ] **EnhancedParticipationStatusDto** - Add `CanPurchaseAdditionalSessions` boolean
- [ ] **EventPaymentPage data** - Include user's existing ticket IDs for this event

---

## 7. Visual Reference

### Ticket Selection Card States

**Normal (selectable):**
```
┌─────────────────────────────────────────────┐
│ ☐ Friday Only                    $35 - $50  │
│   Sun, Dec 1                                │
└─────────────────────────────────────────────┘
```

**Selected:**
```
┌─────────────────────────────────────────────┐
│ ☑ Friday Only                    $35 - $50  │ (border: burgundy)
│   Sun, Dec 1                                │
└─────────────────────────────────────────────┘
```

**Disabled (session overlap):**
```
┌─────────────────────────────────────────────┐
│ ☐ Full Weekend                   $75 - $100 │ (opacity: 0.5)
│   Sun, Dec 1 • Sat, Dec 7 • Sun, Dec 8      │
│   Sessions overlap with selected ticket     │
└─────────────────────────────────────────────┘
```

**Already Purchased:**
```
┌─────────────────────────────────────────────┐
│ ☑ Friday Only              Already Purchased│ (checkbox disabled)
│   Sun, Dec 1                                │
└─────────────────────────────────────────────┘
```

### Session Availability Display (ParticipationCard)

```
Session Availability
────────────────────────────────────
Sun, Dec 1 • 6:00 PM - 9:00 PM     12 sold, 8 Available
Sat, Dec 7 • 10:00 AM - 5:00 PM    20 sold, 5 Available
Sun, Dec 8 • 10:00 AM - 3:00 PM    15 sold, 10 Available
```

---

## 8. Quality Checklist

- [x] Uses existing components (no new pages/modals)
- [x] Follows established patterns in codebase
- [x] Minimal changes to existing UI
- [x] No redesign of admin interface
- [x] No redesign of checkout flow
- [x] No new "My Tickets" page
- [x] Uses "X sold, Y Available" format
- [x] Uses standard date • time format
- [x] Prevents overlap via disabled checkboxes (no popup modal)

---

## Document Status

**Version**: 2.0 - Minimal Changes Approach
**Created**: 2025-12-08
**Revised**: 2025-12-08 (complete rewrite based on user feedback)
**Status**: Draft - Pending Approval

**Changes from v1.0:**
- Removed all new page designs (My Tickets, Admin Ticket Config)
- Removed confirmation page redesign
- Removed progress bar capacity displays
- Added focus on existing code reuse
- Added specific file locations and line numbers
- Added "Already Purchased" display requirement
- Changed from modal popup to disabled checkbox approach for overlap prevention
