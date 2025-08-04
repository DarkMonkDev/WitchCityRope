# Witch City Rope - Site Map
<!-- Last Updated: 2025-08-04 -->
<!-- Owner: Architecture Team -->
<!-- Status: Active -->

## Overview
This document provides the complete site map for the WitchCityRope application, showing all pages, their relationships, and access controls. This is the authoritative reference for navigation structure.

*Based on completed wireframes and current implementation*

## Public Pages (Unauthenticated Users)

```
Landing Page (/)
├── About Section
├── Upcoming Events Preview (limited info)
├── Community Values
├── Join Community CTA → Vetting Application
└── Login/Register

Event List (/events)
├── Public Classes (full details visible)
│   ├── Event cards with:
│   │   ├── Title, date, time
│   │   ├── Teacher name
│   │   ├── Price range
│   │   └── Register button → Login prompt
│   └── Event Detail Page → Login prompt
└── Member Meetups (limited visibility)
    ├── Title and date only
    └── "Login to see details" message

Vetting Application (/join)
├── Multi-step form
├── Account creation integrated
└── Submit → Confirmation page

Login/Register (/auth)
├── Email/Password login
├── Google OAuth option
├── Create account
├── Forgot password
└── Two-factor authentication setup
```

## Member Pages (Authenticated Users)

```
Member Dashboard (/dashboard)
├── Welcome message with member status
├── Upcoming events (registered)
├── Recent activity
├── Quick actions:
│   ├── Browse events
│   ├── View tickets
│   └── Update profile
└── Announcements/notices

Event List (/events) - Full Access
├── All events visible with complete details
├── Filters:
│   ├── Event type (Class/Meetup)
│   ├── Date range
│   ├── Teacher
│   └── Availability
└── Event cards showing:
    ├── Full description preview
    ├── Venue info (members only)
    ├── Capacity/spots available
    ├── Price (fixed or sliding scale)
    └── Register/RSVP buttons

Event Detail (/events/{id})
├── Full event information
├── Teacher bio
├── Venue details with map
├── Registration options:
│   ├── Ticket type selection
│   ├── Price selection (if sliding)
│   └── Payment via PayPal
├── Attendee count (X/Y spots)
└── Refund policy

My Tickets (/tickets)
├── Upcoming events with ticket details
├── Past events
├── Refund requests (if within window)
└── Download/print options

Profile (/profile)
├── Edit profile info
├── Change password
├── Privacy settings
├── 2FA management
└── Membership status
```

## Admin Pages

```
Admin Dashboard (/admin)
├── Statistics overview
├── Recent activity
├── Quick actions
└── Alerts/notifications

Event Management (/admin/events)
├── Events list with admin controls
├── Create Event → Event Creation Form
├── Edit/duplicate/cancel events
├── View attendees
└── Email attendees

Event Creation (/admin/events/create)
├── Basic Info tab
│   ├── Event type
│   ├── Title, description
│   ├── Teacher assignment
│   └── Schedule
├── Tickets/Orders tab
│   ├── Pricing setup
│   ├── Ticket types
│   ├── Current orders
│   └── Volunteer tickets
├── Emails tab
│   ├── Template selection
│   ├── Custom emails
│   └── Save templates
└── Volunteers/Staff tab
    ├── Current volunteers
    ├── Task management
    └── Add volunteers

Vetting Queue (/admin/vetting)
├── Applications list
├── Filters and sorting
├── Bulk actions
└── Application review page

Vetting Review (/admin/vetting/{id})
├── Application details
├── Reviewer notes
├── Status history
├── Approve/deny/request info
└── Email applicant

Member Management (/admin/members)
├── Member directory
├── Search and filters
├── Member profiles
├── Status management
└── Notes/flags

Financial Reports (/admin/reports)
├── Revenue by event
├── Sliding scale usage
├── Refund history
└── Export options

Safety & Incidents (/admin/safety)
├── Incident reports queue
├── Report details
├── Investigation notes
└── Analytics
```

## Staff Pages

```
Event Check-in (/checkin)
├── Event selection (today's events)
└── Check-in interface
    ├── Search by name
    ├── Attendee list
    ├── Check-in status
    └── Payment/waiver verification

Check-in Modal (overlay)
├── Attendee photo
├── Payment status
├── Waiver status
├── Special notes
└── Confirm check-in button
```

## Utility Pages

```
Anonymous Incident Report (/report)
├── No login required
├── Incident details form
├── Optional contact info
└── Confirmation with report ID

404 Error Page
403 Forbidden Page
500 Error Page
```

## Mobile Navigation Structure

```
Bottom Navigation (Members):
├── Home (Dashboard)
├── Events
├── Tickets
└── More
    ├── Profile
    ├── Settings
    └── Logout

Hamburger Menu (All users):
├── Main navigation items
├── User status indicator
└── Context-specific options
```

## Site Map Maintenance Process

### When to Update
The site map should be updated when:
1. New pages or routes are added
2. Navigation structure changes
3. Access control requirements change
4. Pages are removed or consolidated
5. Mobile navigation patterns change

### Update Process
1. **Before Implementation**: Update site map during feature planning
2. **During Development**: Verify implementation matches site map
3. **After Deployment**: Confirm production routes match documentation
4. **Regular Review**: Quarterly review for accuracy

### Validation Checklist
- [ ] All routes match actual application URLs
- [ ] Access controls are correctly documented
- [ ] Parent-child relationships are accurate
- [ ] Mobile navigation reflects desktop structure
- [ ] Error pages are included
- [ ] New features are represented

### Related Documentation
- `/docs/functional-areas/*/current-state/user-flows.md` - Detailed user journeys
- `/docs/architecture/ARCHITECTURE.md` - System architecture
- `/docs/00-START-HERE.md` - Documentation navigation
- Route definitions in Blazor components (`@page` directives)

### Route Naming Conventions
- Public pages: Simple, descriptive URLs (`/events`, `/join`)
- Member pages: Contextual prefixes (`/member/dashboard`, `/member/events`)
- Admin pages: `/admin/` prefix for all administrative functions
- API endpoints: `/api/` prefix with RESTful patterns

---

*This site map is maintained by the Architecture Team. For questions or updates, see the project PROGRESS.md file.*