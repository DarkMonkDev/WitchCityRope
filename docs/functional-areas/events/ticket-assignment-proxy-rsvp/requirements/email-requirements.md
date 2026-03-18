# Email Requirements: Ticket Assignment & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Purpose: Specifications for new email templates needed for this feature -->

## New Email Templates Required

### Template 1: Ticket Assignment Notification

**Trigger:** When a ticket is assigned to a user (either at checkout or post-purchase from dashboard)
**Recipient:** The assignee (Principal)
**Send Timing:** Immediately upon assignment

**Subject Line:** "[Delegate SceneName] purchased a ticket for you to [Event Title]"

**Required Content:**
- Who purchased the ticket (Delegate's scene name)
- Event name, date, time, venue (location based on vetting status / privacy rules)
- Session(s) covered by the ticket
- Ticket type name
- Clear call-to-action button: "Accept Your Ticket"
- Link to the event page or dashboard where they can accept
- Explanation that they need to accept the event waiver and terms of service
- Note: "This ticket will remain available for you to accept until the event begins"

**Template Variables:**
- `{{DelegateSceneName}}` - Who bought the ticket
- `{{EventTitle}}` - Event name
- `{{EventDate}}` - Event date/time (formatted for display)
- `{{EventVenue}}` - Venue name (privacy-aware)
- `{{SessionNames}}` - Comma-separated session names
- `{{TicketTypeName}}` - Ticket type purchased
- `{{AcceptUrl}}` - Direct link to accept the ticket
- `{{RecipientSceneName}}` - The assignee's scene name

---

### Template 2: RSVP Assignment Notification

**Trigger:** When a proxy RSVP is created for a user
**Recipient:** The assignee (Principal)
**Send Timing:** Immediately upon creation

**Subject Line:** "[Delegate SceneName] RSVP'd for you to [Event Title]"

**Required Content:**
- Who created the RSVP (Delegate's scene name)
- Event name, date, time, venue
- Clear call-to-action button: "Accept Your RSVP"
- Link to accept
- Explanation that they need to accept the event waiver and terms of service
- Note: "This RSVP will remain available for you to accept until the event begins"

**Template Variables:**
- Same as Template 1 (minus ticket-specific fields)
- `{{DelegateSceneName}}`
- `{{EventTitle}}`
- `{{EventDate}}`
- `{{EventVenue}}`
- `{{AcceptUrl}}`
- `{{RecipientSceneName}}`

---

### Template 3: Ticket Acceptance Reminder (1 Day Before Event)

**Trigger:** Scheduled job - 1 day before event's first session start time
**Recipient:** Any user with `EventAttendance.Status = PendingAcceptance` AND `AttendanceType = Ticket` for the upcoming event
**Send Timing:** 24 hours before event's first session
**Send Limit:** One reminder per pending assignment (tracked to prevent duplicates)

**Subject Line:** "Reminder: Accept your ticket for [Event Title] - Event is tomorrow!"

**Required Content:**
- Urgency messaging: "Your ticket for [Event Title] is still waiting for your acceptance"
- Who purchased it (Delegate's scene name)
- Event name, date, time, venue
- Clear call-to-action button: "Accept Your Ticket Now"
- Direct link to accept
- Reminder about waiver and ToS requirement
- Note: "You can accept this ticket right up until the event, but we recommend accepting now to avoid any issues"

**Template Variables:**
- Same as Template 1 plus:
- `{{EventStartTime}}` - Specific start time for urgency

---

### Template 4: RSVP Acceptance Reminder (1 Day Before Event)

**Trigger:** Scheduled job - 1 day before event's first session start time
**Recipient:** Any user with `EventAttendance.Status = PendingAcceptance` AND `AttendanceType = RSVP` for the upcoming event
**Send Timing:** 24 hours before event's first session
**Send Limit:** One reminder per pending RSVP

**Subject Line:** "Reminder: Accept your RSVP for [Event Title] - Event is tomorrow!"

**Required Content:**
- Same pattern as Template 3 but for RSVPs
- Urgency messaging
- Who created the RSVP
- Event details
- Accept button/link
- Waiver reminder

**Template Variables:**
- Same as Template 3

---

## New Recipient Group

### "Unaccepted Assignments for Upcoming Events"

**Purpose:** Target users who have pending ticket/RSVP assignments for events happening within 24 hours

**Filter Criteria:**
- `EventAttendance.Status = PendingAcceptance`
- `EventAttendance.AttendanceType` IN (RSVP, Ticket)
- Event's first session `StartTime` is within 24 hours from now
- User has not already been sent a reminder for this specific assignment

**Usage:**
- Used by the scheduled reminder job
- Could also be used by admins for ad-hoc email sends

---

## Implementation Notes

### Existing Email Infrastructure
- Emails are sent fire-and-forget (current pattern)
- Email templates exist for RSVP confirmation, ticket purchase confirmation, cancellation
- New templates should follow the same patterns and branding

### Scheduled Job for Reminders
- Needs a new background job/service that runs daily
- Checks for events starting within 24 hours
- Queries PendingAcceptance attendances for those events
- Sends reminder emails
- Tracks "reminder sent" flag to prevent duplicates (could be stored in `EventAttendance.Metadata` JSONB field or a separate tracking field)

### Privacy Considerations
- Venue location in emails should follow existing privacy rules (full address for vetted, city/state for non-vetted)
- Delegate's real name should NOT be included - use scene name only
