# Authentication System - Business Requirements
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Active -->

## Overview
The WitchCityRope authentication system manages user access, roles, and permissions for the rope bondage community platform. The system supports multiple user types with different capabilities based on their authentication and vetting status.

## User Types and Capabilities

### 1. Anonymous Users (Not Logged In)
**Can:**
- View the public homepage and general information pages
- View all events (including member-only events)
- View event details and descriptions
- Submit an incident report
- Access safety resources and educational content
- View the calendar of upcoming events
- Access the login and registration pages

**Cannot:**
- RSVP for any events
- Purchase tickets for any events
- View member profiles
- Access member dashboard
- View attendance lists

### 2. Authenticated but Not Vetted Members
**Can:**
- All anonymous user capabilities, plus:
- Access their member dashboard
- Update their profile information
- Purchase tickets to educational classes/workshops
- Submit a vetting application
- View their own tickets and RSVPs
- Update account settings (email, password)
- Enable two-factor authentication (when implemented)

**Cannot:**
- RSVP for social events
- Purchase tickets to social events
- View other member profiles
- Access member-only content

### 3. Authenticated - Vetted Members
**Can:**
- All non-vetted member capabilities, plus:
- RSVP for social events
- Purchase tickets to social events
- View their RSVPs and tickets to future and past events
- View other vetted member profiles
- Access member-only educational resources
- Participate in community discussions

**Cannot:**
- Access administrative functions
- Modify events
- View vetting applications
- Access financial reports

### 4. Staff/Organizer Role
**Scope:** Assigned to specific events or all events
**Can:**
- Full edit capabilities for assigned events:
  - Create/edit event details
  - Manage ticket types and pricing
  - View and export attendee lists
  - Send event-specific communications
  - Check in attendees at events
  - Cancel or reschedule events

**Cannot:**
- Access other admin functions outside of event management
- View financial reports (unless specifically granted)
- Manage user accounts
- Process vetting applications

### 5. Teacher Role
**Can:**
- All vetted member capabilities, plus:
- Access teacher-specific tab on user dashboard
- Update their teaching bio
- Update teacher-specific information fields:
  - Areas of expertise
  - Teaching philosophy
  - Certifications
  - Availability for private lessons
  - Class preferences
- View roster for their classes
- Send messages to their students

**Cannot:**
- Edit events they're not assigned to
- Access administrative functions

### 6. Admin Users
**Can:**
- All capabilities of all other roles, plus:
- Manage all user accounts
- Process vetting applications
- View all financial reports
- Access system configuration
- Manage content pages
- View security logs and incident reports
- Assign roles to users
- Export system data

## Authentication Features

### Registration Process
- Email-based registration with verification infrastructure
- Required fields: Email, password, scene name (display name), date of birth
- Age verification: Must be 21 or older
- Scene name must be unique across the system
- Email must be unique (can use username or email to login)
- Legal name captured but encrypted for privacy
- Email verification available but not currently enforced

### Login Options
- Email or username (scene name) login
- Password authentication
- Optional "Remember me" for extended sessions (7 days)
- Account lockout after 5 failed attempts
- Last login timestamp tracking

### Session Management
- Cookie-based sessions for web application (7-day sliding expiration)
- JWT tokens for API access
- Sessions remain active during user activity
- No automatic session timeout warnings
- Users can manually log out

### Password Requirements
- Minimum 8 characters
- Must include:
  - At least one uppercase letter
  - At least one lowercase letter
  - At least one number
  - At least one special character
- Account lockout after 5 failed attempts

### Two-Factor Authentication (2FA)
- Infrastructure exists but not yet implemented
- When implemented will support:
  - Authenticator apps (Google Authenticator, Authy, etc.)
  - Backup codes
  - Optional for members, required for admins

## Security Features

### Account Protection
- Account lockout after 5 failed login attempts
- Email notification of login from new device (planned)
- Secure password storage using ASP.NET Core Identity
- HTTPS required in production

### Privacy Controls
- Scene names for public display (real names encrypted)
- Profile visibility controls
- GDPR-compliant data handling
- Ability to delete account and data

## Business Rules

### Age Requirements
- Must be 21 or older to register
- Age calculated from date of birth
- Cannot be modified after registration

### Vetting Requirements
- Must be authenticated to apply for vetting
- Vetting application includes:
  - Community references
  - Agreement to community standards
  - Background information
- Admin review required for approval
- Vetting status can be revoked for violations
- Vetting status determines access to social events

### Event Access Rules
- Educational events: Open to all authenticated users for ticket purchase
- Social events: RSVP/tickets restricted to vetted members only
- Private events: May have additional access restrictions
- Free events still require RSVP for capacity management
- Event visibility: All users can see all events, but access varies

### Role Assignment
- All users start as "Authenticated but Not Vetted"
- Vetting approval promotes to "Vetted Member" status
- Teacher role assigned by admin based on qualifications
- Staff/Organizer role assigned per event or globally
- Admin role restricted to trusted community leaders
- Multiple roles can be assigned to one user

### Account Status
- Active accounts can access all granted features
- Inactive accounts cannot login but data is preserved
- Suspended accounts for policy violations
- Deleted accounts remove all personal data

## Integration Points

### Email System
- Registration confirmation emails (when enabled)
- Password reset emails (when implemented)
- Event notifications
- System announcements

### Payment System
- Authenticated users can purchase tickets
- Payment history tracked per user
- Refund requests tied to user account

### Event Management
- Authentication determines event access
- User roles affect event capabilities
- Attendance tracking linked to user accounts

---

*This document describes the business-level requirements for the authentication system. For technical implementation details, see [functional-design.md](functional-design.md)*