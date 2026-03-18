# Ticket Assignment & Proxy RSVP System

<!-- Last Updated: 2026-03-18 -->
<!-- Status: Phase 1 - Requirements & Design -->
<!-- Owner: Main Agent / Business Requirements -->

## Overview

This feature enables users to purchase tickets for other users and RSVP on behalf of other users, with an authorization-based "Authorized Contacts" system that controls who can act on behalf of whom.

## Key Concepts

1. **Authorized Contacts**: Users designate specific people who are allowed to buy tickets or RSVP on their behalf (from their profile settings)
2. **Purchase & Assign**: At checkout, a delegate can buy multiple tickets and assign them to their authorized contacts
3. **Pending Acceptance**: Assigned tickets/RSVPs require the recipient to personally accept the event waiver and Terms of Service before the ticket becomes active
4. **Proxy RSVP**: Delegates can RSVP to free events on behalf of their authorized contacts (same pending acceptance flow)

## Folder Structure

```
ticket-assignment-proxy-rsvp/
  README.md                          # This file - feature overview
  research/
    codebase-analysis.md             # Current system analysis (models, services, gaps)
    industry-research-summary.md     # Best practices from Eventbrite, Ticketmaster, GoPassage, etc.
  requirements/
    architectural-decisions.md       # All design decisions made with stakeholder
    use-cases.md                     # Detailed use cases with user flows
    business-rules.md                # Business rules, constraints, edge cases
    email-requirements.md            # New email template specifications
  design/
    (future - functional specs, database design, UI wireframes)
```

## Status

| Phase | Status | Date |
|-------|--------|------|
| Research | Complete | 2026-03-18 |
| Requirements | In Progress | 2026-03-18 |
| Design | Not Started | - |
| Implementation | Not Started | - |
| Testing | Not Started | - |

## Quick Links

- [Codebase Analysis](./research/codebase-analysis.md)
- [Industry Research](./research/industry-research-summary.md)
- [Architectural Decisions](./requirements/architectural-decisions.md)
- [Use Cases](./requirements/use-cases.md)
- [Business Rules](./requirements/business-rules.md)
- [Email Requirements](./requirements/email-requirements.md)
