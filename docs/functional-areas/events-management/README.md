# Events Management Documentation
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Events Team -->
<!-- Status: Active -->

## Overview
The Events Management system handles all aspects of event creation, management, RSVPs, ticketing, and check-in for WitchCityRope community events. This includes both educational workshops and social events with different access rules based on member vetting status.

## Quick Links
- **Current Requirements**: [current-state/business-requirements.md](current-state/business-requirements.md)
- **Technical Design**: [current-state/functional-design.md](current-state/functional-design.md)
- **User Flows**: [current-state/user-flows.md](current-state/user-flows.md)
- **Test Coverage**: [current-state/test-coverage.md](current-state/test-coverage.md)
- **Active Work**: [new-work/status.md](new-work/status.md)

## Key Concepts
- **Event Types**: Educational (open to all) vs Social (vetted members only)
- **Capacity Management**: Limited spots with waitlist functionality
- **Pricing Tiers**: Member vs non-member pricing
- **RSVP vs Tickets**: Free events use RSVP, paid events use tickets
- **Check-in Process**: QR codes and manual check-in

## System Components

### Public Features
- Event browsing and filtering
- Event detail pages with descriptions
- RSVP/ticket purchase flow
- Member dashboard with upcoming events
- Calendar view

### Admin Features
- Event creation and editing
- Attendee management
- Check-in functionality
- Financial reporting
- Event analytics

### Staff/Organizer Features
- Limited to assigned events only
- Full edit capabilities for their events
- Attendee list access
- Communication tools

## Business Rules
- **Age Restriction**: All attendees must be 21+
- **Vetting Requirements**: Social events require vetted status
- **Refund Policy**: 48-hour cancellation window
- **Waitlist**: Automatic promotion when spots open
- **Capacity**: Hard limits enforced

## Related Areas
- **Authentication**: Determines event access rights
- **Payments**: Handles ticket purchases
- **Membership Vetting**: Controls social event access
- **User Dashboard**: Displays user's events

## Contact
- Technical Owner: Events Team
- Business Owner: Community Events Coordinator
- Last Major Update: 2025-08-04 (Consolidation from multiple docs)