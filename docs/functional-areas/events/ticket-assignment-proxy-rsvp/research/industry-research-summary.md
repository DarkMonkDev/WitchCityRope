# Industry Research Summary: Ticket Transfer & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Purpose: Summarize external research for future agent reference -->
<!-- Full research document: docs/functional-areas/events/research/2026-03-18-ticket-transfer-proxy-rsvp-research.md -->

## Platforms Analyzed

### Ticketmaster
- Post-purchase transfer with SMS verification
- New barcode issued to recipient, sender's ticket invalidated
- Heavy security (SafeTix rotating barcodes)
- Designed for massive scale - overkill for our needs

### Eventbrite
- Treats transfer as "editing order information"
- Simple but less auditable, no formal acceptance step
- Not suitable due to our waiver requirements

### Showpass
- Post-purchase transfer via email link
- Clean and straightforward
- Missing waiver management

### GoPassage (Best Fit)
- "Gift at checkout" model - buyer designates recipient during purchase
- Recipient gets email to accept and must create/log into account
- Buyer can track acceptance status
- **Closest to our needs** - matches natural community behavior of "I'm buying tickets for me and my friend"

### TicketSpice (Best Waiver Model)
- Waiver signed AFTER purchase, per-attendee
- Tickets withheld until waiver signed
- **This pattern maps perfectly to our needs** for pending acceptance + waiver

## Key Findings

### 1. No Major Platform Has User-to-User Delegate Authorization
Our "Authorized Contacts" concept is novel. Existing patterns:
- RSVPify: Organizer-initiated proxy registration (admin-only)
- SpotMe: Corporate assistant patterns (executive assistants registering for bosses)
- Meetup: Organizer can manually add attendees, but no peer delegation

**Implication:** We're designing something custom. Authorization must flow FROM the person being represented TO the delegate.

### 2. Waiver Management Is a Universal Gap
No major platform handles waiver re-signing for transferred tickets well. TicketSpice's model (waiver per-attendee, ticket withheld until signed) is the industry best practice.

### 3. Assign-at-Checkout Is the Recommended Primary Flow
Rather than post-purchase transfer, the recommended UX is:
1. Purchaser selects ticket quantity
2. For each additional ticket, select an authorized contact
3. Complete payment
4. Assigned tickets enter PENDING_ACCEPTANCE
5. Recipient accepts + signs waiver
6. Ticket becomes ACTIVE

### 4. Irrevocability Is Clean
Once a ticket is ACTIVE (accepted + waiver signed), it cannot be reclaimed. Declined/unassigned tickets return to purchaser for reassignment. Simpler than reversible transfers.

## Recommended Model for WitchCityRope

**Hybrid of GoPassage + TicketSpice patterns:**
- GoPassage's "assign at checkout" flow (also support post-purchase assignment)
- TicketSpice's "waiver-gated acceptance" (ticket not active until waiver signed)
- Custom "Authorized Contacts" system (no industry equivalent)
- Both waiver AND Terms of Service must be accepted by recipient

## Sources

Full citations with URLs available in the detailed research document at:
`docs/functional-areas/events/research/2026-03-18-ticket-transfer-proxy-rsvp-research.md`
