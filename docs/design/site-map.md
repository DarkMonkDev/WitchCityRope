# Witch City Rope - Site Map

*Based on completed wireframes as of December 2024*

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