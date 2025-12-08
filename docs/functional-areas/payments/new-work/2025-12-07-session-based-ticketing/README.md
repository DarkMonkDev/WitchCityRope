# Session-Based Ticketing Enhancement

<!-- Last Updated: 2025-12-07 -->
<!-- Version: 1.0 -->
<!-- Owner: TBD -->
<!-- Status: Phase 0 - Initialized -->

## Overview

This feature work transitions WitchCityRope's ticketing system from **per-event ticket limitations** to **per-session ticket limitations**, enabling more granular capacity management for multi-session events.

## Business Context

### Current System (Event-Based)
- Ticket types are configured at the EVENT level
- Capacity limits apply to the entire event
- All sessions within an event share the same ticket pool
- Limitation: Cannot have different ticket types or capacities for different sessions of the same event

### Proposed System (Session-Based)
- Ticket types configured at the SESSION level
- Capacity limits apply to individual sessions
- Each session can have unique ticket types, pricing, and availability
- Benefit: Enables classes with beginner/advanced sessions, different pricing tiers, or session-specific capacity

## Architectural Impact

### Major Changes Required
1. **Database Schema**: Move ticket type relationships from Events to Sessions
2. **Business Logic**: Update participation rules to check session capacity
3. **Frontend UI**: Session-specific ticket purchase interface
4. **Admin Interface**: Session-level ticket type configuration
5. **Migration Strategy**: Data migration for existing event-based tickets

### Affected Systems
- EventParticipations table (add SessionId foreign key)
- TicketTypes table (change EventId FK to SessionId FK)
- RSVP/Ticketing business logic
- PayPal payment processing (ensure session tracking)
- Admin event management UI
- User ticket purchase flow

## Related Documentation

### Current Implementation
- **Current Ticketing System**: `/home/chad/repos/witchcityrope/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/`
- **Database Design**: `/home/chad/repos/witchcityrope/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/database-design.md`
- **Business Requirements**: `/home/chad/repos/witchcityrope/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/requirements/business-requirements.md`

### Reference Materials
- **PayPal Integration**: `/home/chad/repos/witchcityrope/docs/functional-areas/payments/handoffs/paypal-webhook-integration-complete-2025-09-14.md`
- **Master Index**: `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md`

## Work Structure

```
/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/
├── README.md                       # This file
├── requirements/                   # Business requirements and functional specs
├── design/                        # Database design, technical architecture
├── research/                      # Technology research and analysis
└── handoffs/                      # Agent handoff documents
```

## Next Steps

1. **Phase 1 - Requirements**: Define business requirements for session-based ticketing
2. **Phase 2 - Design**: Database schema changes, migration strategy, UI/UX design
3. **Phase 3 - Implementation**: Backend API, frontend UI, data migration
4. **Phase 4 - Testing**: Comprehensive test suite for new functionality
5. **Phase 5 - Finalization**: Documentation, deployment, lessons learned

## Quality Gates

TBD based on work type (Feature/Enhancement)

## Session Started
**Date**: 2025-12-07
**Initialized By**: Librarian Agent
**Status**: Folder structure created, ready for requirements phase
