# WitchCityRope Events Business Logic

## Overview
WitchCityRope has two types of events with different attendance and payment models:

1. **Classes** (Educational workshops, skill-building sessions)
2. **Social Events** (Community gatherings, rope jams, social meetups)

## Event Types and Actions

### Classes
- **Payment**: REQUIRED - Must purchase a ticket in advance
- **RSVP**: NOT AVAILABLE - Classes do not have an RSVP option
- **At-door attendance**: NOT ALLOWED - Must have pre-purchased ticket
- **UI Actions**: 
  - "Purchase Ticket" button only
  - No RSVP option shown

### Social Events  
- **Payment**: OPTIONAL - Suggested donation/ticket price
- **RSVP**: AVAILABLE - Can RSVP to reserve a spot for free
- **At-door attendance**: ALLOWED - Can attend without pre-purchasing
- **UI Actions**:
  - "RSVP" button - Reserves a spot without payment
  - "Purchase Ticket" button - Buy a ticket at suggested price
  - BOTH buttons can be available simultaneously
  - If user has RSVP'd, they can STILL purchase a ticket later

## User Flow Examples

### Social Event - New Attendee
1. Views event detail page
2. Sees both "RSVP" and "Purchase Ticket" options
3. Can choose either:
   - Click "RSVP" → Reserves spot for free → Can still buy ticket later if desired
   - Click "Purchase Ticket" → Pays suggested price → Counts as both RSVP and paid

### Social Event - Already RSVP'd
1. Views event detail page
2. Sees "You've RSVP'd for this event" message
3. STILL sees "Purchase Ticket" button
4. Can choose to support the event by purchasing a ticket even though already RSVP'd

### Class - Any User
1. Views event detail page
2. Sees ONLY "Purchase Ticket" button
3. Must purchase to attend
4. No RSVP option visible

## Database Implications
- Need to track RSVP status separately from ticket purchase status
- A user can have:
  - RSVP only (social events)
  - Ticket only (classes or social events) 
  - Both RSVP and ticket (social events only)

## API Endpoints Needed
- `POST /api/events/{id}/rsvp` - For free RSVP to social events
- `POST /api/events/{id}/tickets` - For purchasing tickets
- `GET /api/events/{id}/attendance` - Get user's RSVP and ticket status

## Implementation Notes
- Event type (class vs social) must be clearly defined in the event model
- UI must conditionally show buttons based on event type
- Backend must enforce rules (e.g., reject RSVP attempts for classes)
- Reports should distinguish between RSVP count and paid ticket count