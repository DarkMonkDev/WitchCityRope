# Authentication System - Business Requirements
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 1.0 -->
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
- Enable two-factor authentication

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
- Email-based registration with verification
- Required fields: Email, password, chosen display name
- Optional profile information can be added after registration
- Email verification required before accessing member features

### Login Options
- Email and password login
- Optional two-factor authentication (2FA)
- "Remember me" option for trusted devices
- Password reset via email

### Session Management
- Sessions remain active during user activity
- No automatic session timeout warnings
- Users can manually log out
- "Remember me" extends session duration

### Password Requirements
- Minimum 8 characters
- Must include uppercase, lowercase, number, and special character
- Password strength indicator during registration
- Prevents reuse of last 5 passwords

### Two-Factor Authentication (2FA)
- Optional for all users
- Supports authenticator apps (Google Authenticator, Authy, etc.)
- Backup codes provided
- Can be enforced for admin accounts

## Security Features

### Account Protection
- Account lockout after 5 failed login attempts
- CAPTCHA after 3 failed attempts
- Email notification of login from new device
- Ability to view and revoke active sessions

### Privacy Controls
- Users can control profile visibility
- Option to use display name instead of real name
- Vetted status can be hidden from profile
- GDPR-compliant data export and deletion

## Business Rules

### Vetting Requirements
- Must be authenticated to apply for vetting
- Vetting application includes:
  - Community references
  - Agreement to community standards
  - Background information
- Admin review required for approval
- Vetting status can be revoked for violations

### Event Access Rules
- Class events: Open to all authenticated users (ticket purchase)
- Social events: RSVP/tickets restricted to vetted members only
- Private events: May have additional access restrictions
- Free events still require RSVP (capacity management)

### Role Assignment
- Users start as "Authenticated but Not Vetted"
- Vetting approval promotes to "Vetted Member"
- Teacher role assigned by admin based on qualifications
- Staff/Organizer role assigned per event or globally
- Admin role restricted to trusted community leaders

---

*This document describes the business-level requirements for the authentication system. For technical implementation details, see functional-design.md*